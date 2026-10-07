using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reclamation.Blight
{
    // Explicit command-line smoke mode only; normal play keeps existing lab behavior.
    public sealed class CombatDemoSmoke : MonoBehaviour
    {
        public Shader[] requiredShaders;
        private bool failed;

        private IEnumerator Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "--combat-demo-smoke") < 0) yield break;
            Application.logMessageReceived += ObserveLog;
            yield return null;
            IEnumerator checks = Check(args);
            while (true)
            {
                bool next = false;
                try { next = checks.MoveNext(); }
                catch (Exception exception) { failed = true; Debug.LogException(exception); }
                if (!next) break;
                yield return checks.Current;
            }
            Debug.Log(failed ? "COMBAT_DEMO_SMOKE_FAILED" : "COMBAT_DEMO_SMOKE_PASSED");
            Application.logMessageReceived -= ObserveLog;
            Application.Quit(failed ? 1 : 0);
        }

        private void ObserveLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true;
        }

        private IEnumerator Check(string[] args)
        {
            var lab = GetComponent<BlightCombatLab>();
            Require(lab != null && lab.PlayerFighter != null, "Lab/player initialized");
            Require(Camera.main != null, "Main camera initialized");
            var playerVisual = GetComponent<SidekickDuelBridge>();
            Require(playerVisual != null, "Player presentation bridge configured");
            playerVisual.RefreshPresentation();
            Require(playerVisual.Ready, "Sidekick player installed");
            Require(Keyboard.current != null && Mouse.current != null, "Keyboard/mouse devices available");
            foreach (Shader shader in requiredShaders)
                Require(shader != null && shader.isSupported, "Required shader supported");
            lab.InputEnabled = lab.AutomaticSimulation = false;
            foreach (BlightScenario scenario in Enum.GetValues(typeof(BlightScenario)))
            {
                lab.SelectScenario(scenario);
                Require(lab.Scenario == scenario && lab.PlayerFighter != null && lab.PlayerFighter.Alive, "Scenario " + scenario);
                lab.SetPaused(false);
                for (int i = 0; i < 60; i++) lab.Simulate(1f / 60f);
                playerVisual.RefreshPresentation();
                Require(scenario == BlightScenario.LimbDamage ? !playerVisual.Ready : playerVisual.Ready, "Player presentation for " + scenario);
                if (playerVisual.Ready) Require(playerVisual.ModelRoot.parent == lab.SidekickPlayerRoot, "Player-only model ownership");
                yield return null;
                foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                    foreach (Material material in renderer.sharedMaterials)
                        Require(material != null && material.shader != null && material.shader.isSupported &&
                            material.shader.name != "Hidden/InternalErrorShader", "Renderable material: " + renderer.name);
                Debug.Log("COMBAT_DEMO_SCENARIO_OK: " + scenario);
                lab.ResetFight();
                Require(lab.PlayerFighter.Alive && !lab.Paused, "Reset " + scenario);
            }
            lab.SetExpandedCompany(true);
            Require(lab.ExpandedCompany && lab.PlanCompanyAssault(true), "Expanded Company plan");
            lab.RecallCompany();
            Require(!lab.CompanyPlanActive, "Company recall cancels plan");
            lab.SelectScenario(BlightScenario.Squad);
            lab.SetPaused(true);
            Require(lab.Paused, "Pause");
            lab.SetPaused(false);
            string capture = null;
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "--combat-demo-capture") capture = args[i + 1];
            if (capture != null)
            {
                // A hidden smoke-test window may not expose a screen backbuffer.
                // Render the actual game camera offscreen; this verifies world visuals, not IMGUI.
                yield return null;
                playerVisual.RefreshPresentation();
                Camera camera = Camera.main;
                RenderTexture previousTarget = camera.targetTexture;
                RenderTexture previousActive = RenderTexture.active;
                var target = new RenderTexture(1440, 960, 24);
                var screenshot = new Texture2D(1440, 960, TextureFormat.RGB24, false);
                try
                {
                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    screenshot.ReadPixels(new Rect(0, 0, 1440, 960), 0, 0);
                    screenshot.Apply();
                    File.WriteAllBytes(capture, screenshot.EncodeToPNG());
                }
                finally
                {
                    camera.targetTexture = previousTarget;
                    RenderTexture.active = previousActive;
                    target.Release();
                    Destroy(target);
                    Destroy(screenshot);
                }
                Require(File.Exists(capture), "Screenshot saved");
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Combat demo smoke: " + message);
        }
    }
}
