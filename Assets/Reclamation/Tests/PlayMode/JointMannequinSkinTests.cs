using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Reclamation.Tests
{
    public sealed class JointMannequinSkinTests
    {
        private GameObject root;
        private JointMannequinLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Continuous body test"); lab = root.AddComponent<JointMannequinLab>(); lab.enabled = false;
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 30;
            lab.View.cullingMask = 1 << 30;
            yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup() { Object.Destroy(root); yield return null; }
        [UnityTest] public IEnumerator BodyIsOneClosedWeightedSurface()
        {
            Mesh mesh = lab.Skin.BodyMesh;
            Assert.That(mesh.vertexCount, Is.GreaterThan(1000));
            int[] triangles = mesh.triangles;
            var edges = new Dictionary<ulong, int>();
            int[] parent = Enumerable.Range(0, mesh.vertexCount).ToArray();
            int Find(int a) { while (parent[a] != a) { parent[a] = parent[parent[a]]; a = parent[a]; } return a; }
            for (int i = 0; i < triangles.Length; i += 3)
                for (int e = 0; e < 3; e++)
                {
                    int a = triangles[i + e], b = triangles[i + (e + 1) % 3];
                    ulong key = ((ulong)(uint)Math.Min(a, b) << 32) | (uint)Math.Max(a, b);
                    edges.TryGetValue(key, out int count); edges[key] = count + 1;
                    parent[Find(a)] = Find(b);
                }
            Assert.That(edges.Values.All(count => count == 2), Is.True, "Every surface edge must belong to exactly two triangles; no open joint seams.");
            Assert.That(Enumerable.Range(0, parent.Length).Select(Find).Distinct().Count(), Is.EqualTo(1), "Head, torso, limbs and digits must form one connected surface.");
            Assert.That(mesh.boneWeights.Length, Is.EqualTo(mesh.vertexCount));
            foreach (BoneWeight w in mesh.boneWeights)
            {
                Assert.That(w.weight0 + w.weight1 + w.weight2 + w.weight3, Is.EqualTo(1).Within(.0001f));
                foreach (int index in new[] { w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3 }) Assert.That(index, Is.InRange(0, lab.Skin.Surface.bones.Length - 1));
            }
            yield return null; LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SkinSwitchPreservesPoseAndDeformationStaysBounded()
        {
            JointMannequin rig = lab.Character;
            var baked = new Mesh();
            try
            {
                foreach (JointMannequin.Pose pose in Enum.GetValues(typeof(JointMannequin.Pose)))
                    for (int sample = 0; sample <= 8; sample++)
                    {
                        rig.Sample(pose, sample / 8f);
                        var before = rig.Joints.Values.Select(t => t.localRotation).ToArray();
                        lab.SetContinuous(false); Assert.That(lab.Skin.Surface.enabled, Is.False);
                        lab.SetContinuous(true); Assert.That(lab.Skin.Surface.enabled, Is.True);
                        int i = 0; foreach (Transform joint in rig.Joints.Values) Assert.That(Quaternion.Angle(before[i++], joint.localRotation), Is.LessThan(.001f));
                        lab.Skin.Surface.BakeMesh(baked);
                        foreach (Vector3 p in baked.vertices)
                        {
                            Assert.That(float.IsNaN(p.x) || float.IsInfinity(p.x), Is.False);
                            Assert.That(p.x, Is.InRange(-1.5f, 1.5f)); Assert.That(p.y, Is.InRange(-.08f, 2.5f)); Assert.That(p.z, Is.InRange(-1.5f, 1.5f));
                        }
                        Assert.That(rig.Weapon.parent, Is.EqualTo(rig.Joints["Wrist_R"]));
                    }
            }
            finally { Object.Destroy(baked); }
            yield return null; LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CaptureJointBendsAndOriginalComparison()
        {
            string directory = Environment.GetEnvironmentVariable("RECLAMATION_BODY_CAPTURES");
            Assert.That(lab.Skin.Surface.sharedMaterial.shader.isSupported, Is.True);
            Assert.That(lab.Character.Joints.Count, Is.EqualTo(43));
            if (string.IsNullOrEmpty(directory)) { yield return null; yield break; }
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "mesh-stats.txt"), lab.Skin.BodyMesh.vertexCount + " vertices\n" + lab.Skin.BodyMesh.triangles.Length / 3 + " triangles\n" + lab.Skin.GenerationSeconds.ToString("F2") + " generation seconds");
            foreach (JointMannequin.Pose pose in new[] { JointMannequin.Pose.Relaxed, JointMannequin.Pose.Guard, JointMannequin.Pose.HeavyAttack, JointMannequin.Pose.Block, JointMannequin.Pose.Dodge, JointMannequin.Pose.Run })
            {
                lab.Select(pose); lab.Character.Sample(pose, pose == JointMannequin.Pose.HeavyAttack ? .38f : .5f);
                foreach (bool skin in new[] { false, true })
                {
                    lab.SetContinuous(skin); yield return null;
                    string prefix = Path.Combine(directory, pose + (skin ? "-skin" : "-joints"));
                    Capture(prefix + "-angle.png", new Vector3(2.6f,1.4f,3), new Vector3(0,.92f,0));
                    Capture(prefix + "-upper.png", new Vector3(1.7f,1.4f,2), new Vector3(0,1.28f,0), 28);
                    Capture(prefix + "-side.png", new Vector3(3.8f,1,0), new Vector3(0,.92f,0));
                }
            }
            lab.Select(JointMannequin.Pose.Relaxed); lab.SetContinuous(true); yield return null;
            Capture(Path.Combine(directory,"feet.png"),new Vector3(1,.45f,1.3f),new Vector3(0,.18f,.03f),28);
            LogAssert.NoUnexpectedReceived();
        }
        private void Capture(string path, Vector3 position, Vector3 focus, float fov = 34)
        {
            Camera camera=lab.View; camera.rect=new Rect(0,0,1,1); camera.fieldOfView=fov; camera.transform.position=position; camera.transform.LookAt(focus);
            var target=new RenderTexture(900,900,24); var image=new Texture2D(900,900,TextureFormat.RGB24,false); var previous=RenderTexture.active;
            try { camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,900,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG()); }
            finally { camera.targetTexture=null;RenderTexture.active=previous;target.Release();Object.Destroy(target);Object.Destroy(image); }
        }
    }
}
