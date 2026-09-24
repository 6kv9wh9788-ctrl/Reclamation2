using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class BlightCompanyRulesTests
    {
        [Test] public void RiskPreferencesProduceDifferentDecisionsFromTheSameReport()
        {
            var cautious = new CompanyCommander("A", CommanderRisk.Cautious);
            var bold = new CompanyCommander("B", CommanderRisk.Bold);
            Assert.That(cautious.ShouldWithdraw(3, 2, 1), Is.True);
            Assert.That(bold.ShouldWithdraw(3, 2, 1), Is.False);
            Assert.That(bold.ShouldWithdraw(0, 2, .1f), Is.True, "Bold does not mean immune to casualties.");
        }
        [Test] public void PracticeRewardsDistinctDutiesNotRepeatedOrderClicks()
        {
            var commander = new CompanyCommander("A", CommanderRisk.Balanced);
            float interval = commander.AssessmentInterval;
            Assert.That(commander.Credit(CompanyOrder.Scout, CompanySite.West), Is.True);
            Assert.That(commander.Credit(CompanyOrder.Scout, CompanySite.West), Is.False);
            Assert.That(commander.Credit(CompanyOrder.Withdraw, CompanySite.West), Is.False);
            Assert.That(commander.Experience, Is.EqualTo(10));
            Assert.That(commander.Credit(CompanyOrder.Scout, CompanySite.East), Is.True);
            Assert.That(commander.Judgment, Is.EqualTo(1)); Assert.That(commander.AssessmentInterval, Is.LessThan(interval));
            Assert.That(commander.Risk, Is.EqualTo(CommanderRisk.Balanced));
            Assert.That(commander.Credit((CompanyOrder)99, CompanySite.Gate), Is.False);
        }
        [Test] public void IntelligenceRemainsALastSeenRecordUntilAnotherObservation()
        {
            var contact = new CompanyContact("Thrall"); contact.Observe(Vector3.forward * 3, 10, true);
            Assert.That(contact.Position, Is.EqualTo(Vector3.forward * 3)); Assert.That(contact.LastSeen, Is.EqualTo(10));
            Assert.That(contact.KnownAlive, Is.True);
            contact.Observe(Vector3.right * 5, 20, false);
            Assert.That(contact.KnownAlive, Is.False); Assert.That(contact.LastSeen, Is.EqualTo(20));
        }
    }
}
