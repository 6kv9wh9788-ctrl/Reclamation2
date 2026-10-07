using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo
    {
        private sealed class GroundPatch
        {
            public readonly List<Vector3> vertices=new List<Vector3>();
            public readonly List<int> triangles=new List<int>();
        }
        private readonly Dictionary<Color,GroundPatch> groundPatches=new Dictionary<Color,GroundPatch>();
        private void SurfacePatch(Vector2 center,float width,float depth,float yaw,Color color,float lift=.12f,int field=-1,AtlasVillageLayout layout=null)
        {
            if(!groundPatches.TryGetValue(color,out var patch)){patch=new GroundPatch();groundPatches[color]=patch;}
            int nx=Mathf.Max(1,Mathf.CeilToInt(width)),nz=Mathf.Max(1,Mathf.CeilToInt(depth)),start=patch.vertices.Count;
            var rotation=Quaternion.Euler(0,yaw,0);
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
            {
                float along=z/(float)nz-.5f;float edge=field>=0?layout.FieldEdge(field,along):1;
                var offset=rotation*new Vector3((x/(float)nx-.5f)*width*edge,0,along*depth);var point=center+new Vector2(offset.x,offset.z);
                patch.vertices.Add(new Vector3(point.x,Surface(point)+lift,point.y));
            }
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int v=start+z*(nx+1)+x;patch.triangles.AddRange(new[]{v,v+nx+1,v+1,v+1,v+nx+1,v+nx+2});}
        }
        private void BuildQuarryGround(Vector2 center,float yaw,int seed)
        {
            var color=new Color(.4f,.4f,.35f);
            if(!groundPatches.TryGetValue(color,out var patch)){patch=new GroundPatch();groundPatches[color]=patch;}
            var rotation=Quaternion.Euler(0,yaw,0);const int sides=36,rings=16;int start=patch.vertices.Count;
            for(int ring=0;ring<=rings;ring++)for(int i=0;i<=sides;i++)
            {
                float a=i*Mathf.PI*2/sides,r=ring/(float)rings*(1+.1f*Mathf.Sin(a*5+seed%9));
                var offset=rotation*new Vector3(Mathf.Cos(a)*13*r,0,Mathf.Sin(a)*17*r);var p=center+new Vector2(offset.x,offset.z);
                patch.vertices.Add(new Vector3(p.x,Surface(p)+.1f,p.y));
            }
            for(int ring=0;ring<rings;ring++)for(int i=0;i<sides;i++){int v=start+ring*(sides+1)+i;patch.triangles.AddRange(new[]{v,v+1,v+sides+1,v+1,v+sides+2,v+sides+1});}
        }
        private void SealGroundPatches()
        {
            foreach(var pair in groundPatches)
            {
                var mesh=new Mesh{name="Terrain-following paths and fields",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(pair.Value.vertices);mesh.SetTriangles(pair.Value.triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
                var go=new GameObject(mesh.name);go.transform.SetParent(world,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=Mat(pair.Key);
            }
            groundPatches.Clear();
        }
        private Vector3 HinterlandGround(Vector2 point)=>new Vector3(point.x,Surface(point),point.y);
        private int workplaceTour;
        public void VisitWorkplace()
        {
            if(!developedVillages.Contains(selected)){message="Choose a developed settlement to visit its workplaces.";return;}
            var site=Model.sites[selected];var layout=LayoutFor(selected);int stop=workplaceTour++%3;
            Vector2 local=stop==0?layout.Field(1)+layout.Hinterland(new Vector2(-17,0)):layout.Collection(stop==1?1:4);
            var p=site.point+local;hero.position=new Vector3(p.x,Surface(p),p.y);yaw=layout.HinterlandAngle+90;pitch=24;zoom=20;mapOpen=false;UpdateVillagePeople(0);UpdateDaylight();UpdateCamera(true);
            message=new[]{"Farmland: three varied fields outside the town. H visits the forest next.","Forest edge: woodcutters carry timber to the log collection point. H visits the quarry.","Quarry: exposed stone and ore beside the rock face. H returns to the fields."}[stop];
        }
        private Vector2 ResourceDisplayPoint(AtlasSite site,AtlasDeposit deposit)
        {
            if(!developedVillages.Contains(site.id))return deposit.point;
            var layout=LayoutFor(site.id);
            return site.point+(deposit.kind==AtlasResource.Food?layout.Field(1):layout.Hinterland(deposit.kind==AtlasResource.Wood?new Vector2(106,-55):deposit.kind==AtlasResource.Gold?new Vector2(106,39):deposit.kind==AtlasResource.Niter?new Vector2(106,64):new Vector2(108,50)));
        }
        private void BuildHinterland(AtlasSite site,Material path)
        {
            var layout=LayoutFor(site.id);var random=new System.Random(layout.Seed^38193);
            Vector2 P(Vector2 p)=>site.point+layout.Hinterland(p);
            for(int field=0;field<3;field++)
            {
                Vector2 center=site.point+layout.Field(field);
                var size=layout.FieldSize(field);
                SurfacePatch(center,size.x,size.y,layout.HinterlandAngle,new Color(.29f,.23f,.14f),.12f,field,layout);
                int columns=field==2?20:28,rows=20;
                for(int x=0;x<columns;x++)for(int z=0;z<rows;z++)
                {
                    float along=(z+.5f)/rows-.5f;
                    Vector2 plant=center+layout.Hinterland(new Vector2(((x+.5f)/columns-.5f)*(size.x-1)*layout.FieldEdge(field,along),along*(size.y-1)));
                    art.Place(field==2?"Vegetable":"Grain",HinterlandGround(plant)+Vector3.up*.12f,layout.HinterlandAngle);
                }
                for(int side=-1;side<=1;side+=2)for(int j=0;j<8;j++)
                {
                    var fence=center+layout.Hinterland(new Vector2((j-3.5f)*size.x/8*layout.FieldEdge(field,side*.5f),side*(size.y*.5f+.7f)));art.Place("FieldFence",HinterlandGround(fence),layout.HinterlandAngle);
                }
                var collection=site.point+layout.Collection(field*3);art.Place("HarvestCrates",HinterlandGround(collection+layout.Hinterland(Vector2.down*3)),layout.HinterlandAngle);
            }
            // A real forest patch shares the surrounding woodland's distribution and has an accessible edge.
            for(int i=0;i<180;i++)
            {
                var local=new Vector2(101+(float)random.NextDouble()*29,-84+(float)random.NextDouble()*54);var point=P(local);
                if(Mathf.Pow((local.x-115)/17,2)+Mathf.Pow((local.y+57)/30,2)>1.05f)continue;
                if(!Model.Walkable(point)||Model.Nearest(point).id!=site.id)continue;
                art.Place("WorkTree",HinterlandGround(point),random.Next(360),Vector3.one*(1.15f+(float)random.NextDouble()*.6f));
                if(i%3==0)art.Place("ForestBrush",HinterlandGround(point+Vector2.right),random.Next(360));
            }
            for(int i=1;i<18;i+=6)art.Place("WorkTree",HinterlandGround(site.point+layout.WorkTarget(i)),layout.HinterlandAngle,Vector3.one*1.2f);
            art.Place("Wood",HinterlandGround(P(new Vector2(79,-59))),layout.HinterlandAngle);
            BuildQuarryGround(P(new Vector2(106,49.5f)),layout.HinterlandAngle,layout.Seed);
            for(int i=0;i<9;i++)
            {
                var point=P(new Vector2(108+random.Next(8),40+i*3));
                art.Place("WorkRock",HinterlandGround(point)-Vector3.up*(.5f+(float)random.NextDouble()),random.Next(360),new Vector3(3+random.Next(4),3+random.Next(4),3+random.Next(3)));
            }
            for(int i=0;i<45;i++)
            {
                float a=(float)random.NextDouble()*Mathf.PI*2,r=6+(float)random.NextDouble()*12;
                var p=P(new Vector2(110+Mathf.Cos(a)*r*.6f,50+Mathf.Sin(a)*r));
                art.Place("WorkRock",HinterlandGround(p)-Vector3.up*.1f,random.Next(360),Vector3.one*(.15f+(float)random.NextDouble()*.6f));
            }
            for(int i=4;i<18;i+=6)art.Place("OreFace",HinterlandGround(site.point+layout.WorkTarget(i)),layout.HinterlandAngle);
            foreach(var deposit in site.deposits)
                if(deposit.kind==AtlasResource.Gold||deposit.kind==AtlasResource.Niter)
                    art.Place(deposit.kind.ToString(),HinterlandGround(P(new Vector2(106,deposit.kind==AtlasResource.Gold?39:64))),layout.HinterlandAngle,Vector3.one*.65f);
            art.Place("HarvestCrates",HinterlandGround(P(new Vector2(82,54))),layout.HinterlandAngle);
            // Draw the shared town access once, then branch into distinct working districts.
            var road=layout.WorkRoad(0);for(int i=1;i<road.Length-3;i++)BuildLane(site.point+road[i-1],site.point+road[i],2.6f,path);
            BuildLane(P(new Vector2(58,-62)),P(new Vector2(58,60)),2.6f,path);
            foreach(int resident in new[]{0,3,6,1,4})BuildLane(site.point+layout.CollectionGate(resident),site.point+layout.Collection(resident),2,path);
            for(int i=0;i<18;i++)if(i%3!=2)BuildLane(site.point+layout.Collection(i),site.point+layout.Job(i),1.6f,path);
        }
    }
    public sealed partial class AtlasArtKit
    {
        private void BuildHinterlandAssets()
        {
            Make("Grain",b=>{b.Beam(Vector3.zero,Vector3.up*.8f,.035f,Straw);b.Round(Vector3.up*.72f,new Vector3(.1f,.3f,.1f),Straw,5);});
            Make("Vegetable",b=>b.Oval(Vector3.up*.2f,new Vector3(.3f,.25f,.3f),new Color(.35f,.47f,.19f),6,3));
            Make("CropTile",b=>{b.Box(new Vector3(0,.025f,0),new Vector3(6,.05f,6),new Color(.29f,.23f,.14f));for(int row=0;row<5;row++)for(int x=0;x<7;x++){var p=new Vector3((x-3)*.8f,0,(row-2)*1.2f);b.Beam(p,p+Vector3.up*.8f,.035f,Straw);b.Round(p+Vector3.up*.72f,new Vector3(.1f,.3f,.1f),Straw,5);}});
            Make("VegetableTile",b=>{b.Box(new Vector3(0,.025f,0),new Vector3(6,.05f,6),new Color(.26f,.22f,.13f));for(int row=0;row<4;row++)for(int x=0;x<5;x++)b.Oval(new Vector3((x-2)*1.15f,.2f,(row-1.5f)*1.4f),new Vector3(.3f,.25f,.3f),new Color(.35f,.47f,.19f),6,3);});
            Make("FieldFence",b=>{b.Beam(Vector3.zero,Vector3.up*1.1f,.12f,Timber);b.Beam(new Vector3(-1.5f,.65f,0),new Vector3(1.5f,.65f,0),.1f,Timber);});
            Make("RockGround",b=>b.Box(new Vector3(0,.03f,0),new Vector3(5,.06f,5),new Color(.4f,.4f,.35f)));
            Make("OreFace",b=>{b.Oval(new Vector3(0,.65f,0),new Vector3(1.3f,1,1.2f),Stone,7,4);for(int i=0;i<4;i++)b.Oval(new Vector3(-.5f,.4f+i*.3f,(i%2-.5f)*.6f),new Vector3(.2f,.13f,.18f),Iron,5,3);});
            Make("HarvestCrates",b=>{for(int i=0;i<3;i++){var p=new Vector3((i-1)*.85f,.3f,0);b.Box(p,new Vector3(.75f,.6f,.8f),Timber*1.5f);b.Oval(p+Vector3.up*.35f,new Vector3(.3f,.18f,.3f),Straw,6,3);}});
            Make("ForestBrush",b=>{b.Oval(new Vector3(0,.5f,0),new Vector3(1.2f,.7f,1),new Color(.17f,.28f,.12f),7,4);b.Beam(new Vector3(-1,0,-.4f),new Vector3(.6f,.25f,1),.22f,Timber);});
            Make("OreLoad",b=>{b.Round(Vector3.zero,new Vector3(.2f,.3f,.17f),Timber,8);b.Oval(Vector3.up*.15f,new Vector3(.18f,.16f,.15f),Stone,7,3);});
            Make("LogBundle",b=>{for(int i=0;i<3;i++)b.Round(new Vector3((i-1)*.11f,0,0),new Vector3(.085f,.8f,.085f),Timber*1.5f,6,Quaternion.Euler(0,0,90));});
        }
    }
}
