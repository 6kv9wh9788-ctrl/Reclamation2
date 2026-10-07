using System;
using System.IO;
using Reclamation.Atlas;
using Reclamation.Blight;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reclamation.Editor
{
    public static class AtlasSyntyReview
    {
        private static SyntyRole reviewRole=SyntyRole.Player;
        private static int reviewVariant;
        private static bool roleReview;
        public static void CaptureRoles()
        {
            string output=Environment.GetEnvironmentVariable("RECLAMATION_SYNTY_REVIEW");
            roleReview=true;
            foreach(var role in new[]{SyntyRole.CompanyCommander,SyntyRole.PlatoonCommander,SyntyRole.Soldier,SyntyRole.Raider,SyntyRole.Farmer,SyntyRole.Woodworker,SyntyRole.Merchant})
                for(int variant=0;variant<(role==SyntyRole.Raider?3:role==SyntyRole.Soldier?2:1);variant++)
                {
                    reviewRole=role;reviewVariant=variant;
                    Environment.SetEnvironmentVariable("RECLAMATION_SYNTY_REVIEW",Path.Combine(output,role+"-"+variant));Capture();
                }
            Environment.SetEnvironmentVariable("RECLAMATION_SYNTY_REVIEW",output);
        }
        // Disposable review scene; no saved scene or vendor asset is modified.
        public static void Capture()
        {
            string output=Environment.GetEnvironmentVariable("RECLAMATION_SYNTY_REVIEW");
            if(string.IsNullOrEmpty(output))throw new InvalidOperationException("Set RECLAMATION_SYNTY_REVIEW.");
            Directory.CreateDirectory(output);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientLight=new Color(.60f,.63f,.68f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.fog=false;
            var light=new GameObject("Review key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(35,-35,0);
            var root=new GameObject("Synty review");var visual=root.AddComponent<AtlasSyntyVisual>();visual.Build(null,reviewRole==SyntyRole.Raider,reviewRole,reviewVariant);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.06f,0);floor.transform.localScale=new Vector3(30,.1f,30);
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(.21f,.25f,.27f);floor.GetComponent<Renderer>().sharedMaterial=mat;
            var camera=new GameObject("Review camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.16f,.19f);camera.fieldOfView=32;
            var target=new RenderTexture(1000,1000,24);camera.targetTexture=target;
            string report="Vertices: "+visual.SkinVertices+"\n";
            foreach(var id in new[]{HumanBodyBones.Hips,HumanBodyBones.Chest,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftFoot})report+=id+": "+visual.Bone(id).position+"\n";
            foreach(string pose in roleReview?new[]{"idle","run","block"}:new[]{"idle","walk","run","light-load","light-contact","heavy-load","heavy-contact","block","dodge","stagger","defeat","left-hand"})
            {
                var fighter=new DuelFighter();bool moving=pose=="walk"||pose=="run";
                if(pose.StartsWith("light")||pose.StartsWith("heavy"))
                {fighter.Attack(BlightEquipment.Weapon(BlightWeapon.Sword,pose.StartsWith("heavy")));fighter.Advance(fighter.Strike.Windup*(pose.EndsWith("load")?.65f:1));}
                if(pose=="block")fighter.Blocking=true;
                if(pose=="dodge"){fighter.Dodge();fighter.Advance(.16f);}
                if(pose=="stagger")fighter.Receive(20,true,false);
                if(pose=="defeat")fighter.Receive(100,false,false);
                for(int i=0;i<20;i++)visual.Sample(fighter,moving,pose=="run",1f/60);
                report+=pose+" elbow clearance: "+visual.MinimumElbowClearance+"\n";
                var shield=root.transform.Find("Faction shield");if(pose=="left-hand")shield.gameObject.SetActive(false);
                // Camera.Render alone does not invalidate edit-mode skinning between poses.
                // Bake each posed renderer for faithful stills, leaving runtime assets untouched.
                var skins=visual.GetComponentsInChildren<SkinnedMeshRenderer>();
                var bakedObjects=new System.Collections.Generic.List<GameObject>();
                var bakedMeshes=new System.Collections.Generic.List<Mesh>();
                foreach(var skin in skins)
                {
                    var mesh=new Mesh();skin.BakeMesh(mesh);bakedMeshes.Add(mesh);
                    var copy=new GameObject("Review skin",typeof(MeshFilter),typeof(MeshRenderer));copy.transform.SetParent(skin.transform,false);
                    copy.GetComponent<MeshFilter>().sharedMesh=mesh;copy.GetComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                    skin.enabled=false;bakedObjects.Add(copy);
                }
                for(int angle=0;angle<3;angle++)
                {
                    Vector3 direction=Quaternion.Euler(0,angle==0?20:angle==1?90:180,0)*Vector3.forward;
                    Vector3 aim=new Vector3(0,pose.StartsWith("heavy")?1.3f:1.12f,0);
                    camera.transform.position=aim+direction*(pose.StartsWith("heavy")?6:4.8f);camera.transform.LookAt(aim);
                    if(pose=="left-hand"){aim=visual.Bone(HumanBodyBones.LeftHand).position;camera.transform.position=aim+direction*.9f;camera.transform.LookAt(aim);}
                    camera.Render();RenderTexture.active=target;
                    var texture=new Texture2D(1000,1000,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1000,1000),0,0);texture.Apply();
                    File.WriteAllBytes(Path.Combine(output,pose+"-"+angle+".png"),texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                }
                foreach(var copy in bakedObjects)UnityEngine.Object.DestroyImmediate(copy);
                foreach(var mesh in bakedMeshes)UnityEngine.Object.DestroyImmediate(mesh);
                foreach(var skin in skins)skin.enabled=true;
                shield.gameObject.SetActive(!visual.Civilian);
            }
            File.WriteAllText(Path.Combine(output,"review.txt"),report);
            RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(mat);
            Debug.Log("ATLAS_SYNTY_REVIEW_COMPLETE");
        }
    }
}
