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
            if (State != InfectionState.Turned || isolated || nextTarget == null || !nav.isOnNavMesh) return;
            target = nextTarget;
            nav.speed = 3.3f * simulationSpeed;
            nav.acceleration = 20f * simulationSpeed * simulationSpeed;
            if (NavMesh.SamplePosition(target.transform.position, out NavMeshHit hit, 1, nav.areaMask)
                && nav.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                nav.SetPath(path);
        }

        public void SetSimulationPaused(bool paused)
        {
            if (nav != null && nav.isActiveAndEnabled && nav.isOnNavMesh)
                nav.isStopped = paused || isolated || State == InfectionState.Symptomatic;
        }

        public void FleeFrom(Vector3 threat, float simulationSpeed)
        {
            if (!Infectable || isolated || !nav.isOnNavMesh) return;
            Vector3 away = transform.position - threat;
            away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = transform.right;
            Vector3 desired = transform.position + away.normalized * 7;
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 4, nav.areaMask)
                && nav.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                routine.enabled = false;
                nav.speed = 3.8f * simulationSpeed;
                nav.SetPath(path);
            }
        }

        public void ResumeRoutineIfSafe()
        {
            if (State == InfectionState.Healthy || State == InfectionState.Exposed)
                routine.enabled = !isolated;
        }

        private void ApplyState()
        {
            bool canRoutine = !isolated && (State == InfectionState.Healthy || State == InfectionState.Exposed);
            routine.enabled = canRoutine;
            if (!canRoutine && nav.isActiveAndEnabled && nav.isOnNavMesh) nav.ResetPath();
            if (State == InfectionState.Neutralized) gameObject.SetActive(false);
            if (body == null) return;
            body.material.color = State == InfectionState.Exposed ? healthyColor :
                State == InfectionState.Symptomatic ? new Color(0.85f, 0.75f, 0.15f) :
                State == InfectionState.Turned ? new Color(0.3f, 0.7f, 0.25f) :
                isolated ? new Color(0.4f, 0.75f, 1f) : healthyColor;
        }
    }
}
