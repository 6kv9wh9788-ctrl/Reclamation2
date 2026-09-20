using NUnit.Framework;
using Reclamation.Outbreak;

namespace Reclamation.Tests
{
    public sealed class InfectionRulesTests
    {
        [Test] public void HealthyPersonProgressesThroughAllInfectionStages()
        {
            var infection = new InfectionTimeline();
            Assert.That(infection.Expose(100, 20, 10), Is.True);
            Assert.That(infection.State, Is.EqualTo(InfectionState.Exposed));
            infection.Advance(119.9);
            Assert.That(infection.State, Is.EqualTo(InfectionState.Exposed));
            infection.Advance(120);
            Assert.That(infection.State, Is.EqualTo(InfectionState.Symptomatic));
            infection.Advance(130);
            Assert.That(infection.State, Is.EqualTo(InfectionState.Turned));
        }

        [Test] public void LargeTimeJumpTransitionsDirectlyToTurned()
        {
            var infection = new InfectionTimeline();
            infection.Expose(10, 5, 5);
            infection.Advance(100);
            Assert.That(infection.State, Is.EqualTo(InfectionState.Turned));
        }

        [Test] public void DuplicateExposureCannotResetIncubation()
        {
            var infection = new InfectionTimeline();
            infection.Expose(100, 20, 10);
            Assert.That(infection.Expose(110, 200, 200), Is.False);
            Assert.That(infection.SymptomMinute, Is.EqualTo(120));
        }

        [Test] public void InvalidExposureInputsAreRejected()
        {
            var infection = new InfectionTimeline();
            Assert.That(infection.Expose(0, 0, 10), Is.False);
            Assert.That(infection.Expose(double.NaN, 10, 10), Is.False);
            Assert.That(infection.Expose(0, 10, double.PositiveInfinity), Is.False);
            Assert.That(infection.State, Is.EqualTo(InfectionState.Healthy));
        }

        [Test] public void OnlyTurnedPersonCanBeNeutralized()
        {
            var infection = new InfectionTimeline();
            Assert.That(infection.Neutralize(), Is.False);
            infection.Expose(0, 1, 1);
            Assert.That(infection.Neutralize(), Is.False);
            infection.Advance(2);
            Assert.That(infection.Neutralize(), Is.True);
            Assert.That(infection.State, Is.EqualTo(InfectionState.Neutralized));
            Assert.That(infection.Neutralize(), Is.False);
        }

        [Test] public void TimeCannotMoveStateBackward()
        {
            var infection = new InfectionTimeline();
            infection.Expose(100, 20, 10);
            infection.Advance(130);
            infection.Advance(0);
            Assert.That(infection.State, Is.EqualTo(InfectionState.Turned));
        }
    }
}
