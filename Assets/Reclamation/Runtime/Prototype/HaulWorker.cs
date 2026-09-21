using System;
using System.Collections.Generic;
using Reclamation.Outbreak;
using UnityEngine;
using UnityEngine.AI;

namespace Reclamation.Prototype
{
    [RequireComponent(typeof(NavMeshAgent), typeof(SurvivorNeeds), typeof(MedicalCondition))]
    [RequireComponent(typeof(SurvivorMorale))]
    public sealed class HaulWorker : MonoBehaviour
    {
        public enum WorkerState
        {
            Idle, MovingToResource, MovingToStockpile, WaitingForWork,
            MovingToSupplies, MovingToShelter, MovingToBuild, Building,
            MovingToMeal, Eating, MovingToBed, Sleeping,
            MovingToFarm, Harvesting, MovingFoodToStore,
            MovingToTreatment, Treating,
            MovingToRecreation, Socializing
        }

        [SerializeField] private string workerName = "Survivor";
        [SerializeField] private HaulJobBoard jobBoard;
        [SerializeField] private FoodStore foodStore;
        [SerializeField] private MedicineStore medicineStore;
        [SerializeField] private RecreationSpot recreationSpot;
        [SerializeField] private List<Bed> beds = new();
        [SerializeField, Min(0.05f)] private float arrivalDistance = 0.35f;
        [SerializeField, Min(0.05f)] private float decisionInterval = 0.5f;
        // Cargo belongs to the survivor, not the currently executing job.
        [SerializeField] private bool carrying;
        [SerializeField, Min(0)] private int carriedFood;

        private readonly Dictionary<int, float> _failedUntil = new();
        private NavMeshPath _path;
        private NavMeshAgent _agent;
        private ResourcePile _claimedSource;
        private ShelterBlueprint _shelter;
        private SurvivorNeeds _needs;
        private MedicalCondition _medical;
        private SurvivorMorale _morale;
        private Bed _bed;
        private FarmPlot _farm;
        private float _mealProgress;
        private float _treatmentProgress;
        private float _harvestProgress;
        private float _nextDecisionTime;
        private Vector3 _interactionPoint;
        private Vector3 _lastProgressPosition;
        private float _lastProgressTime;
        private int _observedPolicyRevision;

        public string WorkerName => workerName;
        public string WorkerId => $"worker:{GetInstanceID()}";
        public bool Carrying => carrying;
        public int CarriedFood => carriedFood;
        public WorkerState State { get; private set; }
        public string DecisionExplanation { get; private set; } = "Waiting for simulation";
        public float LastWinningScore { get; private set; }
        public SurvivorNeeds Needs => _needs;
        public MedicalCondition Medical => _medical;
        public SurvivorMorale Morale => _morale;
        private SettlementPolicy Policy => jobBoard == null ? null : jobBoard.Policy;
        private SettlementFocus Focus => Policy == null ? SettlementFocus.Balanced : Policy.Focus;
        private float MealThreshold => Policy == null ? SurvivorNeeds.UrgentHunger : Policy.MealThreshold;
        private float RestThreshold => Policy == null ? SurvivorNeeds.UrgentEnergy : Policy.RestThreshold;
        private float TreatmentThreshold => Policy == null ? MedicalCondition.TreatmentThreshold : Policy.TreatmentThreshold;
        private float MoraleThreshold => Policy == null ? SurvivorMorale.RecoveryThreshold : Policy.MoraleThreshold;
        private bool ShouldEat => _needs.Hunger >= MealThreshold;
        private bool ShouldSleep => _needs.Energy <= RestThreshold;
        private bool ShouldTreat => _medical.Injury >= TreatmentThreshold;
        private bool ShouldRecoverMorale => _morale.Morale <= MoraleThreshold;

        public void Configure(string displayName, HaulJobBoard board)
        {
            if (jobBoard != board) ReleaseReservations();
            workerName = displayName;
            jobBoard = board;
            _observedPolicyRevision = Policy == null ? 0 : Policy.Revision;
        }

        public void ConfigureNeeds(FoodStore store, float startingHunger = 20f, float hungerPerMinute = 12f)
        {
            foodStore = store;
            if (_needs == null) _needs = GetComponent<SurvivorNeeds>();
            _needs.Configure(startingHunger, hungerPerMinute);
        }

        public void ConfigureRest(IEnumerable<Bed> availableBeds, float startingEnergy = 100f,
            float energyDrainPerMinute = 8f, float recoveryPerSecond = 25f)
        {
            beds.Clear();
            if (availableBeds != null) beds.AddRange(availableBeds);
            if (_needs == null) _needs = GetComponent<SurvivorNeeds>();
            _needs.ConfigureEnergy(startingEnergy, energyDrainPerMinute, recoveryPerSecond);
        }

        public void ConfigureMedicine(MedicineStore store, float startingInjury = 0)
        {
            medicineStore = store;
            if (_medical == null) _medical = GetComponent<MedicalCondition>();
            if (_medical == null) _medical = gameObject.AddComponent<MedicalCondition>();
            _medical.Configure(startingInjury);
        }

        public void ConfigureMorale(RecreationSpot spot, float startingMorale = 70)
        {
            recreationSpot = spot;
            if (_morale == null) _morale = GetComponent<SurvivorMorale>();
            if (_morale == null) _morale = gameObject.AddComponent<SurvivorMorale>();
            _morale.Configure(startingMorale);
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _needs = GetComponent<SurvivorNeeds>();
            _medical = GetComponent<MedicalCondition>();
            if (_medical == null) _medical = gameObject.AddComponent<MedicalCondition>();
            _morale = GetComponent<SurvivorMorale>();
            if (_morale == null) _morale = gameObject.AddComponent<SurvivorMorale>();
            _path = new NavMeshPath();
        }

        private void OnEnable()
        {
            State = WorkerState.Idle;
            _nextDecisionTime = 0f;
            _observedPolicyRevision = Policy == null ? 0 : Policy.Revision;
        }

        private void Update()
        {
            _needs.Advance(Time.deltaTime, State == WorkerState.Sleeping);
            _morale.Advance(Time.deltaTime, _needs, _medical);
            if (jobBoard == null || jobBoard.Destination == null
                || !jobBoard.Destination.isActiveAndEnabled
                || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            {
                CancelCurrentJob("Waiting for stockpile/navigation; cargo retained");
                return;
            }

            bool alreadyHandlingNeed = State == WorkerState.MovingToMeal || State == WorkerState.Eating ||
                State == WorkerState.MovingToBed || State == WorkerState.Sleeping ||
                State == WorkerState.MovingToTreatment || State == WorkerState.Treating ||
                State == WorkerState.MovingToRecreation || State == WorkerState.Socializing;
            bool ordinaryWorkInProgress = State != WorkerState.Idle && State != WorkerState.WaitingForWork;
            if (Policy != null && Policy.Revision != _observedPolicyRevision)
            {
                _observedPolicyRevision = Policy.Revision;
                if (ordinaryWorkInProgress && !alreadyHandlingNeed)
                {
                    CancelCurrentJob($"Player changed settlement focus to {Policy.Label}; replanning");
                    return;
                }
            }
            if (ordinaryWorkInProgress && !alreadyHandlingNeed && CanStartAnyUrgentNeed())
            {
                CancelCurrentJob("An urgent personal need interrupted ordinary settlement work");
                if (TryStartUrgentNeed()) return;
            }

            if (State == WorkerState.Idle || State == WorkerState.WaitingForWork)
            {
                if (Time.time < _nextDecisionTime) return;
                _nextDecisionTime = Time.time + decisionInterval / _morale.WorkSpeedMultiplier;

                if (TryStartUrgentNeed()) return;

                if (carriedFood > 0)
                {
                    if (foodStore != null && foodStore.isActiveAndEnabled &&
                        BeginTravel(foodStore.transform.position))
                    {
                        State = WorkerState.MovingFoodToStore;
                        DecisionExplanation = $"Delivering {carriedFood} harvested food";
                    }
                    else DecisionExplanation = "Food store unreachable; keeping harvested food";
                    return;
                }

                if (carrying)
                {
                    if (Focus == SettlementFocus.BuildDefense && TryStartShelterJob()) return;
                    TryStartWoodDelivery();
                    return;
                }

                if (TryStartOrdinaryWork()) return;
                State = WorkerState.WaitingForWork;
                DecisionExplanation = $"No available {Focus} work; waiting";
                return;
            }

            if (State == WorkerState.MovingToResource &&
                (_claimedSource == null || !_claimedSource.isActiveAndEnabled))
            {
                CancelCurrentJob("Resource disappeared");
                return;
            }

            bool farmJob = State == WorkerState.MovingToFarm || State == WorkerState.Harvesting;
            if (farmJob && (_farm == null || !_farm.isActiveAndEnabled || !_farm.Mature))
            {
                CancelCurrentJob("Farm plot unavailable; reservation released");
                return;
            }

            bool mealJob = State == WorkerState.MovingToMeal || State == WorkerState.Eating;
            bool foodDelivery = State == WorkerState.MovingFoodToStore;
            if ((mealJob || foodDelivery) && (foodStore == null || !foodStore.isActiveAndEnabled))
            {
                CancelCurrentJob("Food store unavailable; meal reservation released");
                return;
            }
            bool treatmentJob = State == WorkerState.MovingToTreatment || State == WorkerState.Treating;
            if (treatmentJob && (medicineStore == null || !medicineStore.isActiveAndEnabled))
            {
                CancelCurrentJob("Medicine store unavailable; treatment reservation released");
                return;
            }
            bool recreationJob = State == WorkerState.MovingToRecreation || State == WorkerState.Socializing;
            if (recreationJob && (recreationSpot == null || !recreationSpot.isActiveAndEnabled))
            {
                CancelCurrentJob("Gathering spot unavailable; social reservation released");
                return;
            }
            bool sleepJob = State == WorkerState.MovingToBed || State == WorkerState.Sleeping;
            if (sleepJob && (_bed == null || !_bed.isActiveAndEnabled))
            {
                CancelCurrentJob("Bed unavailable; reservation released");
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
                : farmJob ? _farm.transform.position
                : mealJob ? foodStore.transform.position
                : foodDelivery ? foodStore.transform.position
                : treatmentJob ? medicineStore.transform.position
                : recreationJob ? recreationSpot.ActivityPoint(WorkerId)
                : sleepJob ? _bed.RestPoint
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
                if (workDistance.magnitude > 0.8f ||
                    !_shelter.TryWork(WorkerId, Time.deltaTime * _morale.WorkSpeedMultiplier))
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

            if (State == WorkerState.Harvesting)
            {
                _harvestProgress += Time.deltaTime * _morale.WorkSpeedMultiplier;
                DecisionExplanation = $"Harvesting food: {Mathf.Clamp01(_harvestProgress / 2f):P0}";
                if (_harvestProgress < 2f) return;
                if (!_farm.TryHarvest(WorkerId, out int servings))
                {
                    CancelCurrentJob("Harvest unavailable; replanning");
                    return;
                }
                carriedFood += servings;
                _farm = null;
                if (foodStore != null && foodStore.isActiveAndEnabled &&
                    BeginTravel(foodStore.transform.position))
                {
                    State = WorkerState.MovingFoodToStore;
                    DecisionExplanation = $"Carrying {carriedFood} food to storage";
                    return;
                }
                CancelCurrentJob("Food store unreachable; keeping harvested food");
                return;
            }

            if (State == WorkerState.Treating)
            {
                _treatmentProgress += Time.deltaTime;
                DecisionExplanation = $"Treating injury: {Mathf.Clamp01(_treatmentProgress / 3f):P0}";
                if (_treatmentProgress >= 3f)
                {
                    if (medicineStore.ConsumeReserved(WorkerId) && _medical.Treat(TreatmentThreshold))
                    {
                        Combatant fighter = GetComponent<Combatant>();
                        if (fighter != null) fighter.RestoreAfterTreatment(MedicalCondition.TreatmentRelief);
                        DecisionExplanation = "Treatment complete; returning to settlement work";
                    }
                    else DecisionExplanation = "Treatment supplies unavailable; replanning";
                    State = WorkerState.Idle;
                    _nextDecisionTime = Time.time + 0.1f;
                }
                return;
            }

            if (State == WorkerState.Socializing)
            {
                _morale.Recover(Time.deltaTime * 18f);
                DecisionExplanation = $"Recovering morale: {_morale.Morale:0}/100";
                if (_morale.Morale >= SurvivorMorale.RecoveryTarget)
                {
                    recreationSpot.Release(WorkerId);
                    State = WorkerState.Idle;
                    DecisionExplanation = "Morale recovered; returning to settlement work";
                    _nextDecisionTime = Time.time + 0.1f;
                }
                return;
            }

            if (State == WorkerState.Sleeping)
            {
                DecisionExplanation = $"Sleeping: {_needs.Energy:0}/{100}";
                if (_needs.Rested)
                {
                    _bed.Release(WorkerId); _bed = null;
                    State = WorkerState.Idle;
                    DecisionExplanation = "Rested; returning to settlement work";
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
            else if (State == WorkerState.MovingToFarm)
            {
                State = WorkerState.Harvesting;
                _harvestProgress = 0;
                DecisionExplanation = "Harvesting a mature crop";
                return;
            }
            else if (State == WorkerState.MovingFoodToStore)
            {
                foodStore.AddServings(carriedFood);
                DecisionExplanation = $"Stored {carriedFood} harvested food";
                carriedFood = 0;
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
            else if (State == WorkerState.MovingToTreatment)
            {
                State = WorkerState.Treating;
                _treatmentProgress = 0;
                DecisionExplanation = "Using one reserved medical supply";
                return;
            }
            else if (State == WorkerState.MovingToRecreation)
            {
                State = WorkerState.Socializing;
                DecisionExplanation = "Taking time to recover with the group";
                return;
            }
            else if (State == WorkerState.MovingToBed)
            {
                State = WorkerState.Sleeping;
                DecisionExplanation = "Sleeping in reserved bed";
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

        private bool TryStartUrgentNeed()
        {
            if (TryStartTreatment()) return true;
            float mealUrgency = ShouldEat ? Mathf.InverseLerp(MealThreshold, 100, _needs.Hunger) : -1;
            float sleepUrgency = ShouldSleep ? Mathf.InverseLerp(RestThreshold, 0, _needs.Energy) : -1;
            bool sleepFirst = sleepUrgency > mealUrgency;
            if (sleepFirst)
            {
                if (TryStartSleep()) return true;
                if (TryStartMeal()) return true;
            }
            else
            {
                if (TryStartMeal()) return true;
                if (TryStartSleep()) return true;
            }
            return TryStartRecreation();
        }

        private bool CanStartAnyUrgentNeed()
        {
            if (ShouldTreat && medicineStore != null && medicineStore.isActiveAndEnabled &&
                medicineStore.AvailableSupplies > 0 && CanReach(medicineStore.transform.position, out _)) return true;
            if (ShouldEat && foodStore != null && foodStore.isActiveAndEnabled &&
                foodStore.AvailableServings > 0 && CanReach(foodStore.transform.position, out _)) return true;
            if (TryFindAvailableBed(out _, false)) return true;
            return ShouldRecoverMorale && recreationSpot != null && recreationSpot.isActiveAndEnabled &&
                !recreationSpot.Full && CanReach(recreationSpot.transform.position, out _);
        }

        private bool TryStartRecreation()
        {
            if (!ShouldRecoverMorale || recreationSpot == null || !recreationSpot.isActiveAndEnabled) return false;
            if (!recreationSpot.TryReserve(WorkerId))
            {
                DecisionExplanation = "Low morale — gathering spot is full";
                return false;
            }
            if (!BeginTravel(recreationSpot.ActivityPoint(WorkerId)))
            {
                recreationSpot.Release(WorkerId);
                DecisionExplanation = "Low morale — gathering spot unreachable";
                return false;
            }
            State = WorkerState.MovingToRecreation;
            DecisionExplanation = _morale.Panicked ? "Panic interrupted ordinary work" :
                "Taking a morale break before returning to work";
            return true;
        }

        private bool TryStartTreatment()
        {
            if (!ShouldTreat || medicineStore == null || !medicineStore.isActiveAndEnabled) return false;
            if (!medicineStore.TryReserve(WorkerId))
            {
                DecisionExplanation = "Injured — no unreserved medical supplies available";
                return false;
            }
            if (!BeginTravel(medicineStore.transform.position))
            {
                medicineStore.Release(WorkerId);
                DecisionExplanation = "Injured — medicine store unreachable";
                return false;
            }
            State = WorkerState.MovingToTreatment;
            DecisionExplanation = _medical.Critical ? "Critical injury outranks ordinary work" :
                "Seeking treatment before returning to work";
            return true;
        }

        private bool TryStartMeal()
        {
            if (!ShouldEat || foodStore == null || !foodStore.isActiveAndEnabled) return false;
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

        private bool TryStartSleep()
        {
            if (!TryFindAvailableBed(out Bed bed, true))
            {
                if (ShouldSleep) DecisionExplanation = "Tired — no reachable bed available";
                return false;
            }
            _bed = bed;
            if (!BeginTravel(_bed.RestPoint))
            {
                _bed.Release(WorkerId); _bed = null;
                DecisionExplanation = "Exhausted — reserved bed became unreachable";
                return false;
            }
            State = WorkerState.MovingToBed;
            DecisionExplanation = "Fatigue is the most urgent personal need";
            return true;
        }

        private bool TryFindAvailableBed(out Bed result, bool reserve)
        {
            result = null;
            if (!ShouldSleep) return false;
            foreach (Bed bed in beds)
            {
                if (bed == null || !bed.isActiveAndEnabled || bed.Occupied || !CanReach(bed.RestPoint, out _)) continue;
                if (reserve && !bed.TryReserve(WorkerId)) continue;
                result = bed; return true;
            }
            return false;
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

        private bool TryStartFarmJob()
        {
            if (carrying || carriedFood > 0 || foodStore == null || !foodStore.isActiveAndEnabled ||
                !CanReach(foodStore.transform.position, out _)) return false;
            if (!jobBoard.TryClaimFarm(WorkerId, transform.position, out _farm,
                farm => CanReach(farm.transform.position, out _))) return false;
            if (!BeginTravel(_farm.transform.position))
            {
                CancelCurrentJob("Farm plot unreachable");
                return false;
            }
            State = WorkerState.MovingToFarm;
            DecisionExplanation = "Harvesting mature food for the settlement";
            return true;
        }

        private bool TryStartOrdinaryWork()
        {
            var jobs = new[] { SettlementJob.Food, SettlementJob.BuildDefense, SettlementJob.Supplies };
            Array.Sort(jobs, (left, right) => JobPriority(right).CompareTo(JobPriority(left)));
            foreach (SettlementJob job in jobs)
            {
                bool started = job == SettlementJob.Food ? TryStartFarmJob() :
                    job == SettlementJob.BuildDefense ? TryStartShelterJob() : TryStartWoodJob();
                if (started) return true;
            }
            return false;
        }

        private int JobPriority(SettlementJob job)
        {
            if (Policy != null) return Policy.Priority(job);
            return job == SettlementJob.Food ? 60 : job == SettlementJob.BuildDefense ? 50 : 40;
        }

        private bool TryStartWoodDelivery()
        {
            if (!BeginTravel(jobBoard.Destination.transform.position))
            {
                DecisionExplanation = "Stockpile unreachable; keeping cargo";
                return false;
            }
            State = WorkerState.MovingToStockpile;
            DecisionExplanation = "Delivering carried wood";
            return true;
        }

        private bool TryStartWoodJob()
        {
            // Do not collect wood when there is nowhere reachable to deliver it.
            if (!CanReach(jobBoard.Destination.transform.position, out _))
            {
                DecisionExplanation = "Stockpile unreachable; skipping supply work";
                return false;
            }
            bool claimed = jobBoard.TryClaimBest(WorkerId, transform.position,
                out _claimedSource, out float score, out string explanation, CanUseSource);
            LastWinningScore = score;
            DecisionExplanation = explanation;
            if (!claimed) return false;
            if (!BeginTravel(_claimedSource.transform.position))
            {
                CancelCurrentJob("Resource unreachable");
                return false;
            }
            State = WorkerState.MovingToResource;
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
            if (medicineStore != null && State != WorkerState.Treating) medicineStore.Release(WorkerId);
            if (recreationSpot != null && State != WorkerState.Socializing) recreationSpot.Release(WorkerId);
            if (_bed != null && State != WorkerState.Sleeping) _bed.Release(WorkerId);
            _shelter = null;
            _claimedSource = null;
            _farm = null;
        }

        /// <summary>Interrupt work without discarding the survivor's inventory.</summary>
        public void CancelCurrentJob(string reason)
        {
            if (_claimedSource != null) _failedUntil[_claimedSource.GetInstanceID()] = Time.time + 5f;
            ReleaseReservations();
            if (foodStore != null) foodStore.Release(WorkerId);
            if (medicineStore != null) medicineStore.Release(WorkerId);
            if (recreationSpot != null) recreationSpot.Release(WorkerId);
            if (_bed != null) { _bed.Release(WorkerId); _bed = null; }
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.ResetPath();
            State = WorkerState.WaitingForWork;
            _harvestProgress = 0;
            _treatmentProgress = 0;
            DecisionExplanation = reason;
            _nextDecisionTime = Time.time + decisionInterval;
            // Never clear cargo as a side effect of cancelling movement.
        }

        private void OnDisable() => CancelCurrentJob("Paused; cargo retained");
    }
}
