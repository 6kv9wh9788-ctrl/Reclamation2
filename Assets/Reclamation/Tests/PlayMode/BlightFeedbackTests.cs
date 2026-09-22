using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class BlightFeedbackTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        private Transform enemy;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Feedback test"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null;
            Arrange();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { Object.Destroy(root); yield return null; }
        private Transform Actor(string name)
        {
            foreach (Transform child in root.transform)
                if (child.name == name && child.gameObject.activeSelf) return child;
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

        [UnityTest] public IEnumerator FeedbackDoesNotChangeHealthStaminaOrSeverOutcome()
        {
            lab.CombatFeedbackEnabled = false; Heavy(); Heavy();
            float health = lab.GetFighter(enemy).Health, stamina = lab.PlayerFighter.Stamina;
            Assert.That(lab.GetLimbs(enemy).Missing(BodyRegion.RightArm), Is.True);
            Arrange(); lab.CombatFeedbackEnabled = true; Heavy(); Heavy();
            Assert.That(lab.GetFighter(enemy).Health, Is.EqualTo(health).Within(0.001f));
            Assert.That(lab.PlayerFighter.Stamina, Is.EqualTo(stamina).Within(0.001f));
            Assert.That(lab.GetLimbs(enemy).Missing(BodyRegion.RightArm), Is.True);
            Assert.That(lab.FeedbackEventCount, Is.EqualTo(2));
            Assert.That(lab.LastCombatImpact, Is.EqualTo(CombatImpact.Sever));
            Advance(10); yield return null; Advance(1);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator MissPauseAndResetDoNotLeaveStaleImpactEvents()
        {
            enemy.position = Vector3.forward * 6; Heavy();
            Assert.That(lab.FeedbackEventCount, Is.Zero);
            enemy.position = Vector3.forward * 1.8f; Heavy();
            Assert.That(lab.FeedbackEventCount, Is.EqualTo(1));
            lab.SetPaused(true); Advance(1);
            Assert.That(lab.FeedbackEventCount, Is.EqualTo(1));
            lab.ResetFight();
            Assert.That(lab.FeedbackEventCount, Is.Zero);
            Assert.That(lab.LastCombatImpact, Is.EqualTo(CombatImpact.None));
            Assert.That(lab.CombatAudioEnabled, Is.False, "Reset preserves mute preference.");
            yield return null;
        }

        [TestCase("Blocked", DuelAction.Ready, DuelAction.Ready, CombatImpact.Block)]
        [TestCase("Dodged", DuelAction.Dodge, DuelAction.Dodge, CombatImpact.Dodge)]
        [TestCase("Armoured windup", DuelAction.Windup, DuelAction.Windup, CombatImpact.Armoured)]
        [TestCase("Hit", DuelAction.Windup, DuelAction.Stagger, CombatImpact.Interrupt)]
        [TestCase("Hit", DuelAction.Ready, DuelAction.Stagger, CombatImpact.Hit)]
        [TestCase("Guard broken", DuelAction.Ready, DuelAction.Stagger, CombatImpact.GuardBreak)]
        public void OutcomeCuesDistinguishInterruptedAttacksFromArmourAndBlocks(string result,
            DuelAction before, DuelAction after, CombatImpact expected)
            => Assert.That(BlightCombatLab.ClassifyImpact(result, before, after), Is.EqualTo(expected));
    }
}
