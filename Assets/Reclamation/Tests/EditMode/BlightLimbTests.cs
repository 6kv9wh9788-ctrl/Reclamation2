using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class BlightLimbTests
    {
        [Test] public void ArmLossRemovesHeavyAndBothArmsForceBites()
        {
            var limbs = new BlightLimbs();
            Assert.That(limbs.Attack(2).Heavy, Is.True);
            Assert.That(limbs.Damage(BodyRegion.RightArm, 35, true), Is.False);
            Assert.That(limbs.Damage(BodyRegion.RightArm, 35, true), Is.True);
            Assert.That(limbs.Attack(2).Heavy, Is.False);
            limbs.Damage(BodyRegion.LeftArm, 50, true);
            Assert.That(limbs.BiteOnly, Is.True);
            Assert.That(limbs.Attack(2).Reach, Is.EqualTo(1.15f));
        }

        [Test] public void LegWoundSlowsThenSeverForcesCrawl()
        {
            var limbs = new BlightLimbs();
            limbs.Damage(BodyRegion.LeftLeg, 35, true);
            Assert.That(limbs.Limping, Is.True);
            Assert.That(limbs.SpeedMultiplier, Is.EqualTo(0.55f));
            limbs.Damage(BodyRegion.LeftLeg, 35, true);
            Assert.That(limbs.Crawling, Is.True);
            Assert.That(limbs.SpeedMultiplier, Is.EqualTo(0.24f));
            Assert.That(limbs.Attack(2).Heavy, Is.False);
        }

        [Test] public void BluntAndInvalidDamageCannotSever()
        {
            var limbs = new BlightLimbs();
            Assert.That(limbs.Damage(BodyRegion.RightArm, 100, false), Is.False);
            Assert.That(limbs.Integrity(BodyRegion.RightArm), Is.EqualTo(1));
            foreach (float amount in new[] { -1f, float.NaN, float.PositiveInfinity })
                Assert.That(limbs.Damage(BodyRegion.RightArm, amount, true), Is.False);
            Assert.That(limbs.Damage(BodyRegion.Torso, 1000, true), Is.False);
        }

        [Test] public void SweepCatchesBetweenFrameContactButRejectsDistantTarget()
        {
            Assert.That(BlightBladeSweep.Hits(new Vector3(-1, 0, 0), new Vector3(-1, 0, 2),
                new Vector3(1, 0, 0), new Vector3(1, 0, 2), Vector3.forward, Vector3.forward, 0.15f), Is.True);
            Assert.That(BlightBladeSweep.Hits(Vector3.zero, Vector3.forward, Vector3.right,
                Vector3.right + Vector3.forward, Vector3.forward * 4, Vector3.forward * 4, 0.15f), Is.False);
            Assert.That(BlightBladeSweep.SegmentDistanceSquared(Vector3.zero, Vector3.zero, Vector3.up), Is.EqualTo(1));
        }

        [Test] public void SweepAccountsForMovingTarget()
        {
            Assert.That(BlightBladeSweep.Hits(Vector3.zero, Vector3.forward * 2, Vector3.zero, Vector3.forward * 2,
                new Vector3(-1, 0, 1), new Vector3(1, 0, 1), 0.15f), Is.True);
        }

        [Test] public void SeverInterruptCancelsWindupWithoutRevivingDeadFighter()
        {
            var fighter = new DuelFighter(); fighter.Attack(true);
            fighter.Interrupt(0.8f);
            Assert.That(fighter.Action, Is.EqualTo(DuelAction.Stagger));
            Assert.That(fighter.Advance(1), Is.False, "An interrupted attack cannot impact.");
            fighter.Receive(1000, false, false); fighter.Interrupt(0.8f);
            Assert.That(fighter.Action, Is.EqualTo(DuelAction.Defeated));
        }
    }
}
