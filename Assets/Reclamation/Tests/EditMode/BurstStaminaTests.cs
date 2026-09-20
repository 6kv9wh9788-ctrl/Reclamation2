using NUnit.Framework;
using Reclamation.Outbreak;

namespace Reclamation.Tests
{
    public sealed class BurstStaminaTests
    {
        [Test] public void BurstExhaustsAfterItsDuration()
        {
            var stamina = new BurstStamina(4, 12);
            stamina.Advance(1, true);
            Assert.That(stamina.Bursting, Is.True);
            Assert.That(stamina.Fraction, Is.EqualTo(0.75f).Within(0.0001f));
            stamina.Advance(3, true);
            Assert.That(stamina.Bursting, Is.False);
            Assert.That(stamina.Fraction, Is.EqualTo(0));
        }

        [Test] public void ContinuousRequestMustWaitForFullRecovery()
        {
            var stamina = new BurstStamina(2, 10);
            stamina.Advance(2, true);
            stamina.Advance(5, true);
            Assert.That(stamina.Bursting, Is.False);
            Assert.That(stamina.Fraction, Is.EqualTo(0.5f));
            stamina.Advance(5, true);
            Assert.That(stamina.Bursting, Is.False);
            stamina.Advance(0.5f, true);
            Assert.That(stamina.Bursting, Is.True);
            Assert.That(stamina.Fraction, Is.EqualTo(0.75f));
        }

        [Test] public void InterruptedSprintRequiresRecoveryBeforeRestart()
        {
            var stamina = new BurstStamina(4, 12);
            stamina.Advance(2, true);
            stamina.Advance(1, false);
            stamina.Advance(1, true);
            Assert.That(stamina.Bursting, Is.False);
            Assert.That(stamina.Fraction, Is.EqualTo(2f / 3).Within(0.0001f));
        }

        [Test] public void ZeroOrInvalidElapsedTimeCannotDrainOrRefill()
        {
            var stamina = new BurstStamina(4, 12);
            stamina.Advance(1, true);
            stamina.Advance(0, false);
            stamina.Advance(-1, true);
            stamina.Advance(float.NaN, true);
            stamina.Advance(float.PositiveInfinity, true);
            Assert.That(stamina.Bursting, Is.True);
            Assert.That(stamina.Fraction, Is.EqualTo(0.75f));
        }

        [Test] public void EquivalentSimulationTimeProducesEquivalentDrain()
        {
            var regular = new BurstStamina(4, 12);
            var accelerated = new BurstStamina(4, 12);
            for (int i = 0; i < 12; i++) regular.Advance(0.125f, true);
            accelerated.Advance(0.125f * 12, true);
            Assert.That(accelerated.Fraction, Is.EqualTo(regular.Fraction).Within(0.0001f));
        }
    }
}
