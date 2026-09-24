using System.Text;
using UnityEngine;

namespace Reclamation.Blight
{
    public static class BlightMapProjection
    {
        public static Vector2 ToMap(Rect map, Vector3 world)
            => new Vector2(map.x + Mathf.InverseLerp(-15, 15, world.x) * map.width,
                map.y + Mathf.InverseLerp(20, -23, world.z) * map.height);
        public static Vector3 ToWorld(Rect map, Vector2 point)
            => new Vector3(Mathf.Lerp(-15, 15, Mathf.InverseLerp(map.xMin, map.xMax, point.x)), 0,
                Mathf.Lerp(20, -23, Mathf.InverseLerp(map.yMin, map.yMax, point.y)));
    }

    public sealed partial class BlightCombatLab
    {
        private sealed class MapOrder
        {
            public CompanyOrder order;
            public CompanySite site;
            public bool noFail;
        }
        private readonly MapOrder[] mapOrders = new MapOrder[3];
        private bool mapWasPaused, mapAssaultQueued, mapNoFail, mapAssaultNoFail;
        private string mapNotice = "";
        public bool CompanyMapOpen { get; private set; }
        public bool CompanyAssaultQueued => mapAssaultQueued;
        public int PendingCompanyOrders
        { get { int count = 0; foreach (MapOrder order in mapOrders) if (order != null) count++; return count; } }
        public int MappedContactCount => companyContacts.Count;
        private Rect CommandMapWindow => new Rect(UiWidth / 2 - 550, UiHeight / 2 - 345, 1100, 690);
        private Rect CommandMapCanvas => new Rect(CommandMapWindow.x + 18, CommandMapWindow.y + 60, 420, 602);
        private Vector3 CompanyAssaultRally(int index) => VillageDefense ? VillageSite(index == 0 ? CompanySite.Gate : index == 1 ? CompanySite.West : CompanySite.Fortress) : index == 0 ? new Vector3(0, 0, 3) * CompanyScale :
            index == 1 ? new Vector3(-12, 0, 10) * CompanyScale : CompanySitePosition(CompanySite.Gate);

        private void ClearCommandMap()
        {
            CompanyMapOpen = mapAssaultQueued = mapWasPaused = false;
            mapNoFail = mapAssaultNoFail = false;
            mapNotice = ""; for (int i = 0; i < mapOrders.Length; i++) mapOrders[i] = null;
        }
        public bool OpenCompanyMap()
        {
            if (Scenario != BlightScenario.Company || companyReportOpen || CompanyMapOpen || player == null || !player.fighter.Alive) return false;
            RefreshCompanyFog(); ObserveCompanyContacts();
            mapWasPaused = paused; CompanyMapOpen = true;
            mapAssaultQueued = false; for (int i = 0; i < mapOrders.Length; i++) mapOrders[i] = null;
            mapNotice = "Select platoon, click a named destination, then queue a mission. Orders wait for Apply.";
            SetPaused(true); return true;
        }
        public bool QueueCompanyOrder(int index, CompanyOrder order, CompanySite site, bool noFail = false)
        {
            if (!CompanyMapOpen || index < 0 || index >= company.Count || CompanyLiving(index) == 0 ||
                (int)order < 0 || (int)order > 4 || (int)site < 0 || (int)site > 3) return false;
            if (mapAssaultQueued)
            {
                mapAssaultQueued = false;
                mapNotice = "Queued assault template replaced by individual orders. Nothing has been sent yet.";
            }
            mapOrders[index] = new MapOrder { order = order, site = site, noFail = order == CompanyOrder.Assault && noFail }; return true;
        }
        public bool QueueCompanyAssault(bool noFail = false)
        {
            if (!CompanyMapOpen || LivingCompanions == 0 || !VillageDefense && (CompanyLiving(0) == 0 || CompanyLiving(1) == 0)) return false;
            for (int i = 0; i < mapOrders.Length; i++) mapOrders[i] = null;
            mapAssaultQueued = true; mapAssaultNoFail = noFail && !VillageDefense;
            mapNotice = VillageDefense ? "Queued defense: main gate / west gate / square reserve." : "Queued plan: Mara main, Ivo west flank, Tess reserve. Main waits for flank readiness."; return true;
        }
        public bool QueueCompanyRecall()
        {
            if (!CompanyMapOpen) return false;
            mapAssaultQueued = false;
            for (int i = 0; i < company.Count; i++) mapOrders[i] = CompanyLiving(i) > 0 ?
                new MapOrder { order = CompanyOrder.Withdraw, site = CompanySite.Gate } : null;
            mapNotice = "Recall queued for every surviving platoon."; return true;
        }
        public bool CloseCompanyMap(bool apply)
        {
            if (!CompanyMapOpen) return false;
            // Validate the whole batch before changing a single platoon's assignment.
            if (apply)
            {
                if (player == null || !player.fighter.Alive) { mapNotice = "Cannot apply orders: player defeated."; return false; }
                if (mapAssaultQueued && !VillageDefense && (CompanyLiving(0) == 0 || CompanyLiving(1) == 0))
                { mapNotice = "The assault needs both main and flank platoons. Revise or discard the plan."; return false; }
                for (int i = 0; i < mapOrders.Length; i++)
                    if (mapOrders[i] != null && CompanyLiving(i) == 0)
                    { mapNotice = company[i].commander.Name + " has no survivors. Revise or discard the plan."; return false; }
            }
            bool resumePaused = mapWasPaused;
            if (apply)
            {
                // Existing validated order APIs reject paused input; temporarily lift the
                // flag synchronously. No simulation step or animation runs here.
                SetPaused(false);
                if (mapAssaultQueued) PlanCompanyAssault(mapAssaultNoFail);
                else for (int i = 0; i < mapOrders.Length; i++)
                    if (mapOrders[i] != null) GiveCompanyOrder(i, mapOrders[i].order, mapOrders[i].site, mapOrders[i].noFail);
            }
            ClearCommandMap(); SetPaused(resumePaused); return true;
        }
        public string CompanyMapBriefing()
        {
            if (Scenario != BlightScenario.Company) return "No company operation active.";
            var text = new StringBuilder("COMPANY BRIEFING\n");
            text.Append("Operation time: ").Append(simulationTime.ToString("0.0")).Append("s; all contact ages use simulation time.\n");
            for (int i = 0; i < company.Count; i++)
            {
                CommandPlatoon p = company[i];
                text.Append(p.commander.Name).Append(" | ").Append(CompanyLiving(i)).Append("/" + CompanyCapacity(i) + " | ").Append(p.commander.Risk)
                    .Append(" | ").Append(p.order).Append(' ').Append(p.order == CompanyOrder.Withdraw ? (VillageDefense ? "Square" : "Camp") : CompanySiteName(p.site)).Append(" | ").Append(p.phase).Append(p.noFail ? " | NO FAIL" : " | discretionary").Append("\n  ").Append(p.reason).Append('\n');
                if (CompanyMapOpen && mapOrders[i] != null) text.Append("  QUEUED: ").Append(mapOrders[i].order).Append(' ').Append(CompanySiteName(mapOrders[i].site)).Append(mapOrders[i].noFail ? " | NO FAIL" : "").Append('\n');
            }
            if (CompanyMapOpen && mapAssaultQueued) text.Append(VillageDefense ? "QUEUED: village defense deployment." : "QUEUED: coordinated assault; main waits for west flank; reserve at Gate.").Append(mapAssaultNoFail ? " NO FAIL.\n" : " Discretionary.\n");
            text.Append(VillageDefense ? VillageObjective() : CompanySupplyReport).Append("\n");
            text.Append("Recorded contacts (not a complete enemy roster):\n");
            foreach (CompanyContact c in companyContacts)
                text.Append(c.Name).Append(c.KnownAlive ? " | last known alive" : " | confirmed defeated")
                    .Append(" | position ").Append(c.Position.x.ToString("0.0")).Append(", ").Append(c.Position.z.ToString("0.0"))
                    .Append(" | age ").Append((simulationTime - c.LastSeen).ToString("0.0")).Append("s\n");
            return text.ToString();
        }

        private Vector2 MapPoint(Vector3 point) => BlightMapProjection.ToMap(CommandMapCanvas, point / CompanyScale);
        private void MapLine(Vector2 start, Vector2 end, Color color, bool dashed = false)
        {
            Vector2 delta = end - start; float length = delta.magnitude; if (length < .1f) return;
            Matrix4x4 old = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
            if (dashed) for (float x = 0; x < length; x += 12) Fill(new Rect(start.x + x, start.y - 1, Mathf.Min(7, length - x), 2), color);
            else Fill(new Rect(start.x, start.y - 1, length, 2), color);
            GUI.matrix = old;
        }
        private void DrawCommandMap()
        {
            if (!CompanyMapOpen || Scenario != BlightScenario.Company) return;
            Rect window = CommandMapWindow, map = CommandMapCanvas;
            Fill(new Rect(0, 0, UiWidth, UiHeight), new Color(.025f, .035f, .045f, 1));
            Fill(window, new Color(.07f, .09f, .12f, .99f)); GUI.Box(window, GUIContent.none);
            GUI.Label(new Rect(window.x + 20, window.y + 12, 1050, 32), "COMMAND MAP — PAUSED PLANNING", title);
            Fill(map, new Color(.14f, .19f, .17f));
            for (int x = -15; x <= 15; x += 5) MapLine(MapPoint(new Vector3(x, 0, -23) * CompanyScale), MapPoint(new Vector3(x, 0, 20) * CompanyScale), new Color(.25f, .31f, .28f));
            for (int z = -20; z <= 20; z += 5) MapLine(MapPoint(new Vector3(-15, 0, z) * CompanyScale), MapPoint(new Vector3(15, 0, z) * CompanyScale), new Color(.25f, .31f, .28f));
            for (int i = 0; i < terrain.Count; i++)
            {
                Rect wall = terrain.WallAt(i); Vector2 top = MapPoint(new Vector3(wall.xMin, 0, wall.yMax));
                Vector2 bottom = MapPoint(new Vector3(wall.xMax, 0, wall.yMin));
                Fill(new Rect(top.x, top.y, bottom.x - top.x, bottom.y - top.y), new Color(.5f, .48f, .47f));
            }
            DrawCompanyFog(map);
            for (int i = 0; i < company.Count; i++)
            {
                if (CompanyLiving(i) == 0) continue;
                Vector2 start = MapPoint(CompanyCenter(company[i]));
                Vector3 activeGoal = company[i].order == CompanyOrder.Escort && company[i].phase != CompanyPhase.Completed && company[i].phase != CompanyPhase.Returning ? player.root.position : company[i].goal;
                MapLine(start, MapPoint(activeGoal), new Color(.25f, .65f, .95f, .6f));
                if (mapAssaultQueued) MapLine(start, MapPoint(CompanyAssaultRally(i)), new Color(1, .8f, .25f), true);
                else if (mapOrders[i] != null) MapLine(start, MapPoint(mapOrders[i].order == CompanyOrder.Withdraw ? CompanyCampSlot(i) :
                    mapOrders[i].order == CompanyOrder.Escort ? player.root.position : CompanySitePosition(mapOrders[i].site)), new Color(1, .8f, .25f), true);
            }
            Vector2 campPoint = MapPoint(camp);
            GUI.Label(new Rect(campPoint.x - 38, campPoint.y + 14, 100, 23), VillageDefense ? "SQUARE" : "CAMP", centered);
            for (int i = 0; i < 4; i++)
            {
                Vector2 point = MapPoint(CompanySitePosition((CompanySite)i));
                float markerWidth = VillageDefense ? 104 : 84;
                Rect marker = new Rect(Mathf.Clamp(point.x - markerWidth / 2, map.xMin, map.xMax - markerWidth), point.y - 12, markerWidth, 25);
                if (GUI.Button(marker, (selectedCompanySite == (CompanySite)i ? "> " : "") + CompanySiteName((CompanySite)i), button)) selectedCompanySite = (CompanySite)i;
            }
            // Draw reports only. Never obtain marker positions from enemy actor transforms.
            foreach (CompanyContact c in companyContacts)
            {
                Vector2 point = MapPoint(c.Position); float age = simulationTime - c.LastSeen;
                Color color = !c.KnownAlive ? new Color(.55f, .55f, .55f) : age > 12 ? new Color(.68f, .48f, .28f) : new Color(.95f, .4f, .16f);
                Fill(new Rect(point.x - 10, point.y - 10, 20, 20), color);
                GUI.Label(new Rect(point.x - 10, point.y - 10, 20, 20), new GUIContent(!c.KnownAlive ? "X" : c.Name.Contains("Hulk") ? "H" : "T",
                    c.Name + " | " + (c.KnownAlive ? "last known alive" : "confirmed defeated") + " | " + Mathf.FloorToInt(age) + "s ago"), centered);
            }
            for (int i = 0; i < company.Count; i++)
            {
                if (CompanyLiving(i) == 0) continue;
                Vector2 point = MapPoint(CompanyCenter(company[i]));
                Fill(new Rect(point.x - 4, point.y - 4, 8, 8), new Color(.2f, .8f, 1));
                float y = Mathf.Clamp(point.y - 12 + (i - 1) * 28, map.yMin, map.yMax - 25);
                if (GUI.Button(new Rect(Mathf.Clamp(point.x - 16, map.xMin, map.xMax - 34), y, 34, 25), (selectedPlatoon == i ? ">" : "") + (i + 1), button)) selectedPlatoon = i;
            }
            Vector2 hero = MapPoint(player.root.position);
            GUI.Label(new Rect(hero.x - 10, hero.y - 10, 20, 20), "P", centered);
            GUI.Label(new Rect(map.x + 8, map.y + 5, 80, 24), "NORTH ↑", small);
            DrawMapPlanningPanel(window);
        }
        private void DrawMapPlanningPanel(Rect window)
        {
            float x = window.x + 460, width = 620, y = window.y + 60;
            for (int i = 0; i < 3; i++)
                if (GUI.Button(new Rect(x + i * 205, y, 199, 34), (selectedPlatoon == i ? "> " : "") + company[i].commander.Name + " (" + CompanyLiving(i) + "/" + CompanyCapacity(i) + ")\n" + CompanyActivity(i), button)) selectedPlatoon = i;
            CommandPlatoon selected = company[selectedPlatoon];
            GUI.Label(new Rect(x, y + 42, width, 55), (selected.noFail ? "NO FAIL | " : "") + selected.commander.Risk + " | Judgment " + selected.commander.Judgment + " | " + selected.phase +
                "\n" + selected.reason, label);
            GUI.Label(new Rect(x, y + 105, width, 26), "Destination: " + CompanySiteName(selectedCompanySite) + " — select a named marker on the map", label);
            bool old = GUI.enabled; GUI.enabled = old && CompanyLiving(selectedPlatoon) > 0;
            for (int i = 0; i < 5; i++)
                if (GUI.Button(new Rect(x + i * 123, y + 138, 117, 30), ((CompanyOrder)i).ToString(), button)) QueueCompanyOrder(selectedPlatoon, (CompanyOrder)i, selectedCompanySite, mapNoFail);
            GUI.enabled = old;
            if (GUI.Button(new Rect(x, y + 180, 300, 30), VillageDefense ? "Queue defense deployment" : "Queue coordinated assault", button)) QueueCompanyAssault(mapNoFail);
            if (GUI.Button(new Rect(x + 310, y + 180, 300, 30), "Queue company recall", button)) QueueCompanyRecall();
            mapNoFail = GUI.Toggle(new Rect(x, y + 214, width, 24), mapNoFail, "NO FAIL for next assault: accept losses; no automatic withdrawal");
            GUI.Label(new Rect(x, y + 239, width, 20), "PENDING — current assignments remain active until Apply", label);
            for (int i = 0; i < 3; i++)
            {
                string pending = mapAssaultQueued && VillageDefense ? (i == 0 ? "Defend main gate" : i == 1 ? "Defend west gate" : "Reserve at square") : mapAssaultQueued ? (i == 0 ? "Main approach; wait for flank" + (mapAssaultNoFail ? " [NO FAIL]" : "") : i == 1 ? "West flank; then assault" + (mapAssaultNoFail ? " [NO FAIL]" : "") : "Reserve at Gate") :
                    mapOrders[i] == null ? "No change" : mapOrders[i].order + " " + (mapOrders[i].order == CompanyOrder.Withdraw ? (VillageDefense ? "to square" : "to camp") : CompanySiteName(mapOrders[i].site)) + (mapOrders[i].noFail ? " [NO FAIL]" : "");
                GUI.Label(new Rect(x, y + 265 + i * 30, 535, 27), company[i].commander.Name + ": " + pending, small);
                if (!mapAssaultQueued && mapOrders[i] != null && GUI.Button(new Rect(x + 540, y + 265 + i * 30, 70, 24), "Clear", button)) mapOrders[i] = null;
            }
            GUI.Label(new Rect(x, y + 355, width, 56), mapNotice, label);
            GUI.Label(new Rect(x, y + 418, width, 44), "Blue: active intent | Yellow dashes: queued intent\nT/H: last-seen thrall/Hulk | X: confirmed defeat | P: your hero\nBright: visible | Dim: explored | Dark: unknown. Sites are prior mission intel.", small);
            if (ExpandedCompany && !VillageDefense) GUI.Label(new Rect(x, y + 466, width, 24), CompanySupplyReport, small);
            GUI.Label(new Rect(x, y + 495, width, 42), string.IsNullOrEmpty(GUI.tooltip) ? "Hover contacts for age. Reports can be stale; lines are intent, not routes." : GUI.tooltip, small);
            if (GUI.Button(new Rect(x, window.yMax - 55, 200, 35), mapWasPaused ? "Apply; remain paused" : "Apply & resume", button)) CloseCompanyMap(true);
            if (GUI.Button(new Rect(x + 210, window.yMax - 55, 200, 35), "Discard / close [Esc]", button)) CloseCompanyMap(false);
            if (GUI.Button(new Rect(x + 420, window.yMax - 55, 190, 35), "Copy briefing", button))
            { GUIUtility.systemCopyBuffer = CompanyMapBriefing(); mapNotice = "Briefing copied. Paste it with your playtest feedback if useful."; }
        }
    }
}
