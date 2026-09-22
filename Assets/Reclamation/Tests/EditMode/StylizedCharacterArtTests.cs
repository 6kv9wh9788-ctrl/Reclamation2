using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class StylizedCharacterArtTests
    {
        [TestCase(false)] [TestCase(true)]
        public void GeneratedResourcesHaveValidMeshesBonesAndClips(bool thrall)
        {
            CharacterArtData data = StylizedCharacterArt.Load(thrall);
            Assert.That(data, Is.Not.Null); Assert.That(data.bones.Length, Is.EqualTo(10));
            Assert.That(data.clips.Length, Is.EqualTo(10));
            int triangles = 0;
            foreach (CharacterArtPart part in data.parts)
            {
                Assert.That(part.bone, Is.InRange(0, 9));
                Assert.That(part.normals.Length, Is.EqualTo(part.positions.Length));
                foreach (int index in part.triangles) Assert.That(index, Is.InRange(0, part.positions.Length / 3 - 1));
                foreach (float position in part.positions) Assert.That(float.IsNaN(position) || float.IsInfinity(position), Is.False);
                triangles += part.triangles.Length / 3;
            }
            Assert.That(triangles, Is.InRange(3000, 5000));
            foreach (CharacterArtClip clip in data.clips)
            {
                Assert.That(clip.frames[0].time, Is.EqualTo(0));
                Assert.That(clip.frames[clip.frames.Length - 1].time, Is.EqualTo(clip.duration).Within(.001f));
                foreach (CharacterArtFrame frame in clip.frames)
                {
                    Assert.That(frame.rotations.Length, Is.EqualTo(40)); Assert.That(frame.offsets.Length, Is.EqualTo(30));
                    for (int i = 0; i < 10; i++)
                    {
                        float length = 0; for (int q = 0; q < 4; q++) length += frame.rotations[i * 4 + q] * frame.rotations[i * 4 + q];
                        Assert.That(length, Is.EqualTo(1).Within(.001f));
                    }
                }
            }
        }
    }
}
