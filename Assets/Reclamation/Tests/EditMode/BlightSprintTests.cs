using NUnit.Framework;
using Reclamation.Blight;

namespace Reclamation.Tests
{
    public sealed class BlightSprintTests
    {
        [Test] public void SprintDrainsSharedStaminaAndDelaysRecovery()
        {
            var f = new DuelFighter();
            for (int i = 0; i < 60; i++) { Assert.That(f.TrySprint(1f / 60), Is.True); f.Advance(1f / 60); }
            Assert.That(f.Stamina, Is.EqualTo(82).Within(.01f));
            float before = f.Stamina; f.Advance(.3f); f.Recover(.1f);
            Assert.That(f.Stamina, Is.EqualTo(before));
            f.Advance(.5f); f.Advance(.1f); Assert.That(f.Stamina, Is.GreaterThan(before));
        }
        [Test] public void SprintCannotCancelCommittedActionsOrRunThroughGuard()
        {
            var f = new DuelFighter(); f.Blocking = true;
            Assert.That(f.TrySprint(.1f), Is.False); Assert.That(f.Stamina, Is.EqualTo(100));
            f.Blocking = false; f.Attack(true);
            Assert.That(f.TrySprint(.1f), Is.False); Assert.That(f.Action, Is.EqualTo(DuelAction.Windup));
            f = new DuelFighter(); f.Dodge(); Assert.That(f.TrySprint(.1f), Is.False);
        }
        [Test] public void ExhaustionRespectsDodgeAndAttackCosts()
        {
            var f = new DuelFighter(); Assert.That(f.TrySprint(5), Is.True);
            Assert.That(f.Stamina, Is.EqualTo(10)); Assert.That(f.Dodge(), Is.False);
            Assert.That(f.Attack(false), Is.False); Assert.That(f.TrySprint(1), Is.False);
            Assert.That(f.Stamina, Is.EqualTo(10));
        }
        [TestCase(0f)] [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidSprintStepDoesNotChangeStamina(float seconds)
        { var f = new DuelFighter(); Assert.That(f.TrySprint(seconds), Is.False); Assert.That(f.Stamina, Is.EqualTo(100)); }
    }
}
