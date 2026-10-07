using System.Collections.Generic;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.Rendering;

namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo : MonoBehaviour
    {
        public Shader[] requiredShaders;
        private Transform world, hero;
        private AtlasCharacterVisual rig;
        private Camera view;
        private readonly List<Object> owned=new List<Object>();
        private readonly List<Matrix4x4[]> woods=new List<Matrix4x4[]>(), trunks=new List<Matrix4x4[]>();
        private readonly List<Matrix4x4[]> armies=new List<Matrix4x4[]>();
        private readonly List<Renderer> flags=new List<Renderer>();
        private Mesh leafMesh, cubeMesh, soldierMesh;
        private Material leaves, bark, soldier, water;
        private Material[] banners;
        private Texture2D mapTexture;
        public WorldAtlasModel Model { get; private set; }
        public Vector3 PlayerPosition => hero.position;
        private Vector3 Ground(Vector2 p,float offset=0)=>new Vector3(p.x,Model.Height(p.x,p.y)+offset,p.y);
        private Material Mat(Color color)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color;m.enableInstancing=true;
            m.SetFloat("_Smoothness",.05f);owned.Add(m);return m;
        }
        private Transform Shape(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material material,Transform parent=null)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent?parent:world,false);go.transform.position=pos;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;Destroy(go.GetComponent<Collider>());return go.transform;
        }
        public void LoadMap(AtlasSize size,int slots)
        {
            ResetScoutEncounter();ResetAtlasDefense();
            if(world){world.gameObject.SetActive(false);Destroy(world.gameObject);}
            foreach(var o in owned)if(o)Destroy(o);owned.Clear();woods.Clear();trunks.Clear();armies.Clear();flags.Clear();
            if(art!=null)art.Dispose();art=new AtlasArtKit();hallRoofs.Clear();hallRoofColliders.Clear();hallCenters.Clear();hallRotations.Clear();villageLayouts.Clear();developedVillages.Clear();lanterns.Clear();clockHours=9;day=1;groundPatches.Clear();
            Model=new WorldAtlasModel(size,slots);world=new GameObject("Atlas survey world").transform;world.SetParent(transform,false);
            selected=Model.factions[0].capital;simulation=false;strategyClock=0;message="Choose a region on the atlas, then explore it on foot.";
            BuildLandscape();BuildSites();BuildWoods();BuildHero();BuildArmies();BuildMapTexture();BuildVillagePeople();BuildSettlementGuards();BuildRecon();BuildWildlife();Physics.SyncTransforms();
            Travel(selected);yaw=25;pitch=18;zoom=16;UpdateCamera(true);mapOpen=false;debug=false;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=size==AtlasSize.Small?.0007f:.00045f;
            RenderSettings.fogColor=new Color(.65f,.77f,.79f);RenderSettings.ambientLight=new Color(.55f,.6f,.65f);UpdateDaylight();RefreshKnowledge();
            if(defenseMode)InitializeAtlasDefense();
        }
        private void BuildLandscape()
        {
            const int n=161;float step=Model.Extent/(n-1);var vertices=new Vector3[n*n];var uv=new Vector2[n*n];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){float px=x*step-Model.Extent/2,pz=z*step-Model.Extent/2;vertices[z*n+x]=new Vector3(px,Model.Height(px,pz),pz);uv[z*n+x]=new Vector2(x/(float)n,z/(float)n);}
            var tris=new List<int>[5];for(int i=0;i<5;i++)tris[i]=new List<int>();
            for(int z=0;z<n-1;z++)for(int x=0;x<n-1;x++)
            {
                int a=z*n+x,b=a+1,c=a+n,d=c+1;float h=(vertices[a].y+vertices[d].y)*.5f;
                int band=h<4?4:h>85?3:h>42?2:(x/9+z/7)%3==0?1:0;
                tris[band].AddRange(new[]{a,c,b,b,c,d});
            }
            var mesh=new Mesh{name="Survey terrain"};owned.Add(mesh);mesh.vertices=vertices;mesh.uv=uv;mesh.subMeshCount=5;
            for(int i=0;i<5;i++)mesh.SetTriangles(tris[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var ground=new GameObject("Landform");ground.transform.SetParent(world,false);ground.AddComponent<MeshFilter>().sharedMesh=mesh;
            ground.AddComponent<MeshRenderer>().sharedMaterials=new[]{Mat(new Color(.32f,.43f,.24f)),Mat(new Color(.4f,.48f,.26f)),Mat(new Color(.46f,.49f,.35f)),Mat(new Color(.49f,.51f,.49f)),Mat(new Color(.73f,.67f,.46f))};
            water=Mat(new Color(.15f,.39f,.46f));Shape("Water",PrimitiveType.Cube,new Vector3(0,-.4f,0),new Vector3(Model.Extent,.5f,Model.Extent),water);
            var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);cubeMesh=temp.GetComponent<MeshFilter>().sharedMesh;Destroy(temp);
            leafMesh=Cone();leaves=Mat(new Color(.18f,.33f,.23f));bark=Mat(new Color(.28f,.23f,.15f));
        }
        private Mesh Cone()
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int ring=0;ring<3;ring++)for(int i=0;i<7;i++)
            {
                float a=i*Mathf.PI*2/7,b=(i+1)*Mathf.PI*2/7,r=3-ring*.6f,y=2+ring*2.5f;
                int n=v.Count;v.Add(new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r));v.Add(new Vector3(0,y+6,0));v.Add(new Vector3(Mathf.Cos(b)*r,y,Mathf.Sin(b)*r));t.AddRange(new[]{n,n+1,n+2});
            }
            var m=new Mesh{name="Atlas pine"};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();owned.Add(m);return m;
        }
        private void BuildWoods()
        {
            var random=new System.Random(Model.Size==AtlasSize.Small?193:811);var trees=new List<Matrix4x4>();var stems=new List<Matrix4x4>();
            int count=Model.Size==AtlasSize.Small?28000:75000;
            for(int i=0;i<count;i++)
            {
                var p=new Vector2(((float)random.NextDouble()-.5f)*Model.Extent,((float)random.NextDouble()-.5f)*Model.Extent);
                float h=Model.Height(p.x,p.y);if(h<5||h>95||Vector2.Distance(Model.Nearest(p).point,p)<85)continue;
                if(Mathf.PerlinNoise(p.x/150+20,p.y/150+20)<.38f)continue;
                var nearest=Model.Nearest(p);if(developedVillages.Contains(nearest.id)&&LayoutFor(nearest.id).ReservedLand(p-nearest.point))continue;
                float scale=1+(float)random.NextDouble()*.8f;
                trees.Add(Matrix4x4.TRS(Ground(p),Quaternion.Euler(0,random.Next(360),0),Vector3.one*scale));
                stems.Add(Matrix4x4.TRS(Ground(p,3*scale),Quaternion.identity,new Vector3(.75f,6,.75f)*scale));
            }
            Batch(trees,woods);Batch(stems,trunks);
        }
        private static void Batch(List<Matrix4x4> source,List<Matrix4x4[]> target)
        {for(int i=0;i<source.Count;i+=1023)target.Add(source.GetRange(i,Mathf.Min(1023,source.Count-i)).ToArray());}
        private Color OwnerColor(int owner)=>owner>=0?Model.factions[owner].color:owner==-2?new Color(.85f,.73f,.4f):owner==-3?new Color(.75f,.25f,.27f):new Color(.67f,.7f,.63f);
        private void BuildSites()
        {
            banners=new Material[Model.factions.Count+3];for(int i=0;i<banners.Length;i++)banners[i]=Mat(OwnerColor(i-3));
            var clearing=Mat(new Color(.46f,.43f,.28f));
            Material[] resources={Mat(new Color(.75f,.7f,.3f)),bark,Mat(new Color(.58f,.6f,.58f)),Mat(new Color(.85f,.62f,.16f)),Mat(new Color(.7f,.75f,.82f))};
            foreach(var s in Model.sites)
            {
                bool inhabited=s.owner==-2||s.owner>=0&&Model.factions[s.owner].family==0;
                Vector2 bannerPoint=s.point+(inhabited?new Vector2(8,12):Vector2.zero);
                Shape(s.name,PrimitiveType.Cylinder,Ground(bannerPoint,2.5f),new Vector3(.18f,2.5f,.18f),bark);
                var cloth=Shape("Territory banner",PrimitiveType.Cube,Ground(bannerPoint+Vector2.right*.8f,4.3f),new Vector3(1.6f,1.1f,.08f),banners[s.owner+3]);flags.Add(cloth.GetComponent<Renderer>());
                if(inhabited)BuildInhabitedSite(s,clearing);
                else
                {
                    Shape("Settlement clearing",PrimitiveType.Cylinder,Ground(s.point,.08f),new Vector3(65,.06f,65),clearing);
                    if(s.owner!=-1)
                    {
                        for(int j=0;j<(s.capital?6:3);j++)PlaceSolid("Cottage"+(j%3),s.point+new Vector2((j%3-1)*14,-16-j/3*15),180);
                        art.Place("Lair",Ground(s.point+new Vector2(-8,0)));
                    }
                }
                foreach(var d in s.deposits)if(!inhabited)art.Place(d.kind.ToString(),new Vector3(d.point.x,Surface(d.point),d.point.y),d.kind==AtlasResource.Food?0:s.id*37%360);
            }
            SealGroundPatches();art.Seal();
            // Two landmark arches suggest cave mouths; underground regions are a later map layer.
            foreach(int id in new[]{Model.sites.Count/3,Model.sites.Count*2/3})
            {
                Vector2 p=Model.sites[id].point+Vector2.up*65;var stone=resources[2];
                Shape("Cavern landmark left",PrimitiveType.Cube,Ground(p+Vector2.left*7,6),new Vector3(6,12,8),stone);
                Shape("Cavern landmark right",PrimitiveType.Cube,Ground(p+Vector2.right*7,6),new Vector3(6,12,8),stone);
                Shape("Cavern landmark lintel",PrimitiveType.Cube,Ground(p,13),new Vector3(20,5,10),stone);
            }
        }
        private void BuildHero()
        {
            hero=new GameObject("Player explorer").transform;hero.SetParent(world,false);rig=hero.gameObject.AddComponent<AtlasCharacterVisual>();
            rig.Build(2,true);rig.Play("Idle");
            if(!view)
            {
                view=new GameObject("Explorer camera").AddComponent<Camera>();view.transform.SetParent(transform,false);view.gameObject.AddComponent<AudioListener>();view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.65f,.77f,.79f);view.fieldOfView=58;
                daylight=new GameObject("Atlas sun").AddComponent<Light>();daylight.transform.SetParent(transform,false);daylight.type=LightType.Directional;daylight.shadows=LightShadows.Soft;
                moonlight=new GameObject("Atlas moon").AddComponent<Light>();moonlight.transform.SetParent(transform,false);moonlight.type=LightType.Directional;moonlight.color=new Color(.56f,.68f,1);moonlight.transform.rotation=Quaternion.Euler(40,145,0);
            }
            view.farClipPlane=Model.Extent*2;view.nearClipPlane=.15f;
        }
        private void BuildArmies()
        {
            // Static yard figures convey footprint only; they are not 128 simulated combat agents.
            var parts=new List<CombineInstance>();
            void Box(Vector3 p,Vector3 s){parts.Add(new CombineInstance{mesh=cubeMesh,transform=Matrix4x4.TRS(p,Quaternion.identity,s)});}
            Box(new Vector3(0,1.25f,0),new Vector3(.6f,.65f,.35f));Box(new Vector3(0,1.85f,0),Vector3.one*.32f);
            Box(new Vector3(-.19f,.55f,0),new Vector3(.22f,.9f,.25f));Box(new Vector3(.19f,.55f,0),new Vector3(.22f,.9f,.25f));
            Box(new Vector3(.45f,1.2f,0),new Vector3(.15f,.6f,.2f));Box(new Vector3(-.45f,1.2f,0),new Vector3(.15f,.6f,.2f));
            soldierMesh=new Mesh{name="128 soldier scale figure"};soldierMesh.CombineMeshes(parts.ToArray());owned.Add(soldierMesh);soldier=Mat(new Color(.5f,.58f,.61f));
            foreach(var f in Model.factions)
            {
                var site=Model.sites[f.capital];var batch=new Matrix4x4[128];
                for(int i=0;i<128;i++){var p=site.point+new Vector2((i%16-7.5f)*1.7f,-52+i/16*1.9f);batch[i]=Matrix4x4.TRS(Ground(p),Quaternion.identity,Vector3.one);}
                armies.Add(batch);
            }
        }
        private void BuildMapTexture()
        {
            const int n=256;mapTexture=new Texture2D(n,n,TextureFormat.RGB24,false);owned.Add(mapTexture);var colors=new Color[n*n];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                float px=(x/(float)(n-1)-.5f)*Model.Extent,pz=(z/(float)(n-1)-.5f)*Model.Extent,h=Model.Height(px,pz);
                colors[z*n+x]=h<1?new Color(.12f,.29f,.36f):Color.Lerp(new Color(.28f,.4f,.24f),new Color(.74f,.72f,.6f),Mathf.Clamp01(h/150));
                if(h>5&&h<95&&Mathf.PerlinNoise(px/150+20,pz/150+20)>.43f&&Vector2.Distance(Model.Nearest(new Vector2(px,pz)).point,new Vector2(px,pz))>85)colors[z*n+x]*=.72f;
            }
            mapTexture.SetPixels(colors);mapTexture.Apply();
        }
        private void DrawInstances()
        {
            art.Draw();
            foreach(var batch in woods)Graphics.DrawMeshInstanced(leafMesh,0,leaves,batch,batch.Length,null,ShadowCastingMode.Off,false);
            foreach(var batch in trunks)Graphics.DrawMeshInstanced(cubeMesh,0,bark,batch,batch.Length,null,ShadowCastingMode.Off,false);
            if(showArmies)for(int i=0;i<armies.Count;i++)if(Knowledge.Visible(Model.sites[Model.factions[i].capital].point))Graphics.DrawMeshInstanced(soldierMesh,0,banners[i+3],armies[i],128,null,ShadowCastingMode.Off,false);
            for(int i=0;i<flags.Count;i++){flags[i].enabled=Knowledge.Visible(Model.sites[i].point);flags[i].sharedMaterial=banners[Model.sites[i].owner+3];}
        }
        private void OnDestroy(){if(art!=null)art.Dispose();foreach(var o in owned)if(o)Destroy(o);}
    }
}
