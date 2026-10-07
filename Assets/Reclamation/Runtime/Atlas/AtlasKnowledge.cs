using UnityEngine;
namespace Reclamation.Atlas
{
    // Cartographic knowledge is separate from authoritative world state.
    public sealed class AtlasKnowledge
    {
        public const int Resolution = 128;
        private readonly WorldAtlasModel model;
        private readonly int[] territory = new int[Resolution*Resolution];
        private readonly bool[] visible = new bool[Resolution*Resolution], explored = new bool[Resolution*Resolution];
        private readonly int[] owners;
        public const int UnknownOwner = -99;
        public AtlasKnowledge(WorldAtlasModel model)
        {
            this.model=model; owners=new int[model.sites.Count];
            for(int i=0;i<owners.Length;i++)owners[i]=UnknownOwner;
            for(int z=0;z<Resolution;z++)for(int x=0;x<Resolution;x++)territory[z*Resolution+x]=model.Nearest(Point(x,z)).id;
        }
        public bool Allied(int owner)=>owner>=0&&(owner==0||model.factions[owner].alliance==model.factions[0].alliance);
        private Vector2 Point(int x,int z)=>new Vector2((x+.5f)/Resolution-.5f,(z+.5f)/Resolution-.5f)*model.Extent;
        private int Index(Vector2 p)=>Mathf.Clamp((int)((p.y/model.Extent+.5f)*Resolution),0,Resolution-1)*Resolution+Mathf.Clamp((int)((p.x/model.Extent+.5f)*Resolution),0,Resolution-1);
        public bool Visible(Vector2 p)=>visible[Index(p)];
        public bool Explored(Vector2 p)=>explored[Index(p)];
        public int ReportedOwner(int site)=>owners[site];
        public void BeginObservation()
        {
            for(int i=0;i<visible.Length;i++){visible[i]=Allied(model.sites[territory[i]].owner);explored[i]|=visible[i];}
        }
        public void Reveal(Vector2 point,float radius)
        {
            int cx=Mathf.FloorToInt((point.x/model.Extent+.5f)*Resolution),cz=Mathf.FloorToInt((point.y/model.Extent+.5f)*Resolution),r=Mathf.CeilToInt(radius/model.Extent*Resolution)+1;
            for(int z=Mathf.Max(0,cz-r);z<=Mathf.Min(Resolution-1,cz+r);z++)for(int x=Mathf.Max(0,cx-r);x<=Mathf.Min(Resolution-1,cx+r);x++)
                if((Point(x,z)-point).sqrMagnitude<=radius*radius){int i=z*Resolution+x;visible[i]=explored[i]=true;}
        }
        public void FinishObservation(Color32[] pixels)
        {
            for(int i=0;i<owners.Length;i++)if(Visible(model.sites[i].point))owners[i]=model.sites[i].owner;
            for(int i=0;i<visible.Length;i++)pixels[i]=new Color32(9,17,22,visible[i]?(byte)0:explored[i]?(byte)165:(byte)255);
        }
    }
}
