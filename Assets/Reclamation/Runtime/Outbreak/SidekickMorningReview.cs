using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reclamation.Blight
{
    [DefaultExecutionOrder(150)]
    public sealed class SidekickMorningReview : MonoBehaviour
    {
        public BlightCombatLab lab;
        public SidekickDuelBridge bridge;
        private DuelFighter fighter;
        private BlightWeapon weapon;
        private string action = "Idle";
        private readonly string[] actions = { "Idle", "Walk", "Light", "Heavy", "Block", "Dodge", "Hit", "Defeat" };
        private float elapsed, clock, speed = 1f, yaw = 35, distance = 3.6f;
        private bool rehearsal = true, frozen, auto;
        private int sequence, impacts;
        private string message = "Safe rehearsal: the opponent is frozen. No gameplay damage is applied.";
        private float maxGap;
        private readonly List<Row> rows = new List<Row>();
        [Serializable] private sealed class Row
        { public string weapon, action; public float supportGapCm; public int impactTransitions; }
        [Serializable] private sealed class Report
        {
            public string revision = "Sidekick grounded motion 2";
            public string note = "Rehearsal observations, not automated visual approval or battle performance results.";
            public string unity, gpu, cpu;
            public Row[] rows;
        }
        private float Scale => Mathf.Max(.3f, Mathf.Min(Screen.width / 1200f, Screen.height / 800f));
        private Rect Panel => new Rect(Screen.width / Scale - 350, rehearsal ? 16 : 140, 334, rehearsal ? 650 : 155);
        private void Start() { lab.AutomaticSimulation = false; lab.InputEnabled = false; lab.SidekickReviewActive = true; }
        private void Choose(string name)
        {
            action = name; fighter = new DuelFighter(); elapsed = 0; impacts = 0; maxGap = 0;
            if (name == "Light" || name == "Heavy") fighter.Attack(BlightEquipment.Weapon(weapon, name == "Heavy"));
            if (name == "Block") fighter.Blocking = true;
            if (name == "Dodge") fighter.Dodge();
            if (name == "Hit") fighter.Receive(12, true, true);
            if (name == "Defeat") fighter.Receive(1000, true, true);
            bridge.ReviewFighter = fighter; bridge.ReviewWeapon = weapon;
        }
        private void LateUpdate()
        {
            if (!lab || !bridge) return;
            var mouse = Mouse.current;
            bool over = mouse != null && Panel.Contains(new Vector2(mouse.position.ReadValue().x, Screen.height - mouse.position.ReadValue().y) / Scale);
            if (!rehearsal)
            {
                lab.InputEnabled = !over;
                if (over) lab.SetPlayerLocomotion(Vector3.zero, false, false);
                return;
            }
            lab.AutomaticSimulation = false; lab.InputEnabled = false; lab.SidekickReviewActive = true;
            if (!lab.SidekickAnimationSource) return;
            if (!bridge.Ready) { message = "Waiting for Sidekick installation. If this persists, check Console for setup errors."; return; }
            if (fighter == null) Choose(action);
            float dt = frozen ? 0 : Mathf.Min(Time.unscaledDeltaTime, .1f) * speed;
            elapsed += dt; clock += dt;
            for (float remain = dt; remain > .000001f;)
            {
                float step = Mathf.Min(remain, 1f / 120f); remain -= step;
                if (fighter.Advance(step)) impacts++;
            }
            bridge.ReviewDelta = dt;
            bridge.ReviewWalking = action == "Walk";
            lab.SidekickRehearse(fighter, weapon, action == "Walk", clock);
            if (elapsed > .1f && bridge.Ready && bridge.SupportGripActive) maxGap = Mathf.Max(maxGap, bridge.LeftGripError * 100);
            if (auto && elapsed >= 3.1f)
            {
                Record(); sequence++;
                if (sequence >= actions.Length * 3)
                { auto = false; message = "Guided sequence complete: " + rows.Count + " observations. Export report; visual review is still required."; }
                else { weapon = (BlightWeapon)(sequence / actions.Length); Choose(actions[sequence % actions.Length]); }
            }
            var camera = lab.SidekickCamera; var root = lab.SidekickPlayerRoot;
            if (camera && root)
            {
                Vector3 center = root.position + Vector3.up * 1.15f;
                camera.transform.position = center + Quaternion.Euler(8, yaw, 0) * Vector3.forward * distance;
                camera.transform.LookAt(center);
            }
        }
        private void Record()
        {
            rows.Add(new Row { weapon = weapon.ToString(), action = action, supportGapCm = maxGap, impactTransitions = impacts });
        }
        private void Live(BlightScenario scenario)
        {
            auto = false; rehearsal = false; frozen = false; fighter = null;
            bridge.ReviewFighter = null;
            lab.SidekickReviewActive = false; lab.SelectScenario(scenario);
            lab.AutomaticSimulation = true; lab.InputEnabled = true;
        }
        private void Practice()
        {
            rehearsal = true; auto = false; frozen = false;
            lab.AutomaticSimulation = false; lab.InputEnabled = false;
            lab.SelectScenario(BlightScenario.Duel); lab.SidekickReviewActive = true;
            Choose("Idle");
        }
        private void Export()
        {
            try
            {
                var report = new Report { unity = Application.unityVersion, gpu = SystemInfo.graphicsDeviceName,
                    cpu = SystemInfo.processorType, rows = rows.ToArray() };
                string path = Path.Combine(Application.persistentDataPath, "SidekickReview-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".json");
                File.WriteAllText(path, JsonUtility.ToJson(report, true));
                message = "Report saved; full path in Console."; Debug.Log("Sidekick review report: " + path);
            }
            catch (Exception exception) { message = "Export failed: " + exception.Message; Debug.LogException(exception); }
        }
        private void OnGUI()
        {
            var matrix = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1));
            GUILayout.BeginArea(Panel, GUI.skin.box);
            GUILayout.Label("SIDEKICK GROUNDED MOTION v2");
            if (!rehearsal)
            {
                GUILayout.Label("Live " + lab.Scenario + " — normal rules. R resets.");
                if (GUILayout.Button("Return to safe rehearsal")) Practice();
                GUILayout.Label("Inspect attacks, block, dodge and health. Use H for controls.");
            }
            else
            {
                GUILayout.Label("SAFE REHEARSAL — opponent and gameplay frozen");
                GUILayout.Label(weapon + " / " + action + " / " + (fighter == null ? "Starting" : fighter.Action.ToString()));
                GUILayout.Label("Support-hand gap: " + (bridge.LeftGripError * 100).ToString("F1") + " cm (when gripping)");
                GUILayout.BeginHorizontal();
                foreach (BlightWeapon kind in Enum.GetValues(typeof(BlightWeapon)))
                    if (GUILayout.Button(kind.ToString())) { auto = false; weapon = kind; Choose(action); }
                GUILayout.EndHorizontal();
                for (int row = 0; row < 2; row++)
                {
                    GUILayout.BeginHorizontal();
                    for (int col = 0; col < 4; col++)
                    { string name = actions[row * 4 + col]; if (GUILayout.Button(name)) { auto = false; Choose(name); } }
                    GUILayout.EndHorizontal();
                }
                frozen = GUILayout.Toggle(frozen, "Freeze pose");
                GUILayout.Label("Playback speed: " + speed.ToString("F2") + "x"); speed = GUILayout.HorizontalSlider(speed, .15f, 1);
                GUILayout.Label("Camera orbit"); yaw = GUILayout.HorizontalSlider(yaw, -180, 180);
                GUILayout.Label("Camera distance"); distance = GUILayout.HorizontalSlider(distance, 1.4f, 6);
                if (GUILayout.Button(auto ? "Stop guided sequence" : "Run 24-case guided sequence"))
                {
                    auto = !auto;
                    if (auto) { rows.Clear(); sequence = 0; weapon = BlightWeapon.Sword; frozen = false; Choose(actions[0]); }
                }
                if (GUILayout.Button("Record current observation")) { Record(); message = "Recorded " + weapon + " / " + action; }
                if (GUILayout.Button("Export observations to JSON")) Export();
                GUILayout.Label(message);
                GUILayout.Space(8); GUILayout.Label("LIVE PLAYTESTS (fresh encounter)");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Thrall duel")) Live(BlightScenario.Duel);
                if (GUILayout.Button("Hulk")) Live(BlightScenario.Hulk);
                if (GUILayout.Button("Squad")) Live(BlightScenario.Squad);
                GUILayout.EndHorizontal();
                GUILayout.Label("Rehearsal uses the same fighter timing and source poses, but applies no hits to the opponent. Grip gap is geometric, not a judgement of how natural the hands look.");
            }
            GUILayout.EndArea(); GUI.matrix = matrix;
        }
        private void OnDestroy()
        {
            if (lab) { lab.SidekickReviewActive = false; lab.InputEnabled = true; lab.AutomaticSimulation = true; }
            if (bridge) bridge.ReviewFighter = null;
        }
    }
}
