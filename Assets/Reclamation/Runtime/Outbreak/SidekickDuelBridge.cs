using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    // Opt-in presentation bridge. Combat, damage, collision and input remain in the lab.
    [DefaultExecutionOrder(200)]
    public sealed partial class SidekickDuelBridge : MonoBehaviour
    {
        public BlightCombatLab lab;
        public GameObject characterPrefab;
        public bool hideUnrelatedPreviews = true;
        public bool showStatus = true;
        private bool hasPose;
        public Transform ModelRoot => model ? model.transform : null;
        public Transform WeaponRoot => weapon;
        public Vector3 weaponGripOffset = Vector3.zero;
        public Vector3 weaponRotationOffset = Vector3.zero;
        [Range(0, 80)] public float fingerCurl = 48;
        public Vector3 rightPalmRotationOffset;
        public Vector3 leftPalmRotationOffset;
        private BlightHeroVisual source;
        private GameObject model;
        private Animator animator;
        private Transform weapon;
        private Material metal;
        private BlightWeapon shownWeapon;
        private readonly List<Segment> segments = new List<Segment>();
        private readonly Dictionary<Transform, Quaternion> rest = new Dictionary<Transform, Quaternion>();
        private readonly Dictionary<SkinnedMeshRenderer, float[]> faceRest = new Dictionary<SkinnedMeshRenderer, float[]>();
        private readonly List<Renderer> hidden = new List<Renderer>();
        private Vector3 hipsRest;
        private string status = "Waiting for combat hero";
        private bool failed;
        private float nextPreviewCheck;
        public DuelFighter ReviewFighter { get; set; }
        public BlightWeapon ReviewWeapon { get; set; }
        public bool Ready => model && !failed;
        public float RightGripError { get; private set; }
        public float LeftGripError { get; private set; }
        public bool SupportGripActive { get; private set; }
        private DuelFighter Fighter => ReviewFighter ?? lab.PlayerFighter;
        private BlightWeapon ActiveWeapon => ReviewFighter == null ? lab.PlayerWeapon : ReviewWeapon;
        private readonly List<HandGrip> hands = new List<HandGrip>();
        private sealed class HandGrip
        {
            public Transform upper, lower, hand;
            public Vector3 center;
            public Quaternion basis, handInForearm;
            public bool left;
            public readonly List<Transform> fingers = new List<Transform>();
            public readonly List<Vector3> curlAxes = new List<Vector3>();
        }
        private struct Segment
        { public Transform target, end, driver, driverEnd; }

        public static string Validate(GameObject prefab)
        {
            if (!prefab) return "Choose a completed character prefab.";
            var list = prefab.GetComponentsInChildren<Animator>(true);
            if (list.Length != 1) return "Expected one Animator on the selected character.";
            string check = SidekickCharacterTest.Check(list[0]);
            if (!check.StartsWith("PASS")) return check;
            foreach (var id in new[] { HumanBodyBones.Hips, HumanBodyBones.Head, HumanBodyBones.LeftUpperArm,
                HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.RightUpperArm,
                HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, HumanBodyBones.LeftUpperLeg,
                HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.RightUpperLeg,
                HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot })
                if (!list[0].GetBoneTransform(id)) return "Missing required bone: " + id;
            return "PASS";
        }
        private void LateUpdate() => RefreshPresentation();

        public void RefreshPresentation()
        {
            if (!lab || failed) return;
            if (!ReferenceEquals(source, lab.SidekickAnimationSource))
            {
                Cleanup(); source = lab.SidekickAnimationSource;
                if (!source) return;
                try { Install(); }
                catch (System.Exception e) { Cleanup(); failed = true; status = "Sidekick setup failed: " + e.Message; Debug.LogException(e); return; }
            }
            if (!model || !source) return;
            if (lab.Paused && ReviewFighter == null && hasPose) return;
            if (hideUnrelatedPreviews && Time.unscaledTime >= nextPreviewCheck)
            {
                nextPreviewCheck = Time.unscaledTime + .5f;
                foreach (var renderer in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
                    if (renderer.gameObject.scene == gameObject.scene && !renderer.transform.IsChildOf(lab.transform) && renderer.enabled)
                    { renderer.enabled = false; if (!hidden.Contains(renderer)) hidden.Add(renderer); }
            }
            model.transform.localRotation = Quaternion.identity;
            model.transform.localPosition = Vector3.zero;
            foreach (var pair in rest) pair.Key.localRotation = pair.Value;
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            // Source and character share an actor parent; translation is presentation only.
            hips.localPosition = hipsRest;
            foreach (var segment in segments)
            {
                Vector3 from = segment.end.position - segment.target.position;
                Vector3 to = segment.driverEnd.position - segment.driver.position;
                if (from.sqrMagnitude > .000001f && to.sqrMagnitude > .000001f)
                    segment.target.rotation = Quaternion.FromToRotation(from, to) * segment.target.rotation;
            }
            if (!weapon || shownWeapon != ActiveWeapon) BuildWeapon();
            ApplyGroundedBody();
            ApplyDodgeBody();
            ApplyWeaponGrip();
            ApplyGroundedDefeat();
            ApplyFace();
            hasPose = true;
        }
        private void Install()
        {
            string check = Validate(characterPrefab);
            if (check != "PASS") throw new System.InvalidOperationException(check);
            model = Instantiate(characterPrefab, source.transform.parent, false);
            model.name = "Sidekick playable hero"; model.SetActive(true);
            model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
            foreach (var b in model.GetComponentsInChildren<MonoBehaviour>(true)) b.enabled = false;
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (var b in model.GetComponentsInChildren<Rigidbody>(true)) b.isKinematic = true;
            animator = model.GetComponentInChildren<Animator>(true);
            animator.gameObject.SetActive(true); animator.applyRootMotion = false;
            animator.runtimeAnimatorController = null; animator.Rebind(); animator.Update(0); animator.enabled = false;
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) rest[t] = t.localRotation;
            CacheHand(false); CacheHand(true);
            hipsRest = animator.GetBoneTransform(HumanBodyBones.Hips).localPosition;
            CacheGroundedBody();
            Add(HumanBodyBones.Hips, HumanBodyBones.Spine, "Pelvis", "Spine");
            Add(HumanBodyBones.Spine, HumanBodyBones.Chest, "Spine", "Chest");
            Add(HumanBodyBones.Chest, HumanBodyBones.Neck, "Chest", "Neck");
            Add(HumanBodyBones.Neck, HumanBodyBones.Head, "Neck", "Head");
            Add(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, "UpperArm_L", "Forearm_L");
            Add(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, "Forearm_L", "Hand_L");
            Add(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, "UpperArm_R", "Forearm_R");
            Add(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, "Forearm_R", "Hand_R");
            Add(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, "Thigh_L", "Shin_L");
            Add(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, "Shin_L", "Foot_L");
            Add(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, "Thigh_R", "Shin_R");
            Add(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, "Shin_R", "Foot_R");
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!renderer.sharedMesh) continue;
                var weights = new float[renderer.sharedMesh.blendShapeCount];
                for (int i = 0; i < weights.Length; i++) weights[i] = renderer.GetBlendShapeWeight(i);
                faceRest[renderer] = weights;
            }
            foreach (var renderer in source.GetComponentsInChildren<Renderer>(true))
                if (renderer.enabled) { hidden.Add(renderer); renderer.enabled = false; }
            status = "Sidekick hero active — " + segments.Count + " mapped segments. Existing combat rules.";
            if (showStatus) Debug.Log(status);
        }
        private void Add(HumanBodyBones a, HumanBodyBones b, string c, string d)
        {
            var target = animator.GetBoneTransform(a); var end = animator.GetBoneTransform(b);
            var driver = source.Rig.Bone(c); var driverEnd = source.Rig.Bone(d);
            if (target && end && driver && driverEnd) segments.Add(new Segment { target = target, end = end, driver = driver, driverEnd = driverEnd });
        }
        private void CacheHand(bool left)
        {
            var grip = new HandGrip { left = left,
                upper = animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm),
                lower = animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm),
                hand = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand) };
            grip.handInForearm = Quaternion.Inverse(grip.lower.rotation) * grip.hand.rotation;
            string prefix = left ? "Left" : "Right";
            var middle = animator.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
            var index = animator.GetBoneTransform(left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal);
            var little = animator.GetBoneTransform(left ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal);
            Vector3 forward = middle ? middle.position - grip.hand.position : grip.hand.position - grip.lower.position;
            Vector3 across = index && little ? index.position - little.position : model.transform.forward;
            Vector3 normal = Vector3.Cross(forward, across).normalized;
            if (normal.sqrMagnitude < .01f) normal = model.transform.forward;
            grip.center = grip.hand.InverseTransformVector(forward * .55f);
            grip.basis = Quaternion.Inverse(grip.hand.rotation) * Quaternion.LookRotation(forward, normal);
            foreach (string finger in new[] { "Index", "Middle", "Ring", "Little", "Thumb" })
                foreach (string section in new[] { "Proximal", "Intermediate", "Distal" })
                {
                    if (!System.Enum.TryParse(prefix + finger + section, out HumanBodyBones id)) continue;
                    var joint = animator.GetBoneTransform(id); if (!joint) continue;
                    Vector3 direction = joint.childCount > 0 ? joint.GetChild(0).position - joint.position : forward;
                    Vector3 axis = Vector3.Cross(direction.normalized, normal * (left ? 1 : -1)).normalized;
                    if (axis.sqrMagnitude < .01f) axis = across.normalized;
                    grip.fingers.Add(joint); grip.curlAxes.Add(joint.InverseTransformDirection(axis));
                }
            hands.Add(grip);
        }
        private void ApplyWeaponGrip()
        {
            foreach (HandGrip grip in hands)
            {
                if (grip.left && (!Fighter.Alive || Fighter.Action == DuelAction.Dodge || Fighter.Action == DuelAction.Stagger)) continue;
                grip.upper.localRotation = rest[grip.upper]; grip.lower.localRotation = rest[grip.lower]; grip.hand.localRotation = rest[grip.hand];
            }
            var socket = source.Rig.Bone("WeaponSocket_R");
            var chest = animator.GetBoneTransform(HumanBodyBones.Chest) ?? animator.GetBoneTransform(HumanBodyBones.Spine);
            Vector3 shift = chest ? chest.position - source.Rig.Bone("Chest").position : Vector3.zero;
            Quaternion rotation = socket.rotation * Quaternion.Euler(weaponRotationOffset);
            Vector3 contact = socket.position + shift + model.transform.TransformVector(weaponGripOffset);
            OverrideSwordPath(ref contact, ref rotation);
            SupportGripActive = Fighter.Alive && Fighter.Action != DuelAction.Dodge && Fighter.Action != DuelAction.Stagger;
            // Put both palm targets inside their arm reach before solving either arm.
            // Moving only the right anchor after solving can strand the support hand.
            for (int pass = 0; pass < 8; pass++)
                foreach (HandGrip grip in hands)
                {
                    if (grip.left && !SupportGripActive) continue;
                    Quaternion palm = rotation * Quaternion.LookRotation(grip.left ? Vector3.down : Vector3.up, grip.left ? Vector3.left : Vector3.right) *
                        Quaternion.Inverse(grip.basis) * Quaternion.Euler(grip.left ? leftPalmRotationOffset : rightPalmRotationOffset);
                    Vector3 wrist = contact + rotation * (grip.left ? SupportOffset(ActiveWeapon) : Vector3.zero) -
                        palm * Vector3.Scale(grip.center, grip.hand.lossyScale);
                    float reach = Vector3.Distance(grip.upper.position, grip.lower.position) +
                        Vector3.Distance(grip.lower.position, grip.hand.position) - .015f;
                    Vector3 offset = wrist - grip.upper.position;
                    if (offset.magnitude > reach) contact += offset.normalized * (reach - offset.magnitude);
                }
            // Mirrored palm frames keep the index/thumb side toward the blade on both hands.
            for (int i = 0; i < hands.Count; i++)
            {
                var grip = hands[i];
                if (grip.left && !SupportGripActive) continue;
                Vector3 goal = contact + rotation * (grip.left ? SupportOffset(ActiveWeapon) : Vector3.zero);
                Quaternion desired = rotation * Quaternion.LookRotation(grip.left ? Vector3.down : Vector3.up, grip.left ? Vector3.left : Vector3.right) * Quaternion.Inverse(grip.basis) * Quaternion.Euler(grip.left ? leftPalmRotationOffset : rightPalmRotationOffset);
                Vector3 wrist = goal - desired * Vector3.Scale(grip.center, grip.hand.lossyScale);
                ModularHumanRig.SolveGrip(grip.upper, grip.lower, grip.hand, wrist,
                    -model.transform.up + model.transform.right * (grip.left ? -.8f : .8f) + model.transform.forward * .9f);
                // Share axial rotation with the forearm instead of twisting only the wrist.
                Vector3 forearmAxis = (grip.hand.position - grip.lower.position).normalized;
                Quaternion delta = desired * Quaternion.Inverse(grip.lower.rotation * grip.handInForearm);
                Vector3 projected = Vector3.Project(new Vector3(delta.x, delta.y, delta.z), forearmAxis);
                Quaternion twist = new Quaternion(projected.x, projected.y, projected.z, delta.w);
                if (Quaternion.Dot(twist, twist) > .000001f) grip.lower.rotation = twist.normalized * grip.lower.rotation;
                grip.hand.rotation = desired;
                for (int f = 0; f < grip.fingers.Count; f++)
                    grip.fingers[f].localRotation = rest[grip.fingers[f]] * Quaternion.AngleAxis(fingerCurl * (f >= 12 ? .55f : f % 3 == 1 ? 1.25f : f % 3 == 2 ? .7f : 1f), grip.curlAxes[f]);
                if (!grip.left)
                {
                    // Use the reachable palm as the weapon anchor, even if source reach differs.
                    contact = grip.hand.TransformPoint(grip.center);
                }
            }
            weapon.SetPositionAndRotation(contact, rotation);
            // This is an anchor distance, not a visual quality score.
            RightGripError = Vector3.Distance(hands[0].hand.TransformPoint(hands[0].center), contact);
            LeftGripError = SupportGripActive ? Vector3.Distance(hands[1].hand.TransformPoint(hands[1].center), weapon.TransformPoint(SupportOffset(ActiveWeapon))) : 0;
        }
        public static Vector3 SupportOffset(BlightWeapon kind)
            => new Vector3(0, 0, kind == BlightWeapon.Spear ? .30f : -.15f);
        private void BuildWeapon()
        {
            if (weapon) { weapon.gameObject.SetActive(false); Destroy(weapon.gameObject); }
            if (!metal)
            {
                metal = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                metal.color = new Color(.65f, .7f, .76f);
            }
            shownWeapon = ActiveWeapon;
            weapon = new GameObject("Sidekick " + shownWeapon).transform; weapon.SetParent(model.transform, false);
            if (shownWeapon == BlightWeapon.Spear)
            { Part("Shaft", new Vector3(0, 0, .6f), new Vector3(.045f, .045f, 2.2f)); Part("Point", new Vector3(0, 0, 1.8f), new Vector3(.1f, .035f, .25f)); }
            else if (shownWeapon == BlightWeapon.Axe)
            { Part("Handle", new Vector3(0, 0, .30f), new Vector3(.055f, .055f, 1.05f)); Part("Head", new Vector3(.09f, 0, .75f), new Vector3(.35f, .06f, .22f)); }
            else
            { Part("Grip", new Vector3(0, 0, -.08f), new Vector3(.065f, .065f, .34f)); Part("Guard", new Vector3(0, 0, .1f), new Vector3(.28f, .055f, .07f)); Part("Blade", new Vector3(0, 0, .65f), new Vector3(.075f, .025f, 1.05f)); }
        }
        private void Part(string label, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = label;
            go.transform.SetParent(weapon, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Collider>().enabled = false; go.GetComponent<Renderer>().sharedMaterial = metal;
        }
        private void ApplyFace()
        {
            bool hurt = !Fighter.Alive || Fighter.Action == DuelAction.Stagger;
            bool attack = Fighter.Action == DuelAction.Windup;
            foreach (var pair in faceRest)
                for (int i = 0; i < pair.Value.Length; i++)
                {
                    string name = pair.Key.sharedMesh.GetBlendShapeName(i).ToLowerInvariant();
                    float weight = pair.Value[i];
                    if (name.Contains("jawopen")) weight = hurt ? 55 : attack ? 20 : weight;
                    if (name.Contains("mouthfrown")) weight = hurt ? 55 : weight;
                    pair.Key.SetBlendShapeWeight(i, weight);
                }
        }
        private void Cleanup()
        {
            foreach (var renderer in hidden) if (renderer) renderer.enabled = true;
            hidden.Clear(); segments.Clear(); rest.Clear(); faceRest.Clear(); hands.Clear();
            hasPose = false; RightGripError = LeftGripError = 0; SupportGripActive = false;
            if (model) { model.SetActive(false); Destroy(model); } model = null; weapon = null;
        }
        private void OnDisable() { Cleanup(); source = null; failed = false; }
        private void OnDestroy() { Cleanup(); if (metal) Destroy(metal); }
        private void OnGUI()
        {
            if (showStatus && !GetComponent<SidekickMorningReview>()) GUI.Label(new Rect(16, Screen.height - 24, Screen.width - 32, 22), status);
        }
    }
}
