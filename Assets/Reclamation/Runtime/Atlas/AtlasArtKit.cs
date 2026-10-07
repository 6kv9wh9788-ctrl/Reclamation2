using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Reclamation.Atlas
{
    // Shared, reusable geometry. Static scenery is instanced by kit part/material.
    public sealed partial class AtlasArtKit : IDisposable
    {
        public sealed class Asset
        {
            public Mesh mesh;
            public Material[] materials;
        }
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private readonly Dictionary<string, Asset> assets = new Dictionary<string, Asset>();
        private readonly Dictionary<Color, Material> palette = new Dictionary<Color, Material>();
        private readonly Dictionary<string, List<Matrix4x4>> placements = new Dictionary<string, List<Matrix4x4>>();
        private readonly List<(Asset asset, Matrix4x4[] matrices)> batches = new List<(Asset, Matrix4x4[])>();
        public int StaticInstances { get; private set; }
        private static readonly Color Timber = new Color(.25f,.14f,.075f), Plaster = new Color(.82f,.72f,.52f), Stone = new Color(.43f,.46f,.44f), Roof = new Color(.42f,.19f,.115f), Dark = new Color(.075f,.09f,.095f), Iron = new Color(.2f,.23f,.24f), Straw = new Color(.77f,.57f,.23f);

        private sealed class Builder
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<Color> colors = new List<Color>();
            public readonly Dictionary<Color,List<int>> triangles = new Dictionary<Color,List<int>>();
            public void Face(Color color, params Vector3[] points)
            {
                if(!triangles.ContainsKey(color))triangles[color]=new List<int>();
                int start=vertices.Count;vertices.AddRange(points);
                for(int i=1;i<points.Length-1;i++)triangles[color].AddRange(new[]{start,start+i,start+i+1});
            }
            public void Box(Vector3 center,Vector3 size,Color color,Quaternion rotation=default)
            {
                if(rotation==default)rotation=Quaternion.identity;
                Vector3[] v=new Vector3[8];for(int i=0;i<8;i++)v[i]=center+rotation*Vector3.Scale(new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1),size*.5f);
                Face(color,v[0],v[4],v[6],v[2]);Face(color,v[1],v[3],v[7],v[5]);Face(color,v[0],v[1],v[5],v[4]);Face(color,v[2],v[6],v[7],v[3]);Face(color,v[0],v[2],v[3],v[1]);Face(color,v[4],v[5],v[7],v[6]);
            }
            public void Beam(Vector3 a,Vector3 b,float width,Color color)
            {Box((a+b)*.5f,new Vector3(width,Vector3.Distance(a,b),width),color,Quaternion.FromToRotation(Vector3.up,b-a));}
            public void Round(Vector3 center,Vector3 scale,Color color,int sides=10,Quaternion rotation=default,float taper=1)
            {
                if(rotation==default)rotation=Quaternion.identity;
                for(int i=0;i<sides;i++)
                {
                    float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                    Vector3 P(float angle,float y,float radius)=>center+rotation*Vector3.Scale(new Vector3(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius),scale);
                    var p=P(a,-.5f,1);var q=P(b,-.5f,1);var r=P(b,.5f,taper);var s=P(a,.5f,taper);
                    Face(color,p,s,r,q);Face(color,center+rotation*Vector3.up*scale.y*.5f,r,s);Face(color,center-rotation*Vector3.up*scale.y*.5f,p,q);
                }
            }
            public void Oval(Vector3 center,Vector3 scale,Color color,int sides=10,int rings=6)
            {
                Vector3 P(int ring,int side){float p=-Mathf.PI*.5f+Mathf.PI*ring/rings,a=side*Mathf.PI*2/sides;return center+Vector3.Scale(new Vector3(Mathf.Cos(p)*Mathf.Cos(a),Mathf.Sin(p),Mathf.Cos(p)*Mathf.Sin(a)),scale);}
                for(int r=0;r<rings;r++)for(int s=0;s<sides;s++)Face(color,P(r,s),P(r+1,s),P(r+1,s+1),P(r,s+1));
            }
        }
        private Asset Make(string name,Action<Builder> create)
        {
            var b=new Builder();create(b);var mesh=new Mesh{name="Atlas art / "+name};mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(b.vertices);mesh.subMeshCount=b.triangles.Count;
            var mats=new List<Material>();int sub=0;foreach(var pair in b.triangles)
            {
                mesh.SetTriangles(pair.Value,sub++);
                if(!palette.TryGetValue(pair.Key,out var mat)){mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=pair.Key,enableInstancing=true};mat.SetFloat("_Smoothness",.08f);if(pair.Key==new Color(1,.55f,.12f)){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",pair.Key*2);}palette[pair.Key]=mat;owned.Add(mat);}mats.Add(mat);
            }
            mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);var asset=new Asset{mesh=mesh,materials=mats.ToArray()};assets[name]=asset;return asset;
        }
        public AtlasArtKit()
        {
            for(int i=0;i<3;i++){int variant=i;Make("Cottage"+i,b=>House(b,variant));}
            Make("Storehouse",b=>{House(b,2);for(int i=0;i<3;i++)Barrel(b,new Vector3(4.7f,.1f,i*1.2f-1),.7f);});
            BuildVillageAssets();BuildHinterlandAssets();BuildGuardAssets();BuildWildlifeAssets();
            Make("Market",Market);Make("Well",Well);Make("Tower",Tower);
            Make("Food",Farm);Make("Wood",Logs);Make("Stone",b=>Rocks(b,Stone));Make("Gold",b=>Rocks(b,new Color(.88f,.65f,.17f)));Make("Niter",b=>Rocks(b,new Color(.78f,.85f,.82f)));
            Make("Lair",b=>{for(int i=0;i<5;i++)b.Round(new Vector3((i-2)*1.5f,2+i%2,0),new Vector3(1,4+i%2*2,1),new Color(.23f,.25f,.22f),5,Quaternion.Euler(0,0,(i-2)*-8),.12f);});
            for(int i=0;i<3;i++){int variant=i;Make("Body"+i,b=>Body(b,variant));Make("Head"+i,b=>Head(b,variant));Make("Arm"+i,b=>Arm(b,variant));}
            Make("Leg",b=>{b.Round(new Vector3(0,-.29f,0),new Vector3(.115f,.58f,.13f),new Color(.27f,.29f,.26f),8,default,.8f);b.Oval(new Vector3(0,-.58f,.065f),new Vector3(.125f,.14f,.23f),Timber,8,4);});
        }
        private static void Gable(Builder b,float width,float depth,float eave,float peak,Color color)
        {
            float w=width*.5f,d=depth*.5f;
            b.Face(color,new Vector3(-w,eave,-d),new Vector3(0,peak,-d),new Vector3(w,eave,-d));
            b.Face(color,new Vector3(w,eave,d),new Vector3(0,peak,d),new Vector3(-w,eave,d));
            b.Face(Roof,new Vector3(-w,eave,d),new Vector3(0,peak,d),new Vector3(0,peak,-d),new Vector3(-w,eave,-d));
            b.Face(Roof,new Vector3(0,peak,d),new Vector3(w,eave,d),new Vector3(w,eave,-d),new Vector3(0,peak,-d));
            for(int side=-1;side<=1;side+=2)for(int row=0;row<5;row++)
            {
                float t=(row+.5f)/5,x=side*w*t,y=Mathf.Lerp(peak,eave,t)+.045f;
                b.Beam(new Vector3(x,y,-d),new Vector3(x,y,d),.13f,Roof*.85f);
            }
            b.Beam(new Vector3(0,peak+.08f,-d-.15f),new Vector3(0,peak+.08f,d+.15f),.2f,Timber);
            foreach(float z in new[]{-d,d}){b.Beam(new Vector3(-w,eave,z),new Vector3(0,peak,z),.22f,Timber);b.Beam(new Vector3(w,eave,z),new Vector3(0,peak,z),.22f,Timber);}
        }
        private static void House(Builder b,int variant)
        {
            float top=variant==2?5.4f:3.8f;var plaster=variant==1?new Color(.7f,.73f,.59f):Plaster;
            b.Box(new Vector3(0,.35f,0),new Vector3(8.2f,.7f,7.2f),Stone);b.Box(new Vector3(0,(top+.6f)*.5f,0),new Vector3(8,top-.6f,7),plaster);
            foreach(float x in new[]{-3.9f,0,3.9f})foreach(float z in new[]{-3.55f,3.55f})b.Box(new Vector3(x,top*.5f,z),new Vector3(.22f,top,.22f),Timber);
            foreach(float y in new[]{.8f,top-.12f})b.Box(new Vector3(0,y,0),new Vector3(8.25f,.22f,7.25f),Timber);
            Gable(b,9,8,top,top+2.7f,plaster);
            b.Box(new Vector3(0,1.45f,-3.58f),new Vector3(1.35f,2.3f,.12f),Timber);
            for(int i=0;i<5;i++)b.Box(new Vector3(-.5f+i*.25f,1.45f,-3.66f),new Vector3(.025f,2.1f,.025f),Dark);
            b.Round(new Vector3(.43f,1.4f,-3.74f),new Vector3(.08f,.08f,.08f),Iron,8);
            foreach(float x in new[]{-2.5f,2.5f})
            {
                b.Box(new Vector3(x,2.3f,-3.57f),new Vector3(1.4f,1.4f,.16f),Timber);b.Box(new Vector3(x,2.3f,-3.67f),new Vector3(1.12f,1.12f,.06f),Dark);
                b.Box(new Vector3(x,2.3f,-3.72f),new Vector3(.09f,1.2f,.08f),Straw);b.Box(new Vector3(x,2.3f,-3.72f),new Vector3(1.2f,.09f,.08f),Straw);
                b.Box(new Vector3(x,1.5f,-3.82f),new Vector3(1.7f,.15f,.6f),Timber);
                b.Beam(new Vector3(x-.65f,.9f,3.6f),new Vector3(x+.65f,top-.3f,3.6f),.16f,Timber);
            }
            b.Box(new Vector3(2.5f,top+1.5f,1.5f),new Vector3(.85f,3.3f,.9f),Stone);b.Box(new Vector3(2.5f,top+3.2f,1.5f),new Vector3(1.1f,.25f,1.15f),Stone*.8f);
            b.Box(new Vector3(0,.18f,-4.1f),new Vector3(1.9f,.36f,1.1f),Stone);
            if(variant==1)Barrel(b,new Vector3(-4.5f,0,-2.5f),.65f);
        }
        private static void Barrel(Builder b,Vector3 p,float scale)
        {
            b.Round(p+Vector3.up*scale,new Vector3(.65f,2,.65f)*scale,Timber*1.5f,12);
            foreach(float y in new[]{.3f,1.65f})b.Round(p+Vector3.up*y*scale,new Vector3(.68f,.13f,.68f)*scale,Iron,12);
            for(int i=0;i<4;i++)b.Box(p+new Vector3((i-1.5f)*.25f,2.02f,0)*scale,new Vector3(.02f,.025f,1)*scale,Timber);
        }
        private static void Market(Builder b)
        {
            foreach(float x in new[]{-2.5f,2.5f})foreach(float z in new[]{-1.6f,1.6f})b.Beam(new Vector3(x,0,z),new Vector3(x,3.7f,z),.16f,Timber);
            b.Box(new Vector3(0,1.05f,0),new Vector3(5, .22f,2),Timber);
            for(int i=0;i<8;i++){float x=(i-3.5f)*.7f;b.Box(new Vector3(x,3.6f,0),new Vector3(.7f,.12f,4),i%2==0?Plaster:new Color(.31f,.48f,.4f),Quaternion.Euler(12,0,0));}
            for(int i=0;i<4;i++)
            {
                float x=(i-1.5f)*1.1f;b.Box(new Vector3(x,1.27f,0),new Vector3(.95f,.3f,1.1f),Straw);
                for(int j=0;j<6;j++)b.Oval(new Vector3(x+(j%3-1)*.22f,1.53f,(j/3-.5f)*.3f),Vector3.one*.17f,i%2==0?new Color(.6f,.22f,.13f):new Color(.54f,.64f,.2f),6,4);
            }
            Barrel(b,new Vector3(3.3f,0,0),.7f);
        }
        private static void Well(Builder b)
        {
            for(int row=0;row<3;row++)for(int i=0;i<12;i++){float a=(i+row*.5f)*Mathf.PI/6;b.Box(new Vector3(Mathf.Sin(a)*1.15f,.22f+row*.4f,Mathf.Cos(a)*1.15f),new Vector3(.65f,.38f,.4f),Stone,Quaternion.Euler(0,a*Mathf.Rad2Deg,0));}
            b.Round(new Vector3(0,.2f,0),new Vector3(.95f,.1f,.95f),new Color(.12f,.28f,.3f));
            foreach(float x in new[]{-1.6f,1.6f})b.Beam(new Vector3(x,0,0),new Vector3(x,3.5f,0),.2f,Timber);
            b.Beam(new Vector3(-1.6f,2.5f,0),new Vector3(1.6f,2.5f,0),.18f,Timber);b.Beam(new Vector3(0,.8f,0),new Vector3(0,2.5f,0),.045f,Straw);Gable(b,4,3,3.3f,4.4f,Plaster);
        }
        private static void Tower(Builder b)
        {
            b.Box(new Vector3(0,4,0),new Vector3(6,8,6),Stone);b.Box(new Vector3(0,8,0),new Vector3(6.8f,.5f,6.8f),Stone*.8f);
            for(int i=0;i<4;i++)foreach(float side in new[]{-3f,3f}){b.Box(new Vector3(-2.7f+i*1.8f,8.8f,side),new Vector3(.9f,1.2f,.65f),Stone);b.Box(new Vector3(side,8.8f,-2.7f+i*1.8f),new Vector3(.65f,1.2f,.9f),Stone);}
            for(int row=0;row<7;row++)b.Box(new Vector3(0,1+row, -3.015f),new Vector3(6,.045f,.04f),Stone*.7f);
            b.Box(new Vector3(0,1.5f,-3.08f),new Vector3(1.5f,3,.1f),Timber);foreach(float x in new[]{-1.7f,1.7f})b.Box(new Vector3(x,5.6f,-3.05f),new Vector3(.22f,1.3f,.1f),Dark);
        }
        private static void Farm(Builder b)
        {
            b.Box(new Vector3(0,.04f,0),new Vector3(13,.08f,12),new Color(.29f,.22f,.12f));
            for(int row=0;row<9;row++)for(int x=0;x<16;x++)
            {
                var p=new Vector3((x-7.5f)*.7f,0,(row-4)*1.2f);b.Beam(p,p+Vector3.up*.95f,.035f,Straw);
                b.Round(p+Vector3.up*.85f,new Vector3(.11f,.42f,.11f),Straw,5,default,.4f);
                b.Beam(p+Vector3.up*.35f,p+new Vector3(.25f,.68f,.1f),.035f,new Color(.43f,.49f,.2f));
            }
            for(int i=0;i<5;i++)foreach(float z in new[]{-6.5f,6.5f})b.Beam(new Vector3(-6+i*3,0,z),new Vector3(-6+i*3,1.1f,z),.12f,Timber);
            foreach(float z in new[]{-6.5f,6.5f})b.Beam(new Vector3(-6,.7f,z),new Vector3(6,.7f,z),.1f,Timber);
        }
        private static void Logs(Builder b)
        {
            for(int row=0;row<3;row++)for(int i=0;i<4-row;i++)
            {
                var p=new Vector3((i-(3-row)*.5f)*.9f,.48f+row*.8f,0);
                b.Round(p,new Vector3(.46f,5,.46f),Timber,10,Quaternion.Euler(90,0,0));
                foreach(float z in new[]{-2.51f,2.51f}){b.Round(p+Vector3.forward*z,new Vector3(.38f,.03f,.38f),Straw,10,Quaternion.Euler(90,0,0));b.Round(p+Vector3.forward*(z*1.005f),new Vector3(.12f,.03f,.12f),Timber*1.8f,8,Quaternion.Euler(90,0,0));}
            }
            b.Round(new Vector3(3,.6f,1),new Vector3(.8f,1.2f,.8f),Timber,9);b.Round(new Vector3(3,1.22f,1),new Vector3(.75f,.03f,.75f),Straw,9);
        }
        private static void Rocks(Builder b,Color vein)
        {
            for(int i=0;i<7;i++){float a=i*2.4f;var p=new Vector3(Mathf.Sin(a)*(1+i*.26f),.55f+i%3*.35f,Mathf.Cos(a)*(1+i*.26f));b.Oval(p,new Vector3(1.5f,1+i%3*.4f,1.15f),i%2==0?Stone:Stone*.83f,7,4);if(vein!=Stone)for(int j=0;j<3;j++)b.Oval(p+new Vector3((j-1)*.38f,.9f+i%3*.35f,-.15f),new Vector3(.3f,.25f,.2f),vein,5,3);}
        }
        private static Color Cloth(int variant)=>variant==0?new Color(.36f,.5f,.42f):variant==1?new Color(.5f,.32f,.23f):new Color(.35f,.43f,.57f);
        private static Color Skin(int variant)=>variant==1?new Color(.5f,.31f,.21f):new Color(.76f,.53f,.36f);
        private static void Body(Builder b,int variant)
        {
            b.Round(new Vector3(0,1.13f,0),new Vector3(.27f,.64f,.17f),Cloth(variant),10,default,1.14f);
            b.Round(new Vector3(0,.77f,0),new Vector3(.24f,.28f,.16f),new Color(.27f,.29f,.26f),10);
            b.Round(new Vector3(0,.86f,0),new Vector3(.285f,.075f,.185f),Timber,10);
            b.Box(new Vector3(0,1.02f,.18f),new Vector3(.32f,.46f,.035f),variant==1?Straw:Plaster);
            b.Round(new Vector3(0,1.5f,0),new Vector3(.085f,.14f,.09f),Skin(variant),8);
        }
        private static void Head(Builder b,int variant)
        {
            b.Oval(Vector3.zero,new Vector3(.17f,.22f,.17f),Skin(variant));b.Oval(new Vector3(0,.11f,-.045f),new Vector3(.175f,.15f,.15f),Timber,10,4);
            foreach(float x in new[]{-.064f,.064f})b.Oval(new Vector3(x,.025f,.155f),new Vector3(.02f,.022f,.018f),Dark,6,4);
            b.Oval(new Vector3(0,-.025f,.173f),new Vector3(.035f,.05f,.045f),Skin(variant),6,4);
            if(variant==0){b.Round(new Vector3(0,.17f,0),new Vector3(.32f,.035f,.3f),Straw,12);b.Round(new Vector3(0,.25f,0),new Vector3(.18f,.16f,.18f),Straw,10,default,.7f);}
            if(variant==2)b.Oval(new Vector3(0,.16f,-.02f),new Vector3(.19f,.12f,.18f),Plaster,10,4);
        }
        private static void Arm(Builder b,int variant)
        {
            b.Round(new Vector3(0,-.14f,0),new Vector3(.105f,.28f,.11f),Cloth(variant),8,default,.85f);
            b.Round(new Vector3(0,-.38f,.025f),new Vector3(.075f,.24f,.075f),Skin(variant),8);
            b.Oval(new Vector3(0,-.54f,.035f),new Vector3(.077f,.105f,.07f),Skin(variant),8,4);
        }
        public void Place(string name,Vector3 position,float yaw=0,Vector3 scale=default)
        {
            if(!placements.ContainsKey(name))placements[name]=new List<Matrix4x4>();
            placements[name].Add(Matrix4x4.TRS(position,Quaternion.Euler(0,yaw,0),scale==default?Vector3.one:scale));StaticInstances++;
        }
        public void Seal()
        {
            batches.Clear();
            foreach(var pair in placements)
            {
                // Small spatial batches avoid oversized instance buffers and improve culling locality.
                var cells=new Dictionary<Vector2Int,List<Matrix4x4>>();
                foreach(var matrix in pair.Value)
                {
                    var key=new Vector2Int(Mathf.FloorToInt(matrix.m03/160),Mathf.FloorToInt(matrix.m23/160));
                    if(!cells.TryGetValue(key,out var cell)){cell=new List<Matrix4x4>();cells[key]=cell;}cell.Add(matrix);
                }
                foreach(var cell in cells.Values)for(int i=0;i<cell.Count;i+=128)batches.Add((assets[pair.Key],cell.GetRange(i,Mathf.Min(128,cell.Count-i)).ToArray()));
            }
        }
        public void Draw()
        {foreach(var batch in batches)for(int sub=0;sub<batch.asset.materials.Length;sub++)Graphics.DrawMeshInstanced(batch.asset.mesh,sub,batch.asset.materials[sub],batch.matrices,batch.matrices.Length,null,ShadowCastingMode.On,true);}
        public Transform Object(string name,Transform parent,Vector3 position)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;var a=assets[name];go.AddComponent<MeshFilter>().sharedMesh=a.mesh;go.AddComponent<MeshRenderer>().sharedMaterials=a.materials;return go.transform;
        }
        public void Dispose(){foreach(var o in owned)if(o)UnityEngine.Object.Destroy(o);owned.Clear();}
    }
}
