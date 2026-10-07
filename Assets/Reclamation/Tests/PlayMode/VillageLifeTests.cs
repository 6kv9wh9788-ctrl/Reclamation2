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
    public sealed class VillageLifeTests
    {
        private GameObject root;
        private BlightCombatLab lab;
        private string directory, path;
        [UnitySetUp] public IEnumerator Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "ReclamationLife-" + Guid.NewGuid().ToString("N")); path = Path.Combine(directory, "village.json");
            root = new GameObject("Village persistence test"); lab = root.AddComponent<BlightCombatLab>(); lab.InputEnabled = lab.AutomaticSimulation = false;
            yield return null; lab.SelectScenario(BlightScenario.Founding);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Object.Destroy(root); yield return null;
            if (Directory.Exists(directory)) { foreach (string file in Directory.GetFiles(directory)) File.Delete(file); Directory.Delete(directory); }
        }
        private void Step(float seconds) { for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++) lab.Simulate(1f / 60); }
        private void ClearRoad()
        {
            foreach (Transform child in lab.transform) if (child.name.Contains("raider")) lab.GetFighter(child).Receive(10000, false, false);
            lab.SidekickPlayerRoot.position = lab.CachePosition; Assert.That(lab.Interact(), Is.True);
            lab.SidekickPlayerRoot.position = lab.CampPosition + Vector3.back * 2; Assert.That(lab.Interact(), Is.True);
        }
        [UnityTest] public IEnumerator RoundTripKeepsCasualtiesResearchAndPartialBuilding()
        {
            ClearRoad(); lab.ResearchFounding(FoundingVillage.Technology.WorkCrews); Step(16); lab.ConstructFounding(false);
            lab.ResearchFounding(FoundingVillage.Technology.PatrolDoctrine); Step(6);
            lab.GetFighter(lab.FoundingCommander).Receive(10000, false, false);
            float build = lab.Village.BuildSeconds, research = lab.Village.ResearchSeconds;
            Assert.That(lab.SaveVillage(path), Is.True, lab.VillageSaveStatus);
            lab.ResetFight(); Assert.That(lab.LoadVillage(path), Is.True, lab.VillageSaveStatus);
            Assert.That(lab.Village.BuildSeconds, Is.EqualTo(build)); Assert.That(lab.Village.ResearchSeconds, Is.EqualTo(research));
            Assert.That(lab.GetFighter(lab.FoundingCommander).Alive, Is.False); Assert.That(lab.LivingEnemies, Is.Zero);
            Assert.That(lab.Village.Timber, Is.EqualTo(2)); Assert.That(lab.Interact(), Is.False);
            Step(16); Assert.That(lab.Village.Storehouse, Is.True); Assert.That(lab.Village.PatrolDoctrine, Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator CorruptionFallsBackAndInvalidLoadDoesNotResetPlay()
        {
            Assert.That(lab.SaveVillage(path), Is.True); ClearRoad(); Assert.That(lab.SaveVillage(path), Is.True);
            File.WriteAllText(path, "{broken"); Assert.That(lab.LoadVillage(path), Is.True);
            Assert.That(lab.Village.SuppliesDelivered, Is.False); StringAssert.Contains("backup", lab.VillageSaveStatus);
            var before = lab.PlayerFighter; File.WriteAllText(path + ".bak", "{}");
            Assert.That(lab.LoadVillage(path), Is.False); Assert.That(lab.PlayerFighter, Is.SameAs(before));
            yield return null;
        }
        [UnityTest] public IEnumerator SaveRejectsBusyOrDistantPlayerAndDoesNotCreateFile()
        {
            lab.SidekickPlayerRoot.position = Vector3.zero; Assert.That(lab.SaveVillage(path), Is.False); Assert.That(File.Exists(path), Is.False);
            lab.SidekickPlayerRoot.position = lab.CampPosition + Vector3.back * 2;
            Assert.That(lab.RequestPlayerAttack(false), Is.True); Assert.That(lab.SaveVillage(path), Is.False);
            Step(6); Assert.That(lab.SaveVillage(path), Is.True, lab.VillageSaveStatus); yield return null;
        }
        [UnityTest] public IEnumerator WorkersDeliverRealSuppliesAndPauseFreezesTheirWork()
        {
            ClearRoad(); lab.ResearchFounding(FoundingVillage.Technology.WorkCrews); Step(16); lab.ConstructFounding(false); Step(22);
            int food = lab.CivilianFood; lab.SetPaused(true); Step(30); Assert.That(lab.CivilianFood, Is.EqualTo(food));
            lab.SetPaused(false); Step(60); Assert.That(lab.CivilianFood, Is.GreaterThan(food));
            Assert.That(lab.SaveVillage(path), Is.True, lab.VillageSaveStatus); food = lab.CivilianFood;
            lab.ResetFight(); Assert.That(lab.LoadVillage(path), Is.True); Assert.That(lab.CivilianFood, Is.EqualTo(food)); yield return null;
        }
        [UnityTest] public IEnumerator MaraReassignsScoutDutyAndPreservesPlayerDirective()
        {
            var first = lab.transform.Find("Mara's soldier 1"); lab.GetFighter(first).Receive(80, false, false);
            lab.IssueFoundingDirective(FoundingDirective.ScoutRoad); Step(16);
            Assert.That(lab.ScoutReportReceived, Is.True); Assert.That(lab.CommanderDirective, Is.EqualTo(FoundingDirective.ScoutRoad));
            Assert.That(first.position.z, Is.LessThan(-14)); StringAssert.Contains("wounded", lab.MaraDutyStatus); yield return null;
        }
    }
}
