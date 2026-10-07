using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Reclamation.Blight
{
    public sealed partial class BattleCrowdPerformanceLab
    {
        public CrowdObjectiveScenario Objective { get; private set; }
        private GameObject objectiveVisual;
        public void OpenObjective()
        {
            measuring = busy = false; noiseClick = false;
            SquadMode = NavigationMode = PerceptionMode = CommunicationMode = true;
            navigationLayout = CrowdNavigation.Layout.Wall;
            Reset(96, true);
            Objective = new CrowdObjectiveScenario(); Simulation = Objective.Simulation;
            selectedSquad = 0; observer = 31; pendingCommand = CrowdSquadCommands.Command.Move;
            SetLayers(false, true, true); NavigationVisuals();
            View.transform.position = new Vector3(0, 42, -40);
            View.transform.LookAt(new Vector3(0, 0, 3));
            objectiveVisual = new GameObject("Relay capture boundary");
            objectiveVisual.transform.SetParent(transform, false);
            var line = objectiveVisual.AddComponent<LineRenderer>();
            line.sharedMaterial = library.friendly; line.startColor = line.endColor = Color.cyan;
            line.startWidth = line.endWidth = .18f; line.loop = true; line.positionCount = 64;
            line.shadowCastingMode = ShadowCastingMode.Off;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2 / 64;
                line.SetPosition(i, Objective.Center + new Vector3(Mathf.Cos(angle) * CrowdObjectiveScenario.Radius, .12f, Mathf.Sin(angle) * CrowdObjectiveScenario.Radius));
            }
        }
        private void ObjectiveGUI()
        {
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Matrix4x4 old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            GUI.Box(new Rect(10, 10, 1260, 175), "SECURE THE RELAY  |  Tactical prototype");
            GUI.Label(new Rect(26, 40, 900, 25), "Bring at least 4 soldiers into the blue circle. Keep it free of enemies for 10 consecutive seconds.");
            GUI.Label(new Rect(26, 66, 1000, 25), "Blue: " + Objective.FriendlyAlive + "/64   Orange: " + Objective.EnemyAlive + "/32   |   Time: " + Mathf.CeilToInt(Mathf.Max(0, CrowdObjectiveScenario.TimeLimit - Objective.Elapsed)) + "s   |   Capture: " + Objective.Progress.ToString("F1") + "/10s   |   Reports: " + Simulation.Communication.Delivered);
            GUI.enabled = Objective.Outcome == CrowdObjectiveScenario.State.Running;
            for (int s = 0; s < 2; s++) if (GUI.Button(new Rect(26 + s * 130, 102, 120, 30), (selectedSquad == s ? "> " : "") + "Squad " + (s + 1))) { selectedSquad = s; observer = s * 32 + 31; }
            if (GUI.Button(new Rect(300, 102, 100, 30), "Move")) pendingCommand = CrowdSquadCommands.Command.Move;
            if (GUI.Button(new Rect(410, 102, 100, 30), "Attack")) pendingCommand = CrowdSquadCommands.Command.Attack;
            if (GUI.Button(new Rect(520, 102, 100, 30), "Hold")) IssueSquads(CrowdSquadCommands.Command.Hold, Vector3.zero);
            GUI.enabled = true;
            if (GUI.Button(new Rect(1110, 42, 140, 30), "Restart round")) { OpenObjective(); GUI.matrix = old; return; }
            GUI.Label(new Rect(26, 144, 1100, 28), "Right-click ground: " + pendingCommand + "   |   Move travels without attacking; Attack advances and engages; Hold defends in place.");
            string state = Objective.Outcome == CrowdObjectiveScenario.State.Running
                ? (Objective.EnemyInside > 0 ? "RELAY CONTESTED" : Objective.FriendlyInside < 4 ? "RELAY NEEDS 4 FRIENDLIES" : "SECURING RELAY")
                : Objective.Outcome == CrowdObjectiveScenario.State.Secured ? "RELAY SECURED - VICTORY" : Objective.Outcome == CrowdObjectiveScenario.State.Defeated ? "SQUADS LOST - DEFEAT" : "TIME EXPIRED";
            Vector3 p = View.WorldToScreenPoint(Objective.Center + Vector3.up * 2);
            GUI.Box(new Rect(p.x / scale - 145, (Screen.height - p.y) / scale - 22, 290, 28), state);
            for (int s = 0; s < 2; s++)
            {
                var squad = Simulation.Squads.Squads[s]; p = View.WorldToScreenPoint(squad.anchor + Vector3.up * 2);
                GUI.Label(new Rect(950, 100 + s * 22, 290, 22), (selectedSquad == s ? "> " : "") + "Squad " + (s + 1) + ": " + squad.command);
            }
            var aware = Simulation.Perception.States[observer];
            string sensed = !Simulation.Units[observer].fighter.Alive ? "FALLEN" : aware.visibleTarget >= 0 ? "SEES ENEMY" : Simulation.Perception.TryKnownPosition(observer, out _) ? aware.reported ? "ALLY REPORT" : aware.heard ? "HEARD NOISE" : "LAST SEEN" : "UNAWARE";
            GUI.Box(new Rect(10, Screen.height / scale - 66, 1260, 56), "Selected squad observer: " + sensed + "  |  Yellow: sight cone / known position. Casualties do not respawn.\nWall blocks vision. Try separate approaches around its ends, then converge on the relay. No player combat changes.");
            GUI.matrix = old;
        }
        private IEnumerator ObjectivePreview()
        {
            yield return new WaitForSeconds(.3f);
            Directory.CreateDirectory(output);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "objective-start.png"));
            yield return new WaitForSeconds(.3f);
            Simulation.Squads.Issue(0, CrowdSquadCommands.Command.Attack, new Vector3(-18, 0, 8));
            Simulation.Squads.Issue(1, CrowdSquadCommands.Command.Attack, new Vector3(18, 0, 8));
            yield return new WaitForSeconds(20);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "objective-contact.png"));
            Simulation.Squads.Issue(0, CrowdSquadCommands.Command.Attack, Objective.Center + Vector3.left * 2);
            Simulation.Squads.Issue(1, CrowdSquadCommands.Command.Attack, Objective.Center + Vector3.right * 2);
            while (Objective.Outcome == CrowdObjectiveScenario.State.Running) yield return null;
            File.WriteAllText(Path.Combine(output, "round-result.txt"), Objective.Outcome + "\nElapsed: " + Objective.Elapsed + "\nFriendly: " + Objective.FriendlyAlive + "\nEnemy: " + Objective.EnemyAlive + "\nReports: " + Simulation.Communication.Delivered);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "objective-result.png"));
            yield return new WaitForSeconds(.5f);
            Debug.Log("OBJECTIVE_PREVIEW_COMPLETED: " + Objective.Outcome); Application.Quit(0);
        }
    }
}
