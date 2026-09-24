using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;
namespace Reclamation.Tests
{
    public sealed class BlightCompanyFieldTests
    {
        private GameObject root; private BlightCombatLab lab;
        private readonly string[] soldiers = {"Mara - swordswoman", "Bren - spearman", "Ivo - swordsman", "Nessa - spearfighter", "Tess - swordswoman", "Oren - spearman"};
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Field tests"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = lab.AutomaticSimulation = lab.CombatAudioEnabled = false;
            yield return null; lab.SelectScenario(BlightScenario.Company);
        }
        [UnityTearDown] public IEnumerator Cleanup() { Object.Destroy(root); yield return null; }
        private Transform Actor(string name)
        {
            foreach (Transform t in root.transform) if (t.gameObject.activeSelf && t.name == name) return t;
            Assert.Fail("Missing " + name); return null;
        }
        private void Advance(float seconds) { for (int i = 0; i < Mathf.CeilToInt(seconds * 4); i++) lab.Simulate(.25f); }
        private void RemoveEnemiesExcept(string name)
        {
            foreach (Transform t in root.transform)
                if (t.gameObject.activeSelf && t.name != name && (t.name.Contains("thrall") || t.name.StartsWith("Fortress")))
                { var f = lab.GetFighter(t); if (f != null) f.Receive(10000, false, false); }
        }
        [UnityTest] public IEnumerator NoFailCommitsBothAttackersButRecallAlwaysWorks()
        {
            lab.OpenCompanyMap(); lab.QueueCompanyAssault(true); lab.CloseCompanyMap(true);
            Assert.That(lab.CompanyNoFail(0), Is.True); Assert.That(lab.CompanyNoFail(1), Is.True);
            Assert.That(lab.CompanyNoFail(2), Is.False);
            // Injured soldiers away from camp would trigger the ordinary withdrawal threshold.
            Actor(soldiers[0]).position = new Vector3(-1,0,0); Actor(soldiers[1]).position = new Vector3(1,0,0);
            lab.GetFighter(Actor(soldiers[0])).Receive(85, false, false);
            Advance(2);
            Assert.That(lab.GetCompanyPhase(0), Is.Not.EqualTo(CompanyPhase.Returning));
            lab.GetFighter(Actor(soldiers[2])).Receive(10000, false, false);
            lab.GetFighter(Actor(soldiers[3])).Receive(10000, false, false); lab.Simulate(.25f);
            Assert.That(lab.GetCompanyPhase(0), Is.EqualTo(CompanyPhase.Fighting));
            Assert.That(lab.CompanyNoFail(0), Is.True);
            lab.RecallCompany(); Assert.That(lab.GetCompanyOrder(0), Is.EqualTo(CompanyOrder.Withdraw));
            Assert.That(lab.CompanyNoFail(0), Is.False); Assert.That(lab.CompanyPlanActive, Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator MultiplePlatoonsStrikeFromReachInsteadOfOrbiting()
        {
            RemoveEnemiesExcept("West roaming thrall");
            Transform target = Actor("West roaming thrall"); target.position = Vector3.zero;
            lab.GetFighter(target).Interrupt(10);
            for (int i = 0; i < 3; i++) lab.GiveCompanyOrder(i, CompanyOrder.Assault, CompanySite.Fortress, true);
            for (int i = 0; i < soldiers.Length; i++) Actor(soldiers[i]).position = Quaternion.Euler(0, i * 60, 0) * Vector3.forward * 1.4f;
            // All six can wind up before the first impacts kill the isolated target.
            for (int i = 0; i < 46; i++) lab.Simulate(1f / 60);
            int attacking = 0;
            foreach (string name in soldiers) if (lab.GetFighter(Actor(name)).AttackSequence > 0) attacking++;
            Assert.That(attacking, Is.GreaterThanOrEqualTo(4), "Ready soldiers in valid reach should attack without waiting on a formation point.");
            yield return null;
        }
        [UnityTest] public IEnumerator SoldierClosesSmallReachGapAgainstHulk()
        {
            lab.SetExpandedCompany(true); RemoveEnemiesExcept("Fortress Hulk");
            Transform hulk=Actor("Fortress Hulk"), mara=Actor(soldiers[0]);
            hulk.position=new Vector3(0,0,32); lab.GetFighter(hulk).Interrupt(10);
            float reach=BlightEquipment.Weapon(BlightWeapon.Sword,false).Reach;
            mara.position=hulk.position-Vector3.forward*(reach+.1f);
            lab.GiveCompanyOrder(0,CompanyOrder.Assault,CompanySite.Fortress,true);
            for (int i=0;i<120;i++) lab.Simulate(1f/60);
            Assert.That(lab.GetFighter(mara).AttackSequence,Is.GreaterThan(0),"Small formation tolerances must not strand an attacker outside weapon reach.");
            yield return null;
        }
        [UnityTest] public IEnumerator SoldierCanDodgeBeyondOriginalArenaBounds()
        {
            lab.SetExpandedCompany(true); RemoveEnemiesExcept("Fortress Hulk");
            Transform hulk=Actor("Fortress Hulk"), mara=Actor(soldiers[0]);
            hulk.position=new Vector3(0,0,32); hulk.rotation=Quaternion.LookRotation(Vector3.back);
            mara.position=new Vector3(0,0,30.5f);
            lab.GiveCompanyOrder(0,CompanyOrder.Assault,CompanySite.Fortress,true);
            var strike=BlightEquipment.Enemy(BlightEnemy.Hulk,0); strike.Windup=.3f;
            Assert.That(lab.GetFighter(hulk).Attack(strike),Is.True);
            lab.Simulate(.15f); lab.Simulate(.02f);
            Assert.That(lab.GetFighter(mara).Action,Is.EqualTo(DuelAction.Dodge));
            yield return null;
        }
        [UnityTest] public IEnumerator ReserveReleaseAndCardsReflectActualOrders()
        {
            lab.PlanCompanyAssault();
            Assert.That(lab.CompanyReserveHeld,Is.True);
            Assert.That(lab.CompanyActivity(0),Is.EqualTo("Staging"));
            Assert.That(lab.ReleaseCompanyReserve(),Is.True);
            Assert.That(lab.CompanyReserveHeld,Is.False);
            Assert.That(lab.GetCompanyOrder(2),Is.EqualTo(CompanyOrder.Assault));
            lab.RecallCompany();
            Assert.That(lab.CompanyActivity(2),Is.EqualTo("Withdrawing"));
            lab.SetPaused(true); Assert.That(lab.ReleaseCompanyReserve(),Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator ExpandedAssaultTraversesBridgeAndReleasesBothStagingGroups()
        {
            lab.SetExpandedCompany(true);
            foreach (Transform t in root.transform)
            {
                if (!t.gameObject.activeSelf || t.name == "Company fighter" || System.Array.IndexOf(soldiers,t.name) >= 0) continue;
                var fighter=lab.GetFighter(t); if (fighter != null) fighter.Receive(10000,false,false);
            }
            Assert.That(lab.PlanCompanyAssault(true),Is.True);
            for (int step=0;step<1200 && lab.CompanyPlanActive;step++)
            {
                lab.Simulate(.1f);
                if (step%10==0) yield return null;
            }
            Assert.That(lab.CompanyPlanActive,Is.False,"Main and flank must traverse the bridge and reach their rally points within 120 simulated seconds.");
            Assert.That(lab.CompanyLiving(0),Is.EqualTo(2)); Assert.That(lab.CompanyLiving(1),Is.EqualTo(2));
            Assert.That(lab.GetCompanyPhase(0),Is.Not.EqualTo(CompanyPhase.Staging));
            Assert.That(lab.GetCompanyPhase(1),Is.Not.EqualTo(CompanyPhase.Staging));
        }
        [UnityTest] public IEnumerator ExpandedRegionHasBridgePatrolsAndResetRestoresBounds()
        {
            lab.SetExpandedCompany(true);
            Assert.That(lab.CampPosition.z, Is.EqualTo(-41.8f).Within(.001f));
            Assert.That(lab.CompanySitePosition(CompanySite.Fortress).z, Is.EqualTo(28.6f).Within(.001f));
            Assert.That(lab.TerrainPassable(new Vector3(0,0,-8),new Vector3(0,0,0),.43f), Is.True);
            Assert.That(lab.TerrainPassable(new Vector3(10,0,-8),new Vector3(10,0,0),.43f), Is.False);
            Assert.That(lab.TerrainSight(new Vector3(10,0,-8),new Vector3(10,0,0)), Is.True, "Water blocks walking, not sight.");
            Vector3 before = Actor("West roaming thrall").position; Advance(5);
            Assert.That(Vector3.Distance(before, Actor("West roaming thrall").position), Is.GreaterThan(.5f));
            Assert.That(lab.CompanySupplyReport, Does.StartWith("No report"));
            lab.SetPaused(true); before = Actor("West roaming thrall").position; Advance(2);
            Assert.That(Actor("West roaming thrall").position, Is.EqualTo(before));
            lab.SelectScenario(BlightScenario.Duel); Assert.That(lab.ExpandedCompany, Is.False);
            Assert.That(lab.TerrainPassable(new Vector3(10,0,-8),new Vector3(10,0,0),.43f), Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator DeliveriesFundGrowthAndDeadGatherersStopIt()
        {
            lab.SetExpandedCompany(true);
            // Speed up travel only. Real work timers, delivery processing and spawn logic run.
            for (int cycle = 0; cycle < 2; cycle++)
            {
                Actor("Blight gatherer west").position = new Vector3(-21,0,36);
                Actor("Blight gatherer east").position = new Vector3(23,0,28); Advance(12.25f);
                Actor("Blight gatherer west").position = new Vector3(-5,0,31);
                Actor("Blight gatherer east").position = new Vector3(5,0,31); Advance(12.25f);
            }
            Assert.That(lab.CompanyReinforcements, Is.EqualTo(1));
            lab.GetFighter(Actor("Blight gatherer west")).Receive(10000, false, false);
            lab.GetFighter(Actor("Blight gatherer east")).Receive(10000, false, false);
            Advance(30); Assert.That(lab.CompanyReinforcements, Is.EqualTo(1));
            lab.ResetFight(); Assert.That(lab.CompanyReinforcements, Is.Zero);
            Assert.That(lab.CompanySupplyReport, Does.StartWith("No report"));
            yield return null;
        }
    }
}
