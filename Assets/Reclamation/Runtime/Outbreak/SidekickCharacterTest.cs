using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Reclamation.Blight
{
    // An isolated art/rig test. Does not replace the gameplay character or apply damage.
    public sealed class SidekickCharacterTest : MonoBehaviour
    {
        public Animator subject;
        public AnimationClip optionalClip;
        public Camera testCamera;
        private readonly Dictionary<HumanBodyBones, Transform> bones = new Dictionary<HumanBodyBones, Transform>();
        private static readonly string[] Sides = { "Left", "Right" };
        private readonly List<SkinnedMeshRenderer> faces = new List<SkinnedMeshRenderer>();
        private readonly List<int> shapeIndices = new List<int>();
        private readonly List<float> shapeRest = new List<float>();
        private PlayableGraph graph;
        private string mode = "Rest";
        private float elapsed, yaw, distance = 3.5f;
        private int shape;
        private float weight;
        private string report;
        private bool ready;
        public Transform characterRoot;
        private readonly List<SkinnedMeshRenderer> hiddenPreviews = new List<SkinnedMeshRenderer>();
        private float nextIsolation;
        private string filter = "";
        private string faceStatus = "Select a probe to apply it immediately.";
        private bool faceCloseup;
        private readonly Dictionary<HumanBodyBones, Quaternion> referenceRotations = new Dictionary<HumanBodyBones, Quaternion>();
        private readonly Dictionary<HumanBodyBones, Vector3> referencePositions = new Dictionary<HumanBodyBones, Vector3>();
        private readonly Dictionary<HumanBodyBones, Quaternion> previousRotations = new Dictionary<HumanBodyBones, Quaternion>();
        private Vector3 referenceRootPosition;
        private Quaternion referenceRootRotation;
        private float poseDegrees;
        private int drivenJoints;

        private void IsolateSubject()
        {
            if (!characterRoot || Time.unscaledTime < nextIsolation) return;
            nextIsolation = Time.unscaledTime + .5f;
            foreach (var renderer in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.gameObject.scene != gameObject.scene ||
                    renderer.transform.IsChildOf(characterRoot) || !renderer.enabled) continue;
                renderer.enabled = false;
                if (!hiddenPreviews.Contains(renderer)) hiddenPreviews.Add(renderer);
            }
        }
        private static string ShapeKey(string value)
        {
            return value.ToLowerInvariant().Replace("_", "").Replace(".", "").Replace(" ", "");
        }
        private void ResetShapes()
        {
            for (int i = 0; i < faces.Count; i++)
                faces[i].SetBlendShapeWeight(shapeIndices[i], shapeRest[i]);
            if (faces.Count > 0) weight = shapeRest[shape];
        }
        private void Probe(string label, params string[] keys)
        {
            SetMode("Rest"); ResetShapes(); int matches = 0;
            for (int i = 0; i < faces.Count; i++)
            {
                string name = ShapeKey(faces[i].sharedMesh.GetBlendShapeName(shapeIndices[i]));
                foreach (string key in keys)
                    if (name.Contains(key))
                    { faces[i].SetBlendShapeWeight(shapeIndices[i], 85); matches++; break; }
            }
            if (faces.Count > 0) weight = faces[shape].GetBlendShapeWeight(shapeIndices[shape]);
            faceCloseup = true;
            faceStatus = matches > 0 ? label + ": applied " + matches + " matching shape(s)." :
                label + ": no matching names. Use the search below.";
        }

        public static string Check(Animator animator)
        {
            if (!animator) return "FAIL: No Animator. Select a completed, baked Sidekick character prefab.";
            if (!animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman)
                return "FAIL: A valid Humanoid Avatar is required. Check the selected prefab's Animator Avatar.";
            if (animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0)
                return "FAIL: No skinned meshes beneath the Animator. Select the completed character, not a body part.";
            return "PASS: Humanoid Avatar and skinned meshes found. Visual review still required.";
        }

        private void Start()
        {
            if (!characterRoot && subject) characterRoot = subject.transform;
            report = Check(subject);
            ready = report.StartsWith("PASS");
            Debug.Log("Sidekick character test: " + report);
            if (!ready) return;
            subject.applyRootMotion = false;
            subject.runtimeAnimatorController = null;
            subject.Rebind(); subject.Update(0);
            for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                var id = (HumanBodyBones)i; var bone = subject.GetBoneTransform(id);
                if (!bone) continue;
                bones[id] = bone;
            }
            foreach (var pair in bones)
            {
                referenceRotations[pair.Key] = pair.Value.localRotation;
                referencePositions[pair.Key] = pair.Value.localPosition;
                previousRotations[pair.Key] = pair.Value.localRotation;
            }
            referenceRootPosition = subject.transform.localPosition;
            referenceRootRotation = subject.transform.localRotation;
            int slots = 0, vertices = 0;
            var renderers = subject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                slots += renderer.sharedMaterials.Length;
                if (!renderer.sharedMesh) continue;
                vertices += renderer.sharedMesh.vertexCount;
                for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                { faces.Add(renderer); shapeIndices.Add(i); shapeRest.Add(renderer.GetBlendShapeWeight(i)); }
            }
            report += "\n" + renderers.Length + " skinned renderers | " + slots + " material slots | " + vertices + " mesh vertices | " + faces.Count + " blend shapes (includes inactive/LOD meshes).";
            subject.enabled = false;
        }

        private void SetMode(string next)
        {
            if (graph.IsValid()) graph.Destroy();
            if (subject) subject.enabled = false;
            mode = next; elapsed = 0;
            if (next != "Rest") faceCloseup = false;
            if (next != "Clip" || !optionalClip || !ready) return;
            subject.enabled = true;
            graph = PlayableGraph.Create("Sidekick test clip");
            var output = AnimationPlayableOutput.Create(graph, "Character", subject);
            var playable = AnimationClipPlayable.Create(graph, optionalClip);
            playable.SetApplyFootIK(false);
            output.SetSourcePlayable(playable);
            graph.Play();
        }

        private void Aim(HumanBodyBones joint, HumanBodyBones end, Vector3 direction)
        {
            if (!bones.TryGetValue(joint, out var start) || !bones.TryGetValue(end, out var finish)) return;
            Vector3 current = finish.position - start.position;
            if (current.sqrMagnitude < .000001f) return;
            start.rotation = Quaternion.FromToRotation(current, direction.normalized) * start.rotation;
            drivenJoints++;
        }
        private void UpdateSkeletonPose()
        {
            // Restore the actual imported reference before measuring segment directions.
            // No assumption about local bone axes or Humanoid muscle application.
            foreach (var pair in bones)
            {
                previousRotations[pair.Key] = pair.Value.localRotation;
                pair.Value.localRotation = referenceRotations[pair.Key];
                pair.Value.localPosition = referencePositions[pair.Key];
            }
            subject.transform.localPosition = referenceRootPosition;
            subject.transform.localRotation = referenceRootRotation;
            drivenJoints = 0;
            Vector3 forward = subject.transform.forward, down = -subject.transform.up, right = subject.transform.right;
            float wave = Mathf.Sin(elapsed * 4.5f);
            if (mode == "Stagger" && bones.TryGetValue(HumanBodyBones.Spine, out var spine))
            {
                float recoil = Mathf.Sin(Mathf.Clamp01(elapsed / .85f) * Mathf.PI);
                spine.rotation = Quaternion.AngleAxis(-12 * recoil, right) * spine.rotation;
                drivenJoints++;
            }
            if (mode != "Rest")
            {
                for (int side = 0; side < 2; side++)
                {
                    bool left = side == 0;
                    float sign = left ? -1 : 1;
                    var arm = left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm;
                    var elbow = left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm;
                    var hand = left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
                    Vector3 upper = down + right * sign * .12f;
                    Vector3 lower = down + forward * .12f;
                    if (mode == "Walk")
                    {
                        float stride = wave * sign;
                        upper += forward * stride * .2f;
                        lower += forward * stride * .2f;
                        var leg = left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg;
                        var knee = left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg;
                        var foot = left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot;
                        Aim(leg, knee, down - forward * stride * .28f);
                        Aim(knee, foot, down - forward * Mathf.Max(0, -stride) * .45f);
                    }
                    if (mode == "Guard" || mode == "Reach")
                    {
                        float reach = mode == "Reach" ? (Mathf.Sin(elapsed * 2) + 1) * .5f : 0;
                        upper = Vector3.Lerp(down * .7f + forward * .65f + right * sign * .15f, forward + down * .12f, reach);
                        lower = Vector3.Lerp(-down * .8f + forward * .45f, forward, reach);
                    }
                    Aim(arm, elbow, upper);
                    Aim(elbow, hand, lower);
                }
            }
            float blend = 1 - Mathf.Exp(-12 * Time.unscaledDeltaTime);
            poseDegrees = 0;
            foreach (var pair in bones)
            {
                pair.Value.localRotation = Quaternion.Slerp(previousRotations[pair.Key], pair.Value.localRotation, blend);
                poseDegrees = Mathf.Max(poseDegrees, Quaternion.Angle(referenceRotations[pair.Key], pair.Value.localRotation));
            }
        }
        private void LateUpdate()
        {
            if (!ready) return;
            IsolateSubject();
            elapsed += Time.unscaledDeltaTime;
            if (mode != "Clip" && subject) UpdateSkeletonPose();
            if (testCamera)
            {
                Vector3 center = subject.transform.position + Vector3.up * 1.1f;
                if (faceCloseup && bones.TryGetValue(HumanBodyBones.Head, out var head)) center = head.position;
                float cameraDistance = faceCloseup ? .65f : distance;
                testCamera.transform.position = center + Quaternion.Euler(10, yaw, 0) * Vector3.forward * cameraDistance;
                testCamera.transform.LookAt(center);
            }
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 370, Screen.height - 24), GUI.skin.box);
            GUILayout.Label("SIDEKICK — CHARACTER TEST v4");
            GUILayout.Label(report ?? "Starting...");
            if (hiddenPreviews.Count > 0) GUILayout.Label("ISOLATION: hid " + hiddenPreviews.Count + " extra skinned renderer(s) outside your selected character. Originals are not deleted.");
            GUILayout.Label("Skeleton direction probes; stationary walk, no foot IK or weapon grip. Rest is the reference pose.");
            if (ready)
            {
                GUILayout.BeginHorizontal();
                foreach (var name in new[] { "Rest", "Walk", "Guard", "Reach", "Stagger" })
                    if (GUILayout.Button(name)) SetMode(name);
                GUILayout.EndHorizontal();
                if (optionalClip && !optionalClip.legacy && GUILayout.Button("Play supplied clip: " + optionalClip.name)) SetMode("Clip");
                if (!optionalClip) GUILayout.Label("No animation clip supplied. This is OK for the rig test.");
                GUILayout.Label("Current: " + mode + " | Camera orbit");
                if (mode != "Clip") GUILayout.Label("Driven joints: " + drivenJoints + " | Max joint offset: " + poseDegrees.ToString("F1") + " degrees");
                if (mode != "Rest" && mode != "Clip" && elapsed > 1 && poseDegrees < 1)
                    GUILayout.Label("MOVEMENT CHECK FAILED: bones remain near reference. Copy Log setup report.");
                yaw = GUILayout.HorizontalSlider(yaw, -180, 180);
                faceCloseup = GUILayout.Toggle(faceCloseup, "Face close-up");
                GUILayout.Label("Camera distance"); distance = GUILayout.HorizontalSlider(distance, 1.2f, 6);
                if (faces.Count > 0)
                {
                    GUILayout.Space(10);
                    GUILayout.Label("Facial probes (apply immediately; stop clip)");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Blink")) Probe("Blink", "eyeblink", "eyeclose");
                    if (GUILayout.Button("Smile")) Probe("Smile", "mouthsmile");
                    if (GUILayout.Button("Jaw open")) Probe("Jaw open", "jawopen");
                    if (GUILayout.Button("Frown")) Probe("Frown", "mouthfrown");
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button("Restore all shapes")) { SetMode("Rest"); ResetShapes(); faceStatus = "Original shape weights restored."; }
                    GUILayout.Label(faceStatus);
                    GUILayout.Label("Advanced: search shape names (e.g. eye, mouth, jaw)");
                    filter = GUILayout.TextField(filter);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Previous shape")) ChangeShape(-1);
                    if (GUILayout.Button("Next shape")) ChangeShape(1);
                    GUILayout.EndHorizontal();
                    var face = faces[shape]; int index = shapeIndices[shape];
                    GUILayout.Label((shape + 1) + " / " + faces.Count + ": " + face.sharedMesh.GetBlendShapeName(index));
                    GUILayout.Label("Weight: " + Mathf.RoundToInt(weight) + "% (0 usually neutral)");
                    if (GUILayout.Button("Apply selected shape at 85%"))
                    { SetMode("Rest"); ResetShapes(); weight = 85; face.SetBlendShapeWeight(index, weight); }
                    float next = GUILayout.HorizontalSlider(weight, 0, 100);
                    if (!Mathf.Approximately(next, weight)) { weight = next; face.SetBlendShapeWeight(index, weight); }
                    if (GUILayout.Button("Restore shape")) { weight = shapeRest[shape]; face.SetBlendShapeWeight(index, weight); }
                }
                else GUILayout.Label("No blend shapes found on this prefab.");
                if (GUILayout.Button("Log setup report")) Debug.Log("Sidekick test: " + report + "\nMode: " + mode + " | Driven joints: " + drivenJoints + " | Max joint offset: " + poseDegrees.ToString("F1") + " degrees | Animator enabled: " + subject.enabled);
            }
            GUILayout.EndArea();
        }
        private void ChangeShape(int delta)
        {
            faces[shape].SetBlendShapeWeight(shapeIndices[shape], shapeRest[shape]);
            for (int step = 1; step <= faces.Count; step++)
            {
                int candidate = (shape + delta * step % faces.Count + faces.Count) % faces.Count;
                if (!ShapeKey(faces[candidate].sharedMesh.GetBlendShapeName(shapeIndices[candidate])).Contains(ShapeKey(filter))) continue;
                shape = candidate; break;
            }
            weight = shapeRest[shape];
        }
        private void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
            foreach (var renderer in hiddenPreviews) if (renderer) renderer.enabled = true;
        }
    }
}
