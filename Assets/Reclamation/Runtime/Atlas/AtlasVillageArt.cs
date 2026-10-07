using UnityEngine;

namespace Reclamation.Atlas
{
    public sealed partial class AtlasArtKit
    {
        private void BuildVillageAssets()
        {
            for(int variant=0;variant<3;variant++)
            {
                int v=variant;
                Make("UpperArm"+v,b=>b.Round(new Vector3(0,-.15f,0),new Vector3(.105f,.3f,.11f),Cloth(v),8,default,.85f));
                Make("Forearm"+v,b=>{b.Round(new Vector3(0,-.12f,0),new Vector3(.075f,.24f,.075f),Skin(v),8);b.Oval(new Vector3(0,-.26f,.025f),new Vector3(.077f,.095f,.07f),Skin(v),8,4);});
            }
            Make("Hoe",b=>{b.Beam(new Vector3(0,-.55f,0),new Vector3(0,.9f,0),.055f,Timber);b.Box(new Vector3(0,.87f,.1f),new Vector3(.3f,.08f,.25f),Iron);});
            Make("Axe",b=>{b.Beam(new Vector3(0,-.2f,0),new Vector3(0,.65f,0),.07f,Timber);b.Box(new Vector3(.13f,.58f,0),new Vector3(.35f,.2f,.06f),Stone);});
            Make("Pick",b=>{b.Beam(new Vector3(0,-.2f,0),new Vector3(0,.7f,0),.065f,Timber);b.Beam(new Vector3(-.37f,.57f,0),new Vector3(0,.72f,0),.08f,Iron);b.Beam(new Vector3(0,.72f,0),new Vector3(.37f,.57f,0),.08f,Iron);});
            Make("Mug",b=>{b.Round(Vector3.zero,new Vector3(.085f,.19f,.085f),Timber*1.7f,8);b.Round(Vector3.up*.098f,new Vector3(.07f,.008f,.07f),Dark,8);b.Beam(new Vector3(.09f,-.06f,0),new Vector3(.14f,.06f,0),.035f,Timber);});
            Make("Bread",b=>b.Oval(Vector3.zero,new Vector3(.13f,.07f,.085f),Straw,8,4));
            Make("WorkTree",b=>{b.Round(new Vector3(0,2.35f,0),new Vector3(.27f,4.7f,.27f),Timber,8,default,.75f);b.Oval(new Vector3(0,5.1f,0),new Vector3(1.4f,1.5f,1.2f),new Color(.23f,.38f,.16f),8,4);});
            Make("WorkRock",b=>{b.Oval(new Vector3(0,.5f,0),new Vector3(.8f,.65f,.65f),Stone,7,4);b.Oval(new Vector3(.35f,.8f,0),new Vector3(.2f,.18f,.2f),Iron,5,3);});
            Make("Yard",b=>{Barrel(b,new Vector3(0,0,0),.38f);b.Box(new Vector3(1,.35f,.4f),new Vector3(.8f,.7f,.7f),Timber*1.4f);for(int i=0;i<3;i++)b.Beam(new Vector3(-1+i*.65f,0,1.5f),new Vector3(-1+i*.65f,1,1.5f),.09f,Timber);b.Beam(new Vector3(-1,.6f,1.5f),new Vector3(.3f,.6f,1.5f),.08f,Timber);});
            Make("GrassEdge",b=>{for(int i=0;i<5;i++){float x=(i-2)*.13f;b.Beam(new Vector3(x,0,0),new Vector3(x+.1f,.16f+i%3*.07f,.1f),.035f,new Color(.37f,.43f,.21f));}});
            Make("Path",b=>b.Box(Vector3.zero,Vector3.one,new Color(.44f,.35f,.23f)));
            Make("HallRoof",b=>Gable(b,15,11,0,3.5f,Plaster));
            Make("Garden",b=>
            {
                b.Box(new Vector3(0,.03f,0),new Vector3(6,.05f,3.5f),new Color(.29f,.25f,.15f));
                for(int i=0;i<7;i++)for(int row=0;row<2;row++)b.Oval(new Vector3(-2.5f+i*.8f,.35f,-.8f+row*1.6f),new Vector3(.35f,.3f,.35f),new Color(.29f,.43f,.19f),7,4);
                for(int i=0;i<5;i++)b.Oval(new Vector3(-3+i*1.5f,.7f,2.1f),new Vector3(.9f,.7f,.55f),new Color(.24f,.36f,.18f),8,4);
            });
            Make("Bench",b=>
            {
                b.Box(new Vector3(0,.5f,0),new Vector3(1.1f,.16f,3),Timber);b.Box(new Vector3(.5f,.92f,0),new Vector3(.14f,.8f,3),Timber);
                foreach(float z in new[]{-1.15f,1.15f})b.Box(new Vector3(0,.25f,z),new Vector3(.8f,.5f,.18f),Timber);
            });
            Make("CouncilTable",b=>
            {
                b.Box(new Vector3(0,1.15f,0),new Vector3(3,.2f,1.3f),Timber);foreach(float x in new[]{-1.2f,1.2f})b.Box(new Vector3(x,.55f,0),new Vector3(.2f,1.1f,1),Timber);
                b.Box(new Vector3(0,1.27f,0),new Vector3(1.4f,.03f,.85f),Plaster);b.Box(new Vector3(.9f,1.35f,0),new Vector3(.35f,.15f,.5f),new Color(.35f,.2f,.15f));
            });
            Make("Lantern",b=>
            {
                b.Box(new Vector3(0,1.5f,0),new Vector3(.14f,3,.14f),Timber);b.Beam(new Vector3(0,3,0),new Vector3(.45f,3,0),.12f,Iron);
                b.Box(new Vector3(.4f,2.7f,0),new Vector3(.35f,.5f,.35f),new Color(1,.55f,.12f));foreach(float y in new[]{2.45f,2.95f})b.Box(new Vector3(.4f,y,0),new Vector3(.43f,.08f,.43f),Iron);
            });
            Make("TavernSign",b=>
            {
                b.Box(Vector3.zero,new Vector3(.12f,1.1f,1.4f),Timber);b.Box(new Vector3(.07f,0,0),new Vector3(.04f,.65f,.5f),Straw);b.Round(new Vector3(.12f,.05f,.4f),new Vector3(.04f,.25f,.16f),Straw,8);
            });
            Make("HallSign",b=>
            {
                b.Box(Vector3.zero,new Vector3(1.5f,.95f,.12f),Timber);
                for(int i=0;i<3;i++)b.Box(new Vector3((i-1)*.35f,0,-.08f),new Vector3(.15f,.48f,.04f),Straw);
                b.Box(new Vector3(0,-.3f,-.08f),new Vector3(1.15f,.12f,.04f),Straw);b.Face(Straw,new Vector3(-.65f,.3f,-.1f),new Vector3(0,.52f,-.1f),new Vector3(.65f,.3f,-.1f));
            });
            Make("Basket",b=>{b.Round(Vector3.zero,new Vector3(.2f,.3f,.17f),Straw,10,default,1.15f);b.Round(new Vector3(0,.16f,0),new Vector3(.24f,.05f,.2f),Timber,10);});
        }
    }
}
