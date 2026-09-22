using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    [Serializable] public sealed class ModularHumanBone { public string name; public int parent; public float[] position; }
    [Serializable] public sealed class ModularHumanFrame { public float time; public float[] rotations, offsets; }
    [Serializable] public sealed class ModularHumanClip { public string name; public float duration; public bool loop; public ModularHumanFrame[] frames; }
    [Serializable] public sealed class ModularExpression { public string name; public float[] delta; }
    [Serializable] public sealed class ModularHumanPart
    {
        public string name, slot, region;
        public float[] color, positions, normals, weights, faceDelta;
        public int[] triangles, joints;
        public ModularExpression[] expressions;
    }
    [Serializable] public sealed class ModularHumanData
    {
        public string name;
        public ModularHumanBone[] bones;
        public ModularHumanClip[] clips;
        public ModularHumanPart[] parts;
    }

    public sealed class ModularHumanRig : MonoBehaviour
    {
        private ModularHumanData data;
        private Transform[] bones;
        private Animation animationPlayer;
        private readonly List<SkinnedMeshRenderer> renderers = new List<SkinnedMeshRenderer>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<AnimationClip> animationClips = new List<AnimationClip>();
        private readonly List<Rigidbody> bodies = new List<Rigidbody>();
        public bool IsRagdoll { get; private set; }
        public bool HeavyArmour { get; private set; } = true;
        public bool Axe { get; private set; }
        public bool LongHair { get; private set; }
        public bool TwoHandGrip { get; set; }
        public bool FacialMotion { get; set; }
        public string Expression { get; private set; } = "Neutral";
        public float PlaybackSpeed { get; set; } = 1;
        public bool ShowBladeEdge { get; set; }
        public float GripError { get; private set; }
        private LineRenderer edgeLine;
        private bool crowdMode;
        private bool athleticBody;
        private Matrix4x4[] crowdBindPoses;
        private CrowdMeshCache.Entry crowdAssets;
        private SkinnedMeshRenderer crowdRenderer;
        private static readonly Dictionary<bool, ModularHumanData> DataCache = new Dictionary<bool, ModularHumanData>();
        public float SoftFace { get; private set; }
        public int BoneCount => bones == null ? 0 : bones.Length;
        public int PhysicsBodyCount => bodies.Count;
        private static Vector3 ToVector(float[] values, int start = 0) => new Vector3(values[start], values[start + 1], values[start + 2]);
        public string CurrentClip { get; private set; } = "Idle";
        public Transform Bone(string name)
        {
            if (bones != null) foreach (Transform bone in bones) if (bone.name == name) return bone;
            return null;
        }
        public static ModularHumanData Load(bool athletic)
        {
            if (DataCache.TryGetValue(athletic, out ModularHumanData cached)) return cached;
            TextAsset source = Resources.Load<TextAsset>("ReclamationArt/Modular/" + (athletic ? "AthleticHuman" : "BroadHuman"));
            if (source == null) return null;
            return DataCache[athletic] = JsonUtility.FromJson<ModularHumanData>(source.text);
        }

        public void Build(bool athletic, bool forCrowd = false)
        {
            crowdMode = forCrowd;
            if (data != null) throw new InvalidOperationException("Build each modular rig once; create another instance to change body.");
            data = Load(athletic);
            if (data == null) throw new InvalidOperationException("Missing modular human resources.");
            bones = new Transform[data.bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i] = new GameObject(data.bones[i].name).transform;
                bones[i].SetParent(data.bones[i].parent < 0 ? transform : bones[data.bones[i].parent], false);
                bones[i].localPosition = ToVector(data.bones[i].position);
            }
            var bindPoses = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++) bindPoses[i] = bones[i].worldToLocalMatrix * transform.localToWorldMatrix;
            if (crowdMode)
            {
                athleticBody = athletic; LongHair = athletic; crowdBindPoses = bindPoses;
                crowdRenderer = new GameObject("Combined crowd visual").AddComponent<SkinnedMeshRenderer>();
                crowdRenderer.transform.SetParent(transform, false); crowdRenderer.bones = bones; crowdRenderer.rootBone = bones[0];
                crowdRenderer.updateWhenOffscreen = false; crowdRenderer.localBounds = new Bounds(Vector3.up, new Vector3(5, 5, 5));
                SetAppearance(true, false, athletic ? 1 : 0); BuildAnimations(); return;
            }
            var palette = new Dictionary<Color, Material>();
            foreach (ModularHumanPart part in data.parts)
            {
                int count = part.positions.Length / 3;
                var vertices = new Vector3[count]; var normals = new Vector3[count]; var delta = new Vector3[count];
                var weights = new BoneWeight[count];
                for (int i = 0; i < count; i++)
                {
                    vertices[i] = ToVector(part.positions, i * 3);
                    normals[i] = ToVector(part.normals, i * 3); delta[i] = ToVector(part.faceDelta, i * 3);
                    weights[i] = new BoneWeight { boneIndex0 = part.joints[i * 4], boneIndex1 = part.joints[i * 4 + 1],
                        boneIndex2 = part.joints[i * 4 + 2], boneIndex3 = part.joints[i * 4 + 3], weight0 = part.weights[i * 4],
                        weight1 = part.weights[i * 4 + 1], weight2 = part.weights[i * 4 + 2], weight3 = part.weights[i * 4 + 3] };
                }
                var mesh = new Mesh { name = part.name }; mesh.vertices = vertices; mesh.normals = normals;
                mesh.triangles = part.triangles; mesh.boneWeights = weights; mesh.bindposes = bindPoses;
                mesh.AddBlendShapeFrame("SoftFace", 100, delta, null, null); mesh.RecalculateBounds(); meshes.Add(mesh);
                if (part.expressions != null) foreach (ModularExpression expression in part.expressions)
                {
                    var expressionDelta = new Vector3[count];
                    for (int i = 0; i < count; i++) expressionDelta[i] = ToVector(expression.delta, i * 3);
                    mesh.AddBlendShapeFrame(expression.name, 100, expressionDelta, null, null);
                }
                Color color = new Color(part.color[0], part.color[1], part.color[2], part.color[3]);
                if (!palette.TryGetValue(color, out Material material))
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) shader = Shader.Find("Standard");
                    material = new Material(shader) { name = part.name, color = color };
                    if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0);
                    if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", part.slot == "Heavy" ? .35f : .12f);
                    palette.Add(color, material); materials.Add(material);
                }
                var child = new GameObject(part.name); child.transform.SetParent(transform, false);
                var renderer = child.AddComponent<SkinnedMeshRenderer>(); renderer.sharedMesh = mesh;
                renderer.bones = bones; renderer.rootBone = bones[0]; renderer.sharedMaterial = material;
                renderer.updateWhenOffscreen = !crowdMode; renderer.localBounds = new Bounds(Vector3.up, new Vector3(5, 5, 5));
                renderers.Add(renderer);
            }
            BuildAnimations();
            if (!crowdMode) BuildPhysics();
            LongHair = athletic; SetAppearance(true, false, athletic ? 1 : 0);
            if (!crowdMode)
            {
                edgeLine = new GameObject("Cutting edge guide").AddComponent<LineRenderer>();
                edgeLine.transform.SetParent(Bone("WeaponSocket_R"), false);
                edgeLine.useWorldSpace = false; edgeLine.positionCount = 3; edgeLine.widthMultiplier = .008f;
                var guideMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
                guideMaterial.color = Color.yellow; materials.Add(guideMaterial); edgeLine.sharedMaterial = guideMaterial;
                edgeLine.enabled = false;
            }
        }

        private void BuildAnimations()
        {
            animationPlayer = gameObject.AddComponent<Animation>(); animationPlayer.playAutomatically = false;
            foreach (ModularHumanClip source in data.clips)
            {
                if (crowdMode && source.name != "Walk") continue;
                var clip = new AnimationClip { name = source.name, legacy = true, frameRate = 24,
                    wrapMode = source.loop ? WrapMode.Loop : WrapMode.ClampForever };
                for (int bone = 0; bone < bones.Length; bone++)
                {
                    if (crowdMode)
                    {
                        float[] q = source.frames[0].rotations;
                        bones[bone].localRotation = new Quaternion(q[bone * 4], q[bone * 4 + 1], q[bone * 4 + 2], q[bone * 4 + 3]);
                        bones[bone].localPosition = ToVector(data.bones[bone].position) + ToVector(source.frames[0].offsets, bone * 3);
                    }
                    string path = bones[bone].name; Transform ancestor = bones[bone].parent;
                    while (ancestor != transform && ancestor != null) { path = ancestor.name + "/" + path; ancestor = ancestor.parent; }
                    for (int channel = 0; channel < 7; channel++)
                    {
                        bool rotation = channel < 4; int axis = rotation ? channel : channel - 4;
                        var keys = new Keyframe[source.frames.Length];
                        for (int i = 0; i < keys.Length; i++)
                        {
                            float value = rotation ? source.frames[i].rotations[bone * 4 + axis] :
                                data.bones[bone].position[axis] + source.frames[i].offsets[bone * 3 + axis];
                            keys[i] = new Keyframe(source.frames[i].time, value);
                        }
                        if (crowdMode)
                        {
                            bool constant = true;
                            for (int i = 1; i < keys.Length; i++) if (!Mathf.Approximately(keys[i].value, keys[0].value)) { constant = false; break; }
                            if (constant)
                            {
                                continue;
                            }
                        }
                        clip.SetCurve(path, typeof(Transform), (rotation ? "localRotation." : "localPosition.") + "xyzw"[axis], new AnimationCurve(keys));
                    }
                }
                clip.EnsureQuaternionContinuity(); animationClips.Add(clip); animationPlayer.AddClip(clip, clip.name);
            }
            Play(crowdMode ? "Walk" : "Idle");
        }

        public void SetExpression(string expression)
        {
            Expression = expression;
            foreach (SkinnedMeshRenderer renderer in renderers)
                for (int i = 1; i < renderer.sharedMesh.blendShapeCount; i++)
                    renderer.SetBlendShapeWeight(i, renderer.sharedMesh.GetBlendShapeName(i) == expression ? 100 : 0);
        }

        public void SetAnimationPhase(float phase)
        {
            if (animationPlayer != null && animationPlayer[CurrentClip] != null)
                animationPlayer[CurrentClip].normalizedTime = Mathf.Repeat(phase, 1);
        }

        public void SetAppearance(bool heavy, bool axe, float softFace)
        {
            HeavyArmour = heavy; Axe = axe; SoftFace = Mathf.Clamp01(softFace);
            if (crowdMode)
            {
                CrowdMeshCache.Entry next = CrowdMeshCache.Acquire(data, crowdBindPoses, athleticBody, heavy, axe, LongHair);
                crowdRenderer.sharedMesh = next.mesh; crowdRenderer.sharedMaterials = next.materials;
                crowdRenderer.SetBlendShapeWeight(0, SoftFace * 100);
                CrowdMeshCache.Release(crowdAssets); crowdAssets = next; return;
            }
            for (int i = 0; i < renderers.Count; i++)
            {
                ModularHumanPart part = data.parts[i];
                bool visible = part.slot == "Body" ? !(heavy && part.name == "TorsoCloth") :
                    part.slot == "Heavy" ? heavy : part.slot == "HairLong" ? LongHair : part.slot == "HairShort" ? !LongHair :
                    part.slot == "Axe" ? axe : part.slot == "Sword" && !axe;
                renderers[i].gameObject.SetActive(visible); renderers[i].SetBlendShapeWeight(0, SoftFace * 100);
            }
        }
        public void SetHair(bool longHair)
        { LongHair = longHair; SetAppearance(HeavyArmour, Axe, SoftFace); }
        public bool Play(string clip)
        {
            if (IsRagdoll || animationPlayer == null || animationPlayer.GetClip(clip) == null) return false;
            CurrentClip = clip; animationPlayer.Stop(); return animationPlayer.Play(clip);
        }

        private void BuildPhysics()
        {
            string[] names = { "Pelvis", "Chest", "Head", "UpperArm_R", "Forearm_R", "UpperArm_L", "Forearm_L", "Thigh_R", "Shin_R", "Thigh_L", "Shin_L" };
            var colliders = new List<Collider>();
            foreach (string name in names)
            {
                Transform bone = Bone(name); var body = bone.gameObject.AddComponent<Rigidbody>(); body.isKinematic = true;
                body.mass = name == "Pelvis" ? 7 : name == "Chest" ? 8 : name == "Head" ? 4 : 2;
                body.solverIterations = 12; body.solverVelocityIterations = 4; bodies.Add(body);
                var capsule = bone.gameObject.AddComponent<CapsuleCollider>(); capsule.direction = 1;
                bool torso = name == "Pelvis" || name == "Chest"; bool head = name == "Head";
                capsule.radius = torso ? .13f : head ? .11f : name.Contains("Arm") ? .055f : .075f;
                capsule.height = torso ? .28f : head ? .32f : .32f;
                capsule.center = Vector3.up * (torso ? .06f : head ? .1f : -.17f); colliders.Add(capsule);
            }
            foreach (Rigidbody body in bodies)
            {
                Transform parent = body.transform.parent; Rigidbody connected = null;
                while (parent != null && parent != transform && connected == null) { connected = parent.GetComponent<Rigidbody>(); parent = parent.parent; }
                if (connected == null) continue;
                var joint = body.gameObject.AddComponent<CharacterJoint>(); joint.connectedBody = connected;
                joint.axis = Vector3.up; joint.swingAxis = Vector3.right;
                joint.lowTwistLimit = new SoftJointLimit { limit = -25 }; joint.highTwistLimit = new SoftJointLimit { limit = 25 };
                joint.swing1Limit = new SoftJointLimit { limit = body.name.Contains("Arm") ? 65 : 40 };
                joint.swing2Limit = new SoftJointLimit { limit = 30 }; joint.enableProjection = true;
            }
            // Self-collision is deliberately disabled for this first stability test.
            for (int i = 0; i < colliders.Count; i++) for (int j = i + 1; j < colliders.Count; j++) Physics.IgnoreCollision(colliders[i], colliders[j]);
        }
        public void SetRagdoll(bool enabled)
        {
            if (IsRagdoll == enabled || animationPlayer == null) return;
            IsRagdoll = enabled; animationPlayer.Stop(); animationPlayer.enabled = !enabled;
            foreach (Rigidbody body in bodies)
            {
                if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                body.isKinematic = !enabled;
            }
            if (!enabled)
            {
                for (int i = 0; i < bones.Length; i++)
                { bones[i].localPosition = ToVector(data.bones[i].position); bones[i].localRotation = Quaternion.identity; }
                Physics.SyncTransforms(); Play("Idle");
            }
        }

        private void LateUpdate()
        {
            if (data == null || IsRagdoll) return;
            AnimationState state = animationPlayer[CurrentClip];
            if (state != null) state.speed = PlaybackSpeed;
            if (edgeLine != null)
            {
                edgeLine.enabled = ShowBladeEdge;
                edgeLine.SetPosition(0, Axe ? new Vector3(.34f, 0, .76f) : new Vector3(.055f, 0, .17f));
                edgeLine.SetPosition(1, Axe ? new Vector3(.37f, 0, .96f) : new Vector3(.038f, 0, 1.22f));
                edgeLine.SetPosition(2, Axe ? new Vector3(.40f, 0, 1.15f) : new Vector3(.001f, 0, 1.46f));
            }
            if (FacialMotion)
            {
                Bone("Jaw").localRotation = Quaternion.Euler(Mathf.Max(0, Mathf.Sin(Time.time * 3)) * 15, 0, 0);
                Bone("Eye_R").localRotation = Bone("Eye_L").localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 2) * 9, 0);
            }
            if (crowdMode || !TwoHandGrip) { GripError = 0; return; }
            // A shared, reachable weapon pose drives BOTH arms. The blade broad
            // face remains normal to the swing plane, so an edge leads the cut.
            float phase = state == null ? 0 : Mathf.Clamp01(state.normalizedTime);
            bool attack = CurrentClip == "LightAttack" || CurrentClip == "HeavyAttack";
            float angle = 15;
            if (attack)
            {
                float windup = Mathf.SmoothStep(0, 1, phase / .3f);
                float cut = Mathf.SmoothStep(0, 1, (phase - .3f) / .3f);
                float recover = Mathf.SmoothStep(0, 1, (phase - .65f) / .35f);
                angle = Mathf.Lerp(15 - 40 * windup + 140 * cut, 15, recover);
            }
            Transform chest = Bone("Chest"), socket = Bone("WeaponSocket_R");
            // Weapon +X is its cutting edge, +Z its length. With +Y pointing
            // character-right, increasing pitch drives that edge forward/down.
            Quaternion rotation = transform.rotation * Quaternion.AngleAxis(angle, Vector3.right) * Quaternion.LookRotation(Vector3.up, Vector3.right);
            Vector3 anchor = chest.position + transform.rotation * new Vector3(0, 0, .25f) + rotation * new Vector3(0, 0, .11f);
            ApplyGrip("R", anchor, rotation, socket.localPosition);
            ApplyGrip("L", anchor + rotation * new Vector3(0, 0, -.22f), rotation, socket.localPosition);
            GripError = Vector3.Distance(Bone("Hand_L").TransformPoint(socket.localPosition), socket.TransformPoint(new Vector3(0, 0, -.22f)));
            foreach (string side in new[] { "L", "R" })
                foreach (string finger in new[] { "Thumb", "Index", "Middle", "Ring", "Little" })
                    for (int segment = 1; segment <= 3; segment++)
                        Bone(finger + segment + "_" + side).localRotation = Quaternion.Euler(-65, 0, 0);
        }

        private void ApplyGrip(string side, Vector3 contact, Quaternion rotation, Vector3 socketOffset)
        {
            Transform hand = Bone("Hand_" + side);
            SolveGrip(Bone("UpperArm_" + side), Bone("Forearm_" + side), hand,
                contact - rotation * socketOffset, transform.right * (side == "R" ? 1 : -1) - transform.forward * .2f);
            hand.rotation = rotation;
        }

        public static void SolveGrip(Transform upper, Transform lower, Transform hand, Vector3 target, Vector3 hint)
        {
            float a = Vector3.Distance(upper.position, lower.position), b = Vector3.Distance(lower.position, hand.position);
            Vector3 delta = target - upper.position;
            if (a < .001f || b < .001f || delta.sqrMagnitude < .000001f) return;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(a - b) + .001f, a + b - .001f);
            Vector3 direction = delta.normalized, bend = Vector3.ProjectOnPlane(hint, direction).normalized;
            if (bend.sqrMagnitude < .001f) bend = Vector3.Cross(direction, Vector3.up).normalized;
            float along = (a * a - b * b + distance * distance) / (2 * distance);
            Vector3 elbow = upper.position + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation;
            Vector3 reachable = upper.position + direction * distance;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, reachable - lower.position) * lower.rotation;
        }

        private void OnDestroy()
        {
            CrowdMeshCache.Release(crowdAssets); crowdAssets = null;
            foreach (Mesh mesh in meshes) if (mesh != null) Destroy(mesh);
            foreach (Material material in materials) if (material != null) Destroy(material);
            foreach (AnimationClip clip in animationClips) if (clip != null) Destroy(clip);
        }
    }
}
