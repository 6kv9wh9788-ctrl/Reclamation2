using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class FoundingVillageDemo
    {
        private IEnumerator LifeSmoke(BlightCombatLab lab, string[] args)
        {
            string directory = Path.Combine(Application.persistentDataPath, "VillageLifeSmoke");
            int index = Array.IndexOf(args, "--output"); if (index >= 0 && index + 1 < args.Length) directory = args[index + 1];
            Directory.CreateDirectory(directory); string file = Path.Combine(directory, "test-village.json");
            Application.logMessageReceived += Observe;
            lab.InputEnabled = lab.AutomaticSimulation = false;
            // Isolated persistence smoke: prepare a cleared operation without touching the user's save.
            foreach (Transform child in lab.transform) if (child.name.Contains("raider")) lab.GetFighter(child).Receive(10000, false, false);
            lab.SidekickPlayerRoot.position = lab.CachePosition; if (!lab.Interact()) failed = true;
            lab.SidekickPlayerRoot.position = lab.CampPosition + Vector3.back * 2; if (!lab.Interact()) failed = true;
            lab.ResearchFounding(FoundingVillage.Technology.WorkCrews);
            for (int f = 0; f < 920; f++) lab.Simulate(1f / 60);
            lab.ConstructFounding(false); lab.ResearchFounding(FoundingVillage.Technology.PatrolDoctrine);
            for (int f = 0; f < 600; f++) lab.Simulate(1f / 60);
            yield return null; ScreenCapture.CaptureScreenshot(Path.Combine(directory, "construction.png")); yield return new WaitForSeconds(.2f);
            float progress = lab.Village.BuildSeconds;
            if (!lab.SaveVillage(file)) failed = true;
            lab.ResetFight();
            if (!lab.LoadVillage(file) || Mathf.Abs(lab.Village.BuildSeconds - progress) > .001f) failed = true;
            for (int f = 0; f < 800; f++) lab.Simulate(1f / 60);
            lab.ConstructFounding(true);
            for (int f = 0; f < 3600; f++) { lab.Simulate(1f / 60); if (f % 6 == 0) yield return null; }
            if (lab.CivilianFood < 1 || !lab.SaveVillage(file)) failed = true;
            int food = lab.CivilianFood; lab.ResetFight(); if (!lab.LoadVillage(file) || lab.CivilianFood != food) failed = true;
            yield return null; ScreenCapture.CaptureScreenshot(Path.Combine(directory, "village-resumed.png")); yield return new WaitForSeconds(.3f);
            File.WriteAllText(Path.Combine(directory, "result.txt"), "Passed: " + !failed + "\nRestored food: " + lab.CivilianFood + "\nStorehouse: " + lab.Village.Storehouse + "\nWatchpost: " + lab.Village.Watchpost + "\n" + lab.VillageSaveStatus);
            Debug.Log(failed ? "VILLAGE_LIFE_SMOKE_FAILED" : "VILLAGE_LIFE_SMOKE_PASSED");
            Application.logMessageReceived -= Observe; Application.Quit(failed ? 1 : 0);
        }
    }
}
