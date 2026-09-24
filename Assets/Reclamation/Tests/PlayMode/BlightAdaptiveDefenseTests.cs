using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class BlightAdaptiveDefenseTests
    {
        private GameObject root; private BlightCombatLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Adaptive defense tests"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null; lab.SelectScenario(BlightScenario.Horde);
            yield return null; QuietEnemies();
        }
        [UnityTearDown] public IEnumerator Cleanup() { Object.Destroy(root); yield return null; }
        private Transform Actor(string name)
        {
            foreach (Transform child in root.transform) if (child.gameObject.activeSelf && child.name == name) return child;
            Assert.Fail("Missing " + name); return null;
        }
        private Transform Mara => Actor("Mara - swordswoman");
        private Transform Bren => Actor("Bren - spearman");
        private void QuietEnemies()
        {
            int i = 0;
            foreach (Transform child in root.transform)
                if (child.gameObject.activeSelf && child.name.StartsWith("Formation"))
                { child.position = new Vector3(-10 + (i++ % 6) * 4, 0, 16); lab.GetFighter(child).Interrupt(60); }
        }
        private void Advance(float seconds) { for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++) lab.Simulate(1f / 60); }
        private void Pressure()
        {
            Actor("Company fighter").position = new Vector3(13, 0, -18);
            for (int i = 1; i <= 5; i++) Actor("Formation thrall " + i).position = new Vector3(-2 + (i - 1), 0, .5f);
        }
        [UnityTest] public IEnumerator ExhaustedSoldierCreatesSpaceAndHealthyBuddyCovers()
        {
            Vector3 start = Mara.position;
            Assert.That(lab.GetFighter(Mara).TrySprint(4.2f), Is.True); // 24.4 stamina; no artificial health recovery.
            lab.Simulate(.02f);
            Assert.That(lab.CompanionRecovering(Mara), Is.True);
            Assert.That(lab.GetCompanionIntent(Bren), Is.EqualTo("Covering Mara"));
            Advance(.8f); Assert.That(Mara.position.z, Is.LessThan(start.z - .5f));
            Assert.That(lab.GetFighter(Mara).Health, Is.EqualTo(100));
            Advance(3); Assert.That(lab.CompanionRecovering(Mara), Is.False);
            Assert.That(Vector3.Distance(Mara.position, start), Is.LessThan(2.9f));
            yield return null;
        }
        [UnityTest] public IEnumerator SustainedLocalPressureTriggersFallbackButDistantEnemiesDoNot()
        {
            Advance(2); Assert.That(lab.FallingBack, Is.False);
            Pressure(); Advance(.5f); Assert.That(lab.FallingBack, Is.False, "No instant retreat.");
            Advance(1.3f); Assert.That(lab.FallingBack, Is.True);
            Assert.That(lab.Order, Is.EqualTo(SquadOrder.Withdraw));
            Advance(5); Assert.That(lab.FallingBack, Is.False); Assert.That(lab.Order, Is.EqualTo(SquadOrder.Hold));
            Assert.That(Vector3.Distance(Mara.position, lab.FallbackPosition), Is.LessThan(3.5f));
            yield return null;
        }
        [UnityTest] public IEnumerator EnemiesOccludedByWallDoNotTriggerFallback()
        {
            lab.SelectScenario(BlightScenario.Gateway); QuietEnemies();
            Transform hero = Actor("Company fighter"); hero.position = new Vector3(5.7f, 0, -2.5f); hero.rotation = Quaternion.identity;
            lab.GiveOrder(SquadOrder.Hold); Mara.position = new Vector3(5, 0, -2.5f); Bren.position = new Vector3(6.4f, 0, -2.5f);
            hero.position = new Vector3(13, 0, -18);
            for (int i = 1; i <= 5; i++) Actor("Formation thrall " + i).position = new Vector3(4 + i * .5f, 0, 1);
            Advance(2); Assert.That(lab.FallingBack, Is.False); Assert.That(lab.Order, Is.EqualTo(SquadOrder.Hold));
            yield return null;
        }

        [UnityTest] public IEnumerator StandFastDisablesAutonomousRetreatButExplicitFallbackStillWorks()
        {
            lab.SetHoldAtAllCosts(true); Pressure(); Advance(2);
            Assert.That(lab.HoldAtAllCosts, Is.True); Assert.That(lab.FallingBack, Is.False);
            Assert.That(lab.Order, Is.EqualTo(SquadOrder.Hold));
            lab.GiveOrder(SquadOrder.Withdraw); Assert.That(lab.HoldAtAllCosts, Is.False); Assert.That(lab.FallingBack, Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator WoundedSoldierDoesNotHealOrAbandonDefendedArea()
        {
            Vector3 start = Mara.position; lab.GetFighter(Mara).Receive(70, false, false);
            Advance(4);
            Assert.That(lab.GetFighter(Mara).Health, Is.EqualTo(30)); Assert.That(lab.CompanionRecovering(Mara), Is.True);
            Assert.That(Mara.position.z, Is.LessThan(start.z - 1));
            Assert.That(Vector3.Distance(Mara.position, start), Is.LessThan(2.9f));
            Assert.That(lab.GetCompanionIntent(Bren), Is.EqualTo("Covering Mara"));
            yield return null;
        }
        [UnityTest] public IEnumerator DefenseCannotCancelCommittedAttacksAndPauseFreezesDecisions()
        {
            var fighter = lab.GetFighter(Mara); var attack = BlightEquipment.Weapon(BlightWeapon.Sword, true); attack.Windup = 3;
            Assert.That(fighter.Attack(attack), Is.True); fighter.Receive(70, false, false);
            Vector3 start = Mara.position; Advance(.5f);
            Assert.That(fighter.Action, Is.EqualTo(DuelAction.Windup)); Assert.That(Mara.position, Is.EqualTo(start));
            lab.SetPaused(true); float remaining = fighter.Remaining; Advance(2);
            Assert.That(fighter.Remaining, Is.EqualTo(remaining)); Assert.That(Mara.position, Is.EqualTo(start));
            lab.SetHoldAtAllCosts(true); Assert.That(lab.HoldAtAllCosts, Is.False);
            lab.ResetFight(); Assert.That(lab.HoldAtAllCosts, Is.False); Assert.That(lab.CompanionRecovering(Actor("Mara - swordswoman")), Is.False);
            // Let deferred destruction from ResetFight complete before building another scene-sized set of models.
            yield return null;
            lab.SelectScenario(BlightScenario.Duel); yield return null; LogAssert.NoUnexpectedReceived();
        }
    }
}
