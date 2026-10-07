using System.Collections;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class DelegatedDefenseTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Delegated defense test"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = lab.AutomaticSimulation = false; yield return null; lab.SelectScenario(BlightScenario.Founding);
        }
        [UnityTearDown] public IEnumerator Cleanup() { Object.Destroy(root); yield return null; }
        private void Step(float seconds) { for (int i=0;i<Mathf.CeilToInt(seconds*60);i++) lab.Simulate(1f/60); }
        private void Doctrine() { lab.Village.RecoverSupplies(); lab.Village.DeliverSupplies(); lab.Village.StartResearch(FoundingVillage.Technology.PatrolDoctrine); lab.Village.Tick(15); }
        [UnityTest] public IEnumerator DefenseAllocatesAndRotatesWithoutSeparatePatrolOrder()
        {
            Assert.That(lab.DefenseGarrison, Is.EqualTo(2)); Assert.That(lab.DefensePatrol, Is.Zero);
            Doctrine(); Step(1); Assert.That(lab.DefensePatrol, Is.EqualTo(2)); Assert.That(lab.DefenseReserve, Is.EqualTo(3));
            var soldier = lab.transform.Find("Mara's soldier 1"); string duty = lab.DefenseDuty(soldier);
            Step(60); Assert.That(lab.CommanderDirective, Is.EqualTo(FoundingDirective.DefendVillage));
            Assert.That(lab.DefenseDuty(soldier), Is.Not.EqualTo(duty)); Assert.That(lab.DefenseGarrison, Is.EqualTo(2)); yield return null;
        }
        [UnityTest] public IEnumerator ObservedThreatRecallsPatrolAndScheduleResumes()
        {
            Doctrine(); Step(1); Assert.That(lab.DefenseAlert, Is.False);
            var raider = lab.transform.Find("Road raider 1"); raider.position = new Vector3(0,0,-5);
            Step(1); Assert.That(lab.DefenseAlert, Is.True); Assert.That(lab.DefensePatrol, Is.Zero); Assert.That(lab.DefenseGarrison, Is.EqualTo(2));
            foreach (Transform t in lab.transform) if (t.name.Contains("raider")) lab.GetFighter(t).Receive(10000,false,false);
            Step(10); Assert.That(lab.DefenseAlert, Is.False); Assert.That(lab.DefensePatrol, Is.EqualTo(2)); yield return null;
        }
        [UnityTest] public IEnumerator ReducedForcePrioritizesPostsAndRespectsOtherOrders()
        {
            Doctrine(); for (int i=1;i<=3;i++) lab.GetFighter(lab.transform.Find("Mara's soldier " + i)).Receive(80,false,false);
            Step(1); Assert.That(lab.DefenseStatus, Is.EqualTo("UNDERSTAFFED")); Assert.That(lab.DefensePatrol, Is.Zero); Assert.That(lab.DefenseGarrison, Is.EqualTo(2));
            Assert.That(lab.IssueFoundingDirective(FoundingDirective.Accompany), Is.True); Step(1);
            Assert.That(lab.DefenseStatus, Is.EqualTo("DETACHED MISSION")); Assert.That(lab.DefensePatrol, Is.Zero); yield return null;
        }
    }
}
