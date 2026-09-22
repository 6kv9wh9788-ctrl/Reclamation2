using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class CharacterReviewPlayTests
    {
        private GameObject root;
        [UnityTearDown] public IEnumerator Cleanup() { if (root != null) Object.Destroy(root); yield return null; }

        [UnityTest] public IEnumerator BothHandsStayOnHandleThroughCuts()
        {
            foreach (bool athletic in new[] {false, true})
            {
                root = new GameObject("Grip review"); var rig = root.AddComponent<ModularHumanRig>(); rig.Build(athletic);
                rig.TwoHandGrip = true; rig.PlaybackSpeed = 0;
                foreach (bool axe in new[] {false, true}) foreach (string clip in new[] {"Idle", "LightAttack", "HeavyAttack", "Block"})
                {
                    rig.SetAppearance(true, axe, .5f); rig.Play(clip);
                    for (int i = 0; i <= 10; i++)
                    {
                        var animation = root.GetComponent<Animation>();
                        animation[clip].normalizedTime = i / 10f; animation.Sample();
                        // A full frame lets LateUpdate solve both arms; next frame observes its result.
                        yield return null; yield return null;
                        Assert.That(rig.GripError, Is.LessThan(.003f), athletic + " " + axe + " " + clip + " " + i);
                        Assert.That(rig.Bone("Forearm_L").localPosition.magnitude, Is.EqualTo(.38f).Within(.001f));
                    }
                }
                Object.Destroy(root); yield return null;
            }
        }

        [UnityTest] public IEnumerator ExpressionsPreserveIdentityAndResetToNeutral()
        {
            root = new GameObject("Expression review"); var rig = root.AddComponent<ModularHumanRig>(); rig.Build(false);
            rig.SetAppearance(true, false, .65f);
            foreach (string expression in new[] {"Determined", "Angry", "Hurt", "Shout", "Smile", "Neutral"})
            {
                rig.SetExpression(expression); yield return null;
                foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    Assert.That(renderer.GetBlendShapeWeight(0), Is.EqualTo(65).Within(.01f));
                    for (int i = 1; i < renderer.sharedMesh.blendShapeCount; i++)
                        Assert.That(renderer.GetBlendShapeWeight(i), Is.EqualTo(renderer.sharedMesh.GetBlendShapeName(i) == expression ? 100 : 0));
                }
            }
        }

        [UnityTest] public IEnumerator CrowdVariantWalksWithoutPhysicsBodies()
        {
            root = new GameObject("Crowd review"); var rig = root.AddComponent<ModularHumanRig>(); rig.Build(false, true);
            rig.SetAnimationPhase(.35f); yield return null;
            Assert.That(rig.PhysicsBodyCount, Is.Zero);
            Assert.That(root.GetComponentsInChildren<Collider>().Length, Is.Zero);
            Assert.That(rig.CurrentClip, Is.EqualTo("Walk"));
            Assert.That(root.GetComponent<Animation>().IsPlaying("Walk"), Is.True);
        }
    }
}
