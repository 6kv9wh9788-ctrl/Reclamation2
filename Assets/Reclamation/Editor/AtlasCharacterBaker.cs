using System;
using System.Collections.Generic;
using System.IO;
using Reclamation.Blight;
using Reclamation.Atlas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Reclamation.Editor
{
    public static class AtlasCharacterBaker
    {
        private static string AssetPath="Assets/Reclamation/Resources/AtlasCharacters.asset";
        public static void Bake() => BakeLibrary(false);
        public static void BakeCombat() { BakeLibrary(false,true); AtlasOutfitBaker.BakeCombat(); }
        public static void BakePresentation() { BakeLibrary(true); AtlasOutfitBaker.BakePresentation(); }
        private static void BakeLibrary(bool presentation,bool combat=false)
        {
            AssetPath = "Assets/Reclamation/Resources/" + (combat ? "AtlasCombatPoses" : presentation ? "AtlasGuardPoses" : "AtlasCharacters") + ".asset";
            int count = combat ? 97 : presentation ? 165 : 33;
            if(File.Exists(AssetPath))throw new InvalidOperationException("Preserve the existing atlas library before a reviewed rebuild.");
            var a=ScriptableObject.CreateInstance<AtlasCharacterLibrary>();AssetDatabase.CreateAsset(a,AssetPath);
            a.body=new Mesh[count];a.gear=new Mesh[count];a.hand=new Vector3[count];a.head=new Matrix4x4[count];
            var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("Atlas bake");SceneManager.MoveGameObjectToScene(root,scene);
            try{
                var rig=root.AddComponent<JointMannequin>();rig.Build();
                var v=new List<Vector3>();var t=new List<int>();
                for(int index=0;index<count;index++){
                    if(combat)SampleCombatPose(rig,index);else SamplePose(rig, index % 33, presentation ? (index / 33) / 4f : 1);
                    a.hand[index]=rig.Joints["Wrist_L"].TransformPoint(new Vector3(0,-.06f,0));
                    a.head[index]=rig.Joints["Head"].localToWorldMatrix;
                    a.body[index]=Body(rig,8,false);
                    v.Clear();t.Clear();
                    void Part(string bone,Vector3 offset,int sides,Vector3[] rings)
                    {
                        var mesh=RawLoft(sides,rings);int start=v.Count;
                        foreach(Vector3 p in mesh.Item1)v.Add(rig.transform.InverseTransformPoint(rig.Joints[bone].TransformPoint(p+offset)));
                        foreach(int tri in mesh.Item2)t.Add(start+tri);
                    }
                    int n=8;
                    Part("Head",Vector3.zero,n,new[]{new Vector3(-.03f,.10f,.11f),new Vector3(.08f,.105f,.11f),new Vector3(.135f,.045f,.05f)});
                    Part("Chest",Vector3.zero,n,new[]{new Vector3(-.1f,.145f,.10f),new Vector3(.08f,.20f,.13f),new Vector3(.21f,.215f,.125f)});
                    Part("Elbow_L",new Vector3(0,-.15f,.075f),n,new[]{new Vector3(-.18f,.18f,.03f),new Vector3(0,.23f,.035f),new Vector3(.18f,.18f,.03f)});
                    // Sword / spear silhouettes share the same wrist socket; no equipment rules change.
                    Transform socket=rig.Weapon;int start=v.Count;float length=.78f;
                    foreach(Vector3 p in new[]{new Vector3(-.024f,-.14f,-.02f),new Vector3(.024f,-.14f,-.02f),new Vector3(.024f,length,-.02f),new Vector3(-.024f,length,-.02f),new Vector3(-.024f,-.14f,.02f),new Vector3(.024f,-.14f,.02f),new Vector3(.024f,length,.02f),new Vector3(-.024f,length,.02f)})v.Add(rig.transform.InverseTransformPoint(socket.TransformPoint(p)));
                    foreach(int tri in new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7})t.Add(start+tri);
                    a.gear[index]=Mesh("Atlas equipment "+index,v,t);
                }
                EditorUtility.SetDirty(a);AssetDatabase.SaveAssets();Debug.Log("ATLAS_CHARACTERS_BAKED "+a.body[0].vertexCount+" body vertices; 33 shared poses, fixed hands.");
            }finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
        }
        public static void SampleCombatPose(JointMannequin rig,int index)
        {
            if(index<33){SamplePose(rig,index,1);return;}
            var poses=new[]{JointMannequin.Pose.LightAttack,JointMannequin.Pose.HeavyAttack,JointMannequin.Pose.Block,JointMannequin.Pose.Dodge};
            rig.Sample(poses[(index-33)/16],(index-33)%16/15f);
        }
        public static void SamplePose(JointMannequin rig, int pose, float alert)
        {
            rig.Sample(pose == 32 ? JointMannequin.Pose.Guard : pose < 16 ? JointMannequin.Pose.Walk : JointMannequin.Pose.Run, pose % 16 / 16f);
            rig.Joints["Shoulder_L"].localRotation = Quaternion.Euler(-8, 0, -14);
            rig.Joints["Elbow_L"].localRotation = Quaternion.Euler(-65, 0, 0);
            float swing = pose == 32 ? 0 : Mathf.Sin(pose % 16 / 16f * Mathf.PI * 2) * 10;
            void Blend(string bone, Quaternion relaxed)
            { rig.Joints[bone].localRotation = Quaternion.Slerp(relaxed, rig.Joints[bone].localRotation, alert); }
            Blend("Shoulder_R", Quaternion.Euler(swing, 0, 14));
            Blend("Elbow_R", Quaternion.Euler(-14, 0, 0));
            Blend("Wrist_R", Quaternion.Euler(0, -80, -75));
            Blend("Shoulder_L", Quaternion.Euler(-5 - swing * .3f, 0, -16));
            Blend("Elbow_L", Quaternion.Euler(-25, 0, 0));
        }
        private static Mesh Body(JointMannequin rig,int sides,bool weapon)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            void Loft(string bone,params Vector3[] rings)
            {
                Transform joint=rig.Joints[bone];int start=vertices.Count;
                foreach(Vector3 ring in rings)for(int j=0;j<sides;j++)
                {float angle=j*Mathf.PI*2/sides;vertices.Add(rig.transform.InverseTransformPoint(joint.TransformPoint(new Vector3(Mathf.Cos(angle)*ring.y,ring.x,Mathf.Sin(angle)*ring.z))));}
                for(int i=0;i<rings.Length-1;i++)for(int j=0;j<sides;j++)
                {int a=start+i*sides+j,b=start+i*sides+(j+1)%sides,c=a+sides,d=b+sides;triangles.AddRange(new[]{a,b,c,b,d,c});}
                int bottom=vertices.Count;vertices.Add(rig.transform.InverseTransformPoint(joint.TransformPoint(Vector3.up*rings[0].x)));
                int top=vertices.Count;vertices.Add(rig.transform.InverseTransformPoint(joint.TransformPoint(Vector3.up*rings[rings.Length-1].x)));
                for(int j=0;j<sides;j++){int k=(j+1)%sides;triangles.AddRange(new[]{bottom,start+k,start+j,top,start+(rings.Length-1)*sides+j,start+(rings.Length-1)*sides+k});}
            }
            Loft("Pelvis",new Vector3(-.09f,.14f,.10f),new Vector3(.02f,.165f,.105f),new Vector3(.13f,.12f,.08f));
            Loft("Chest",new Vector3(-.14f,.12f,.085f),new Vector3(.06f,.185f,.11f),new Vector3(.22f,.205f,.10f),new Vector3(.29f,.07f,.065f));
            Loft("Head",new Vector3(-.13f,.047f,.05f),new Vector3(-.07f,.08f,.085f),new Vector3(.06f,.085f,.09f),new Vector3(.12f,.035f,.04f));
            foreach(string side in new[]{"L","R"})
            {
                Loft("Shoulder_"+side,new Vector3(-.30f,.042f,.042f),new Vector3(-.10f,.067f,.064f),new Vector3(.035f,.05f,.05f));
                Loft("Elbow_"+side,new Vector3(-.28f,.03f,.03f),new Vector3(-.10f,.048f,.049f),new Vector3(.02f,.041f,.041f));
                // A single closed fist shape, attached at the wrist; no finger vertices or bones.
                Loft("Wrist_"+side,new Vector3(-.12f,.033f,.027f),new Vector3(-.055f,.045f,.035f),new Vector3(0,.028f,.028f));
                Loft("Hip_"+side,new Vector3(-.41f,.05f,.05f),new Vector3(-.14f,.092f,.092f),new Vector3(.015f,.088f,.088f));
                Loft("Knee_"+side,new Vector3(-.4f,.03f,.03f),new Vector3(-.14f,.072f,.072f),new Vector3(.015f,.05f,.05f));
                Transform ankle=rig.Joints["Ankle_"+side];
                int start=vertices.Count;
                Vector3[] points={new Vector3(-.043f,-.14f,-.045f),new Vector3(.043f,-.14f,-.045f),new Vector3(.05f,-.14f,.155f),new Vector3(-.05f,-.14f,.155f),new Vector3(-.034f,-.03f,-.025f),new Vector3(.034f,-.03f,-.025f),new Vector3(.043f,-.08f,.14f),new Vector3(-.043f,-.08f,.14f)};
                foreach(Vector3 p in points)vertices.Add(rig.transform.InverseTransformPoint(ankle.TransformPoint(p)));
                foreach(int t in new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7})triangles.Add(start+t);
            }
            if(weapon)
            {
                Transform socket=rig.Weapon;int start=vertices.Count;
                foreach(Vector3 p in new[]{new Vector3(-.025f,-.08f,0),new Vector3(.025f,-.08f,0),new Vector3(.025f,.75f,0),new Vector3(-.025f,.75f,0)})vertices.Add(rig.transform.InverseTransformPoint(socket.TransformPoint(p)));
                triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3,start+2,start+1,start,start+3,start+2,start});
            }
            for(int i=0;i<triangles.Count;i+=3){int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}
            var mesh=new Mesh{name="Crowd pose "+sides+" sides"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();// Separate cloth, skin and boots while retaining the prototype body topology.
            var groups=new List<int>[] {new List<int>(),new List<int>(),new List<int>()};
            for(int i=0;i<triangles.Count;i+=3){int a=triangles[i];int group=(a>=60&&a<94)||(a>=146&&a<172)||(a>=284&&a<310)?1:(a>=224&&a<232)||(a>=362&&a<370)?2:0;groups[group].AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});}
            mesh.subMeshCount=3;for(int i=0;i<3;i++)mesh.SetTriangles(groups[i],i);
            AssetDatabase.AddObjectToAsset(mesh,AssetPath);return mesh;
        }
        private static Mesh Mesh(string name,List<Vector3> v,List<int> t)
        {var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.AddObjectToAsset(mesh,AssetPath);return mesh;}
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
