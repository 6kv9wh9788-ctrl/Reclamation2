using UnityEngine;
using UnityEngine.AI;

namespace Reclamation.Neighborhood
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class CivilianRoutine : MonoBehaviour
    {
        [SerializeField] private string displayName;
        [SerializeField] private NeighborhoodClock clock;
        [SerializeField] private Transform home;
        [SerializeField] private Transform cafe;
        [SerializeField] private Transform park;
        [SerializeField] private int staggerMinutes;
        private NavMeshAgent agent;
        private NavMeshPath path;
        private Transform activeTarget;
        private bool travelling;
        private float retryAt;
        private float lastProgressAt;
        private Vector3 lastPosition;
        private Vector3 goal;
        private Vector3 requestedPosition;

        public string DisplayName => displayName;
        public string Status { get; private set; } = "Settling in";
        public RoutineDestination Destination { get; private set; }
        public bool AtDestination { get; private set; }
        public NeighborhoodClock Clock => clock;

        public void Configure(string name, NeighborhoodClock timer, Transform residence,
            Transform cafeSeat, Transform parkSeat, int stagger)
        {
            displayName = name; clock = timer; home = residence;
            cafe = cafeSeat; park = parkSeat; staggerMinutes = stagger;
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            path = new NavMeshPath();
        }

        private void Update()
        {
            if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) { Status = "Waiting for navigation"; return; }
            if (clock == null || !clock.isActiveAndEnabled)
            {
                agent.isStopped = true;
                Status = "Waiting for clock";
                return;
            }
            agent.isStopped = clock.Paused;
            agent.speed = 2.5f * clock.Speed;
            agent.acceleration = 16f * clock.Speed * clock.Speed;
            agent.angularSpeed = 720f * clock.Speed;
            if (clock.Paused) { lastProgressAt = Time.time; return; }

            RoutineDestination desired = DailyRoutine.Resolve(clock.MinuteOfDay, staggerMinutes);
            Transform target = desired == RoutineDestination.Home ? home :
                desired == RoutineDestination.Cafe ? cafe : park;
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                agent.ResetPath(); travelling = false; AtDestination = false;
                Status = "Destination unavailable"; return;
            }
            bool changed = target != activeTarget || (target.position - requestedPosition).sqrMagnitude > 0.25f;
            if (changed)
            {
                agent.ResetPath(); travelling = false; AtDestination = false;
                activeTarget = target; Destination = desired; retryAt = 0;
                requestedPosition = target.position;
            }
            if (!travelling && !AtDestination && Time.time >= retryAt)
            {
                retryAt = Time.time + 2;
                if (!NavMesh.SamplePosition(target.position, out NavMeshHit hit, 0.5f, agent.areaMask)
                    || !agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete
                    || !agent.SetPath(path))
                {
                    Status = $"{Destination} unreachable; retrying";
                    return;
                }
                goal = hit.position; travelling = true;
                lastPosition = transform.position; lastProgressAt = Time.time;
                Status = $"Walking to {Destination}";
            }
            if (AtDestination)
            {
                Vector3 drift = transform.position - goal; drift.y = 0;
                if (drift.magnitude > 0.7f) { AtDestination = false; retryAt = 0; }
                return;
            }
            if (!travelling || agent.pathPending) return;
            if (agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                ResetTrip("Route blocked; retrying"); return;
            }
            Vector3 delta = transform.position - goal; delta.y = 0;
            if (delta.magnitude <= 0.45f)
            {
                agent.ResetPath(); travelling = false; AtDestination = true;
                Status = Destination == RoutineDestination.Home ? "At home" :
                    Destination == RoutineDestination.Cafe ? "At the café" : "At the park";
                return;
            }
            if (Vector3.Distance(transform.position, lastPosition) > 0.1f)
            {
                lastPosition = transform.position; lastProgressAt = Time.time;
            }
            if (Time.time - lastProgressAt > 6) ResetTrip("Congestion; replanning");
        }

        private void ResetTrip(string reason)
        {
            agent.ResetPath(); travelling = false; AtDestination = false;
            retryAt = Time.time + 2; Status = reason;
        }
        private void OnDisable()
        {
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.ResetPath();
            travelling = false; AtDestination = false; retryAt = 0;
        }
    }
}
