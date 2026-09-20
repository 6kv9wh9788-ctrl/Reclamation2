using System.Collections.Generic;
using Reclamation.Neighborhood;
using UnityEngine;
using UnityEngine.AI;

namespace Reclamation.Outbreak
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CivilianRoutine))]
    public sealed class OutbreakAgent : MonoBehaviour
    {
        [SerializeField] private string displayName;
        private readonly InfectionTimeline timeline = new();
        private CivilianRoutine routine;
        private NavMeshAgent nav;
        private Renderer body;
        private Color healthyColor;
        private NavMeshPath path;
        private OutbreakAgent target;
        private bool isolated;
        private bool fleeing;
        private bool simulationPaused;
        private float nextRouteAt;
        private EscapeRoutePlanner escapePlanner;
        public bool IsFleeing => fleeing;
        public bool CanFlee => !isolated && (State == InfectionState.Healthy || State == InfectionState.Exposed);
        public string MovementStatus { get; private set; } = "Routine";
        private bool CanNavigate => nav != null && nav.isActiveAndEnabled && nav.isOnNavMesh;

        public string DisplayName => displayName;
        public InfectionState State => timeline.State;
        public bool Isolated => isolated;
        public bool Infectable => State == InfectionState.Healthy && !isolated;
        public bool Infected => State == InfectionState.Exposed || State == InfectionState.Symptomatic || State == InfectionState.Turned;
        public bool Contagious => !isolated && (State == InfectionState.Symptomatic || State == InfectionState.Turned);
        public bool VisibleSymptoms => State == InfectionState.Symptomatic || State == InfectionState.Turned;
        public string PublicStatus => State == InfectionState.Neutralized ? "neutralized" :
            State == InfectionState.Turned ? "TURNED" :
            State == InfectionState.Symptomatic ? "symptomatic" :
            isolated ? "isolated" : "appears healthy";

        public void Configure(string name) => displayName = name;

        private void Awake()
        {
            routine = GetComponent<CivilianRoutine>();
            nav = GetComponent<NavMeshAgent>();
            body = GetComponent<Renderer>();
            healthyColor = body == null ? Color.white : body.sharedMaterial.color;
            path = new NavMeshPath();
            escapePlanner = new EscapeRoutePlanner();
            // Match the human body's width and let local avoidance negotiate passing.
            nav.radius = Mathf.Max(nav.radius, 0.45f);
            nav.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            nav.avoidancePriority = 30 + (int)((uint)GetInstanceID() % 40);
        }

        public bool Expose(double now, double incubation, double symptomatic)
        {
            bool changed = timeline.Expose(now, incubation, symptomatic);
            if (changed) ApplyState();
            return changed;
        }

        public void Simulate(double minute)
        {
            if (timeline.Advance(minute)) ApplyState();
        }

        public void SetIsolated(bool value)
        {
            if (State == InfectionState.Turned || State == InfectionState.Neutralized) return;
            isolated = value;
            ApplyState();
        }

        public bool Neutralize()
        {
            bool changed = timeline.Neutralize();
            if (changed) ApplyState();
            return changed;
        }

        public void SetThreatTarget(OutbreakAgent nextTarget, float simulationSpeed)
        {
            if (State != InfectionState.Turned || isolated || !CanNavigate || simulationPaused) return;
            if (nextTarget == null || !nextTarget.gameObject.activeInHierarchy || nextTarget.Isolated ||
                nextTarget.State == InfectionState.Turned || nextTarget.State == InfectionState.Neutralized)
            {
                target = null; nav.ResetPath(); MovementStatus = "No eligible prey"; return;
            }
            bool changed = target != nextTarget;
            target = nextTarget;
            SetMovementSpeed(3.3f, simulationSpeed);
            // Stop inside transmission range without steering into the target's center.
            nav.stoppingDistance = 0.95f;
            if (!changed && Time.time < nextRouteAt) return;
            nextRouteAt = Time.time + 0.35f / Mathf.Max(1, simulationSpeed);
            Vector3 targetFeet = target.transform.position - Vector3.up * target.nav.baseOffset;
            if (NavMesh.SamplePosition(targetFeet, out NavMeshHit hit, 0.65f, nav.areaMask)
                && nav.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete
                && nav.SetPath(path)) MovementStatus = "Pursuing";
            else { nav.ResetPath(); MovementStatus = "Prey unreachable"; }
        }

        public void SetSimulationPaused(bool paused)
        {
            simulationPaused = paused;
            if (CanNavigate) nav.isStopped = paused || isolated || State == InfectionState.Symptomatic;
        }

        public void FleeFrom(Vector3 threat, float simulationSpeed, IReadOnlyList<OutbreakAgent> population = null)
        {
            if (!CanFlee || !CanNavigate || simulationPaused) return;
            if (!fleeing)
            {
                // OnDisable resets the routine's path; disable before issuing the escape route.
                routine.enabled = false; fleeing = true; nextRouteAt = 0;
            }
            SetMovementSpeed(3.8f, simulationSpeed);
            nav.stoppingDistance = 0.25f;
            if (Time.time < nextRouteAt) return;
            nextRouteAt = Time.time + 0.45f / Mathf.Max(1, simulationSpeed);
            if (escapePlanner.TryFind(nav, threat, population, out Vector3 destination) &&
                nav.CalculatePath(destination, path) && path.status == NavMeshPathStatus.PathComplete && nav.SetPath(path))
                MovementStatus = "Escaping";
            else
            {
                nav.ResetPath();
                MovementStatus = "Cornered; seeking escape";
            }
        }

        private void SetMovementSpeed(float speed, float multiplier)
        {
            multiplier = Mathf.Max(0, multiplier);
            nav.isStopped = false;
            nav.speed = speed * multiplier;
            nav.acceleration = 20 * multiplier * multiplier;
            nav.angularSpeed = 720 * multiplier;
        }

        public void ResumeRoutineIfSafe()
        {
            if (!CanFlee) return;
            if (fleeing && CanNavigate) nav.ResetPath();
            fleeing = false; nextRouteAt = 0;
            if (CanNavigate) nav.stoppingDistance = 0.25f;
            routine.enabled = true;
            MovementStatus = "Routine";
        }

        private void ApplyState()
        {
            bool canRoutine = !isolated && (State == InfectionState.Healthy || State == InfectionState.Exposed);
            if (!canRoutine) { fleeing = false; nextRouteAt = 0; }
            routine.enabled = canRoutine && !fleeing;
            MovementStatus = isolated ? "Isolated" : State == InfectionState.Symptomatic ? "Symptomatic; stopped" : MovementStatus;
            if (!canRoutine && nav.isActiveAndEnabled && nav.isOnNavMesh) nav.ResetPath();
            if (State == InfectionState.Neutralized)
            {
                MovementStatus = "Neutralized";
                gameObject.SetActive(false);
            }
            if (body == null) return;
            body.material.color = State == InfectionState.Exposed ? healthyColor :
                State == InfectionState.Symptomatic ? new Color(0.85f, 0.75f, 0.15f) :
                State == InfectionState.Turned ? new Color(0.3f, 0.7f, 0.25f) :
                isolated ? new Color(0.4f, 0.75f, 1f) : healthyColor;
        }
    }
}
