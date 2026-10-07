using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class FoundingVillageTests
    {
        [Test] public void SuppliesRequireReturnAndCannotBeDuplicated()
        {
            var village = new FoundingVillage(); Assert.That(village.DeliverSupplies(), Is.False);
            Assert.That(village.RecoverSupplies(), Is.True); Assert.That(village.Timber, Is.Zero);
            Assert.That(village.RecoverSupplies(), Is.False); Assert.That(village.DeliverSupplies(), Is.True);
            Assert.That(village.DeliverSupplies(), Is.False); Assert.That(village.RecoverSupplies(), Is.False);
            Assert.That(village.Timber, Is.EqualTo(6)); Assert.That(village.Insight, Is.EqualTo(2));
        }
        [Test] public void ResearchUnlocksRolesAndTracksDevelopIndependently()
        {
            var v = new FoundingVillage(); Assert.That(v.StartResearch(FoundingVillage.Technology.WorkCrews), Is.False);
            v.RecoverSupplies(); v.DeliverSupplies();
            Assert.That(v.StartStorehouse(), Is.False); Assert.That(v.BuildWatchpost(), Is.False);
            Assert.That(v.StartResearch(FoundingVillage.Technology.PatrolDoctrine), Is.True);
            Assert.That(v.StartResearch(FoundingVillage.Technology.WorkCrews), Is.False);
            v.Tick(14); Assert.That(v.PatrolDoctrine, Is.False); v.Tick(1);
            Assert.That(v.BuildWatchpost(), Is.True); Assert.That(v.CivilianStage, Is.EqualTo("Village")); Assert.That(v.Storehouse, Is.False);
            Assert.That(v.StartResearch(FoundingVillage.Technology.PatrolDoctrine), Is.False);
            Assert.That(v.StartResearch(FoundingVillage.Technology.WorkCrews), Is.True); v.Tick(15);
            Assert.That(v.StartStorehouse(), Is.True); Assert.That(v.StartStorehouse(), Is.False);
            v.Tick(19); Assert.That(v.Storehouse, Is.False); v.Tick(1);
            Assert.That(v.Storehouse, Is.True); Assert.That(v.MilitaryStage, Is.EqualTo("Outpost")); Assert.That(v.Timber, Is.Zero);
        }
        [UnityTest] public IEnumerator CommanderDelegatesScoutsAndRequiresPlayerProximity()
        {
            var go = new GameObject("Founding test"); var lab = go.AddComponent<BlightCombatLab>();
            lab.InputEnabled = lab.AutomaticSimulation = false; yield return null;
            try
            {
                lab.SelectScenario(BlightScenario.Founding);
                Assert.That(lab.LivingCompanions, Is.EqualTo(8)); Assert.That(lab.IssueFoundingDirective(FoundingDirective.Peacekeep), Is.False);
                Assert.That(lab.IssueFoundingDirective(FoundingDirective.ScoutRoad), Is.True);
                Vector3 commander = lab.FoundingCommander.position;
                for (int i = 0; i < 900; i++) lab.Simulate(1f / 60);
                Assert.That(lab.ScoutReportReceived, Is.True); Assert.That(Vector3.Distance(commander, lab.FoundingCommander.position), Is.LessThan(3));
                lab.SidekickPlayerRoot.position = new Vector3(14, 0, 18);
                Assert.That(lab.IssueFoundingDirective(FoundingDirective.Accompany), Is.False);
                lab.SetPaused(true); Assert.That(lab.ResearchFounding(FoundingVillage.Technology.WorkCrews), Is.False);
                lab.ResetFight(); Assert.That(lab.Village.Insight, Is.Zero); Assert.That(lab.ScoutReportReceived, Is.False);
                Assert.That(lab.PlayerFighter.Alive, Is.True); Assert.That(lab.Paused, Is.False);
            }
            finally { Object.Destroy(go); }
            yield return null;
        }
        [UnityTest] public IEnumerator ResearchEnablesPatrolAndFallenCommanderRejectsOrders()
        {
            var go = new GameObject("Founding patrol test"); var lab = go.AddComponent<BlightCombatLab>();
            lab.InputEnabled = lab.AutomaticSimulation = false; yield return null;
            try
            {
                lab.SelectScenario(BlightScenario.Founding);
                lab.Village.RecoverSupplies(); lab.Village.DeliverSupplies();
                Assert.That(lab.ResearchFounding(FoundingVillage.Technology.PatrolDoctrine), Is.True);
                for (int i = 0; i < 920; i++) lab.Simulate(1f / 60);
                Assert.That(lab.IssueFoundingDirective(FoundingDirective.Peacekeep), Is.True);
                Transform patrol = lab.transform.Find("Mara's soldier 1"); Vector3 before = patrol.position;
                for (int i = 0; i < 300; i++) lab.Simulate(1f / 60);
                Assert.That(Vector3.Distance(before, patrol.position), Is.GreaterThan(1));
                lab.GetFighter(lab.FoundingCommander).Receive(10000, false, false);
                Assert.That(lab.IssueFoundingDirective(FoundingDirective.Accompany), Is.False);
            }
            finally { Object.Destroy(go); }
            yield return null;
        }
        [UnityTest] public IEnumerator SupplyInteractionRequiresPlayerAndClearRoad()
        {
            var go = new GameObject("Founding interaction test"); var lab = go.AddComponent<BlightCombatLab>();
            lab.InputEnabled = lab.AutomaticSimulation = false; yield return null;
            try
            {
                lab.SelectScenario(BlightScenario.Founding); Assert.That(lab.Interact(), Is.False);
                lab.SidekickPlayerRoot.position = lab.CachePosition; Assert.That(lab.Interact(), Is.False);
                foreach (Transform child in lab.transform)
                    if (child.name.Contains("raider")) lab.GetFighter(child).Receive(10000, false, false);
                Assert.That(lab.Interact(), Is.True); Assert.That(lab.Village.Timber, Is.Zero);
                lab.SidekickPlayerRoot.position = lab.CampPosition; Assert.That(lab.Interact(), Is.True);
                Assert.That(lab.ResearchFounding(FoundingVillage.Technology.WorkCrews), Is.True);
                lab.SetPaused(true); lab.Simulate(.25f); Assert.That(lab.Village.ResearchSeconds, Is.Zero);
                lab.SetPaused(false); for (int i = 0; i < 920; i++) lab.Simulate(1f / 60);
                Assert.That(lab.ConstructFounding(false), Is.True);
                for (int i = 0; i < 1220; i++) lab.Simulate(1f / 60);
                Assert.That(lab.Village.Storehouse, Is.True);
                Assert.That(lab.transform.Find("Founding village/Storehouse construction").localScale.y, Is.GreaterThan(3));
                lab.SelectScenario(BlightScenario.Duel); Assert.That(lab.Village, Is.Null); Assert.That(lab.LivingEnemies, Is.EqualTo(1));
            }
            finally { Object.Destroy(go); }
            yield return null;
        }
    }
}
