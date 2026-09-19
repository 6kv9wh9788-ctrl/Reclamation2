using UnityEngine;
using UnityEngine.AI;

namespace Reclamation.Prototype
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class HaulWorker : MonoBehaviour
    {
        public enum WorkerState
        {
            Idle,
            MovingToResource,
            MovingToStockpile,
            WaitingForWork
        }

        [SerializeField] private string workerName = "Survivor";
        [SerializeField] private HaulJobBoard jobBoard;
        [SerializeField, Min(0.05f)] private float arrivalDistance = 0.35f;
        [SerializeField, Min(0.05f)] private float decisionInterval = 0.5f;

        private NavMeshAgent _agent;
        private ResourcePile _claimedSource;
        private float _nextDecisionTime;
        private bool _carrying;

        public string WorkerName => workerName;
        public string WorkerId => $"worker:{GetInstanceID()}";
        public WorkerState State { get; private set; }
        public string DecisionExplanation { get; private set; } = "Waiting for simulation";
        public float LastWinningScore { get; private set; }

        public void Configure(string displayName, HaulJobBoard board)
        {
            workerName = displayName;
            jobBoard = board;
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            State = WorkerState.Idle;
            _nextDecisionTime = Time.time;
        }

        private void Update()
        {
            if (jobBoard == null || jobBoard.Destination == null || !_agent.isOnNavMesh)
            {
                State = WorkerState.WaitingForWork;
                DecisionExplanation = "Missing job board, stockpile, or NavMesh";
                return;
            }

            switch (State)
            {
                case WorkerState.Idle:
                case WorkerState.WaitingForWork:
                    TryChooseWork();
                    break;
                case WorkerState.MovingToResource:
                    UpdateResourceTravel();
                    break;
                case WorkerState.MovingToStockpile:
                    UpdateStockpileTravel();
                    break;
            }
        }

        private void TryChooseWork()
        {
            if (Time.time < _nextDecisionTime)
            {
                return;
            }

            _nextDecisionTime = Time.time + decisionInterval;
            if (!jobBoard.TryClaimBest(
                    WorkerId,
                    transform.position,
                    out _claimedSource,
                    out float score,
                    out string explanation))
            {
                State = WorkerState.WaitingForWork;
                LastWinningScore = 0f;
                DecisionExplanation = explanation;
                return;
            }

            LastWinningScore = score;
            DecisionExplanation = explanation;
            State = WorkerState.MovingToResource;
            if (!_agent.SetDestination(_claimedSource.transform.position))
            {
                CancelCurrentJob("Could not calculate a path to resource");
            }
        }

        private void UpdateResourceTravel()
        {
            if (_claimedSource == null || !_claimedSource.isActiveAndEnabled)
            {
                CancelCurrentJob("Reserved resource disappeared");
                return;
            }

            if (HasInvalidPath())
            {
                CancelCurrentJob("Resource path became invalid");
                return;
            }

            if (!HasArrived())
            {
                return;
            }

            if (!_claimedSource.TryTakeOne())
            {
                CancelCurrentJob("Reserved resource was empty");
                return;
            }

            jobBoard.Release(_claimedSource, WorkerId);
            _claimedSource = null;
            _carrying = true;
            State = WorkerState.MovingToStockpile;
            DecisionExplanation = "Carrying one wood unit to stockpile";

            if (!_agent.SetDestination(jobBoard.Destination.transform.position))
            {
                CancelCurrentJob("Could not calculate a path to stockpile");
            }
        }

        private void UpdateStockpileTravel()
        {
            if (HasInvalidPath())
            {
                CancelCurrentJob("Stockpile path became invalid");
                return;
            }

            if (!HasArrived())
            {
                return;
            }

            if (_carrying)
            {
                jobBoard.Destination.DepositOne();
                _carrying = false;
            }

            State = WorkerState.Idle;
            DecisionExplanation = "Delivery complete; reconsidering work";
            _nextDecisionTime = Time.time + 0.1f;
        }

        private bool HasArrived()
        {
            return !_agent.pathPending
                && _agent.remainingDistance <= Mathf.Max(arrivalDistance, _agent.stoppingDistance);
        }

        private bool HasInvalidPath()
        {
            return !_agent.pathPending && _agent.pathStatus == NavMeshPathStatus.PathInvalid;
        }

        private void CancelCurrentJob(string reason)
        {
            jobBoard?.Release(_claimedSource, WorkerId);
            _claimedSource = null;
            _carrying = false;
            _agent.ResetPath();
            State = WorkerState.WaitingForWork;
            DecisionExplanation = reason;
            _nextDecisionTime = Time.time + decisionInterval;
        }

        private void OnDisable()
        {
            jobBoard?.ReleaseAll(WorkerId);
        }
    }
}
