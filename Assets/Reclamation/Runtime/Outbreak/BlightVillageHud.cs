using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        private int villagePanel;
        private bool villageDebug;
        private Rect VillagePanelRect => new Rect(UiWidth - 460, 124, 444, Mathf.Min(Campaign!=null&&villagePanel==4?570:440, UiHeight - 205));
        private Rect VillageDebugRect => new Rect(16, 190, 390, 210);
        private bool FoundingUiContains(Vector2 point) => confirmVillageLoad || point.y < 116 || point.y > UiHeight - 64 ||
            (villagePanel != 0 && VillagePanelRect.Contains(point)) || (villageDebug && VillageDebugRect.Contains(point));
        private void DrawFounding()
        {
            EnsureStyles(); var old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(UiScale, UiScale, 1));
            var heading = new GUIStyle(title) { fontSize = 22 };
            var detail = new GUIStyle(small) { fontSize = 13, wordWrap = true };
            var eyebrow = new GUIStyle(small) { fontSize = 11, fontStyle = FontStyle.Bold };
            var body = new GUIStyle(label) { fontSize = 16, wordWrap = true };
            GUI.Box(new Rect(16, 16, 230, 94), GUIContent.none);
            GUI.Label(new Rect(28, 22, 210, 22), "THE FOUNDER", eyebrow);
            Meter(new Rect(28, 46, 206, 19), player.fighter.Health, 100, new Color(.65f,.25f,.23f), "Health " + player.fighter.Health.ToString("0"));
            Meter(new Rect(28, 73, 206, 14), player.fighter.Stamina, 100, new Color(.3f,.55f,.43f), "Stamina " + player.fighter.Stamina.ToString("0"));
            GUI.Box(new Rect(260, 16, UiWidth - 580, 94), GUIContent.none);
            GUI.Label(new Rect(274, 23, UiWidth - 608, 18), "YOUR OBJECTIVE", eyebrow);
            string objective = Campaign!=null?CampaignStatus:FoundingRaidActive || FoundingRaidCompleted ? FoundingRaidStatus : Village.Storehouse && Village.Watchpost ? "A home worth defending" : Village.SuppliesDelivered ? "Develop your village" : Village.SuppliesCarried ? "Bring the supplies home" : "Recover the road supplies";
            GUI.Label(new Rect(274, 43, UiWidth - 608, 28), objective, heading);
            GUI.Label(new Rect(274, 77, UiWidth - 608, 28), Campaign!=null ? (FoundingRaidActive ? "Protect stores / breach "+Mathf.Min(5,breachSeconds).ToString("0.0")+" of 5s | Garrison "+DefenseGarrison : "Operation / perks: plan, develop, save. Two raids in this slice.") : Village.SuppliesDelivered ? "Council: research and construction. Mara: defense plan." : Village.SuppliesCarried ? "F at the blue village circle to deliver." : "Lead the operation or ask Mara to scout. F collects the cache.", detail);
            GUI.Box(new Rect(UiWidth - 306, 16, 290, 94), GUIContent.none);
            GUI.Label(new Rect(UiWidth - 290, 24, 260, 18), "VILLAGE DEFENSE", eyebrow);
            var prior = GUI.color; GUI.color = defenseAlert ? new Color(1,.55f,.4f) : new Color(.65f,.85f,.75f);
            GUI.Label(new Rect(UiWidth - 290, 45, 260, 28), DefenseStatus, heading); GUI.color = prior;
            GUI.Label(new Rect(UiWidth - 290, 79, 260, 24), "Mara's platoon  " + LivingCompanions + "/8 alive", detail);
            if (patrolClock < reportUntil && villagePanel == 0)
            {
                GUI.Box(new Rect(260, 120, UiWidth - 580, 72), GUIContent.none);
                GUI.Label(new Rect(274, 127, UiWidth - 608, 57), CommanderReport, body);
            }
            if (villagePanel != 0) DrawVillagePanel(heading, body, detail, eyebrow);
            DrawVillageToolbar(detail);
            if (villageDebug)
            {
                GUI.Box(VillageDebugRect, GUIContent.none);
                GUI.Label(new Rect(28, 202, 360, 24), "DEVELOPER DIAGNOSTICS - recording aid", eyebrow);
                GUI.Label(new Rect(28, 230, 360, 155), "Simulation: " + simulationTime.ToString("0.0") + "s | Duty cycle: " + ((int)(patrolClock / 60)) +
                    "\nOrder: " + CommanderDirective + " | Alert: " + defenseAlert +
                    "\nGarrison / Patrol / Reserve: " + DefenseGarrison + " / " + DefensePatrol + " / " + DefenseReserve +
                    "\nFood: " + Village.Food + "/20 | Enemies alive: " + LivingEnemies +
                    "\nResearch: " + Village.Research + " " + Village.ResearchSeconds.ToString("0.0") +
                    "\nRaid: " + FoundingRaidStatus + "\nConstruction: " + Village.BuildSeconds.ToString("0.0") + "/20\n" + VillageSaveStatus, detail);
            }
            if (confirmVillageLoad) DrawVillageLoadConfirmation();
            if ((paused || !player.fighter.Alive) && !confirmVillageLoad)
                GUI.Box(new Rect(UiWidth / 2 - 190, UiHeight / 2, 380, 45), paused ? "PAUSED - Esc resumes" : "DEFEATED - R restarts", title);
            GUI.matrix = old;
        }
        private void DrawVillageToolbar(GUIStyle detail)
        {
            float y = UiHeight - 56;
            GUI.Box(new Rect(16, y - 6, UiWidth - 32, 48), GUIContent.none);
            string[] tabs = Campaign==null?new[]{ "Mara / defense", "Council", "Controls" }:new[]{ "Mara / defense", "Council", "Controls", "Operation / perks" };
            for (int i = 0; i < tabs.Length; i++) if (GUI.Button(new Rect(26 + i * 150, y, 142, 30), i==3?tabs[i]:(villagePanel == i + 1 ? "Close " : "") + tabs[i], button)) villagePanel = villagePanel == i + 1 ? 0 : i + 1;
            if (GUI.Button(new Rect(UiWidth - 242, y, 216, 30), (villageDebug ? "Hide" : "Show") + " developer diagnostics", button)) villageDebug = !villageDebug;
        }
        private void DrawVillagePanel(GUIStyle heading, GUIStyle body, GUIStyle detail, GUIStyle eyebrow)
        {
            if(villagePanel==4&&Campaign!=null){DrawCampaignPanel(heading,body,detail,eyebrow);return;}
            Rect r = VillagePanelRect; GUI.Box(r, GUIContent.none); float x = r.x + 16, y = r.y + 14;
            GUI.Label(new Rect(x, y, 380, 30), villagePanel == 1 ? "MARA  /  Defense plan" : villagePanel == 2 ? "THE VILLAGE COUNCIL" : "CONTROLS", heading); y += 38;
            if (villagePanel == 1)
            {
                GUI.Label(new Rect(x, y, 408, 46), DefenseReason, body); y += 54;
                GUI.Label(new Rect(x, y, 408, 24), "POSTS " + DefenseGarrison + "    PATROL " + DefensePatrol + "    RESERVE " + DefenseReserve, eyebrow); y += 30;
                GUI.Label(new Rect(x, y, 408, 46), "Duty change in " + (60 - (int)patrolClock % 60) + "s. Wounded soldiers stay in reserve.\nReserve stays near home; patrol covers the approaches.", detail); y += 48;
                bool command = !paused && player.fighter.Alive && foundingCommander.fighter.Alive && Vector3.Distance(player.root.position, foundingCommander.root.position) <= 12;
                string[] orders = { "1 Accompany me", "2 Defend town", "3 Scout road", "4 Patrol override" };
                for (int i = 0; i < 4; i++)
                {
                    GUI.enabled = command && (i != 3 || Village.PatrolDoctrine);
                    if (GUI.Button(new Rect(x + i % 2 * 207, y + i / 2 * 34, 200, 29), orders[i], button)) IssueFoundingDirective((FoundingDirective)i);
                }
                GUI.enabled = true; y += 70;
                GUI.Label(new Rect(x, y, 408, 22), command ? "Give intent. Mara chooses the assignments." : "Approach Mara (12 m) to change the objective.", detail); y += 27;
                GUI.enabled = Campaign==null&&CanBeginFoundingRaid;
                if (GUI.Button(new Rect(x, y, 408, 29), FoundingRaidStatus, button)) BeginFoundingRaid();
                GUI.enabled = true; y += 34;
                GUI.Label(new Rect(x, y, 408, 30), FoundingRaidActive ? "Saving resumes after the raid. Fight alongside the defense." : "Requires storehouse + watchpost, Defend town, and rest at home.", detail); y += 33;
                GUI.Label(new Rect(x, y, 400, 20), "LATEST REPORT", eyebrow); y += 22;
                for (int i = 0; i < Mathf.Min(1, defenseJournal.Count); i++) { GUI.Label(new Rect(x, y, 408, 46), defenseJournal[i], detail); y += 48; }
            }
            else if (villagePanel == 2)
            {
                GUI.Label(new Rect(x, y, 408, 42), "Village" + (Village.Storehouse ? " + storehouse" : "") + "   /   " + Village.MilitaryStage + "\nTimber " + Village.Timber + "   Insight " + Village.Insight + "   Food " + Village.Food + "/20", body); y += 54;
                GUI.enabled = AtFoundingCouncil && Village.Insight > 0 && Village.Research == FoundingVillage.Technology.None && !Village.WorkCrews;
                if (GUI.Button(new Rect(x, y, 408, 30), Village.WorkCrews ? "Work crews - learned" : "Work crews - 1 insight", button)) ResearchFounding(FoundingVillage.Technology.WorkCrews); y += 35;
                GUI.enabled = AtFoundingCouncil && Village.Insight > 0 && Village.Research == FoundingVillage.Technology.None && !Village.PatrolDoctrine;
                if (GUI.Button(new Rect(x, y, 408, 30), Village.PatrolDoctrine ? "Patrol doctrine - learned" : "Patrol doctrine - 1 insight", button)) ResearchFounding(FoundingVillage.Technology.PatrolDoctrine); y += 35;
                GUI.enabled = AtFoundingCouncil && Village.WorkCrews && !Village.BuildingStorehouse && !Village.Storehouse && Village.Timber >= 4;
                if (GUI.Button(new Rect(x, y, 408, 30), Village.Storehouse ? "Storehouse - complete" : Village.BuildingStorehouse ? "Storehouse - building" : "Build storehouse - 4 timber", button)) ConstructFounding(false); y += 35;
                GUI.enabled = AtFoundingCouncil && Village.PatrolDoctrine && !Village.Watchpost && Village.Timber >= 2;
                if (GUI.Button(new Rect(x, y, 408, 30), Village.Watchpost ? "Watchpost - complete" : "Build watchpost - 2 timber", button)) ConstructFounding(true); y += 37;
                GUI.enabled = true;
                GUI.Label(new Rect(x, y, 408, 45), "Research: " + Village.Research + (Village.Research != FoundingVillage.Technology.None ? " " + Village.ResearchSeconds.ToString("0") + "/15s" : "") + "\n" + (Village.BuildingStorehouse ? "Storehouse: " + Village.BuildSeconds.ToString("0") + "/20s" : "Approach the council (4 m) to develop or save."), detail); y += 48;
                GUI.enabled = CanSaveVillage;
                if (GUI.Button(new Rect(x, y, 200, 30), "Save village", button)) SaveVillage();
                GUI.enabled = true;
                if (GUI.Button(new Rect(x + 207, y, 200, 30), "Load saved", button)) { confirmVillageLoad = true; SetPaused(true); }
                GUI.Label(new Rect(x, y + 39, 408, 48), VillageSaveStatus, detail);
            }
            else GUI.Label(new Rect(x, y, 408, 290), "WASD  Move\nShift  Sprint\nLMB / E  Light / heavy attack\nSpace  Dodge     Ctrl  Guard\nRMB  Orbit     Tab  Lock target\nF  Collect / deliver supplies\n1-4  Give Mara an objective nearby\nEsc  Pause / resume\nR  Fresh unsaved session\n\nSave at the council before quitting. Panels do not pause play. Developer diagnostics are optional and useful in recordings.", body);
        }
        private void DrawVillageLoadConfirmation()
        {
            GUI.Box(new Rect(UiWidth / 2 - 250, UiHeight / 2 - 65, 500, 120), "Load checkpoint? Unsaved play will be replaced.", label);
            if (GUI.Button(new Rect(UiWidth / 2 - 220, UiHeight / 2 - 15, 205, 35), "Load checkpoint", button)) { confirmVillageLoad = false; LoadVillage(); }
            if (GUI.Button(new Rect(UiWidth / 2 + 15, UiHeight / 2 - 15, 205, 35), "Keep playing", button)) { confirmVillageLoad = false; SetPaused(false); }
        }
    }
}
