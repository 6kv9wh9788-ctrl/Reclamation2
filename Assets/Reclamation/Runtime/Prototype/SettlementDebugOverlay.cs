using Reclamation.Neighborhood;
using Reclamation.Outbreak;
using UnityEngine;

namespace Reclamation.Prototype
{
    public sealed class SettlementDebugOverlay : MonoBehaviour
    {
        private enum HudIcon { Wood, Food, Medicine, Bed, Balanced, Build, Supplies, Recovery }

        [SerializeField] private HaulJobBoard jobBoard;
        [SerializeField] private Stockpile stockpile;
        [SerializeField] private FoodStore foodStore;
        [SerializeField] private MedicineStore medicineStore;
        [SerializeField] private RecreationSpot recreationSpot;
        [SerializeField] private Bed[] beds;
        private HaulWorker[] workers;
        private GUIStyle textStyle, detailButtonStyle, resourceNumberStyle, resourceLabelStyle;
        private GUIStyle priorityButtonStyle, priorityActiveStyle, tooltipStyle;
        private Vector2 debugScroll;
        private bool debugExpanded;

        private float LogicalHeight => Screen.height / LabCameraController.UiScale;
        private Rect ResourceRect => new Rect(16, 16, 590, 82);
        private Rect PriorityRect => new Rect(16, LogicalHeight - 102, 516, 86);
        private Rect ContextRect => new Rect(16, LogicalHeight - 300, 420, 190);
        private Rect DebugRect => new Rect(16, 106, 620, Mathf.Max(180, Mathf.Min(650, LogicalHeight - 224)));

        public bool DebugExpanded => debugExpanded;
        public bool ContainsGuiPoint(Vector2 point) => isActiveAndEnabled &&
            (ResourceRect.Contains(point) || PriorityRect.Contains(point) ||
             (!debugExpanded && SelectedWorker() != null && ContextRect.Contains(point)) ||
             (debugExpanded && DebugRect.Contains(point)));

        private void Start()
        {
            workers = FindObjectsByType<HaulWorker>(FindObjectsSortMode.None);
        }

        public void Configure(HaulJobBoard board, Stockpile destination, FoodStore meals = null,
            Bed[] availableBeds = null, MedicineStore medicine = null, RecreationSpot recreation = null)
        {
            jobBoard = board;
            stockpile = destination;
            foodStore = meals;
            beds = availableBeds;
            medicineStore = medicine;
            recreationSpot = recreation;
        }

        private void EnsureStyles()
        {
            if (textStyle != null) return;
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            detailButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14 };
            resourceNumberStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 23,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            resourceLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(0.78f, 0.82f, 0.85f) }
            };
            priorityButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                alignment = TextAnchor.LowerCenter,
                padding = new RectOffset(4, 4, 4, 5)
            };
            priorityActiveStyle = new GUIStyle(priorityButtonStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            tooltipStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 7, 7)
            };
        }

        private void OnGUI()
        {
            EnsureStyles();
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            Color previousBackground = GUI.backgroundColor;
            GUI.matrix = Matrix4x4.Scale(new Vector3(LabCameraController.UiScale, LabCameraController.UiScale, 1f));

            DrawResourceRibbon();
            DrawPriorityBar();
            if (debugExpanded) DrawDebugPanel();
            else DrawContextCard();
            DrawTooltip();

            GUI.backgroundColor = previousBackground;
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private void DrawResourceRibbon()
        {
            GUI.Box(ResourceRect, GUIContent.none);
            int occupiedBeds = OccupiedBeds();
            int bedCount = beds == null ? 0 : beds.Length;
            int food = foodStore == null ? 0 : foodStore.Servings;
            int foodReserved = foodStore == null ? 0 : foodStore.ReservedServings;
            int medicine = medicineStore == null ? 0 : medicineStore.Supplies;
            int medicineReserved = medicineStore == null ? 0 : medicineStore.ReservedSupplies;

            DrawResource(new Rect(25, 24, 102, 64), HudIcon.Wood,
                stockpile == null ? "0" : stockpile.StoredUnits.ToString(), "WOOD",
                "Construction wood in the settlement stockpile.", stockpile != null && stockpile.StoredUnits == 0);
            DrawResource(new Rect(132, 24, 102, 64), HudIcon.Food, food.ToString(), "FOOD",
                foodReserved > 0 ? $"{foodReserved} reserved for meals" : "Stored food servings.", food == 0);
            DrawResource(new Rect(239, 24, 102, 64), HudIcon.Medicine, medicine.ToString(), "MEDICINE",
                medicineReserved > 0 ? $"{medicineReserved} reserved for treatment" : "Medical supplies available.",
                medicineStore != null && medicine == 0);
            DrawResource(new Rect(346, 24, 102, 64), HudIcon.Bed, $"{occupiedBeds}/{bedCount}", "BEDS",
                "Occupied beds / total beds.", bedCount > 0 && occupiedBeds >= bedCount);

            string detailText = debugExpanded ? "Hide details" : "Details";
            if (GUI.Button(new Rect(463, 37, 126, 38), new GUIContent(detailText,
                "Show simulation decisions, reservations, farms, and individual needs."), detailButtonStyle))
                debugExpanded = !debugExpanded;
        }

        private void DrawResource(Rect rect, HudIcon icon, string value, string label, string tooltip, bool warning)
        {
            GUI.Box(rect, new GUIContent(string.Empty, tooltip));
            Color iconColor = warning ? new Color(1f, 0.42f, 0.3f) : new Color(0.56f, 0.82f, 0.68f);
            DrawIcon(icon, new Rect(rect.x + 8, rect.y + 9, 30, 30), iconColor);
            Color old = GUI.color;
            GUI.color = warning ? new Color(1f, 0.55f, 0.42f) : Color.white;
            GUI.Label(new Rect(rect.x + 42, rect.y + 5, rect.width - 47, 38), value, resourceNumberStyle);
            GUI.color = old;
            GUI.Label(new Rect(rect.x + 3, rect.y + 42, rect.width - 6, 18), label, resourceLabelStyle);
        }

        private void DrawPriorityBar()
        {
            GUI.Box(PriorityRect, GUIContent.none);
            SettlementPolicy policy = jobBoard == null ? null : jobBoard.Policy;
            GUI.Label(new Rect(PriorityRect.x + 10, PriorityRect.y + 4, 112, 20), "CIVILIAN FOCUS", resourceLabelStyle);
            if (policy == null)
            {
                GUI.Label(new Rect(PriorityRect.x + 126, PriorityRect.y + 27, 370, 25),
                    "No settlement policy is active.", textStyle);
                return;
            }

            DrawPriority(policy, SettlementFocus.Balanced, HudIcon.Balanced, "Balanced", 24,
                "Keep food, construction, and supply work in balance.");
            DrawPriority(policy, SettlementFocus.Food, HudIcon.Food, "Food", 122,
                "Prioritize farming, harvesting, and maintaining meal reserves.");
            DrawPriority(policy, SettlementFocus.BuildDefense, HudIcon.Build, "Build", 220,
                "Prioritize shelter construction and perimeter defenses.");
            DrawPriority(policy, SettlementFocus.Supplies, HudIcon.Supplies, "Supplies", 318,
                "Prioritize collecting and delivering general supplies.");
            DrawPriority(policy, SettlementFocus.Recovery, HudIcon.Recovery, "Recovery", 416,
                "Prioritize food, rest, treatment, and morale recovery needs.");
        }

        private void DrawPriority(SettlementPolicy policy, SettlementFocus focus, HudIcon icon,
            string label, float xOffset, string tooltip)
        {
            Rect rect = new Rect(PriorityRect.x + xOffset, PriorityRect.y + 20, 88, 58);
            bool active = policy.Focus == focus;
            Color oldBackground = GUI.backgroundColor;
            GUI.backgroundColor = active ? new Color(0.28f, 0.78f, 0.5f) : new Color(0.72f, 0.75f, 0.78f);
            if (GUI.Button(rect, new GUIContent(label, tooltip), active ? priorityActiveStyle : priorityButtonStyle))
                policy.SetFocus(focus);
            GUI.backgroundColor = oldBackground;
            DrawIcon(icon, new Rect(rect.center.x - 12, rect.y + 7, 24, 24),
                active ? Color.white : new Color(0.76f, 0.82f, 0.86f));
        }

        private void DrawDebugPanel()
        {
            GUILayout.BeginArea(DebugRect, GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("SETTLEMENT DETAILS — DEBUG", textStyle);
            if (GUILayout.Button("Collapse", detailButtonStyle, GUILayout.Width(100))) debugExpanded = false;
            GUILayout.EndHorizontal();
            debugScroll = GUILayout.BeginScrollView(debugScroll);
            SettlementPolicy policy = jobBoard == null ? null : jobBoard.Policy;
            GUILayout.Label($"Player focus: {(policy == null ? "NONE" : policy.Label)}", textStyle);
            GUILayout.Label($"Stockpile: {(stockpile == null ? 0 : stockpile.StoredUnits)} wood", textStyle);
            GUILayout.Label($"Food: {(foodStore == null ? 0 : foodStore.Servings)} servings " +
                $"({(foodStore == null ? 0 : foodStore.ReservedServings)} reserved)", textStyle);
            if (medicineStore != null) GUILayout.Label($"Medicine: {medicineStore.Supplies} supplies " +
                $"({medicineStore.ReservedSupplies} reserved)", textStyle);
            if (recreationSpot != null) GUILayout.Label($"Gathering spot: {recreationSpot.Status}", textStyle);
            GUILayout.Label($"Active reservations: {(jobBoard == null ? 0 : jobBoard.OpenReservationCount)}", textStyle);
            GUILayout.Label($"Beds: {OccupiedBeds()}/{(beds == null ? 0 : beds.Length)} occupied", textStyle);
            if (jobBoard != null) foreach (FarmPlot farm in jobBoard.Farms)
                if (farm != null) GUILayout.Label($"{farm.name}: {farm.Status}", textStyle);
            GUILayout.Space(8);

            if (workers != null) foreach (HaulWorker worker in workers)
            {
                if (worker == null) continue;
                GUILayout.Label($"{worker.WorkerName} — {worker.State} — wood: {(worker.Carrying ? 1 : 0)} · food: {worker.CarriedFood}", textStyle);
                GUILayout.Label($"    hunger: {(worker.Needs == null ? 0 : worker.Needs.Hunger):0}/100 · " +
                    $"energy: {(worker.Needs == null ? 0 : worker.Needs.Energy):0}/100 · " +
                    $"injury: {(worker.Medical == null ? 0 : worker.Medical.Injury):0}/100 · " +
                    $"morale: {(worker.Morale == null ? 0 : worker.Morale.Morale):0}/100", textStyle);
                GUILayout.Label($"    {worker.DecisionExplanation}", textStyle);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawContextCard()
        {
            HaulWorker worker = SelectedWorker();
            if (worker == null) return;
            GUI.Box(ContextRect, GUIContent.none);
            GUI.Label(new Rect(ContextRect.x + 14, ContextRect.y + 8, 280, 27),
                worker.WorkerName, resourceNumberStyle);
            LabCameraController camera = Camera.main == null ? null : Camera.main.GetComponent<LabCameraController>();
            if (GUI.Button(new Rect(ContextRect.xMax - 38, ContextRect.y + 8, 26, 24),
                new GUIContent("×", "Close the selected civilian card."), detailButtonStyle) && camera != null)
                camera.ClearSelection();

            Combatant combat = worker.GetComponent<Combatant>();
            if (combat != null)
                GUI.Label(new Rect(ContextRect.x + 245, ContextRect.y + 12, 125, 20),
                    combat.ExperienceStatus, resourceLabelStyle);

            float y = ContextRect.y + 40;
            DrawMeter(y, "HEALTH", combat == null ? 0 : combat.Health, false, combat == null);
            DrawMeter(y + 23, "HUNGER", worker.Needs == null ? 0 : worker.Needs.Hunger, true, worker.Needs == null);
            DrawMeter(y + 46, "ENERGY", worker.Needs == null ? 0 : worker.Needs.Energy, false, worker.Needs == null);
            DrawMeter(y + 69, "INJURY", worker.Medical == null ? 0 : worker.Medical.Injury, true, worker.Medical == null);
            DrawMeter(y + 92, "MORALE", worker.Morale == null ? 0 : worker.Morale.Morale, false, worker.Morale == null);
            GUI.Label(new Rect(ContextRect.x + 14, ContextRect.yMax - 28, ContextRect.width - 28, 20),
                "TASK  " + TaskLabel(worker.State), resourceLabelStyle);
        }

        private void DrawMeter(float y, string label, float value, bool danger, bool unavailable)
        {
            Rect labelRect = new Rect(ContextRect.x + 14, y, 72, 18);
            Rect barRect = new Rect(ContextRect.x + 88, y + 3, 268, 12);
            GUI.Label(labelRect, label, resourceLabelStyle);
            DrawRect(barRect, new Color(0.08f, 0.1f, 0.12f, 0.9f));
            if (!unavailable)
            {
                float normalized = Mathf.Clamp01(value / 100f);
                Color low = new Color(0.92f, 0.28f, 0.22f);
                Color high = new Color(0.3f, 0.8f, 0.45f);
                Color fill = danger ? Color.Lerp(high, low, normalized) : Color.Lerp(low, high, normalized);
                DrawRect(new Rect(barRect.x + 1, barRect.y + 1, (barRect.width - 2) * normalized,
                    barRect.height - 2), fill);
            }
            GUI.Label(new Rect(ContextRect.x + 362, y - 2, 44, 20),
                unavailable ? "—" : Mathf.RoundToInt(value).ToString(), resourceLabelStyle);
        }

        private static string TaskLabel(HaulWorker.WorkerState state)
        {
            return state switch
            {
                HaulWorker.WorkerState.MovingToResource => "Gathering wood",
                HaulWorker.WorkerState.MovingToStockpile => "Delivering wood",
                HaulWorker.WorkerState.MovingToSupplies => "Collecting supplies",
                HaulWorker.WorkerState.MovingToShelter => "Supplying construction",
                HaulWorker.WorkerState.MovingToBuild => "Going to construction",
                HaulWorker.WorkerState.Building => "Building",
                HaulWorker.WorkerState.MovingToMeal => "Getting food",
                HaulWorker.WorkerState.Eating => "Eating",
                HaulWorker.WorkerState.MovingToBed => "Going to bed",
                HaulWorker.WorkerState.Sleeping => "Sleeping",
                HaulWorker.WorkerState.MovingToFarm => "Going to farm",
                HaulWorker.WorkerState.Harvesting => "Harvesting food",
                HaulWorker.WorkerState.MovingFoodToStore => "Delivering food",
                HaulWorker.WorkerState.MovingToTreatment => "Seeking treatment",
                HaulWorker.WorkerState.Treating => "Receiving treatment",
                HaulWorker.WorkerState.MovingToRecreation => "Seeking company",
                HaulWorker.WorkerState.Socializing => "Recovering morale",
                HaulWorker.WorkerState.WaitingForWork => "Waiting for work",
                _ => "Available"
            };
        }

        private static HaulWorker SelectedWorker()
        {
            Camera main = Camera.main;
            if (main == null) return null;
            LabCameraController controller = main.GetComponent<LabCameraController>();
            return controller == null || controller.SelectedTarget == null
                ? null : controller.SelectedTarget.GetComponent<HaulWorker>();
        }

        private int OccupiedBeds()
        {
            int occupied = 0;
            if (beds != null) foreach (Bed bed in beds) if (bed != null && bed.Occupied) occupied++;
            return occupied;
        }

        private void DrawTooltip()
        {
            if (string.IsNullOrEmpty(GUI.tooltip)) return;
            var content = new GUIContent(GUI.tooltip);
            Vector2 size = tooltipStyle.CalcSize(content);
            size.x = Mathf.Clamp(size.x, 180, 360);
            size.y = tooltipStyle.CalcHeight(content, size.x);
            Vector2 mouse = Event.current.mousePosition;
            Rect rect = new Rect(mouse.x + 14, mouse.y + 18, size.x, size.y);
            rect.x = Mathf.Min(rect.x, Screen.width / LabCameraController.UiScale - rect.width - 8);
            rect.y = Mathf.Min(rect.y, LogicalHeight - rect.height - 8);
            GUI.Box(rect, GUI.tooltip, tooltipStyle);
        }

        private static void DrawIcon(HudIcon icon, Rect rect, Color color)
        {
            switch (icon)
            {
                case HudIcon.Wood:
                    DrawRect(new Rect(rect.x + 2, rect.y + 5, rect.width - 4, 7), color);
                    DrawRect(new Rect(rect.x + 2, rect.y + 18, rect.width - 4, 7), color);
                    DrawRect(new Rect(rect.x + 5, rect.y + 7, 3, 3), new Color(0.2f, 0.25f, 0.22f));
                    DrawRect(new Rect(rect.x + rect.width - 8, rect.y + 20, 3, 3), new Color(0.2f, 0.25f, 0.22f));
                    break;
                case HudIcon.Food:
                    DrawRect(new Rect(rect.x + 7, rect.y + 9, rect.width - 14, rect.height - 13), color);
                    DrawRect(new Rect(rect.x + 4, rect.y + 13, rect.width - 8, rect.height - 20), color);
                    DrawRect(new Rect(rect.center.x, rect.y + 3, 3, 8), color);
                    DrawRect(new Rect(rect.center.x + 3, rect.y + 4, 7, 4), color);
                    break;
                case HudIcon.Medicine:
                case HudIcon.Recovery:
                    DrawRect(new Rect(rect.center.x - 4, rect.y + 2, 8, rect.height - 4), color);
                    DrawRect(new Rect(rect.x + 2, rect.center.y - 4, rect.width - 4, 8), color);
                    break;
                case HudIcon.Bed:
                    DrawRect(new Rect(rect.x + 2, rect.y + 9, 5, rect.height - 7), color);
                    DrawRect(new Rect(rect.x + 7, rect.y + 13, rect.width - 9, 12), color);
                    DrawRect(new Rect(rect.x + 9, rect.y + 9, 8, 6), color);
                    DrawRect(new Rect(rect.x + rect.width - 5, rect.y + 22, 3, 6), color);
                    break;
                case HudIcon.Balanced:
                    DrawRect(new Rect(rect.center.x - 2, rect.y + 3, 4, rect.height - 5), color);
                    DrawRect(new Rect(rect.x + 3, rect.y + 7, rect.width - 6, 3), color);
                    DrawRect(new Rect(rect.x + 4, rect.y + 11, 8, 8), color);
                    DrawRect(new Rect(rect.x + rect.width - 12, rect.y + 11, 8, 8), color);
                    DrawRect(new Rect(rect.x + 6, rect.y + rect.height - 4, rect.width - 12, 3), color);
                    break;
                case HudIcon.Build:
                    DrawRect(new Rect(rect.x + 2, rect.y + 3, 9, 8), color);
                    DrawRect(new Rect(rect.x + 14, rect.y + 3, 8, 8), color);
                    DrawRect(new Rect(rect.x + 7, rect.y + 13, 10, 8), color);
                    DrawRect(new Rect(rect.x + 1, rect.y + 22, 22, 3), color);
                    break;
                case HudIcon.Supplies:
                    DrawRect(new Rect(rect.x + 2, rect.y + 4, rect.width - 4, rect.height - 6), color);
                    DrawRect(new Rect(rect.x + 5, rect.y + 7, rect.width - 10, 3), new Color(0.2f, 0.25f, 0.22f));
                    DrawRect(new Rect(rect.center.x - 2, rect.y + 5, 4, rect.height - 8), new Color(0.2f, 0.25f, 0.22f));
                    break;
            }
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
