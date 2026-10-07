using System;
using System.Collections.Generic;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Atlas
{
    public enum SyntyRole { Player, CompanyCommander, PlatoonCommander, Soldier, Raider, Farmer, Woodworker, Merchant }
    public enum SyntyReadiness { Relaxed, Alert, Combat }

    public sealed partial class AtlasSyntyVisual
    {
        public SyntyRole Role { get; private set; }
        public int Variant { get; private set; }
        public bool Civilian => Role>=SyntyRole.Farmer;
        private AtlasCharacterVisual torchOwner;
        private readonly DuelFighter civilianState=new DuelFighter();
        public SyntyReadiness Readiness {get;set;}=SyntyReadiness.Combat;
        public float WeaponReadiness {get;private set;}=1;
        private void BuildRoleBody()
        {
            bool commander=Role==SyntyRole.CompanyCommander;
            bool officer=Role==SyntyRole.PlatoonCommander;
            bool military=commander||officer||Role==SyntyRole.Soldier;
            bool helmet=officer||Role==SyntyRole.Soldier&&Variant%2==0;
            var originals=model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var body=originals[0];var head=Bone(HumanBodyBones.Head);var neck=Bone(HumanBodyBones.Neck);
            foreach(var skin in originals)
            {
                var source=skin.sharedMesh;if(!source)continue;
                var weights=source.boneWeights;var bones=skin.bones;
                bool HeadBone(int b)=>b>=0&&b<bones.Length&&bones[b]&&(bones[b]==neck||!helmet&&(bones[b]==head||bones[b].IsChildOf(head)));
                bool Keep(int vertex)
                {
                    var w=weights[vertex];return (HeadBone(w.boneIndex0)?w.weight0:0)+(HeadBone(w.boneIndex1)?w.weight1:0)+(HeadBone(w.boneIndex2)?w.weight2:0)+(HeadBone(w.boneIndex3)?w.weight3:0)>.49f;
                }
                var copy=Instantiate(source);copy.name="Role head / "+Role;meshes.Add(copy);
                for(int sub=0;sub<copy.subMeshCount;sub++)
                {
                    var sourceTriangles=copy.GetTriangles(sub);var kept=new List<int>();
                    for(int t=0;t<sourceTriangles.Length;t+=3)
                        if(Keep(sourceTriangles[t])&&Keep(sourceTriangles[t+1])&&Keep(sourceTriangles[t+2]))kept.AddRange(new[]{sourceTriangles[t],sourceTriangles[t+1],sourceTriangles[t+2]});
                    copy.SetTriangles(kept,sub);
                }
                skin.sharedMesh=copy;
            }
            var skeleton=new Dictionary<string,Transform>();foreach(var t in model.GetComponentsInChildren<Transform>(true))skeleton[t.name]=t;
            Transform Resolve(Transform source)
            {
                if(skeleton.TryGetValue(source.name,out var result))return result;
                if(!source.parent)throw new InvalidOperationException("No matching Synty skeleton root: "+source.name);
                var parent=Resolve(source.parent);var go=new GameObject(source.name);go.transform.SetParent(parent,false);
                go.transform.localPosition=source.localPosition;go.transform.localRotation=source.localRotation;go.transform.localScale=source.localScale;
                skeleton[source.name]=go.transform;return go.transform;
            }
            var steel=Material(commander?new Color(.52f,.43f,.26f):officer?new Color(.40f,.45f,.48f):new Color(.26f,.29f,.31f),.4f);
            Color clothColor=Role==SyntyRole.Farmer?new Color(.47f,.37f,.21f):Role==SyntyRole.Woodworker?new Color(.22f,.31f,.19f):Role==SyntyRole.Merchant?new Color(.39f,.19f,.28f):Role==SyntyRole.Raider?new Color(.27f+Variant%3*.065f,.12f,.075f):new Color(.08f,.24f,.27f);
            var cloth=Material(clothColor);var leather=Material(new Color(.17f,.105f,.06f));
            void Module(string family,string part,Material material)
            {
                string name="SK_"+family+"_"+part+"_HU01";
                var prefab=Resources.Load<GameObject>("Meshes/Outfits/Starter/"+name);
                if(!prefab)throw new InvalidOperationException("Missing role module: "+name);
                foreach(var source in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var go=new GameObject(Role+" / "+part);go.transform.SetParent(body.transform,false);
                    var renderer=go.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=source.sharedMesh;
                    var mats=new Material[source.sharedMesh.subMeshCount];for(int i=0;i<mats.Length;i++)mats[i]=material;renderer.sharedMaterials=mats;
                    var mapped=new Transform[source.bones.Length];for(int i=0;i<mapped.Length;i++)mapped[i]=Resolve(source.bones[i]);
                    renderer.bones=mapped;renderer.rootBone=body.rootBone;renderer.localBounds=body.localBounds;
                    for(int shape=0;shape<source.sharedMesh.blendShapeCount;shape++)
                    {int match=body.sharedMesh.GetBlendShapeIndex(source.sharedMesh.GetBlendShapeName(shape));if(match>=0)renderer.SetBlendShapeWeight(shape,body.GetBlendShapeWeight(match));}
                }
            }
            const string knight="FANT_KNGT_17",plain="SCFI_CIVL_09";
            bool heavy=military||Role==SyntyRole.Raider&&Variant%3==0;
            Module(heavy?knight:plain,"10TORS",heavy?steel:cloth);
            foreach(string part in new[]{"11AUPL","12AUPR"})Module(military?knight:plain,part,military?steel:cloth);
            foreach(string part in new[]{"13ALWL","14ALWR"})Module(military?knight:plain,part,military?steel:leather);
            foreach(string part in new[]{"15HNDL","16HNDR"})Module(military?knight:plain,part,military?steel:leather);
            Module(military?knight:plain,"17HIPS",cloth);
            foreach(string part in new[]{"18LEGL","19LEGR"})Module(commander||officer?knight:plain,part,commander||officer?steel:cloth);
            foreach(string part in new[]{"20FOTL","21FOTR"})Module(military?knight:plain,part,military?steel:leather);
            if(commander||officer){Module(knight,"29ASHL",steel);Module(knight,"30ASHR",steel);}
            else if(Role==SyntyRole.Raider&&Variant%3!=2)Module(knight,Variant%2==0?"29ASHL":"30ASHR",steel);
            if(helmet){Module(knight,"22AHED",steel);Module(knight,"23AFAC",steel);}
        }
        private void BuildRoleDetails()
        {
            if(Civilian){sword.gameObject.SetActive(false);shield.gameObject.SetActive(false);return;}
            // Existing helmet plume, armor and star/name UI identify officers.
            // Do not add a primitive in the imported head bone's rotated local axes.
            if(Role==SyntyRole.Raider&&Variant%3==2)
            {
                // Smaller shield silhouette for lightly equipped raiders; rules remain identical.
                shield.localScale=Vector3.one*.8f;
            }
        }
        public void HideOriginal(IEnumerable<Renderer> renderers)
        {
            foreach(var r in renderers)if(r&&!r.transform.IsChildOf(transform)&&!original.Contains(r))original.Add(r);
            SetVisible(true);
        }
        public void BindTorch(AtlasCharacterVisual source){torchOwner=source;}
        private void LateUpdate()
        {
            if(!torchOwner||!initialized)return;
            var torch=torchOwner.transform.Find("Night watch torch");
            bool lit=torchOwner.TorchLit;
            shield.gameObject.SetActive(!lit);
            if(lit&&torch)
            {
                torch.SetPositionAndRotation(arms[0].end.TransformPoint(arms[0].palm),transform.rotation);
                foreach(var renderer in torch.GetComponentsInChildren<Renderer>())renderer.enabled=true;
            }
        }
        public void SampleCivilian(Transform torso,Transform leftUpper,Transform leftLower,Transform rightUpper,Transform rightLower,bool walking,float delta)
        {
            Sample(civilianState,walking,false,delta);
            Rotate(HumanBodyBones.Chest,torso.localEulerAngles);
            void Arm(int index,Transform upper,Transform lower)
            {
                var arm=arms[index];
                Vector3 target=transform.InverseTransformPoint(lower.TransformPoint(new Vector3(0,-.27f,0)));
                PoseArm(index,target,Quaternion.identity,new Vector3(index==0?-.6f:.6f,-1,.25f));
            }
            Arm(0,leftUpper,leftLower);Arm(1,rightUpper,rightLower);
            SetVisible(true);
        }
        public Vector3 RightPalm=>arms[1].end.TransformPoint(arms[1].palm);
    }
}
