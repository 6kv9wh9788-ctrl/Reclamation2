using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class BlightCommandMapTests
    {
        private GameObject root; private BlightCombatLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Command map tests"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = false; lab.AutomaticSimulation = false; lab.CombatAudioEnabled = false;
            yield return null; lab.SelectScenario(BlightScenario.Company);
        }
        [UnityTearDown] public IEnumerator Cleanup() { Object.Destroy(root); yield return null; }
        private Transform Actor(string name)
        {
            foreach (Transform t in root.transform) if (t.gameObject.activeSelf && t.name == name) return t;
            Assert.Fail("Missing " + name); return null;
        }
        [UnityTest] public IEnumerator FogSharesExplorationRemembersItAndResets()
        {
            lab.OpenCompanyMap();
            Assert.That(lab.CompanyTerrainVisible(15, 4), Is.True);
            Assert.That(lab.CompanyTerrainExplored(15, 39), Is.False);
            lab.CloseCompanyMap(false);
            Actor("Mara - swordswoman").position = new Vector3(-11, 0, 5);
            lab.OpenCompanyMap();
            Assert.That(lab.CompanyTerrainVisible(4, 27), Is.True);
            lab.CloseCompanyMap(false);
            Actor("Mara - swordswoman").position = lab.CampPosition;
            lab.OpenCompanyMap();
            Assert.That(lab.CompanyTerrainVisible(4, 27), Is.False);
            Assert.That(lab.CompanyTerrainExplored(4, 27), Is.True);
            lab.ResetFight(); lab.OpenCompanyMap();
            Assert.That(lab.CompanyTerrainExplored(4, 27), Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator PlanningFreezesSimulationAndDiscardPreservesOrders()
        {
            lab.GiveCompanyOrder(0, CompanyOrder.Scout, CompanySite.West);
            Assert.That(lab.OpenCompanyMap(), Is.True);
            Vector3 before = Actor("Mara - swordswoman").position;
            string briefing = lab.CompanyMapBriefing();
            lab.Simulate(.2f);
            Assert.That(Actor("Mara - swordswoman").position, Is.EqualTo(before));
            Assert.That(lab.CompanyMapBriefing(), Is.EqualTo(briefing));
            lab.QueueCompanyOrder(0, CompanyOrder.Assault, CompanySite.Fortress);
            Assert.That(lab.GetCompanyOrder(0), Is.EqualTo(CompanyOrder.Scout));
            Assert.That(lab.CloseCompanyMap(false), Is.True);
            Assert.That(lab.GetCompanyOrder(0), Is.EqualTo(CompanyOrder.Scout));
            Assert.That(lab.Paused, Is.False); Assert.That(lab.PendingCompanyOrders, Is.Zero);
            yield return null;
        }
        [UnityTest] public IEnumerator ApplyReplacesQueuedOrdersAndRestoresExistingPause()
        {
            lab.SetPaused(true); lab.OpenCompanyMap();
            lab.QueueCompanyOrder(0, CompanyOrder.Scout, CompanySite.West);
            lab.QueueCompanyOrder(0, CompanyOrder.Assault, CompanySite.Fortress);
            lab.QueueCompanyOrder(1, CompanyOrder.Escort, CompanySite.East);
            Assert.That(lab.PendingCompanyOrders, Is.EqualTo(2));
            Assert.That(lab.CloseCompanyMap(true), Is.True);
            Assert.That(lab.Paused, Is.True);
            Assert.That(lab.GetCompanyOrder(0), Is.EqualTo(CompanyOrder.Assault));
            Assert.That(lab.GetCompanyOrder(1), Is.EqualTo(CompanyOrder.Escort));
            Assert.That(lab.GetCompanyOrder(2), Is.EqualTo(CompanyOrder.Defend));
            yield return null;
        }
        [UnityTest] public IEnumerator TemplatesReplaceIndividualOrdersAndRecallReplacesTemplate()
        {
            lab.OpenCompanyMap(); lab.QueueCompanyOrder(0, CompanyOrder.Scout, CompanySite.West);
            Assert.That(lab.QueueCompanyAssault(), Is.True); Assert.That(lab.PendingCompanyOrders, Is.Zero);
            Assert.That(lab.CompanyPlanActive, Is.False);
            lab.QueueCompanyOrder(1, CompanyOrder.Escort, CompanySite.East);
            Assert.That(lab.CompanyAssaultQueued, Is.False);
            lab.QueueCompanyAssault(); lab.QueueCompanyRecall();
            Assert.That(lab.CompanyAssaultQueued, Is.False); Assert.That(lab.PendingCompanyOrders, Is.EqualTo(3));
            lab.CloseCompanyMap(true);
            for (int i = 0; i < 3; i++) Assert.That(lab.GetCompanyOrder(i), Is.EqualTo(CompanyOrder.Withdraw));
            lab.OpenCompanyMap(); lab.QueueCompanyAssault(); lab.CloseCompanyMap(true);
            Assert.That(lab.CompanyPlanActive, Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator InvalidBatchDoesNotPartiallyApplyAndResetClearsMap()
        {
            lab.OpenCompanyMap(); lab.QueueCompanyOrder(0, CompanyOrder.Scout, CompanySite.West);
            lab.QueueCompanyOrder(1, CompanyOrder.Scout, CompanySite.East);
            lab.GetFighter(Actor("Ivo - swordsman")).Receive(10000, false, false);
            lab.GetFighter(Actor("Nessa - spearfighter")).Receive(10000, false, false);
            Assert.That(lab.CloseCompanyMap(true), Is.False);
            Assert.That(lab.GetCompanyOrder(0), Is.EqualTo(CompanyOrder.Defend));
            Assert.That(lab.CompanyMapOpen, Is.True); Assert.That(lab.Paused, Is.True);
            lab.ResetFight(); Assert.That(lab.CompanyMapOpen, Is.False); Assert.That(lab.PendingCompanyOrders, Is.Zero);
            lab.SelectScenario(BlightScenario.Duel); Assert.That(lab.OpenCompanyMap(), Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator BriefingUsesLastObservationAndDoesNotRevealUnseenHulk()
        {
            Assert.That(lab.MappedContactCount, Is.Zero);
            Assert.That(lab.CompanyMapBriefing(), Does.Not.Contain("Fortress Hulk"));
            Actor("Mara - swordswoman").position = new Vector3(-11, 0, 5);
            lab.Simulate(.02f);
            CompanyContact contact = lab.GetCompanyContact("West roaming thrall"); Assert.That(contact, Is.Not.Null);
            Vector3 known = contact.Position;
            Actor("Mara - swordswoman").position = lab.CampPosition;
            Actor("West roaming thrall").position = new Vector3(14, 0, 19);
            lab.Simulate(.02f); lab.OpenCompanyMap();
            Assert.That(contact.Position, Is.EqualTo(known));
            Assert.That(lab.CompanyMapBriefing(), Does.Contain("West roaming thrall"));
            Assert.That(lab.CompanyMapBriefing(), Does.Not.Contain("Fortress Hulk"));
            yield return null;
        }
    }
}
