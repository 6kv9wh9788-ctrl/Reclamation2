using NUnit.Framework;
using Reclamation.Blight;

namespace Reclamation.Tests
{
    public sealed class BlightDefenseTests
    {
        [Test] public void RecoveryHasSeparateEntryAndExitThresholds()
        {
            Assert.That(BlightDefenseRules.Recovering(false, 1, 29), Is.True);
            Assert.That(BlightDefenseRules.Recovering(true, 1, 50), Is.True);
            Assert.That(BlightDefenseRules.Recovering(false, 1, 50), Is.False);
            Assert.That(BlightDefenseRules.Recovering(true, 1, 65), Is.False);
            Assert.That(BlightDefenseRules.Recovering(false, .3f, 100), Is.True);
        }
        [Test] public void WithdrawalRequiresSignificantPressureRelativeToSupport()
        {
            Assert.That(BlightDefenseRules.Overwhelmed(2, .5f), Is.False);
            Assert.That(BlightDefenseRules.Overwhelmed(4, 3), Is.False);
            Assert.That(BlightDefenseRules.Overwhelmed(4, 2), Is.True);
            Assert.That(BlightDefenseRules.Overwhelmed(6, 3), Is.True);
        }
    }
}
