using System;
using System.Collections.Generic;
using System.IO;
using Reclamation.Blight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Reclamation.Editor
{
    public static class CrowdBattlefieldBuilder
    {
        private const string Path="Assets/Reclamation/Resources/CrowdPerformance/BattlefieldAssets.asset";
        public static void Bake()
        {
            if(File.Exists(Path))throw new InvalidOperationException("Battlefield assets already exist; preserve them before an intentional rebuild.");
            var a=ScriptableObject.CreateInstance<CrowdBattlefieldAssets>();AssetDatabase.CreateAsset(a,Path);
            a.pipeline=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset"));a.pipeline.name="Isolated battlefield pipeline";
            AssetDatabase.AddObjectToAsset(a.pipeline,a);
            var settings=new SerializedObject(a.pipeline);settings.FindProperty("m_ShadowDistance").floatValue=110;settings.FindProperty("m_ShadowCascadeCount").intValue=2;settings.FindProperty("m_MainLightShadowmapResolution").intValue=2048;settings.ApplyModifiedPropertiesWithoutUndo();
            Material Mat(string name,Color color,float metal=0)
            {var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};m.color=color;m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",metal>0?.3f:.05f);AssetDatabase.AddObjectToAsset(m,a);return m;}
            a.metal=Mat("Equipment steel",new Color(.42f,.45f,.48f),.5f);a.ground=Mat("Earth",new Color(.24f,.29f,.16f));a.rock=Mat("Stone",new Color(.28f,.29f,.27f));a.wood=Mat("Trunk",new Color(.18f,.10f,.055f));a.leaves=Mat("Canopy",new Color(.12f,.23f,.09f));
            var v=new List<Vector3>();var t=new List<int>();
            for(int z=0;z<=100;z++)for(int x=0;x<=100;x++)v.Add(new Vector3(x-50,CrowdBattlefieldAssets.Height(x-50,z-50),z-50));
            for(int z=0;z<100;z++)for(int x=0;x<100;x++){int i=z*101+x;t.AddRange(new[]{i,i+101,i+1,i+1,i+101,i+102});}
            a.terrain=Mesh("Terrain 100 x 100 m",v,t);
            a.rockMesh=Loft("Rock",6,new[]{new Vector3(0,.8f,.7f),new Vector3(.6f,1,.7f),new Vector3(1,.4f,.4f)});
            a.trunkMesh=Loft("Tree trunk",6,new[]{new Vector3(0,.16f,.16f),new Vector3(3,.12f,.12f)});
            a.canopyMesh=Loft("Tree canopy",7,new[]{new Vector3(2,1.6f,1.6f),new Vector3(3.4f,1.1f,1.1f),new Vector3(5,.02f,.02f)});
            Scene scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("Equipment bake rig");SceneManager.MoveGameObjectToScene(root,scene);
            try
            {
                var rig=root.AddComponent<JointMannequin>();rig.Build();a.equipment=new Mesh[192];
                for(int team=0;team<2;team++)for(int lod=0;lod<2;lod++)for(int motion=0;motion<3;motion++)for(int frame=0;frame<16;frame++)
                {
                    int index=((team*2+lod)*3+motion)*16+frame;
                    if(motion==2 && frame>0){a.equipment[index]=a.equipment[index-frame];continue;}
                    rig.Sample(motion==0?JointMannequin.Pose.Run:motion==1?JointMannequin.Pose.LightAttack:JointMannequin.Pose.Guard,motion==0?frame/16f:frame/15f);
                    v.Clear();t.Clear();
                    void Part(string bone,Vector3 offset,int sides,Vector3[] rings)
                    {
                        var mesh=RawLoft(sides,rings);int start=v.Count;
                        foreach(Vector3 p in mesh.Item1)v.Add(rig.transform.InverseTransformPoint(rig.Joints[bone].TransformPoint(p+offset)));
                        foreach(int tri in mesh.Item2)t.Add(start+tri);
                    }
                    int n=lod==0?8:4;
                    Part("Head",Vector3.zero,n,new[]{new Vector3(-.03f,.10f,.11f),new Vector3(.08f,.105f,.11f),new Vector3(.135f,.045f,.05f)});
                    Part("Chest",Vector3.zero,n,new[]{new Vector3(-.1f,.145f,.10f),new Vector3(.08f,.20f,.13f),new Vector3(.21f,.215f,.125f)});
                    Part("Elbow_L",new Vector3(0,-.15f,.075f),n,new[]{new Vector3(-.18f,.18f,.03f),new Vector3(0,.23f,.035f),new Vector3(.18f,.18f,.03f)});
                    // Sword / spear silhouettes share the same wrist socket; no equipment rules change.
                    Transform socket=rig.Weapon;int start=v.Count;float length=team==0?.78f:1.5f;
                    foreach(Vector3 p in new[]{new Vector3(-.024f,-.14f,-.02f),new Vector3(.024f,-.14f,-.02f),new Vector3(.024f,length,-.02f),new Vector3(-.024f,length,-.02f),new Vector3(-.024f,-.14f,.02f),new Vector3(.024f,-.14f,.02f),new Vector3(.024f,length,.02f),new Vector3(-.024f,length,.02f)})v.Add(rig.transform.InverseTransformPoint(socket.TransformPoint(p)));
                    foreach(int tri in new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7})t.Add(start+tri);
                    a.equipment[index]=Mesh("Equipment "+index,v,t);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
            EditorUtility.SetDirty(a);AssetDatabase.SaveAssets();Debug.Log("BATTLEFIELD_BAKED: terrain "+a.terrain.vertexCount+" vertices; equipment near "+a.equipment[0].vertexCount+" / far "+a.equipment[48].vertexCount);
        }
        private static Mesh Mesh(string name,List<Vector3> v,List<int> t)
        {var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.AddObjectToAsset(mesh,Path);return mesh;}
        private static Mesh Loft(string name,int sides,Vector3[] rings){var data=RawLoft(sides,rings);return Mesh(name,data.Item1,data.Item2);}
        private static Tuple<List<Vector3>,List<int>> RawLoft(int sides,Vector3[] rings)
        {
            var v=new List<Vector3>();var t=new List<int>();
            foreach(Vector3 r in rings)for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Cos(a)*r.y,r.x,Mathf.Sin(a)*r.z));}
            for(int i=0;i<rings.Length-1;i++)for(int j=0;j<sides;j++){int a=i*sides+j,b=i*sides+(j+1)%sides,c=a+sides,d=b+sides;t.AddRange(new[]{a,c,b,b,c,d});}
            int bottom=v.Count;v.Add(Vector3.up*rings[0].x);int top=v.Count;v.Add(Vector3.up*rings[rings.Length-1].x);
            for(int j=0;j<sides;j++){int k=(j+1)%sides;t.AddRange(new[]{bottom,j,k,top,(rings.Length-1)*sides+k,(rings.Length-1)*sides+j});}
            return Tuple.Create(v,t);
        }
    }
}
