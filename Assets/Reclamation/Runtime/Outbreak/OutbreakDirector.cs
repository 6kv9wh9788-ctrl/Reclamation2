using System.Collections.Generic;
using Reclamation.Neighborhood;
using UnityEngine;

namespace Reclamation.Outbreak
{
    public sealed class OutbreakDirector : MonoBehaviour
    {
        [SerializeField] private NeighborhoodClock clock;
        [SerializeField] private OutbreakAgent[] population;
        [SerializeField] private OutbreakAgent visitor;
        [SerializeField] private SafeZone safeZone;
        [SerializeField] private int scenarioSeed = 614;
        [SerializeField] private float contactSeconds = 1.5f;
        [SerializeField] private float contactRadius = 1.4f;
        private readonly Dictionary<long, float> exposure = new();
        private GUIStyle label, button;
        private bool seeded;
        private bool collapsed;
        private Vector2 panelScroll;
        private Rect PanelRect => new Rect(16, 16, 560, collapsed ? 130 : 700);
        public bool ContainsGuiPoint(Vector2 point) => isActiveAndEnabled && PanelRect.Contains(point);
        private string eventLog = "A visitor has entered the neighborhood.";

        public string Outcome { get; private set; } = "Normal life";
        public int HealthyCount { get; private set; }
        public int InfectedCount { get; private set; }
        public int TurnedCount { get; private set; }
        public int NeutralizedCount { get; private set; }
        public IReadOnlyList<OutbreakAgent> Population => population;
        public SafeZone SafeZone => safeZone;

        public void Configure(NeighborhoodClock timer, OutbreakAgent[] agents, OutbreakAgent arrivingVisitor, int seed,
            SafeZone refuge = null)
        {
            clock = timer; population = agents; visitor = arrivingVisitor; scenarioSeed = seed; safeZone = refuge;
        }

        public void AttachSafeZone(SafeZone refuge) => safeZone = refuge;

        private void Update()
        {
            if (clock == null || population == null) return;
            if (!seeded && clock.MinuteOfDay >= 8 * 60)
            {
                var random = new System.Random(scenarioSeed);
                double incubation = 32 + random.Next(-5, 6);
                visitor.Expose(clock.MinuteOfDay, incubation, 18);
                seeded = true;
                Outcome = "Undetected infection";
                eventLog = $"08:00 — Visitor exposed; symptoms expected after ~{incubation:0} minutes.";
            }

            foreach (OutbreakAgent person in population)
                if (person != null)
                {
                    person.Simulate(clock.MinuteOfDay);
                    person.SetSimulationPaused(clock.Paused);
                }
            if (!clock.Paused)
            {
                SimulateContacts();
                UpdateBehavior();
                foreach (OutbreakAgent person in population)
                    if (person != null) person.AdvanceMovement(Time.deltaTime * clock.Speed, clock.Speed);
            }
            CountAndResolve();
        }

        private void SimulateContacts()
        {
            float scaledSeconds = Time.deltaTime * clock.Speed;
            for (int i = 0; i < population.Length; i++)
            {
                OutbreakAgent source = population[i];
                if (source == null || !source.Contagious || !source.gameObject.activeInHierarchy) continue;
                for (int j = 0; j < population.Length; j++)
                {
                    OutbreakAgent target = population[j];
                    if (target == null || !target.Infectable || target == source) continue;
                    float distance = Vector3.Distance(source.transform.position, target.transform.position);
                    long key = ((long)source.GetInstanceID() << 32) ^ (uint)target.GetInstanceID();
                    if (distance <= contactRadius && (safeZone == null || safeZone.Perimeter == null ||
                        !safeZone.Perimeter.BlocksContact(source.transform.position, target.transform.position)))
                    {
                        exposure.TryGetValue(key, out float seconds);
                        seconds += scaledSeconds;
                        exposure[key] = seconds;
                        if (seconds >= contactSeconds)
                        {
                            double incubation = source.State == InfectionState.Turned ? 12 : 24;
                            if (target.Expose(clock.MinuteOfDay, incubation, 15))
                                eventLog = $"{clock.DisplayTime} — {target.DisplayName} was exposed.";
                            exposure.Remove(key);
                        }
                    }
                    else exposure.Remove(key);
                }
            }
        }

        private void UpdateBehavior()
        {
            PerimeterDefense perimeter = safeZone == null ? null : safeZone.Perimeter;
            if (perimeter != null) perimeter.Tick(population, clock.Speed, clock.Paused);
            foreach (OutbreakAgent person in population)
            {
                if (person == null || !person.gameObject.activeInHierarchy) continue;
                if (perimeter != null && perimeter.IsWorker(person)) continue;
                OutbreakAgent nearestThreat = null;
                float dangerDistance = float.MaxValue;
                foreach (OutbreakAgent other in population)
                {
                    if (other == null || other == person || other.State != InfectionState.Turned
                        || !other.gameObject.activeInHierarchy || !other.Contagious) continue;
                    if (perimeter != null && !person.CanReachPoint(other.FeetPosition)) continue;
                    float distance = Vector3.Distance(person.transform.position, other.transform.position);
                    if (distance < dangerDistance) { dangerDistance = distance; nearestThreat = other; }
                }
                if (person.State == InfectionState.Turned)
                {
                    OutbreakAgent prey = FindNearestPrey(person);
                    person.SetThreatTarget(prey, clock.Speed, perimeter);
                }
                else if (nearestThreat != null && dangerDistance < (person.IsFleeing ? 9 : 7))
                    person.FleeFrom(nearestThreat.transform.position, clock.Speed, population);
                else if (person.ZoneAssignment == RefugeAssignment.Evacuating)
                    person.ContinueEvacuation(clock.Speed);
                else person.ResumeRoutineIfSafe();
            }
        }

        private OutbreakAgent FindNearestPrey(OutbreakAgent hunter)
        {
            OutbreakAgent result = null; float best = float.MaxValue;
            bool resultReachable = false;
            foreach (OutbreakAgent person in population)
            {
                if (person == null || !person.gameObject.activeInHierarchy || person == hunter || person.Isolated || person.IsProtected ||
                    person.State == InfectionState.Turned || person.State == InfectionState.Neutralized) continue;
                float distance = Vector3.Distance(hunter.transform.position, person.transform.position);
                bool reachable = hunter.CanReachPoint(person.FeetPosition);
                if ((reachable && !resultReachable) || (reachable == resultReachable && distance < best))
                { best = distance; result = person; resultReachable = reachable; }
            }
            return result;
        }

        private void CountAndResolve()
        {
            HealthyCount = InfectedCount = TurnedCount = NeutralizedCount = 0;
            foreach (OutbreakAgent person in population)
            {
                if (person == null) continue;
                if (person.State == InfectionState.Healthy) HealthyCount++;
                else if (person.State == InfectionState.Turned) TurnedCount++;
                else if (person.State == InfectionState.Neutralized) NeutralizedCount++;
                else InfectedCount++;
            }
            if (!seeded) Outcome = "Normal life";
            else if (HealthyCount + InfectedCount == 0) Outcome = "Humanity lost";
            else if (InfectedCount == 0 && TurnedCount == 0) Outcome = "Outbreak eradicated";
            else if (TurnedCount > 0) Outcome = "Active outbreak";
            else Outcome = "Infection developing";
        }

        private void OnGUI()
        {
            if (clock == null || population == null) return;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
                button = new GUIStyle(GUI.skin.button) { fontSize = 17 };
            }
            float scale = LabCameraController.UiScale;
            Matrix4x4 prior = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUILayout.BeginArea(PanelRect, GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("RECLAMATION — PATIENT ZERO", label);
            if (GUILayout.Button(collapsed ? "Expand" : "Collapse", button, GUILayout.Width(100))) collapsed = !collapsed;
            GUILayout.EndHorizontal();
            GUILayout.Label(clock.DisplayTime + (clock.Paused ? " (paused)" : $" ({clock.Speed:0}×)"), label);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(clock.Paused ? "Resume" : "Pause", button)) clock.SetPaused(!clock.Paused);
            foreach (float speed in new[] { 1f, 4f, 12f })
                if (GUILayout.Button($"{speed:0}×", button)) clock.SetSpeed(speed);
            GUILayout.EndHorizontal();
            if (!collapsed)
            {
                panelScroll = GUILayout.BeginScrollView(panelScroll);
                GUILayout.Label($"OUTCOME: {Outcome}", label);
                GUILayout.Label($"Healthy {HealthyCount} | Developing {InfectedCount} | Turned {TurnedCount} | Neutralized {NeutralizedCount}", label);
                GUILayout.Label(eventLog, label);
                if (safeZone != null)
                    GUILayout.Label($"REFUGE {safeZone.ShelteredCount}/{safeZone.ShelterCapacity} | " +
                        $"QUARANTINE {safeZone.QuarantinedCount}/{safeZone.QuarantineCapacity} | " +
                        $"EN ROUTE {safeZone.EvacuatingCount}" + (safeZone.Breached ? " | BREACH" : ""), label);
                GUILayout.Space(5);
                if (safeZone != null && safeZone.Perimeter != null)
                {
                    var defense = safeZone.Perimeter;
                    GUILayout.Label($"DEFENSE WOOD {defense.Wood} | RESERVED {defense.ReservedWood} | SPENT {defense.SpentWood}", label);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Build next (4)", button)) defense.RequestWork(false);
                    if (GUILayout.Button("Repair (2)", button)) defense.RequestWork(true);
                    if (GUILayout.Button("Cancel work", button)) defense.CancelWork();
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button("Open / close gate", button)) defense.ToggleGate(population);
                    GUILayout.Label(defense.Status, label);
                    foreach (var section in defense.Sections)
                        if (section != null) GUILayout.Label($"{section.name}: {section.Health:0}/120" +
                            (section.IsGate && section.Built ? section.Open ? " OPEN" : " CLOSED" : ""), label);
                }
                foreach (OutbreakAgent person in population)
                {
                    if (person == null) continue;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{person.DisplayName}: {person.PublicStatus}\n{person.MovementStatus} · {person.ExertionStatus}", label, GUILayout.Width(190));
                    var cameraControls = Camera.main == null ? null : Camera.main.GetComponent<LabCameraController>();
                    if (cameraControls != null && person.gameObject.activeInHierarchy &&
                        GUILayout.Button("Follow", button, GUILayout.Width(75))) cameraControls.Follow(person.transform);
                    if (person.State == InfectionState.Turned)
                    {
                        if (GUILayout.Button("Neutralize", button)) { person.Neutralize(); eventLog = $"{person.DisplayName} neutralized."; }
                    }
                    else if (person.ZoneAssignment == RefugeAssignment.Sheltered && person.VisibleSymptoms)
                    {
                        if (GUILayout.Button("Quarantine", button))
                            eventLog = safeZone != null && safeZone.TryTransferToQuarantine(person)
                                ? $"{person.DisplayName} transferred to quarantine."
                                : $"Quarantine is full; {person.DisplayName} remains in the refuge.";
                    }
                    else if (person.State != InfectionState.Neutralized && !person.IsProtected)
                    {
                        if (safeZone != null && person.ZoneAssignment == RefugeAssignment.None &&
                            GUILayout.Button("Evacuate", button, GUILayout.Width(85)))
                        {
                            eventLog = safeZone.RequestEvacuation(person)
                                ? $"Evacuation ordered for {person.DisplayName}."
                                : $"Unable to evacuate {person.DisplayName}; check isolation and capacity.";
                        }
                        else if (safeZone != null && person.ZoneAssignment == RefugeAssignment.Evacuating &&
                            GUILayout.Button("Cancel", button, GUILayout.Width(85)))
                        {
                            safeZone.CancelEvacuation(person);
                            eventLog = $"Evacuation cancelled for {person.DisplayName}.";
                        }
                        if (person.ZoneAssignment == RefugeAssignment.None &&
                            GUILayout.Button(person.Isolated ? "Release" : "Isolate", button, GUILayout.Width(80)))
                        {
                            person.SetIsolated(!person.Isolated);
                            eventLog = $"{person.DisplayName} {(person.Isolated ? "isolated" : "released")}.";
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.Label("Stop and restart Play Mode to reset. Isolation is an abstract prototype action. Isolation is listed above. Yellow head = symptomatic; green head = turned.", label);
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
            GUI.matrix = prior;
        }
    }
}
