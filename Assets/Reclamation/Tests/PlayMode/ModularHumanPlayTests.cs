using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class ModularHumanPlayTests
    {
        private GameObject root;
        [UnityTearDown] public IEnumerator Cleanup() { if (root != null) Object.Destroy(root); yield return null; }
        [UnityTest] public IEnumerator GearAndFaceSwitchingKeepsRigAndClips()
        {
            root = new GameObject("Modular test"); var rig = root.AddComponent<ModularHumanRig>(); rig.Build(false);
            Transform hand = rig.Bone("Hand_R");
            foreach (bool heavy in new[] { false, true }) foreach (bool axe in new[] { false, true })
            {
                rig.SetAppearance(heavy, axe, 1); rig.SetHair(true);
                Assert.That(rig.Bone("Hand_R"), Is.SameAs(hand)); Assert.That(rig.Play("HeavyAttack"), Is.True);
                foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (renderer.name == "TorsoCloth") Assert.That(renderer.gameObject.activeSelf, Is.EqualTo(!heavy));
                    if (renderer.name.StartsWith("Heavy_")) Assert.That(renderer.gameObject.activeSelf, Is.EqualTo(heavy));
                    if (renderer.name.StartsWith("Axe_")) Assert.That(renderer.gameObject.activeSelf, Is.EqualTo(axe));
                    Assert.That(renderer.GetBlendShapeWeight(0), Is.EqualTo(100));
                }
                yield return null;
            }
            Assert.That(rig.BoneCount, Is.EqualTo(57)); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RagdollRecoveryRestoresKinematicAnimationAndAttachments()
        {
            root = new GameObject("Ragdoll test"); var rig = root.AddComponent<ModularHumanRig>(); rig.Build(true);
            Assert.That(rig.PhysicsBodyCount, Is.EqualTo(11));
            for (int round = 0; round < 2; round++)
            {
                rig.SetRagdoll(true); Assert.That(rig.Play("Walk"), Is.False);
                for (int step = 0; step < 10; step++) yield return new WaitForFixedUpdate();
                foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>())
                { Assert.That(body.isKinematic, Is.False); Assert.That(float.IsNaN(body.position.y), Is.False); }
                rig.SetRagdoll(false);
                foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>()) Assert.That(body.isKinematic, Is.True);
                Assert.That(rig.Play("Walk"), Is.True);
                Assert.That(rig.Bone("WeaponSocket_R").parent, Is.EqualTo(rig.Bone("Hand_R")));
                Assert.That(rig.Bone("Pelvis").localPosition.y, Is.EqualTo(.85f).Within(.01f));
            }
            yield return null; LogAssert.NoUnexpectedReceived();
        }
    }
}
