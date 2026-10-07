using System;
using System.Collections.Generic;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Atlas
{
    // Bounded character art trial. This component never moves the actor or advances its fighter.
    [DefaultExecutionOrder(300)]
    public sealed partial class AtlasSyntyVisual : MonoBehaviour
    {
        private Animator animator;
        private Transform model, sword, shield, scabbard;
        private readonly Dictionary<Transform, Quaternion> rest = new Dictionary<Transform, Quaternion>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Renderer> original = new List<Renderer>();
        private readonly List<Renderer> shown = new List<Renderer>();
        private readonly Limb[] arms = new Limb[2], legs = new Limb[2];
        private Vector3 hipsOrigin, lastPosition;
        private float cycle, blend;
        private bool initialized;
        public bool Ready => initialized;
        public Transform ModelRoot => model;
        public Transform Sword => sword;
        public float MinimumElbowClearance { get; private set; }
        public int SkinVertices { get; private set; }
        private sealed class Limb
        {
            public Transform upper, lower, end;
            public Vector3 origin, palm;
            public Quaternion endRotation, palmBasis;
        }
        public void Build(AtlasCharacterVisual previous, bool hostile, SyntyRole role=SyntyRole.Player, int variant=0)
        {
            if (initialized) return;
            Role=role;Variant=variant;
            var prefab = Resources.Load<GameObject>("Player/SidekickPlayer");
            if (!prefab) throw new InvalidOperationException("Missing project-owned SidekickPlayer prefab.");
            if (previous) foreach (var r in previous.GetComponentsInChildren<Renderer>(true))
                if (r.enabled && !r.transform.IsChildOf(transform)) original.Add(r);
            model = Instantiate(prefab, transform, false).transform;
            model.name = hostile ? "Synty raider" : "Synty founder";
            model.gameObject.SetActive(true);
            foreach (var b in model.GetComponentsInChildren<MonoBehaviour>(true)) b.enabled = false;
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (var rb in model.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
            animator = model.GetComponentInChildren<Animator>(true);
            if (!animator || !animator.isHuman) throw new InvalidOperationException("Synty requires a humanoid avatar.");
            animator.gameObject.SetActive(true); animator.applyRootMotion = false;
            animator.runtimeAnimatorController = null; animator.Rebind(); animator.Update(0); animator.enabled = false;
            if(role==SyntyRole.Player)ReplaceSampleProsthetic();else BuildRoleBody();
            model.localPosition = Vector3.zero; model.localRotation = Quaternion.identity; model.localScale = Vector3.one;
            float hipHeight = transform.InverseTransformPoint(Bone(HumanBodyBones.Hips).position).y;
            model.localScale = Vector3.one * (.98f / hipHeight);
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) rest[t] = t.localRotation;
            hipsOrigin = Bone(HumanBodyBones.Hips).localPosition;
            for (int i = 0; i < 2; i++)
            {
                bool left = i == 0;
                arms[i] = Cache(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm,
                    left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm,
                    left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
                legs[i] = Cache(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg,
                    left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg,
                    left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
                var hand = arms[i];
                Transform middle = Bone(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
                Transform index = Bone(left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal);
                Transform little = Bone(left ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal);
                Vector3 forward = middle.position - hand.end.position;
                Vector3 normal = Vector3.Cross(forward, index.position - little.position).normalized;
                hand.palm = hand.end.InverseTransformVector(forward * .55f);
                hand.palmBasis = Quaternion.Inverse(hand.end.rotation) * Quaternion.LookRotation(forward, normal);
                // Fixed grip: no per-frame finger animation or finger IK.
                string side = left ? "Left" : "Right";
                var curled = new Dictionary<Transform,Quaternion>();
                foreach (string finger in new[] { "Index", "Middle", "Ring", "Little", "Thumb" })
                    foreach (string section in new[] { "Proximal", "Intermediate", "Distal" })
                    {
                        var id = (HumanBodyBones)Enum.Parse(typeof(HumanBodyBones), side + finger + section);
                        var joint = animator.GetBoneTransform(id); if (!joint) continue;
                        Vector3 direction = joint.childCount > 0 ? joint.GetChild(0).position-joint.position : forward;
                        Vector3 axis = joint.InverseTransformDirection(Vector3.Cross(direction.normalized, normal * (left ? 1 : -1)).normalized);
                        float curl=left?(finger=="Thumb"?25:section=="Intermediate"?60:section=="Distal"?35:50):(finger=="Thumb"?32:section=="Intermediate"?80:62);
                        curled[joint] = joint.localRotation * Quaternion.AngleAxis(curl, axis);
                    }
                foreach(var pair in curled){pair.Key.localRotation=pair.Value;rest[pair.Key]=pair.Value;}
            }
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (r.sharedMesh) SkinVertices += r.sharedMesh.vertexCount;
                r.updateWhenOffscreen = role==SyntyRole.Player;
            }
            BuildEquipment(hostile);
            BuildRoleDetails();
            shown.AddRange(GetComponentsInChildren<Renderer>(true));
            lastPosition = transform.position; initialized = true;
            Sample(new DuelFighter(), false, false, 0); SetVisible(true);
        }
        public Transform Bone(HumanBodyBones id) => animator.GetBoneTransform(id);
        private void ReplaceSampleProsthetic()
        {
            // PF_SampleFace has an Outlander saw forearm and NO left-hand mesh.
            // Trim only that bone-weighted region on instance-owned mesh copies.
            // Rebind shipped knight modules to the same skeleton; vendor assets stay intact.
            var lower = Bone(HumanBodyBones.LeftLowerArm);
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var body = skins[0];
            foreach(var skin in skins)
            {
                if(!skin.sharedMesh)continue;
                var weights=skin.sharedMesh.boneWeights;var bones=skin.bones;
                bool OnArm(int bone)=>bone>=0&&bone<bones.Length&&bones[bone]&&(bones[bone]==lower||bones[bone].IsChildOf(lower));
                bool Remove(int index)
                {
                    var w=weights[index];
                    float influence=(OnArm(w.boneIndex0)?w.weight0:0)+(OnArm(w.boneIndex1)?w.weight1:0)+(OnArm(w.boneIndex2)?w.weight2:0)+(OnArm(w.boneIndex3)?w.weight3:0);
                    return influence>.49f;
                }
                if(weights.Length==0)continue;
                var copy=Instantiate(skin.sharedMesh);copy.name="Synty trial / prosthetic removed";meshes.Add(copy);
                for(int sub=0;sub<copy.subMeshCount;sub++)
                {
                    var triangles=copy.GetTriangles(sub);var kept=new List<int>();
                    for(int i=0;i<triangles.Length;i+=3)
                        if(!Remove(triangles[i])&&!Remove(triangles[i+1])&&!Remove(triangles[i+2]))
                        {kept.Add(triangles[i]);kept.Add(triangles[i+1]);kept.Add(triangles[i+2]);}
                    copy.SetTriangles(kept,sub);
                }
                copy.RecalculateBounds();skin.sharedMesh=copy;
            }
            var skeleton=new Dictionary<string,Transform>();foreach(var t in model.GetComponentsInChildren<Transform>(true))skeleton[t.name]=t;
            foreach(string part in new[]{"SK_FANT_KNGT_17_13ALWL_HU01","SK_FANT_KNGT_17_15HNDL_HU01"})
            {
                var prefab=Resources.Load<GameObject>("Meshes/Outfits/Starter/"+part);
                if(!prefab)throw new InvalidOperationException("Missing Synty knight arm module: "+part);
                foreach(var source in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var go=new GameObject("Trial knight module / "+part);go.transform.SetParent(body.transform,false);
                    var renderer=go.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=source.sharedMesh;
                    var moduleMaterial=Material(new Color(.29f,.32f,.33f),.35f);
                    var moduleMaterials=new Material[source.sharedMesh.subMeshCount];for(int i=0;i<moduleMaterials.Length;i++)moduleMaterials[i]=moduleMaterial;
                    renderer.sharedMaterials=moduleMaterials;renderer.rootBone=body.rootBone;
                    var mapped=new Transform[source.bones.Length];
                    for(int i=0;i<mapped.Length;i++)
                        if(!skeleton.TryGetValue(source.bones[i].name,out mapped[i]))throw new InvalidOperationException("Missing module bone: "+source.bones[i].name);
                    renderer.bones=mapped;renderer.localBounds=body.localBounds;
                    for(int shape=0;shape<source.sharedMesh.blendShapeCount;shape++)
                    {
                        int originalShape=body.sharedMesh.GetBlendShapeIndex(source.sharedMesh.GetBlendShapeName(shape));
                        if(originalShape>=0)renderer.SetBlendShapeWeight(shape,body.GetBlendShapeWeight(originalShape));
                    }
                }
            }
        }
        private Limb Cache(HumanBodyBones a, HumanBodyBones b, HumanBodyBones c)
        {
            var end = Bone(c);
            return new Limb { upper = Bone(a), lower = Bone(b), end = end,
                origin = transform.InverseTransformPoint(end.position),
                endRotation = Quaternion.Inverse(transform.rotation) * end.rotation };
        }
        private Vector3 World(Vector3 p) => transform.TransformPoint(p);
        private void Rotate(HumanBodyBones id, Vector3 angles)
        {
            var t = Bone(id); if (t) t.rotation = transform.rotation * Quaternion.Euler(angles) * Quaternion.Inverse(transform.rotation) * t.rotation;
        }
        public void Sample(DuelFighter fighter, bool moving, bool sprint, float delta)
        {
            if (!initialized) return;
            foreach (var pair in rest) pair.Key.localRotation = pair.Value;
            model.localPosition = Vector3.zero; model.localRotation = Quaternion.identity;
            var hips = Bone(HumanBodyBones.Hips); hips.localPosition = hipsOrigin;
            Vector3 displacement = transform.position - lastPosition; displacement.y = 0; lastPosition = transform.position;
            delta = Mathf.Clamp(delta, 0, .1f);
            contactTime=Mathf.Max(0,contactTime-delta);
            weaponReboundTime=Mathf.Max(0,weaponReboundTime-delta);
            bool neutral = fighter.Alive && fighter.Action == DuelAction.Ready;
            bool committed=!neutral||fighter.Blocking;
            float desired=Readiness==SyntyReadiness.Combat?1:Readiness==SyntyReadiness.Alert?.55f:0;
            WeaponReadiness=committed?1:Mathf.MoveTowards(WeaponReadiness,desired,delta*3);
            blend = Mathf.MoveTowards(blend, moving && neutral ? 1 : 0, delta * 10);
            float motionCycle=CombatMotion?SampleCombatMotion(displacement,moving,neutral,delta):0;
            cycle = Mathf.Repeat(cycle + (CombatMotion?motionCycle:(moving ? delta * (sprint ? 2.8f : 2.1f) : 0)), 1);
            Vector3 direction = displacement.sqrMagnitude > .00001f ? transform.InverseTransformDirection(displacement.normalized) : Vector3.forward;
            if(CombatMotion)direction=travelDirection;
            float load = 0, hit = 0, follow=0;
            if (fighter.Action == DuelAction.Windup || fighter.Action == DuelAction.Recovery)
                ExchangeWeights(fighter.Action, fighter.Progress, out load, out hit,out follow);
            float duck = fighter.Action == DuelAction.Dodge ? Mathf.Sin(fighter.Progress * Mathf.PI) : 0;
            float stagger = fighter.Action == DuelAction.Stagger ? 1 - fighter.Progress : 0;
            float recoil=ContactRecoil;
            hips.position += transform.TransformVector(new Vector3(.025f*(hit+follow-load), -.055f-.04f*WeaponReadiness-.025f*load-.035f*hit - .20f * duck + .008f * Mathf.Cos(cycle * Mathf.PI * 4) * blend, .025f * hit));
            Rotate(HumanBodyBones.Hips, new Vector3(10 * duck, -14 * load + 18 * hit+22*follow, 0));
            Rotate(HumanBodyBones.Chest, new Vector3(4*WeaponReadiness+3 * blend + 12 * duck+4*hit-5*stagger-(shieldContact?5:11)*recoil, -10*WeaponReadiness-22 * load + 26 * hit+30*follow + 4 * Mathf.Sin(cycle * Mathf.PI * 2) * blend, -3*load+3*hit));
            float breath=CombatMotion&&neutral?Mathf.Sin(breathingPhase*Mathf.PI*2)*(1-blend):0;
            if(CombatMotion)
            {
                hips.position+=transform.TransformVector(new Vector3(.018f*Mathf.Sin(cycle*Mathf.PI*2)*blend,.004f*breath,0));
                Rotate(HumanBodyBones.Hips,new Vector3(0,footTurn*.55f,0));
                Rotate(HumanBodyBones.Chest,new Vector3(.6f*breath,0,-2.5f*direction.x*blend));
            }
            for (int i = 0; i < 2; i++)
            {
                var leg = legs[i];
                Vector3 step = SidekickDuelBridge.GroundedStep(cycle + i * .5f, sprint ? .85f : .65f, sprint ? .14f : .08f) * blend;
                Vector3 target = leg.origin + direction * step.z + Vector3.up * step.y;
                if(CombatMotion)
                {
                    float stride=direction.z<-.2f?.72f:1;
                    target=leg.origin+new Vector3(direction.x*step.z*.55f,step.y,direction.z*step.z*stride);
                    // Keep strafe feet on their own side instead of crossing the knees.
                    target.x=i==0?Mathf.Min(target.x,-.10f):Mathf.Max(target.x,.10f);
                    target.y+=.035f*turnStep*(i==0?Mathf.Max(0,footTurn):Mathf.Max(0,-footTurn))/28;
                }
                // Fixed support points while committed: transfer weight above them.
                target.z += (i == 0 ? .20f : -.20f) * (1 - blend);
                ModularHumanRig.SolveGrip(leg.upper, leg.lower, leg.end, World(target), transform.forward);
                leg.end.rotation = transform.rotation * Quaternion.Euler(0,CombatMotion?footTurn:0,0)*leg.endRotation;
            }
            // The sword hand stays outside the chest. Light cuts travel diagonally;
            // the heavy preparation rises above the weapon shoulder.
            Vector3 ready = new Vector3(.40f, 1.22f, .20f);
            Vector3 raised = fighter.Heavy ? new Vector3(.40f, 1.85f, .14f) : new Vector3(.62f, 1.48f, .14f);
            Vector3 impact = new Vector3(.20f, 1.20f, .69f);
            Vector3 finish=new Vector3(.44f,1.02f,.44f);
            Vector3 handTarget = ready + (raised - ready) * load + (impact - ready) * hit+(finish-ready)*follow - Vector3.up * (.15f * duck);
            handTarget.y += .015f * Mathf.Sin(cycle * Mathf.PI * 4) * blend;
            handTarget.y+=.005f*breath;
            Quaternion blade = Quaternion.Slerp(Quaternion.Slerp(Quaternion.Euler(20, 0, -15),
                Quaternion.Euler(fighter.Heavy ? -35 : -55, -25, fighter.Heavy ? -10 : -45), load), Quaternion.Euler(85, 0, 8), hit);
            blade=Quaternion.Slerp(blade,Quaternion.Euler(110,-35,28),follow);
            if(fighter.Action==DuelAction.Recovery)
            {
                handTarget+=new Vector3(.04f,.06f,-.10f)*WeaponRebound;
                blade=Quaternion.Slerp(blade,Quaternion.Euler(45,-12,12),WeaponRebound*.55f);
            }
            if (fighter.Blocking) { handTarget = new Vector3(.40f, 1.39f, .22f); blade = Quaternion.Euler(5, 0, -15); }
            float sway=Mathf.Sin(cycle*Mathf.PI*2)*.075f*blend;
            handTarget=Vector3.Lerp(new Vector3(.39f,.83f,.02f+sway),handTarget,WeaponReadiness);
            blade=Quaternion.Slerp(Quaternion.Euler(65,0,-15),blade,WeaponReadiness);
            PoseArm(1, handTarget, blade, new Vector3(.5f, -1, .1f));
            Vector3 leftTarget = fighter.Blocking ? new Vector3(-.28f, 1.36f, .34f) : new Vector3(-.32f, 1.28f, .25f);
            leftTarget.y+=.004f*breath;
            leftTarget=Vector3.Lerp(new Vector3(-.40f,.87f,.13f-sway*.3f),leftTarget,WeaponReadiness);
            if(torchOwner&&torchOwner.TorchLit)leftTarget=new Vector3(-.43f,1.20f,.28f);
            if(shieldContact)leftTarget+=new Vector3(.02f,-.025f,-.075f)*recoil;
            PoseArm(0, leftTarget - Vector3.up * (.15f * duck), Quaternion.Euler(10, 0, 0), new Vector3(-.5f, -1, .1f));
            sword.SetPositionAndRotation(arms[1].end.TransformPoint(arms[1].palm), transform.rotation * blade);
            float draw=Mathf.Clamp01(WeaponReadiness/.55f);
            sword.position=Vector3.Lerp(World(new Vector3(.38f,.94f,-.10f)),sword.position,draw);
            sword.rotation=Quaternion.Slerp(transform.rotation*Quaternion.Euler(172,0,-12),sword.rotation,draw);
            scabbard.gameObject.SetActive(!Civilian&&draw<.05f);
            shield.SetPositionAndRotation(arms[0].end.TransformPoint(arms[0].palm) + transform.forward * .12f, transform.rotation*Quaternion.Euler(shieldContact?-9*recoil:0,0,shieldContact?5*recoil:0));
            MinimumElbowClearance = Mathf.Min(transform.InverseTransformPoint(arms[1].lower.position).x,
                -transform.InverseTransformPoint(arms[0].lower.position).x);
            if (!fighter.Alive)
            {
                PoseArm(1,new Vector3(.30f,.82f,.15f),Quaternion.identity,Vector3.right);
                PoseArm(0,new Vector3(-.28f,.85f,.20f),Quaternion.identity,Vector3.left);
                model.localRotation = Quaternion.Euler(0, 0, 86);
                // Lower the torso onto the ground, keeping the actor/collider untouched.
                model.localPosition = new Vector3(.8f, .20f, 0);
                sword.position = transform.TransformPoint(new Vector3(.35f, .06f, .5f));
                sword.rotation = transform.rotation * Quaternion.Euler(90, 30, 0);
                shield.position = transform.TransformPoint(new Vector3(.6f, .10f, -.25f));
                shield.rotation = transform.rotation * Quaternion.Euler(90, 0, 0);
            }
        }
        private void PoseArm(int i, Vector3 palmTarget, Quaternion weaponRotation, Vector3 hint)
        {
            var arm = arms[i];
            Quaternion palm = transform.rotation * weaponRotation * Quaternion.LookRotation(i == 0 ? Vector3.right : Vector3.forward, i == 0 ? Vector3.back : Vector3.right) * Quaternion.Inverse(arm.palmBasis);
            ModularHumanRig.SolveGrip(arm.upper, arm.lower, arm.end, World(palmTarget) - palm * Vector3.Scale(arm.palm,arm.end.lossyScale), transform.TransformDirection(hint));
            arm.end.rotation = palm;
        }
        public void SetVisible(bool value)
        {
            foreach (var r in original) if (r) r.enabled = false;
            foreach (var r in shown) if (r) r.enabled = value;
        }
        private Material Material(Color color, float metal = 0)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.color = color; m.SetFloat("_Smoothness", .22f); m.SetFloat("_Metallic", metal); materials.Add(m); return m;
        }
        private void Part(string name, Transform parent, Vector3 position, Vector3 scale, Material material, PrimitiveType type = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Collider>().enabled = false; go.GetComponent<Renderer>().sharedMaterial = material;
        }
        private void BuildEquipment(bool hostile)
        {
            var steel = Material(new Color(.47f, .52f, .56f), .6f);
            var leather = Material(new Color(.16f, .08f, .035f));
            var trim = Material(new Color(.57f, .39f, .14f), .4f);
            var faction = Material(hostile ? new Color(.46f, .075f, .045f) : new Color(.035f, .27f, .31f));
            sword = new GameObject("One handed arming sword").transform; sword.SetParent(transform, false);
            Part("Wrapped grip", sword, Vector3.zero, new Vector3(.033f, .14f, .033f), leather, PrimitiveType.Cylinder);
            Part("Crossguard", sword, Vector3.up * .13f, new Vector3(.24f, .035f, .065f), trim);
            Part("Pommel", sword, Vector3.down * .13f, Vector3.one * .07f, trim, PrimitiveType.Sphere);
            var blade = new Mesh { name = "Tapered steel blade" }; meshes.Add(blade);
            blade.vertices = new[] { new Vector3(-.035f,.15f,0),new Vector3(0,.15f,.018f),new Vector3(.035f,.15f,0),new Vector3(0,.15f,-.018f),new Vector3(-.026f,.83f,0),new Vector3(0,.83f,.012f),new Vector3(.026f,.83f,0),new Vector3(0,.83f,-.012f),new Vector3(0,1.00f,0) };
            blade.triangles = new[] {0,1,4,1,5,4,1,2,5,2,6,5,2,3,6,3,7,6,3,0,7,0,4,7,4,5,8,5,6,8,6,7,8,7,4,8};
            blade.RecalculateNormals(); blade.RecalculateBounds();
            var go = new GameObject("Blade", typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(sword, false);
            go.GetComponent<MeshFilter>().sharedMesh = blade; go.GetComponent<MeshRenderer>().sharedMaterial = steel;
            Part("Patrol scabbard",sword,new Vector3(0,.57f,0),new Vector3(.085f,.88f,.055f),leather);
            scabbard=sword.Find("Patrol scabbard");scabbard.gameObject.SetActive(false);
            shield = new GameObject("Faction shield").transform; shield.SetParent(transform, false);
            Part("Shield rim", shield, Vector3.zero, new Vector3(.53f,.66f,.08f), steel, PrimitiveType.Sphere);
            Part("Painted face", shield, new Vector3(0,0,.025f), new Vector3(.48f,.61f,.065f), faction, PrimitiveType.Sphere);
            Part(hostile ? "Raider diagonal" : "Founder stripe", shield, new Vector3(0,0,.064f), new Vector3(hostile?.28f:.065f,.48f,.012f), trim);
            if (hostile) shield.GetChild(2).localRotation = Quaternion.Euler(0,0,35);
            Part("Rear grip",shield,new Vector3(0,0,-.12f),new Vector3(.035f,.18f,.035f),leather);
            for(int side=-1;side<=1;side+=2)Part("Grip bracket",shield,new Vector3(0,side*.09f,-.08f),new Vector3(.04f,.03f,.10f),steel);
        }
        private void OnEnable() { if(initialized)SetVisible(true); }
        private void OnDisable()
        {
            foreach(var r in shown)if(r)r.enabled=false;
            foreach (var r in original) if (r) r.enabled = true;
        }
        private void OnDestroy()
        {
            foreach (var m in materials) if (m) Destroy(m);
            foreach (var m in meshes) if (m) Destroy(m);
        }
    }
}
