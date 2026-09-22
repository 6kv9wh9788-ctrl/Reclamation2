using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class ForwardCrowdPlayTests
    {
        private GameObject first, second;
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (first != null) Object.Destroy(first);
            if (second != null) Object.Destroy(second);
            yield return null;
        }

        [UnityTest] public IEnumerator TwoHandCutTravelsForwardAndDown()
        {
            foreach (bool athletic in new[] {false, true})
            {
                first = new GameObject("Forward cut test"); var rig = first.AddComponent<ModularHumanRig>(); rig.Build(athletic);
                rig.TwoHandGrip = true; rig.PlaybackSpeed = 0;
                foreach (bool axe in new[] {false, true}) foreach (string clip in new[] {"LightAttack", "HeavyAttack"})
                {
                    rig.SetAppearance(true, axe, athletic ? 1 : 0); rig.Play(clip);
                    Animation animation = first.GetComponent<Animation>();
                    animation[clip].speed = 0; animation[clip].normalizedTime = .3f; animation.Sample();
                    yield return null; yield return null;
                    Vector3 head = axe ? new Vector3(.37f, 0, 1.03f) : new Vector3(0, 0, 1.4f);
                    Vector3 windup = rig.Bone("WeaponSocket_R").TransformPoint(head);
                    animation[clip].normalizedTime = .6f; animation.Sample();
                    yield return null; yield return null;
                    Vector3 impact = rig.Bone("WeaponSocket_R").TransformPoint(head);
                    Assert.That(impact.z - windup.z, Is.GreaterThan(.65f), clip + " must reach forward");
                    Assert.That(windup.y - impact.y, Is.GreaterThan(.65f), clip + " must cut downward");
                    Assert.That(Mathf.Abs(impact.x), Is.LessThan(.12f), "Cut stays in front of the hero");
                    Assert.That(rig.GripError, Is.LessThan(.003f));
                }
                Object.Destroy(first); yield return null;
            }
        }

        [UnityTest] public IEnumerator MatchingCrowdActorsShareOneMeshAndKeepIndependentAnimation()
        {
            first = new GameObject("Crowd A"); second = new GameObject("Crowd B");
            var a = first.AddComponent<ModularHumanRig>(); var b = second.AddComponent<ModularHumanRig>();
            a.Build(false, true); b.Build(false, true);
            a.SetAppearance(false, true, .2f); b.SetAppearance(false, true, .8f);
            a.SetAnimationPhase(.1f); b.SetAnimationPhase(.6f);
            yield return null;
            var ar = first.GetComponentsInChildren<SkinnedMeshRenderer>();
            var br = second.GetComponentsInChildren<SkinnedMeshRenderer>();
            Assert.That(ar.Length, Is.EqualTo(1)); Assert.That(br.Length, Is.EqualTo(1));
            Assert.That(ar[0].sharedMesh, Is.SameAs(br[0].sharedMesh));
            Assert.That(ar[0].sharedMaterial, Is.SameAs(br[0].sharedMaterial));
            Assert.That(ar[0].GetBlendShapeWeight(0), Is.EqualTo(20).Within(.01f));
            Assert.That(br[0].GetBlendShapeWeight(0), Is.EqualTo(80).Within(.01f));
            Assert.That(first.GetComponent<Animation>()["Walk"].normalizedTime,
                Is.Not.EqualTo(second.GetComponent<Animation>()["Walk"].normalizedTime).Within(.1f));
            int expectedIndices = 0;
            foreach (ModularHumanPart part in ModularHumanRig.Load(false).parts)
                if (part.slot == "Body" || part.slot == "HairShort" || part.slot == "Axe") expectedIndices += part.triangles.Length;
            Assert.That(ar[0].sharedMesh.triangles.Length, Is.EqualTo(expectedIndices));
            Mesh shared = br[0].sharedMesh;
            Object.Destroy(first); yield return null; yield return null;
            Assert.That(shared != null, Is.True, "Removing one actor must not destroy another actor's shared mesh");
            Assert.That(br[0].sharedMesh, Is.SameAs(shared));
            b.SetAppearance(true, false, .8f); yield return null;
            Assert.That(br[0].sharedMesh, Is.Not.SameAs(shared));
            Assert.That(second.GetComponent<Animation>().IsPlaying("Walk"), Is.True);
        }
    }
}
