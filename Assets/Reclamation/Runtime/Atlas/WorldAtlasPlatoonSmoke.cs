using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo
    {
        private IEnumerator PlatoonSmoke()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--output");
            string output = index >= 0 ? args[index + 1] : Path.Combine(Application.persistentDataPath, "PlatoonSmoke");
            Directory.CreateDirectory(output); bool failed = false;
            Application.LogCallback observe = (text, stack, type) => { if (type == LogType.Error || type == LogType.Exception) failed = true; };
            Application.logMessageReceived += observe; AutomaticClock = false;
            foreach (var config in new[] { (AtlasSize.Small, 4), (AtlasSize.Medium, 8), (AtlasSize.Medium, 12) })
            {
                LoadMap(config.Item1, config.Item2); VisitCommander(); SetHour(9); debug = false;
                string label = config.Item1 + "-" + config.Item2;
                failed |= ActiveCompany.Platoons.Count != 4 || ActiveGuardCount != 25;
                yield return null; ScreenCapture.CaptureScreenshot(Path.Combine(output, label + "-company.png")); yield return new WaitForSeconds(.3f);
                failed |= !BeginScoutEncounter();
                for (int i = 0; i < 1500 && ScoutPhase != ScoutEncounterPhase.Responding; i++) TickVillage(.2f);
                failed |= !ActiveCompany.ReportDelivered || ActiveCompany.Platoons.Count(p => p.Order == AtlasPlatoonOrder.SecureApproach) != 1;
                failed |= !VisitScoutContact(); debug = true;
                for (int i = 0; i < 1800 && ScoutSupport < 3 && ScoutPhase == ScoutEncounterPhase.Responding; i++) TickVillage(.2f);
                failed |= ScoutSupport < 3;
                debug = false;
                yield return null; ScreenCapture.CaptureScreenshot(Path.Combine(output, label + "-leaders.png")); yield return new WaitForSeconds(.3f);
                debug = true;
                yield return null; ScreenCapture.CaptureScreenshot(Path.Combine(output, label + "-delegated.png")); yield return new WaitForSeconds(.3f);
                for (int i = 0; i < 500 && scoutEncounter.Active; i++) TickVillage(.2f);
                failed |= ScoutPhase != ScoutEncounterPhase.Secured || !ActiveCompany.LatestReport.Contains("Approach secured");
                VisitCommander();
                yield return null; ScreenCapture.CaptureScreenshot(Path.Combine(output, label + "-report.png")); yield return new WaitForSeconds(.3f);
            }
            File.WriteAllText(Path.Combine(output, "result.txt"), "Passed: " + !failed + "\nFour platoon commanders; delayed report; one delegated reserve response; autonomous deterrence and completion report on all three presets.");
            Application.logMessageReceived -= observe; Application.Quit(failed ? 1 : 0);
        }
    }
}
