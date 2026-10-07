using System;
using System.Collections.Generic;
using System.IO;
using Reclamation.Blight;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reclamation.Editor
{
    public static class BattleCrowdPerformanceBuilder
    {
        private const string Folder="Assets/Reclamation/Resources/CrowdPerformance";
        private const string AssetPath=Folder+"/AnimationLibrary.asset";
        private const string ScenePath="Assets/Scenes/BattleCrowdPerformance.unity";
        [MenuItem("Reclamation/Testing/Open Battle Crowd Performance Lab")]
        public static void Open()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!File.Exists(ScenePath))Prepare();else EditorSceneManager.OpenScene(ScenePath);
        }
        public static void Bake()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
            if(File.Exists(AssetPath))throw new InvalidOperationException("Library already exists. Preserve it; use the reviewed rebuild path.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            Scene preview=EditorSceneManager.NewPreviewScene();var root=new GameObject("Crowd bake rig");SceneManager.MoveGameObjectToScene(root,preview);
            try
            {
                var rig=root.AddComponent<JointMannequin>();rig.Build();
                var library=ScriptableObject.CreateInstance<CrowdAnimationLibrary>();AssetDatabase.CreateAsset(library,AssetPath);
                library.nearRun=new Mesh[16];library.farRun=new Mesh[16];library.nearAttack=new Mesh[16];library.farAttack=new Mesh[16];
                for(int frame=0;frame<16;frame++)
                {
                    rig.Sample(JointMannequin.Pose.Run,frame/16f);
                    library.nearRun[frame]=Body(rig,8,false);library.farRun[frame]=Body(rig,4,false);
                    rig.Sample(JointMannequin.Pose.LightAttack,frame/15f);
                    library.nearAttack[frame]=Body(rig,8,true);library.farAttack[frame]=Body(rig,4,true);
                }
                rig.Sample(JointMannequin.Pose.Guard,0);library.nearIdle=Body(rig,8,true);library.farIdle=Body(rig,4,true);
                library.friendly=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Crowd friendly",enableInstancing=true};library.friendly.color=new Color(.12f,.48f,.8f);
                library.enemy=new Material(library.friendly){name="Crowd enemy"};library.enemy.color=new Color(.8f,.25f,.12f);
                AssetDatabase.AddObjectToAsset(library.friendly,library);AssetDatabase.AddObjectToAsset(library.enemy,library);
                EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
                Debug.Log("CROWD_BAKED: near "+library.nearRun[0].vertexCount+" vertices / far "+library.farRun[0].vertexCount+" vertices; fixed hands, 66 shared poses.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(preview);}
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
            var mesh=new Mesh{name="Crowd pose "+sides+" sides"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.AddObjectToAsset(mesh,AssetPath);return mesh;
        }
        public static void Prepare()
        {
            if(File.Exists(ScenePath))return;
            var library=AssetDatabase.LoadAssetAtPath<CrowdAnimationLibrary>(AssetPath);if(!library)throw new InvalidOperationException("Bake the crowd library first.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Isolated battle crowd benchmark").AddComponent<BattleCrowdPerformanceLab>().library=library;
            // Serialized renderer reference retains the material's instancing variants in player builds.
            var reference=new GameObject("Instancing material reference",typeof(MeshFilter),typeof(MeshRenderer));reference.transform.position=Vector3.down*1000;
            reference.GetComponent<MeshFilter>().sharedMesh=library.nearIdle;reference.GetComponent<MeshRenderer>().sharedMaterial=library.friendly;
            if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new InvalidOperationException("Could not save benchmark scene.");
        }
        public static void BuildWindows()
        {
            string destination=Environment.GetEnvironmentVariable("RECLAMATION_CROWD_BUILD");
            if(string.IsNullOrEmpty(destination) || File.Exists(destination))throw new InvalidOperationException("Set RECLAMATION_CROWD_BUILD to a new executable path.");
            Prepare();var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=destination,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(result.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Crowd player build failed.");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(destination),"BUILD-RESULT.txt"),"Succeeded\n"+result.summary.totalSize+" bytes\n"+result.summary.totalWarnings+" warnings\nNon-development build");
        }
    }
}
