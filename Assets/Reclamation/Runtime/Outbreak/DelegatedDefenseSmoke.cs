using System.Collections;
using System.IO;
using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        // Explicit standalone verification only; never reads or writes the normal player checkpoint.
        public IEnumerator RunDefenseSmoke(string directory)
        {
            Directory.CreateDirectory(directory); InputEnabled = AutomaticSimulation = false;
            bool failed = false;
            Application.LogCallback observe = (message, stack, type) => { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) failed = true; };
            Application.logMessageReceived += observe;
            yield return null; ScreenCapture.CaptureScreenshot(Path.Combine(directory,"quiet-hud.png")); yield return new WaitForSeconds(.3f);
            Village.RecoverSupplies(); Village.DeliverSupplies(); Village.StartResearch(FoundingVillage.Technology.PatrolDoctrine); Village.Tick(15);
            for (int f=0;f<90;f++) Simulate(1f/60);
            failed |= DefensePatrol != 2 || DefenseGarrison != 2 || DefenseReserve != 3;
            villagePanel = 1;
            yield return null; ScreenCapture.CaptureScreenshot(Path.Combine(directory,"commander-plan.png")); yield return new WaitForSeconds(.3f);
            foreach (var a in actors) if (a.root.name == "Road raider 1") a.root.position = new Vector3(0,0,-5);
            for (int f=0;f<90;f++) Simulate(1f/60);
            failed |= !DefenseAlert || DefensePatrol != 0;
            villageDebug = true;
            yield return null; ScreenCapture.CaptureScreenshot(Path.Combine(directory,"threat-diagnostics.png")); yield return new WaitForSeconds(.3f);
            foreach (var a in actors) if (a.enemy) a.fighter.Receive(10000,false,false);
            for (int f=0;f<720;f++) Simulate(1f/60);
            failed |= DefenseAlert || DefensePatrol != 2;
            string checkpoint = Path.Combine(directory,"test-defense.json");
            failed |= !SaveVillage(checkpoint);
            ResetFight(); failed |= !LoadVillage(checkpoint) || DefensePatrol != 2;
            villagePanel = 2;
            yield return null; ScreenCapture.CaptureScreenshot(Path.Combine(directory,"council.png")); yield return new WaitForSeconds(.3f);
            File.WriteAllText(Path.Combine(directory,"result.txt"), "Passed: " + !failed + "\nGarrison: " + DefenseGarrison + "\nPatrol: " + DefensePatrol + "\nReserve: " + DefenseReserve + "\n" + VillageSaveStatus);
            Application.logMessageReceived -= observe;
            Debug.Log(failed ? "DELEGATED_DEFENSE_SMOKE_FAILED" : "DELEGATED_DEFENSE_SMOKE_PASSED");
            Application.Quit(failed ? 1 : 0);
        }
    }
}
