using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class BlightCompanyPlayTests
    {
        private GameObject root; private BlightCombatLab lab;
        private readonly string[][] members = {
            new[] { "Mara - swordswoman", "Bren - spearman" },
            new[] { "Ivo - swordsman", "Nessa - spearfighter" },
            new[] { "Tess - swordswoman", "Oren - spearman" }
        };
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Company tests"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null; lab.SelectScenario(BlightScenario.Company);
        }
        [UnityTearDown] public IEnumerator Cleanup() { Object.Destroy(root); yield return null; }
        private Transform Actor(string name)
        {
            foreach (Transform child in root.transform) if (child.gameObject.activeSelf && child.name == name) return child;
            Assert.Fail("Missing " + name); return null;
        }
        private void GroupAt(int index, Vector3 point)
        {
            for (int i = 0; i < 2; i++) Actor(members[index][i]).position = point + Vector3.right * (i == 0 ? -.85f : .85f);
        }
        private void RecallPositions()
        {
            Actor("Company fighter").position = lab.CampPosition;
            for (int i = 0; i < 3; i++) GroupAt(i, lab.CampPosition + Vector3.right * ((i - 1) * 3));
        }
        private void RemoveEnemies()
        {
            foreach (Transform child in root.transform)
                if (child.gameObject.activeSelf && (child.name.Contains("thrall") || child.name.StartsWith("Fortress")))
                { var fighter = lab.GetFighter(child); if (fighter != null) fighter.Receive(10000, false, false); }
        }
        private void Advance(float seconds) { for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++) lab.Simulate(1f / 60); }

        [UnityTest] public IEnumerator PlatoonsReceiveIndependentOrdersAndStartWithoutEnemyIntel()
        {
            Assert.That(lab.CompanyPlatoonCount, Is.EqualTo(3)); Assert.That(lab.LivingCompanions, Is.EqualTo(6));
            Assert.That(lab.LivingEnemies, Is.EqualTo(5)); Assert.That(lab.KnownHostileCount, Is.Zero);
            Assert.That(lab.GiveCompanyOrder(0, CompanyOrder.Scout, CompanySite.West), Is.True);
            Assert.That(lab.GiveCompanyOrder(1, CompanyOrder.Escort, CompanySite.East), Is.True);
            Assert.That(lab.GetCompanyOrder(0), Is.EqualTo(CompanyOrder.Scout));
            Assert.That(lab.GetCompanyOrder(1), Is.EqualTo(CompanyOrder.Escort));
            Assert.That(lab.GetCompanyOrder(2), Is.EqualTo(CompanyOrder.Defend));
            lab.SetPaused(true); Assert.That(lab.GiveCompanyOrder(0, CompanyOrder.Assault, CompanySite.Fortress), Is.False);
            Assert.That(lab.PlanCompanyAssault(), Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator ScoutReportsOnlyObservedPositionsThenReturnsForCredit()
        {
            Assert.That(lab.GiveCompanyOrder(0, CompanyOrder.Scout, CompanySite.West), Is.True);
            GroupAt(0, BlightCombatLab.CompanyDestination(CompanySite.West)); lab.Simulate(.02f);
            Assert.That(lab.GetCompanyPhase(0), Is.EqualTo(CompanyPhase.Returning));
            CompanyContact known = lab.GetCompanyContact("West roaming thrall"); Assert.That(known, Is.Not.Null);
            Vector3 recorded = known.Position; float seen = known.LastSeen;
            Actor("West roaming thrall").position = new Vector3(14, 0, 19);
            GroupAt(0, lab.CampPosition + Vector3.left * 3); lab.Simulate(.02f);
            Assert.That(known.Position, Is.EqualTo(recorded)); Assert.That(known.LastSeen, Is.EqualTo(seen));
            Assert.That(lab.GetCompanyPhase(0), Is.EqualTo(CompanyPhase.Completed));
            Assert.That(lab.GetCommander(0).Experience, Is.EqualTo(10));
            lab.Simulate(.2f); Assert.That(lab.GetCommander(0).Experience, Is.EqualTo(10));
            yield return null;
        }

        [UnityTest] public IEnumerator AssaultWaitsForBothPlatoonsAndAChangedOrderCancelsThePlan()
        {
            RemoveEnemies(); Assert.That(lab.PlanCompanyAssault(), Is.True);
            GroupAt(0, new Vector3(0, 0, 3)); lab.Simulate(.02f);
            Assert.That(lab.CompanyPlanActive, Is.True); Assert.That(lab.GetCompanyPhase(0), Is.EqualTo(CompanyPhase.Staging));
            GroupAt(1, new Vector3(-12, 0, 10)); lab.Simulate(.02f);
            Assert.That(lab.CompanyPlanActive, Is.False); Assert.That(lab.GetCompanyPhase(0), Is.EqualTo(CompanyPhase.Fighting));
            Assert.That(lab.GetCompanyOrder(2), Is.EqualTo(CompanyOrder.Defend));
            Assert.That(lab.PlanCompanyAssault(), Is.True);
            Assert.That(lab.GiveCompanyOrder(1, CompanyOrder.Withdraw, CompanySite.Gate), Is.True);
            Assert.That(lab.CompanyPlanActive, Is.False); Assert.That(lab.GetCompanyOrder(0), Is.EqualTo(CompanyOrder.Defend));
            yield return null;
        }

        [UnityTest] public IEnumerator OvermatchedScoutsReturnWithoutClaimingMissionSuccess()
        {
            lab.GiveCompanyOrder(0, CompanyOrder.Scout, CompanySite.Fortress);
            GroupAt(0, new Vector3(0, 0, 3));
            Actor("West roaming thrall").position = new Vector3(-1, 0, 6);
            Actor("East roaming thrall").position = new Vector3(1, 0, 6);
            lab.GetFighter(Actor("West roaming thrall")).Interrupt(10);
            lab.GetFighter(Actor("East roaming thrall")).Interrupt(10);
            Advance(1);
            Assert.That(lab.GetCompanyPhase(0), Is.EqualTo(CompanyPhase.Returning));
            Assert.That(lab.GetCommander(0).Experience, Is.Zero);
            yield return null;
        }

        [UnityTest] public IEnumerator EscortCompletesAtDestinationAndDefendDutyEarnsOnlyOnce()
        {
            RemoveEnemies(); lab.GiveCompanyOrder(1, CompanyOrder.Escort, CompanySite.East);
            Actor("Company fighter").position = BlightCombatLab.CompanyDestination(CompanySite.East);
            GroupAt(1, Actor("Company fighter").position); lab.Simulate(.02f);
            Assert.That(lab.GetCompanyPhase(1), Is.EqualTo(CompanyPhase.Completed));
            Assert.That(lab.GetCommander(1).Experience, Is.EqualTo(10));
            lab.GiveCompanyOrder(2, CompanyOrder.Defend, CompanySite.Gate);
            GroupAt(2, BlightCombatLab.CompanyDestination(CompanySite.Gate)); Advance(31);
            Assert.That(lab.GetCommander(2).Experience, Is.EqualTo(10));
            Advance(1); Assert.That(lab.GetCommander(2).Experience, Is.EqualTo(10));
            yield return null;
        }

        [UnityTest] public IEnumerator DebriefRequiresReturnCountsCasualtiesAndResetClearsOperation()
        {
            Assert.That(lab.GiveCompanyOrder(0, CompanyOrder.Scout, CompanySite.West), Is.True);
            GroupAt(0, new Vector3(-11, 0, 0)); Assert.That(lab.ReviewCompanyOperation(), Is.False);
            lab.GetFighter(Actor("Oren - spearman")).Receive(10000, false, false);
            lab.RecallCompany(); RecallPositions(); lab.Simulate(.02f);
            Assert.That(lab.ReviewCompanyOperation(), Is.True); Assert.That(lab.Paused, Is.True);
            Assert.That(lab.CompanyReport, Does.Contain("5/6"));
            lab.ResetFight(); Assert.That(lab.CompanyReportOpen, Is.False); Assert.That(lab.LivingCompanions, Is.EqualTo(6));
            Assert.That(lab.KnownHostileCount, Is.Zero); Assert.That(lab.GetCommander(0).Experience, Is.Zero);
            lab.SelectScenario(BlightScenario.Gateway); yield return null;
            Assert.That(root.transform.Find("Company operation"), Is.Null); Assert.That(lab.CompanyPlatoonCount, Is.Zero);
            Assert.That(lab.LivingCompanions, Is.EqualTo(2)); LogAssert.NoUnexpectedReceived();
        }
    }
}
