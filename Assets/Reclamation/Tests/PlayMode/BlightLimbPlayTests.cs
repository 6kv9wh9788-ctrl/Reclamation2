using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class BlightLimbPlayTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        private Transform enemy;

        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Limb integration test"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false;
            yield return null;
            Arrange();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { Object.Destroy(root); yield return null; }

        private Transform Actor(string name)
        {
            foreach (Transform child in root.transform)
                if (child.gameObject.activeSelf && child.name == name) return child;
            return null;
        }
        private void Arrange()
        {
            lab.SelectScenario(BlightScenario.LimbDamage); lab.LimbPractice = true;
            Actor("Company fighter").position = Vector3.zero;
            enemy = Actor("Blighted Thrall"); enemy.position = Vector3.forward * 1.8f;
            lab.Simulate(0.02f);
        }
        private void Advance(float seconds)
        { for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++) lab.Simulate(1f / 60f); }
        private void Heavy()
        { Assert.That(lab.RequestPlayerAttack(true), Is.True); Advance(2); }

        [UnityTest] public IEnumerator SwordContactsOnceThenSeversArmAndResetRestoresIt()
        {
            Heavy();
            Assert.That(lab.LastLimbHit, Is.EqualTo(BodyRegion.RightArm));
            Assert.That(lab.GetFighter(enemy).Health, Is.EqualTo(150.4f).Within(0.01f), "One contact per swing.");
            Heavy();
            Assert.That(lab.GetLimbs(enemy).Missing(BodyRegion.RightArm), Is.True);
            Assert.That(lab.GetLimbs(enemy).CanHeavy, Is.False);
            Advance(10); yield return null;
            Advance(1); // Missing joints must not be read after detached debris is destroyed.
            lab.ResetFight(); enemy = Actor("Blighted Thrall");
            Assert.That(lab.GetLimbs(enemy).Missing(BodyRegion.RightArm), Is.False);
            Assert.That(lab.GetFighter(enemy).Health, Is.EqualTo(160));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator LowSwingsWoundThenCrawlAndLiveEnemyStillAttacks()
        {
            Assert.That(lab.SetLimbAim(SwingHeight.Legs), Is.True);
            Heavy(); Assert.That(lab.GetLimbs(enemy).Limping, Is.True);
            Heavy(); Assert.That(lab.GetLimbs(enemy).Crawling, Is.True);
            float before = lab.PlayerFighter.Health;
            lab.LimbPractice = false; Advance(7);
            Assert.That(lab.PlayerFighter.Health, Is.LessThan(before), "A crawling enemy remains dangerous at close range.");
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator GoreToggleChangesPresentationButNotOutcome()
        {
            lab.LowGore = true; Heavy(); Heavy();
            float health = lab.GetFighter(enemy).Health;
            lab.SetPaused(true); lab.LowGore = false;
            Assert.That(lab.GetLimbs(enemy).Missing(BodyRegion.RightArm), Is.True);
            Arrange(); lab.LowGore = false; Heavy(); Heavy();
            Assert.That(lab.GetFighter(enemy).Health, Is.EqualTo(health));
            Assert.That(lab.GetLimbs(enemy).Missing(BodyRegion.RightArm), Is.True);
            yield return null;
        }

        [UnityTest] public IEnumerator OutOfReachAndPausedSwingsDoNotDamageAndAimCannotChangeMidAttack()
        {
            enemy.position = Vector3.forward * 6;
            Heavy(); Assert.That(lab.GetFighter(enemy).Health, Is.EqualTo(160));
            enemy.position = Vector3.forward * 1.8f;
            Assert.That(lab.RequestPlayerAttack(true), Is.True);
            Assert.That(lab.SetLimbAim(SwingHeight.Legs), Is.False);
            lab.SetPaused(true); Advance(2);
            Assert.That(lab.GetFighter(enemy).Health, Is.EqualTo(160));
            Assert.That(lab.RequestPlayerAttack(false), Is.False);
            lab.SetPaused(false); Advance(2);
            Assert.That(lab.GetFighter(enemy).Health, Is.LessThan(160));
            yield return null;
        }
    }
}
