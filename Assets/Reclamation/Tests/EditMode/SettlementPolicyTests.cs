using NUnit.Framework;
using Reclamation.Prototype;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class SettlementPolicyTests
    {
        private GameObject policyObject;

        [SetUp]
        public void Setup()
        {
            policyObject = new GameObject("policy");
        }

        [TearDown]
        public void Cleanup() => Object.DestroyImmediate(policyObject);

        [Test]
        public void FocusChangesJobRankingAndOnlyAdvancesRevisionOnChange()
        {
            var policy = policyObject.AddComponent<SettlementPolicy>();
            Assert.That(policy.Priority(SettlementJob.Food),
                Is.GreaterThan(policy.Priority(SettlementJob.Supplies)));
            policy.SetFocus(SettlementFocus.Supplies);
            Assert.That(policy.Priority(SettlementJob.Supplies),
                Is.GreaterThan(policy.Priority(SettlementJob.Food)));
            Assert.That(policy.Revision, Is.EqualTo(1));
            policy.SetFocus(SettlementFocus.Supplies);
            Assert.That(policy.Revision, Is.EqualTo(1));
        }

        [Test]
        public void RecoveryAndFoodFocusAuthorizeEarlierCareWithoutRemovingEmergencyThresholds()
        {
            var policy = policyObject.AddComponent<SettlementPolicy>();
            policy.SetFocus(SettlementFocus.Recovery);
            Assert.That(policy.RestThreshold, Is.GreaterThan(SurvivorNeeds.UrgentEnergy));
            Assert.That(policy.TreatmentThreshold, Is.LessThan(MedicalCondition.TreatmentThreshold));
            Assert.That(policy.MoraleThreshold, Is.GreaterThan(SurvivorMorale.RecoveryThreshold));
            policy.SetFocus(SettlementFocus.Food);
            Assert.That(policy.MealThreshold, Is.LessThan(SurvivorNeeds.UrgentHunger));
        }
    }
}
