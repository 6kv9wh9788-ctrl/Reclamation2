using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class BlightDuelTests
    {
        [Test] public void StrikeResolvesOnceAndCannotCancelRecovery()
        {
            var f = new DuelFighter(); Assert.That(f.Attack(false), Is.True);
            Assert.That(f.Advance(0.1f), Is.False); Assert.That(f.Advance(0.2f), Is.True);
            Assert.That(f.Attack(false), Is.False); Assert.That(f.Dodge(), Is.False);
            Assert.That(f.Advance(1), Is.False); Assert.That(f.CanAct, Is.True);
        }
        [Test] public void FrontalBlockCostsStaminaButRearHitDamages()
        {
            var f = new DuelFighter { Blocking = true };
            Assert.That(f.Receive(18, true, false), Is.EqualTo("Blocked"));
            Assert.That(f.Health, Is.EqualTo(100)); Assert.That(f.Stamina, Is.EqualTo(76));
            f.Receive(18, false, false); Assert.That(f.Health, Is.EqualTo(82));
        }
        [Test] public void GuardBreakAndDodgeHaveConsequences()
        {
            var f = new DuelFighter { Blocking = true };
            f.Receive(30, true, true); f.Receive(30, true, true);
            Assert.That(f.Receive(30, true, true), Is.EqualTo("Guard broken"));
            Assert.That(f.Action, Is.EqualTo(DuelAction.Stagger));
            var dodger = new DuelFighter(); Assert.That(dodger.Dodge(), Is.True);
            Assert.That(dodger.Receive(30, true, true), Is.EqualTo("Dodged"));
            dodger.Advance(0.4f); dodger.Receive(30, true, true);
            Assert.That(dodger.Health, Is.EqualTo(70));
        }
        [Test] public void HeavyInterruptsAndDeathPreventsActions()
        {
            var f = new DuelFighter(); f.Attack(true, true); f.Receive(32, true, true);
            Assert.That(f.Advance(1), Is.False);
            f.Receive(100, true, false);
            Assert.That(f.Attack(false), Is.False); Assert.That(f.Dodge(), Is.False);
        }
        [Test] public void ReachRequiresDistanceAndFacing()
        {
            Assert.That(DuelFighter.InReach(Vector3.zero, Vector3.forward, Vector3.forward * 2, 2.3f), Is.True);
            Assert.That(DuelFighter.InReach(Vector3.zero, Vector3.forward, Vector3.back, 2.3f), Is.False);
            Assert.That(DuelFighter.InReach(Vector3.zero, Vector3.forward, Vector3.forward * 4, 2.3f), Is.False);
        }

        [Test] public void SpearTradesSpeedAndStaminaForReachAndNarrowArc()
        {
            var sword = BlightEquipment.Weapon(BlightWeapon.Sword, false);
            var spear = BlightEquipment.Weapon(BlightWeapon.Spear, false);
            Assert.That(spear.Reach, Is.GreaterThan(sword.Reach));
            Assert.That(spear.Cost, Is.GreaterThan(sword.Cost));
            Assert.That(spear.Windup, Is.GreaterThan(sword.Windup));
            Vector3 diagonal = new Vector3(1.4f, 0, 1.4f);
            Assert.That(DuelFighter.InReach(Vector3.zero, Vector3.forward, diagonal, sword.Reach, sword.HalfAngle), Is.True);
            Assert.That(DuelFighter.InReach(Vector3.zero, Vector3.forward, diagonal, spear.Reach, spear.HalfAngle), Is.False);
        }

        [Test] public void HulkNeedsTwoHeavyHitsWithinOneWindupToInterrupt()
        {
            var hulk = new DuelFighter(260, true);
            Assert.That(hulk.Attack(BlightEquipment.Enemy(BlightEnemy.Hulk, 0)), Is.True);
            Assert.That(hulk.Receive(32, true, true), Is.EqualTo("Armoured windup"));
            Assert.That(hulk.Action, Is.EqualTo(DuelAction.Windup));
            hulk.Receive(32, true, true);
            Assert.That(hulk.Action, Is.EqualTo(DuelAction.Stagger));
            Assert.That(hulk.Advance(1), Is.False, "An interrupted sweep must not deal damage.");
            hulk.Attack(BlightEquipment.Enemy(BlightEnemy.Hulk, 1));
            Assert.That(hulk.Receive(32, true, true), Is.EqualTo("Armoured windup"));
        }

        [Test] public void SweepAndSmashDemandDifferentPositioning()
        {
            var sweep = BlightEquipment.Enemy(BlightEnemy.Hulk, 0);
            var smash = BlightEquipment.Enemy(BlightEnemy.Hulk, 1);
            Assert.That(sweep.MultipleTargets, Is.True);
            Assert.That(smash.MultipleTargets, Is.False);
            Assert.That(smash.Damage, Is.GreaterThan(sweep.Damage));
            Assert.That(DuelFighter.InReach(Vector3.zero, Vector3.forward, Vector3.right * 2,
                sweep.Reach, sweep.HalfAngle), Is.True);
            Assert.That(DuelFighter.InReach(Vector3.zero, Vector3.forward, Vector3.right * 2,
                smash.Reach, smash.HalfAngle), Is.False);
        }

        [Test] public void RecoveryDoesNotReviveOrCancelAnAttack()
        {
            var f = new DuelFighter(); f.Receive(40, true, false);
            f.Attack(false); f.Recover(10);
            Assert.That(f.Health, Is.EqualTo(60));
            f.Advance(1); f.Advance(1); f.Recover(1);
            Assert.That(f.Health, Is.EqualTo(78));
            f.Receive(100, true, false); f.Recover(100);
            Assert.That(f.Alive, Is.False);
        }

        [Test] public void InvalidDamageCannotPoisonHealth()
        {
            var f = new DuelFighter();
            f.Receive(float.NaN, true, true); f.Receive(float.PositiveInfinity, true, true);
            f.Receive(-10, true, true);
            Assert.That(f.Health, Is.EqualTo(100));
            Assert.That(f.CanAct, Is.True);
        }
    }
}
