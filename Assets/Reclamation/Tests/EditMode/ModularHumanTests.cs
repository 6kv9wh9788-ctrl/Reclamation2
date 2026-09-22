using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class ModularHumanTests
    {
        [Test] public void BodyBuildsShareSkeletonAndAnimationContract()
        {
            ModularHumanData broad = ModularHumanRig.Load(false), athletic = ModularHumanRig.Load(true);
            Assert.That(broad, Is.Not.Null); Assert.That(athletic, Is.Not.Null);
            Assert.That(broad.bones.Length, Is.EqualTo(57)); Assert.That(athletic.bones.Length, Is.EqualTo(57));
            for (int i = 0; i < broad.bones.Length; i++)
            {
                Assert.That(athletic.bones[i].name, Is.EqualTo(broad.bones[i].name));
                Assert.That(athletic.bones[i].parent, Is.EqualTo(broad.bones[i].parent));
            }
            for (int i = 0; i < broad.clips.Length; i++)
            {
                Assert.That(athletic.clips[i].name, Is.EqualTo(broad.clips[i].name));
                Assert.That(athletic.clips[i].duration, Is.EqualTo(broad.clips[i].duration));
            }
        }
        [TestCase(false)] [TestCase(true)] public void SkinWeightsAndFaceShapesAreValid(bool athletic)
        {
            ModularHumanData data = ModularHumanRig.Load(athletic); int blended = 0, facial = 0;
            foreach (ModularHumanPart part in data.parts)
            {
                Assert.That(part.weights.Length, Is.EqualTo(part.positions.Length / 3 * 4));
                Assert.That(part.faceDelta.Length, Is.EqualTo(part.positions.Length));
                for (int i = 0; i < part.weights.Length; i += 4)
                {
                    float sum = 0;
                    for (int j = 0; j < 4; j++)
                    { Assert.That(part.joints[i + j], Is.InRange(0, 56)); Assert.That(part.weights[i + j], Is.InRange(0f, 1f)); sum += part.weights[i + j]; }
                    Assert.That(sum, Is.EqualTo(1).Within(.0001f));
                    if (part.weights[i + 1] > .01f && part.weights[i] > .01f) blended++;
                }
                foreach (float value in part.faceDelta) if (Mathf.Abs(value) > .001f) facial++;
            }
            Assert.That(blended, Is.GreaterThan(50), "The rig must contain genuine blended skin weights.");
            Assert.That(facial, Is.GreaterThan(50));
        }
        [Test] public void GripSolverReachesTargetWithoutStretchingBones()
        {
            var upper = new GameObject("upper").transform; var lower = new GameObject("lower").transform; var hand = new GameObject("hand").transform;
            try
            {
                lower.SetParent(upper, false); lower.localPosition = Vector3.down * .38f;
                hand.SetParent(lower, false); hand.localPosition = Vector3.down * .33f;
                Vector3 target = new Vector3(.2f, -.5f, .1f);
                ModularHumanRig.SolveGrip(upper, lower, hand, target, Vector3.forward);
                Assert.That(Vector3.Distance(hand.position, target), Is.LessThan(.002f));
                Assert.That(lower.localPosition.magnitude, Is.EqualTo(.38f).Within(.001f));
                Assert.That(hand.localPosition.magnitude, Is.EqualTo(.33f).Within(.001f));
            }
            finally { Object.DestroyImmediate(upper.gameObject); }
        }
    }
}
