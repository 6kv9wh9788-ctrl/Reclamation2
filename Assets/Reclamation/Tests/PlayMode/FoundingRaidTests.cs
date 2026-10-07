using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Reclamation.Tests
{
    public sealed class FoundingRaidTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        private string path;
        [UnitySetUp] public IEnumerator Setup()
        {
            path = Path.Combine(Path.GetTempPath(), "Raid-" + Guid.NewGuid().ToString("N") + ".json");
            root = new GameObject("Founding raid test"); lab = root.AddComponent<BlightCombatLab>();
            lab.InputEnabled = lab.AutomaticSimulation = false; yield return null; lab.SelectScenario(BlightScenario.Founding);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Object.Destroy(root); yield return null;
            foreach (string p in new[] { path, path + ".bak" }) if (File.Exists(p)) File.Delete(p);
        }
        private void Step(float seconds) { for (int i=0;i<Mathf.CeilToInt(seconds*60);i++) lab.Simulate(1f/60); }
        private void Develop()
        {
            foreach (Transform t in lab.transform) if (t.name.Contains("raider")) lab.GetFighter(t).Receive(10000,false,false);
            var v=lab.Village; v.RecoverSupplies(); v.DeliverSupplies(); v.StartResearch(FoundingVillage.Technology.WorkCrews); v.Tick(15);
            v.StartStorehouse(); v.Tick(20); v.StartResearch(FoundingVillage.Technology.PatrolDoctrine); v.Tick(15); v.BuildWatchpost();
        }
        [UnityTest] public IEnumerator PrerequisitesPauseAndDuplicateStartAreRespected()
        {
            Assert.That(lab.BeginFoundingRaid(), Is.False); Develop(); Assert.That(lab.BeginFoundingRaid(), Is.True);
            Assert.That(lab.BeginFoundingRaid(), Is.False); Assert.That(lab.SaveVillage(path), Is.False);
            lab.SetPaused(true); Step(25); Assert.That(lab.FoundingRaidRemaining, Is.Zero);
            lab.SetPaused(false); Step(19); Assert.That(lab.FoundingRaidRemaining, Is.Zero); Assert.That(lab.DefenseAlert, Is.False);
            Step(2); Assert.That(lab.FoundingRaidRemaining, Is.EqualTo(3));
            lab.ResetFight(); Assert.That(lab.FoundingRaidActive, Is.False); Assert.That(lab.LivingEnemies, Is.EqualTo(3)); yield return null;
        }
        [UnityTest] public IEnumerator NaturalRaidTriggersDefenseAndCanBeRepelled()
        {
            Develop(); Assert.That(lab.BeginFoundingRaid(), Is.True); bool recalled=false;
            for (int i=0;i<7200 && !lab.FoundingRaidCompleted && lab.PlayerFighter.Alive;i++)
            {
                lab.Simulate(1f/60);
                if (lab.DefenseAlert && lab.DefensePatrol == 0 && lab.DefenseGarrison == 2) recalled=true;
                if (i % 600 == 0) yield return null;
            }
            Assert.That(recalled, Is.True, "Mara must observe and recall the patrol"); Assert.That(lab.FoundingRaidCompleted, Is.True, "Raid must be repelled through normal combat");
            Assert.That(lab.PlayerFighter.Alive, Is.True); Assert.That(lab.FoundingRaidRemaining, Is.Zero);
            Assert.That(lab.BeginFoundingRaid(), Is.False); yield return null;
        }
        [UnityTest] public IEnumerator LoadingDuringRaidAndSavingCompletionKeepRosterValid()
        {
            Develop(); Assert.That(lab.SaveVillage(path), Is.True); lab.BeginFoundingRaid(); Step(21);
            Assert.That(lab.LoadVillage(path), Is.True); Assert.That(lab.FoundingRaidActive, Is.False); Assert.That(lab.LivingEnemies, Is.Zero); yield return null;
            Assert.That(lab.BeginFoundingRaid(), Is.True); Step(21);
            foreach (Transform t in lab.transform) if (t.name.StartsWith("Village raider")) lab.GetFighter(t).Receive(10000,false,false);
            Step(10); Assert.That(lab.FoundingRaidCompleted, Is.True); Assert.That(lab.SaveVillage(path), Is.True, lab.VillageSaveStatus);
            lab.ResetFight(); Assert.That(lab.LoadVillage(path), Is.True); Assert.That(lab.FoundingRaidCompleted, Is.True);
            Assert.That(lab.BeginFoundingRaid(), Is.False); Assert.That(lab.LivingEnemies, Is.Zero); yield return null;
        }
    }
}
