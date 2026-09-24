using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class BlightCompanionPlayTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Companion tests"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null; lab.SelectScenario(BlightScenario.Squad);
            foreach (string name in new[] {"Thrall 1", "Thrall 2", "Thrall 3"})
            { Actor(name).position = new Vector3(12, 0, 18); lab.GetFighter(Actor(name)).Interrupt(100); }
            Actor("Company fighter").position = Vector3.zero;
            Actor("Mara - swordswoman").position = new Vector3(-2, 0, -1);
            Actor("Bren - spearman").position = new Vector3(2, 0, -1);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { Object.Destroy(root); yield return null; }
        private Transform Actor(string name)
        {
            foreach (Transform child in root.transform)
                if (child.gameObject.activeSelf && child.name == name) return child;
            return null;
        }
        private void Advance(float seconds)
        { for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++) lab.Simulate(1f / 60f); }

        [UnityTest] public IEnumerator AssaultUsesSeparateWeaponRangesAndPauseFreezesMovement()
        {
            Transform enemy = Actor("Thrall 1"), sword = Actor("Mara - swordswoman"), spear = Actor("Bren - spearman");
            enemy.position = Vector3.forward * 4; lab.GiveOrder(SquadOrder.Assault); Advance(2);
            Assert.That(lab.GetCompanionTarget(sword), Is.EqualTo(enemy));
            Assert.That(lab.GetCompanionTarget(spear), Is.EqualTo(enemy));
            Assert.That(sword.position.x, Is.LessThan(-.3f));
            Assert.That(spear.position.x, Is.GreaterThan(.3f));
            Assert.That(Vector3.Distance(sword.position, spear.position), Is.GreaterThan(1));
            Assert.That(Vector3.Distance(spear.position, enemy.position), Is.GreaterThan(Vector3.Distance(sword.position, enemy.position) + .35f));
            Vector3 before = sword.position; float remaining = lab.GetFighter(sword).Remaining;
            lab.SetPaused(true); Advance(1); yield return null;
            Assert.That(sword.position, Is.EqualTo(before));
            Assert.That(lab.GetFighter(sword).Remaining, Is.EqualTo(remaining));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator FollowFiltersIneligibleEnemiesBeforePickingNearest()
        {
            Transform ally = Actor("Mara - swordswoman"), invalid = Actor("Thrall 1"), valid = Actor("Thrall 2");
            ally.position = Vector3.right * 2.5f;
            invalid.position = Vector3.right * 7.3f; valid.position = Vector3.left * 3;
            lab.GiveOrder(SquadOrder.Follow); lab.Simulate(.01f);
            Assert.That(lab.GetCompanionTarget(ally), Is.EqualTo(valid));
            yield return null;
        }

        [UnityTest] public IEnumerator HoldReturnsToItsOriginalAnchorAfterTargetLeavesLeash()
        {
            Transform ally = Actor("Mara - swordswoman"), enemy = Actor("Thrall 1");
            Vector3 anchor = ally.position; enemy.position = anchor + Vector3.forward * 2.8f;
            lab.GiveOrder(SquadOrder.Hold); Advance(.15f);
            Assert.That(lab.GetCompanionTarget(ally), Is.EqualTo(enemy));
            enemy.position = Vector3.forward * 12; Advance(2);
            Assert.That(lab.GetCompanionTarget(ally), Is.Null);
            Assert.That(Vector3.Distance(ally.position, anchor), Is.LessThan(.6f));
            yield return null;
        }

        [UnityTest] public IEnumerator WithdrawHonorsCommittedAttackThenDisengages()
        {
            Transform ally = Actor("Mara - swordswoman"); ally.position = Vector3.forward * 3;
            DuelFighter fighter = lab.GetFighter(ally); fighter.Attack(BlightEquipment.Weapon(BlightWeapon.Sword, true));
            lab.GiveOrder(SquadOrder.Withdraw); float remaining = fighter.Remaining;
            lab.Simulate(.02f);
            Assert.That(fighter.Action, Is.EqualTo(DuelAction.Windup));
            Assert.That(fighter.Remaining, Is.LessThan(remaining));
            Assert.That(ally.position, Is.EqualTo(Vector3.forward * 3));
            float distance = Vector3.Distance(ally.position, lab.CampPosition); Advance(3);
            Assert.That(Vector3.Distance(ally.position, lab.CampPosition), Is.LessThan(distance - 2));
            Assert.That(lab.GetCompanionTarget(ally), Is.Null);
            Assert.That(fighter.Action, Is.Not.EqualTo(DuelAction.Windup));
            yield return null;
        }

        [UnityTest] public IEnumerator CompanionGuardsTheEarliestThreatRatherThanTheFirstActor()
        {
            Transform ally = Actor("Mara - swordswoman"), slow = Actor("Thrall 1"), fast = Actor("Thrall 2");
            ally.position = Vector3.zero;
            Actor("Company fighter").position = Vector3.back * 8;
            Actor("Bren - spearman").position = new Vector3(5, 0, -5);
            slow.position = Vector3.left * 2; slow.rotation = Quaternion.LookRotation(Vector3.right);
            fast.position = Vector3.forward * 2; fast.rotation = Quaternion.LookRotation(Vector3.back);
            DuelFighter slowFighter = lab.GetFighter(slow), fastFighter = lab.GetFighter(fast);
            slowFighter.Advance(101); fastFighter.Advance(101);
            var heavy = BlightEquipment.Enemy(BlightEnemy.Thrall, 2);
            var light = BlightEquipment.Enemy(BlightEnemy.Thrall, 0);
            slowFighter.Attack(heavy); slowFighter.Advance(heavy.Windup - .6f);
            fastFighter.Attack(light); fastFighter.Advance(light.Windup - .12f);
            lab.Simulate(.01f);
            Assert.That(lab.GetFighter(ally).Blocking, Is.True);
            Assert.That(ally.forward.z, Is.GreaterThan(.95f));
            Assert.That(ally.position.magnitude, Is.LessThan(.01f));
            yield return null;
        }

        [UnityTest] public IEnumerator EmptyStaminaCannotProduceAFreeDodge()
        {
            Transform ally = Actor("Mara - swordswoman"), enemy = Actor("Thrall 1");
            ally.position = Vector3.zero; enemy.position = Vector3.forward * 2;
            Actor("Company fighter").position = Vector3.back * 8;
            Actor("Bren - spearman").position = new Vector3(5, 0, -5);
            enemy.rotation = Quaternion.LookRotation(Vector3.back);
            DuelFighter friend = lab.GetFighter(ally), foe = lab.GetFighter(enemy);
            for (int i = 0; i < 4; i++) { Assert.That(friend.Dodge(), Is.True); friend.Advance(.4f); }
            foe.Advance(101); var heavy = BlightEquipment.Enemy(BlightEnemy.Thrall, 2);
            foe.Attack(heavy); foe.Advance(heavy.Windup - .1f); lab.Simulate(.01f);
            Assert.That(friend.Action, Is.Not.EqualTo(DuelAction.Dodge));
            Assert.That(friend.Blocking, Is.True);
            Assert.That(friend.Stamina, Is.LessThan(1));
            lab.ResetFight(); yield return null;
            Assert.That(lab.Order, Is.EqualTo(SquadOrder.Follow));
            Assert.That(lab.GetCompanionTarget(Actor("Mara - swordswoman")), Is.Null);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
