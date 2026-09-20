using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Reclamation.Prototype
{
    [RequireComponent(typeof(NavMeshAgent), typeof(SurvivorNeeds))]
    public sealed class HaulWorker : MonoBehaviour
    {
        public enum WorkerState
        {
            Idle, MovingToResource, MovingToStockpile, WaitingForWork,
            MovingToSupplies, MovingToShelter, MovingToBuild, Building,
            MovingToMeal, Eating
        }

        [SerializeField] private string workerName = "Survivor";
        [SerializeField] private HaulJobBoard jobBoard;
        [SerializeField] private FoodStore foodStore;
        [SerializeField, Min(0.05f)] private float arrivalDistance = 0.35f;
        [SerializeField, Min(0.05f)] private float decisionInterval = 0.5f;
        // Cargo belongs to the survivor, not the currently executing job.
        [SerializeField] private bool carrying;

        private readonly Dictionary<int, float> _failedUntil = new();
        private NavMeshPath _path;
        private NavMeshAgent _agent;
        private ResourcePile _claimedSource;
        private ShelterBlueprint _shelter;
        private SurvivorNeeds _needs;
        private float _mealProgress;
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
        public SurvivorNeeds Needs => _needs;

        public void Configure(string displayName, HaulJobBoard board)
        {
            if (jobBoard != board) ReleaseReservations();
            workerName = displayName;
            jobBoard = board;
        }

        public void ConfigureNeeds(FoodStore store, float startingHunger = 20f, float hungerPerMinute = 12f)
        {
            foodStore = store;
            if (_needs == null) _needs = GetComponent<SurvivorNeeds>();
            _needs.Configure(startingHunger, hungerPerMinute);
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _needs = GetComponent<SurvivorNeeds>();
            _path = new NavMeshPath();
        }

        private void OnEnable()
        {
            State = WorkerState.Idle;
            _nextDecisionTime = 0f;
        }

        private void Update()
        {
            _needs.Advance(Time.deltaTime);
            if (jobBoard == null || jobBoard.Destination == null
                || !jobBoard.Destination.isActiveAndEnabled
                || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            {
                CancelCurrentJob("Waiting for stockpile/navigation; cargo retained");
                return;
            }

            bool alreadyHandlingMeal = State == WorkerState.MovingToMeal || State == WorkerState.Eating;
            bool ordinaryWorkInProgress = State != WorkerState.Idle && State != WorkerState.WaitingForWork;
            if (_needs.NeedsMeal && ordinaryWorkInProgress && !alreadyHandlingMeal && foodStore != null &&
                foodStore.AvailableServings > 0 && CanReach(foodStore.transform.position, out _))
            {
                CancelCurrentJob("Urgent hunger interrupted ordinary settlement work");
                if (TryStartMeal()) return;
            }

            if (State == WorkerState.Idle || State == WorkerState.WaitingForWork)
            {
                if (Time.time < _nextDecisionTime) return;
                _nextDecisionTime = Time.time + decisionInterval;

                if (TryStartMeal()) return;

                if (TryStartShelterJob()) return;

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

            bool mealJob = State == WorkerState.MovingToMeal || State == WorkerState.Eating;
            if (mealJob && (foodStore == null || !foodStore.isActiveAndEnabled))
            {
                CancelCurrentJob("Food store unavailable; meal reservation released");
                return;
            }

            bool shelterJob = State == WorkerState.MovingToShelter
                || State == WorkerState.MovingToBuild || State == WorkerState.Building
                || State == WorkerState.MovingToSupplies;
            if (shelterJob && (_shelter == null || !_shelter.Available))
            {
                CancelCurrentJob("Blueprint unavailable; cargo retained");
                return;
            }

            Vector3 target = State == WorkerState.MovingToResource
                ? _claimedSource.transform.position
                : mealJob ? foodStore.transform.position
                : shelterJob && State != WorkerState.MovingToSupplies
                    ? _shelter.WorkPoint : jobBoard.Destination.transform.position;
            // A moving/replaced target requires a fresh path.
            Vector3 horizontal = target - _interactionPoint;
            horizontal.y = 0f;
            if (horizontal.sqrMagnitude > 1f)
            {
                CancelCurrentJob("Target moved; replanning");
                return;
            }

            if (State == WorkerState.Building)
            {
                Vector3 workDistance = transform.position - _shelter.WorkPoint;
                workDistance.y = 0;
                if (workDistance.magnitude > 0.8f || !_shelter.TryWork(WorkerId, Time.deltaTime))
                {
                    CancelCurrentJob("Construction interrupted; progress retained");
                    return;
                }
                DecisionExplanation = $"Building shelter: {_shelter.Progress:P0}";
                if (_shelter.Complete)
                {
                    ReleaseReservations();
                    State = WorkerState.Idle;
                    DecisionExplanation = "Shelter completed";
                }
                return;
            }

            if (State == WorkerState.Eating)
            {
                _mealProgress += Time.deltaTime;
                DecisionExplanation = $"Eating meal: {Mathf.Clamp01(_mealProgress / 2f):P0}";
                if (_mealProgress >= 2f)
                {
                    if (foodStore.ConsumeReserved(WorkerId))
                    {
                        _needs.Eat();
                        DecisionExplanation = "Meal complete; returning to settlement work";
                    }
                    else DecisionExplanation = "Reserved meal unavailable; replanning";
                    State = WorkerState.Idle;
                    _nextDecisionTime = Time.time + 0.1f;
                }
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
            else if (State == WorkerState.MovingToSupplies)
            {
                if (!jobBoard.Destination.TryTakeOne())
                {
                    CancelCurrentJob("Storage empty; looking for loose wood");
                    return;
                }
                carrying = true;
                if (!BeginTravel(_shelter.WorkPoint))
                {
                    CancelCurrentJob("Blueprint unreachable; cargo retained");
                    return;
                }
                State = WorkerState.MovingToShelter;
                DecisionExplanation = "Delivering stored wood to shelter";
                return;
            }
            else if (State == WorkerState.MovingToShelter)
            {
                if (!_shelter.TryDeliver(WorkerId))
                {
                    CancelCurrentJob("Delivery no longer needed; keeping wood");
                    return;
                }
                carrying = false;
                ReleaseReservations();
                DecisionExplanation = "Delivered one wood to shelter";
            }
            else if (State == WorkerState.MovingToBuild)
            {
                State = WorkerState.Building;
                DecisionExplanation = "Constructing shelter";
                return;
            }
            else if (State == WorkerState.MovingToMeal)
            {
                State = WorkerState.Eating;
                _mealProgress = 0;
                DecisionExplanation = "Eating a reserved meal";
                return;
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

        private bool TryStartMeal()
        {
            if (!_needs.NeedsMeal || foodStore == null || !foodStore.isActiveAndEnabled) return false;
            if (!foodStore.TryReserve(WorkerId))
            {
                DecisionExplanation = "Hungry — no unreserved meals available";
                return false;
            }
            if (!BeginTravel(foodStore.transform.position))
            {
                foodStore.Release(WorkerId);
                DecisionExplanation = "Hungry — food store unreachable";
                return false;
            }
            State = WorkerState.MovingToMeal;
            DecisionExplanation = "Urgent hunger outranks ordinary settlement work";
            return true;
        }

        private bool CanUseSource(ResourcePile source)
        {
            int id = source.GetInstanceID();
            if (_failedUntil.TryGetValue(id, out float retryAt) && Time.time < retryAt) return false;
            if (CanReach(source.transform.position, out _)) return true;
            _failedUntil[id] = Time.time + 5f;
            return false;
        }

        private bool TryStartShelterJob()
        {
            ShelterBlueprint site = jobBoard.Shelter;
            if (site == null || !site.Available || !CanReach(site.WorkPoint, out _)) return false;

            if (!carrying && site.TryReserveBuild(WorkerId))
            {
                _shelter = site;
                if (!BeginTravel(site.WorkPoint))
                {
                    CancelCurrentJob("Construction path unavailable");
                    return false;
                }
                State = WorkerState.MovingToBuild;
                DecisionExplanation = "Moving to build the funded shelter";
                return true;
            }

            if (!carrying && jobBoard.Destination.StoredUnits == 0) return false;
            if (!site.TryReserveDelivery(WorkerId)) return false;
            _shelter = site;
            Vector3 destination = carrying ? site.WorkPoint : jobBoard.Destination.transform.position;
            if (!BeginTravel(destination))
            {
                CancelCurrentJob("Material delivery path unavailable");
                return false;
            }
            State = carrying ? WorkerState.MovingToShelter : WorkerState.MovingToSupplies;
            DecisionExplanation = carrying ? "Supplying shelter blueprint" : "Collecting stored wood for shelter";
            return true;
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
            if (_shelter != null) _shelter.Release(WorkerId);
            if (foodStore != null && State != WorkerState.Eating) foodStore.Release(WorkerId);
            _shelter = null;
            _claimedSource = null;
        }

        /// <summary>Interrupt work without discarding the survivor's inventory.</summary>
        public void CancelCurrentJob(string reason)
        {
            if (_claimedSource != null) _failedUntil[_claimedSource.GetInstanceID()] = Time.time + 5f;
            ReleaseReservations();
            if (foodStore != null) foodStore.Release(WorkerId);
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.ResetPath();
            State = WorkerState.WaitingForWork;
            DecisionExplanation = reason;
            _nextDecisionTime = Time.time + decisionInterval;
            // Never clear cargo as a side effect of cancelling movement.
        }

        private void OnDisable() => CancelCurrentJob("Paused; cargo retained");
    }
}
