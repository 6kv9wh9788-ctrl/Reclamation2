using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class FoundingVillageDemo : MonoBehaviour
    {
        public Shader[] requiredShaders;
        private bool failed;
        private IEnumerator Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "--founding-smoke") < 0)
            {
                yield return null;
                var current = GetComponent<BlightCombatLab>();
                if (Array.IndexOf(args, "--campaign-smoke") >= 0)
                {
                    Application.runInBackground=true;int outputArg=Array.IndexOf(args,"--output");
                    yield return current.RunDefenseCampaignSmoke(args[outputArg+1],Array.IndexOf(args,"--intercept")>=0);yield break;
                }
                if (Array.IndexOf(args, "--campaign-resume-smoke") >= 0)
                {
                    bool ok=current.LoadVillage()&&current.Campaign!=null&&current.Campaign.completed==2&&current.Campaign.playerPerk!=0&&current.Campaign.commanderPerk!=0;
                    Debug.Log(ok?"CAMPAIGN_RESUME_PASSED":"CAMPAIGN_RESUME_FAILED");Application.Quit(ok?0:1);yield break;
                }
                if (Array.IndexOf(args, "--raid-smoke") >= 0)
                {
                    int raidOutput = Array.IndexOf(args, "--output");
                    yield return current.RunFoundingRaidSmoke(raidOutput >= 0 && raidOutput + 1 < args.Length ? args[raidOutput + 1] : Path.Combine(Application.persistentDataPath, "RaidSmoke")); yield break;
                }
                if (Array.IndexOf(args, "--defense-smoke") >= 0)
                {
                    int outputArg = Array.IndexOf(args, "--output");
                    string directory = outputArg >= 0 && outputArg + 1 < args.Length ? args[outputArg + 1] : Path.Combine(Application.persistentDataPath, "DefenseSmoke");
                    yield return current.RunDefenseSmoke(directory); yield break;
                }
                if (Array.IndexOf(args, "--village-life-smoke") >= 0) { yield return LifeSmoke(current, args); yield break; }
                if (File.Exists(current.VillageSavePath) || File.Exists(current.VillageSavePath + ".bak")) current.LoadVillage();
                if (Array.IndexOf(args, "--village-resume-smoke") >= 0)
                {
                    bool ok = current.Village.Storehouse && current.Village.Watchpost && current.CivilianFood > 0 && current.LivingEnemies == 0;
                    current.InputEnabled = current.AutomaticSimulation = false;
                    yield return null;
                    ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(current.VillageSavePath), "fresh-launch.png"));
                    yield return new WaitForSeconds(.3f);
                    Debug.Log(ok ? "VILLAGE_FRESH_LAUNCH_PASSED" : "VILLAGE_FRESH_LAUNCH_FAILED"); Application.Quit(ok ? 0 : 1);
                }
                yield break;
            }
            Application.logMessageReceived += Observe;
            yield return null;
            string output = Path.Combine(Application.persistentDataPath, "FoundingSmoke");
            int index = Array.IndexOf(args, "--output"); if (index >= 0 && index + 1 < args.Length) output = args[index + 1];
            Directory.CreateDirectory(output);
            var lab = GetComponent<BlightCombatLab>(); lab.InputEnabled = lab.AutomaticSimulation = false;
            yield return new WaitForSeconds(.2f); ScreenCapture.CaptureScreenshot(Path.Combine(output, "village-start.png"));
            yield return new WaitForSeconds(.2f);
            if (!lab.IssueFoundingDirective(FoundingDirective.ScoutRoad)) failed = true;
            for (int frame = 0; frame < 900; frame++) { lab.Simulate(1f / 60); if (frame % 6 == 0) yield return null; }
            if (!lab.ScoutReportReceived) failed = true;
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "scout-report.png")); yield return new WaitForSeconds(.2f);
            lab.IssueFoundingDirective(FoundingDirective.Accompany);
            for (int frame = 0; frame < 5400 && lab.LivingEnemies > 0 && lab.PlayerFighter.Alive; frame++)
            {
                Vector3 goal = new Vector3(0, 0, 13); float nearest = float.MaxValue;
                foreach (Transform child in lab.transform)
                {
                    if (!child.name.Contains("raider")) continue;
                    var fighter = lab.GetFighter(child); if (fighter == null || !fighter.Alive) continue;
                    float distance = Vector3.Distance(child.position, lab.SidekickPlayerRoot.position);
                    if (distance < nearest) { nearest = distance; goal = child.position; }
                }
                lab.SetPlayerLocomotion(nearest < 2 ? Vector3.zero : Vector3.ClampMagnitude(goal - lab.SidekickPlayerRoot.position, 1), false, false);
                if (nearest < 2.2f) lab.RequestPlayerAttack(false);
                lab.Simulate(1f / 60);
                if (frame % 6 == 0) yield return null;
            }
            // Smoke interactions use the exposed actor transform, not real keyboard movement.
            lab.SetPlayerLocomotion(Vector3.zero, false, false);
            for (int frame = 0; frame < 120; frame++) lab.Simulate(1f / 60);
            lab.SidekickPlayerRoot.position = lab.CachePosition; if (!lab.Interact()) failed = true;
            lab.SidekickPlayerRoot.position = lab.CampPosition + Vector3.back * 2; if (!lab.Interact()) failed = true;
            if (!lab.ResearchFounding(FoundingVillage.Technology.WorkCrews)) failed = true;
            for (int frame = 0; frame < 920; frame++) lab.Simulate(1f / 60);
            if (!lab.ConstructFounding(false) || !lab.ResearchFounding(FoundingVillage.Technology.PatrolDoctrine)) failed = true;
            for (int frame = 0; frame < 1220; frame++) { lab.Simulate(1f / 60); if (frame % 6 == 0) yield return null; }
            if (!lab.ConstructFounding(true) || !lab.Village.Storehouse || !lab.Village.PatrolDoctrine) failed = true;
            lab.IssueFoundingDirective(FoundingDirective.DefendVillage);
            for (int frame = 0; frame < 600; frame++) lab.Simulate(1f / 60);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "village-developed.png")); yield return new WaitForSeconds(.3f);
            File.WriteAllText(Path.Combine(output, "smoke-result.txt"), "Passed: " + !failed + "\nPlayer alive: " + lab.PlayerFighter.Alive + "\nHostiles: " + lab.LivingEnemies + "\nCivilian: " + lab.Village.CivilianStage + "\nStorehouse: " + lab.Village.Storehouse + "\nMilitary: " + lab.Village.MilitaryStage + "\nResearch unlocked: " + lab.Village.WorkCrews + ", " + lab.Village.PatrolDoctrine);
            Debug.Log(failed ? "FOUNDING_SMOKE_FAILED" : "FOUNDING_SMOKE_PASSED");
            Application.logMessageReceived -= Observe; Application.Quit(failed ? 1 : 0);
        }
        private void Observe(string message, string stack, LogType kind)
        { if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert) failed = true; }
    }
}
