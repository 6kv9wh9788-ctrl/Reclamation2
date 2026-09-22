using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class StylizedCharacterPlayTests
    {
        private GameObject root;
        [UnityTearDown] public IEnumerator Cleanup() { if (root != null) Object.Destroy(root); yield return null; }

        [UnityTest] public IEnumerator ArtFollowsSeveredArmAndResetRestoresIt()
        {
            root = new GameObject("Stylized combat test"); var lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null; lab.SelectScenario(BlightScenario.LimbDamage); lab.LimbPractice = true;
            Transform player = null, enemy = null;
            foreach (Transform child in root.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                if (child.name == "Company fighter") player = child;
                if (child.name == "Blighted Thrall") enemy = child;
            }
            Assert.That(player.GetComponent<StylizedCharacterArt>(), Is.Not.Null);
            Assert.That(enemy.GetComponent<StylizedCharacterArt>(), Is.Not.Null);
            Transform mesh = null;
            foreach (MeshFilter filter in enemy.GetComponentsInChildren<MeshFilter>())
                if (filter.name == "Forearm_R_blight") mesh = filter.transform;
            Assert.That(mesh, Is.Not.Null);
            player.position = Vector3.zero; enemy.position = Vector3.forward * 1.8f; lab.Simulate(.02f);
            for (int attack = 0; attack < 2; attack++)
            {
                Assert.That(lab.RequestPlayerAttack(true), Is.True);
                for (int i = 0; i < 120; i++) lab.Simulate(1f / 60f);
            }
            Assert.That(lab.GetLimbs(enemy).Missing(BodyRegion.RightArm), Is.True);
            Assert.That(mesh.IsChildOf(enemy), Is.False, "Visible forearm must detach with the damage region.");
            lab.ResetFight(); yield return null;
            Assert.That(root.GetComponentsInChildren<StylizedCharacterArt>().Length, Is.EqualTo(2));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator AnimationPreviewCreatesTwoCharactersAndTenClipsEach()
        {
            root = new GameObject("Motion test"); root.AddComponent<StylizedCharacterPreview>(); yield return null;
            Animation[] animations = root.GetComponentsInChildren<Animation>(); Assert.That(animations.Length, Is.EqualTo(2));
            foreach (Animation animation in animations)
            {
                Assert.That(animation.GetClipCount(), Is.EqualTo(10));
                Assert.That(animation.GetClip("HeavyAttack").length, Is.GreaterThan(1));
                Assert.That(animation.Play("Walk"), Is.True);
            }
            yield return null; LogAssert.NoUnexpectedReceived();
        }
    }
}
