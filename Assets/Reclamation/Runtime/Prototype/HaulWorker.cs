using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Reclamation.Prototype
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class HaulWorker : MonoBehaviour
    {
        public enum WorkerState { Idle, MovingToResource, MovingToStockpile, WaitingForWork }

        [SerializeField] private string workerName = "Survivor";
        [SerializeField] private HaulJobBoard jobBoard;
        [SerializeField, Min(0.05f)] private float arrivalDistance = 0.35f;
        [SerializeField, Min(0.05f)] private float decisionInterval = 0.5f;
        // Cargo belongs to the survivor, not the currently executing job.
        [SerializeField] private bool carrying;

        private readonly Dictionary<int, float> _failedUntil = new();
        private NavMeshPath _path;
        private NavMeshAgent _agent;
        private ResourcePile _claimedSource;
        private float _nextDecisionTime;
        private Vector3 _interactionPoint;
        private Vector3 _lastProgressPosition;
        private float _lastProgressTime;

        public string WorkerName => workerName;
        public string WorkerId => $"worker:{GetInstanceID()}";
        public bool Carrying => carrying;
        public WorkerState State { get; private set; }
        public string DecisionExplanation { get; private set; } = "Waiting for simulation";
        public float LastWinningScore { get; private set; }

        public void Configure(string displayName, HaulJobBoard board)
        {
            if (jobBoard != board) ReleaseReservations();
            workerName = displayName;
            jobBoard = board;
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _path = new NavMeshPath();
        }

        private void OnEnable()
        {
            State = WorkerState.Idle;
            _nextDecisionTime = 0f;
        }

        private void Update()
        {
            if (jobBoard == null || jobBoard.Destination == null
                || !jobBoard.Destination.isActiveAndEnabled
                || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            {
                CancelCurrentJob("Waiting for stockpile/navigation; cargo retained");
                return;
            }

            if (State == WorkerState.Idle || State == WorkerState.WaitingForWork)
            {
                if (Time.time < _nextDecisionTime) return;
                _nextDecisionTime = Time.time + decisionInterval;

                if (carrying)
                {
                    if (BeginTravel(jobBoard.Destination.transform.position))
                    {
                        State = WorkerState.MovingToStockpile;
                        DecisionExplanation = "Delivering carried wood";
                    }
                    else DecisionExplanation = "Stockpile unreachable; keeping cargo";
                    return;
                }

                // Do not collect wood when there is nowhere reachable to deliver it.
                if (!CanReach(jobBoard.Destination.transform.position, out _))
                {
                    DecisionExplanation = "Stockpile unreachable; waiting";
                    return;
                }

                bool claimed = jobBoard.TryClaimBest(WorkerId, transform.position,
                    out _claimedSource, out float score, out string explanation, CanUseSource);
                LastWinningScore = score;
                DecisionExplanation = explanation;
                if (!claimed) { State = WorkerState.WaitingForWork; return; }
                if (!BeginTravel(_claimedSource.transform.position))
                {
                    CancelCurrentJob("Resource unreachable");
                    return;
                }
                State = WorkerState.MovingToResource;
                return;
            }

            if (State == WorkerState.MovingToResource &&
                (_claimedSource == null || !_claimedSource.isActiveAndEnabled))
            {
                CancelCurrentJob("Resource disappeared");
                return;
            }

            Vector3 target = State == WorkerState.MovingToResource
                ? _claimedSource.transform.position : jobBoard.Destination.transform.position;
            // A moving/replaced target requires a fresh path.
            Vector3 horizontal = target - _interactionPoint;
            horizontal.y = 0f;
            if (horizontal.sqrMagnitude > 1f)
            {
                CancelCurrentJob("Target moved; replanning");
                return;
            }

            if (_agent.pathPending) return;
            if (_agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                CancelCurrentJob("Incomplete path; will try other work");
                return;
            }

            if (Vector3.Distance(transform.position, _lastProgressPosition) > 0.1f)
            {
                _lastProgressPosition = transform.position;
                _lastProgressTime = Time.time;
            }

            Vector3 delta = transform.position - _interactionPoint;
            delta.y = 0f;
            bool arrived = delta.magnitude <= Mathf.Max(arrivalDistance, _agent.stoppingDistance) + 0.1f;
            if (!arrived)
            {
                if (Time.time - _lastProgressTime > 8f)
                    CancelCurrentJob("No movement for 8 seconds; replanning");
                return;
            }

            _agent.ResetPath();
            if (State == WorkerState.MovingToResource)
            {
                if (!_claimedSource.TryTakeOne())
                {
                    CancelCurrentJob("Resource empty");
                    return;
                }
                carrying = true;
                ReleaseReservations();
                DecisionExplanation = "Collected one wood; planning delivery";
            }
            else
            {
                jobBoard.Destination.DepositOne();
                carrying = false;
                DecisionExplanation = "Delivery complete";
            }
            State = WorkerState.Idle;
            _nextDecisionTime = Time.time + 0.1f;
        }

        private bool CanUseSource(ResourcePile source)
        {
            int id = source.GetInstanceID();
            if (_failedUntil.TryGetValue(id, out float retryAt) && Time.time < retryAt) return false;
            if (CanReach(source.transform.position, out _)) return true;
            _failedUntil[id] = Time.time + 5f;
            return false;
        }

        private bool CanReach(Vector3 position, out Vector3 point)
        {
            point = position;
            // Small projection permits elevated marker meshes, not remote interaction.
            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, _agent.areaMask)) return false;
            point = hit.position;
            return _agent.CalculatePath(point, _path) && _path.status == NavMeshPathStatus.PathComplete;
        }

        private bool BeginTravel(Vector3 target)
        {
            if (!CanReach(target, out _interactionPoint)) return false;
            _lastProgressTime = Time.time;
            _lastProgressPosition = transform.position;
            return _agent.SetPath(_path);
        }

        private void ReleaseReservations()
        {
            // Release by owner even if Unity has already destroyed the source object.
            if (jobBoard != null) jobBoard.ReleaseAll(WorkerId);
            _claimedSource = null;
        }

        /// <summary>Interrupt work without discarding the survivor's inventory.</summary>
        public void CancelCurrentJob(string reason)
        {
            if (_claimedSource != null) _failedUntil[_claimedSource.GetInstanceID()] = Time.time + 5f;
            ReleaseReservations();
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.ResetPath();
            State = WorkerState.WaitingForWork;
            DecisionExplanation = reason;
            _nextDecisionTime = Time.time + decisionInterval;
            // Never clear cargo as a side effect of cancelling movement.
        }

        private void OnDisable() => CancelCurrentJob("Paused; cargo retained");
    }
}
