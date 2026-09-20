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
        [SerializeField] private ZombieClass zombieClass;
        private bool sieging;
        private bool defenseWorking;
        private float nextDefenseRouteAt;
        private double withdrawalMinute = double.PositiveInfinity;
        private float withdrawalReplan;
        public bool IsWithdrawing { get; private set; }
        public ZombieClass Class => zombieClass;
        public Vector3 FeetPosition => transform.position - Vector3.up * (nav != null ? nav.baseOffset : 0);
        public void SetZombieClass(ZombieClass value) => zombieClass = value;
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
        private readonly BurstStamina humanStamina = new(4, 12);
        private readonly BurstStamina zombieStamina = new(3, 8);
        private bool wantsBurst;
        private SafeZone safeZone;
        private RefugeAssignment refugeAssignment;
        private BurstStamina Stamina => State == InfectionState.Turned ? zombieStamina : humanStamina;
        public float StaminaFraction => Stamina.Fraction;
        public bool IsSprinting => Stamina.Bursting;
        public string ExertionStatus => State == InfectionState.Neutralized ? "" :
            $"{(IsSprinting ? State == InfectionState.Turned ? "Burst" : "Sprint" : StaminaFraction < 1 ? "Recovering" : "Ready")} {StaminaFraction * 100:0}%";
        private float PursuitSpeed => State == InfectionState.Turned
            ? (IsSprinting ? 6.2f : 3.6f) : (IsSprinting ? 5f : 2.8f);
        public bool IsFleeing => fleeing;
        public RefugeAssignment ZoneAssignment => refugeAssignment;
        public bool IsProtected => (refugeAssignment == RefugeAssignment.Sheltered &&
            (safeZone == null || safeZone.Perimeter == null)) ||
            refugeAssignment == RefugeAssignment.Quarantined;
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
            State == InfectionState.Turned ? zombieClass == ZombieClass.Brute ? "BRUTE" : "TURNED" :
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
            if (changed) { withdrawalMinute = now + incubation * 0.55; ApplyState(); }
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

        public void SetThreatTarget(OutbreakAgent nextTarget, float simulationSpeed, PerimeterDefense perimeter = null)
        {
            if (State != InfectionState.Turned || isolated || !CanNavigate || simulationPaused) return;
            if (nextTarget == null || !nextTarget.gameObject.activeInHierarchy || nextTarget.Isolated ||
                nextTarget.State == InfectionState.Turned || nextTarget.State == InfectionState.Neutralized)
            {
                target = null; wantsBurst = false; sieging = false; nav.ResetPath(); MovementStatus = "No eligible prey"; return;
            }
            bool changed = target != nextTarget;
            target = nextTarget;
            // Save the burst for close range, where it can sustain contact instead of
            // spending the whole charge approaching a sprinting civilian.
            wantsBurst = Vector3.Distance(transform.position, target.transform.position) <= 3;
            SetMovementSpeed(PursuitSpeed, simulationSpeed);
            // Stop inside transmission range without steering into the target's center.
            nav.stoppingDistance = 0.95f;
            if (!changed && Time.time < nextRouteAt)
            {
                if (sieging && perimeter != null) perimeter.TrySiege(this, simulationSpeed);
                return;
            }
            nextRouteAt = Time.time + 0.35f / Mathf.Max(1, simulationSpeed);
            Vector3 targetFeet = target.transform.position - Vector3.up * target.nav.baseOffset;
            if (NavMesh.SamplePosition(targetFeet, out NavMeshHit hit, 0.65f, nav.areaMask)
                && nav.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete
                && nav.SetPath(path)) { sieging = false; MovementStatus = "Pursuing"; }
            else
            {
                if (!sieging) { nav.ResetPath(); nextDefenseRouteAt = 0; }
                sieging = perimeter != null; MovementStatus = "Prey unreachable; seeking breach";
                if (!sieging || !perimeter.TrySiege(this, simulationSpeed)) nav.ResetPath();
            }
        }

        public bool CanReachPoint(Vector3 point)
        {
            return CanNavigate && NavMesh.SamplePosition(point, out NavMeshHit hit, 0.65f, nav.areaMask) &&
                nav.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        public bool ShouldWithdraw(double minute) => !isolated && refugeAssignment == RefugeAssignment.None &&
            ((State == InfectionState.Exposed && minute >= withdrawalMinute) || State == InfectionState.Symptomatic);

        public bool TryWithdraw(double minute, float seconds, float speed, IReadOnlyList<OutbreakAgent> population)
        {
            if (!ShouldWithdraw(minute) || simulationPaused || !CanNavigate || !(seconds > 0)) return false;
            if (!IsWithdrawing) { withdrawalReplan = 0; IsWithdrawing = true; }
            routine.enabled = false; fleeing = false; wantsBurst = false;
            SetMovementSpeed(State == InfectionState.Symptomatic ? 1.5f : 2.1f, speed);
            nav.stoppingDistance = 0.35f;
            withdrawalReplan -= seconds;
            MovementStatus = nav.hasPath ? "Seeking somewhere quiet" : "Keeping to themselves";
            if (withdrawalReplan > 0) return true;
            withdrawalReplan = 3;
            Vector3 origin = FeetPosition;
            Vector3 best = origin; float bestScore = QuietScore(origin, population);
            // Prefer low crowd density over travel distance, using reachable ground only.
            for (int ring = 1; ring <= 3; ring++)
                for (int i = 0; i < 12; i++)
                {
                    float angle = i * Mathf.PI / 6;
                    Vector3 candidate = origin + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (ring * 4);
                    if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 0.65f, nav.areaMask)) continue;
                    float score = QuietScore(hit.position, population) - Vector3.Distance(origin, hit.position) * 0.08f;
                    if (score <= bestScore + 0.1f || !CanReachPoint(hit.position)) continue;
                    best = hit.position; bestScore = score;
                }
            if (Vector3.Distance(origin, best) > 0.5f && CanReachPoint(best))
            { nav.SetPath(path); MovementStatus = "Seeking somewhere quiet"; }
            else { nav.ResetPath(); MovementStatus = "Keeping to themselves"; }
            return true;
        }

        private float QuietScore(Vector3 point, IReadOnlyList<OutbreakAgent> population)
        {
            float score = 0;
            foreach (var other in population)
                if (other != null && other != this && other.isActiveAndEnabled && other.State != InfectionState.Neutralized)
                    score -= Mathf.Max(0, 8 - Vector3.Distance(point, other.FeetPosition));
            return score;
        }

        public bool ClearCombatLine(Vector3 point)
        {
            return CanNavigate && NavMesh.SamplePosition(point, out NavMeshHit hit, 0.3f, nav.areaMask) &&
                !nav.Raycast(hit.position, out _);
        }

        public void CombatStop(string status, Transform face)
        {
            if (simulationPaused || !CanNavigate) return;
            routine.enabled = false; fleeing = false; wantsBurst = false;
            nav.ResetPath(); MovementStatus = status;
            if (face != null)
            {
                Vector3 direction = face.position - transform.position; direction.y = 0;
                if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        public bool CombatMove(Vector3 point, float multiplier, float metresPerSecond, float stoppingDistance, string status)
        {
            if (simulationPaused || !CanNavigate || !CanFlee || !CanReachPoint(point)) return false;
            routine.enabled = false; fleeing = false; wantsBurst = false;
            SetMovementSpeed(metresPerSecond, multiplier); nav.stoppingDistance = stoppingDistance;
            MovementStatus = status; return nav.SetPath(path);
        }

        public void CombatPush(Vector3 displacement)
        {
            if (simulationPaused || !CanNavigate || isolated || State == InfectionState.Neutralized) return;
            routine.enabled = false; fleeing = false; wantsBurst = false; IsWithdrawing = false;
            nav.ResetPath(); displacement.y = 0;
            Vector3 origin = FeetPosition;
            // Move along the actual navigation segment, never warp through a wall or
            // take a complete detour as permission to push through an obstacle.
            if (nav.Raycast(origin + displacement, out NavMeshHit hit))
            {
                Vector3 allowed = hit.position - origin; allowed.y = 0;
                displacement = allowed.normalized * Mathf.Max(0, allowed.magnitude - 0.05f);
            }
            nav.Move(displacement);
            MovementStatus = "Knocked back";
        }

        public bool WorkAtDefense(Vector3 point, float speed)
        {
            if (!CanNavigate || simulationPaused || VisibleSymptoms || isolated) return false;
            routine.enabled = false; defenseWorking = true; fleeing = false; wantsBurst = false;
            Vector3 delta = FeetPosition - point; delta.y = 0;
            if (delta.magnitude <= 0.5f)
            {
                nav.ResetPath(); MovementStatus = "Working on perimeter"; return true;
            }
            SetMovementSpeed(2.5f, speed); nav.stoppingDistance = 0.25f;
            if (Time.time >= nextDefenseRouteAt)
            {
                nextDefenseRouteAt = Time.time + 0.4f / Mathf.Max(1, speed);
                if (CanReachPoint(point)) nav.SetPath(path);
                else nav.ResetPath();
            }
            MovementStatus = "Walking to perimeter work"; return false;
        }

        public void EndDefenseWork()
        {
            defenseWorking = false; nextDefenseRouteAt = 0;
            if (CanNavigate) nav.ResetPath();
            ResumeRoutineIfSafe();
        }

        public bool AttackDefense(DefenseSection section, Vector3 point, float speed)
        {
            if (State != InfectionState.Turned || isolated || simulationPaused || !CanNavigate || !section.Blocking) return false;
            wantsBurst = false;
            Vector3 delta = FeetPosition - point; delta.y = 0;
            if (delta.magnitude <= 0.5f)
            {
                nav.ResetPath();
                section.TakeDamage(zombieClass, Time.deltaTime * speed);
                MovementStatus = $"Attacking {section.name}";
                return true;
            }
            SetMovementSpeed(3.6f, speed); nav.stoppingDistance = 0.25f;
            if (Time.time >= nextDefenseRouteAt)
            {
                nextDefenseRouteAt = Time.time + 0.4f / Mathf.Max(1, speed);
                if (CanReachPoint(point)) nav.SetPath(path);
                else nav.ResetPath();
            }
            MovementStatus = $"Approaching {section.name}";
            return true;
        }

        public void AssignSafeZone(SafeZone zone)
        {
            if (zone == null || State == InfectionState.Turned || State == InfectionState.Neutralized) return;
            safeZone = zone;
            IsWithdrawing = false;
            refugeAssignment = RefugeAssignment.Evacuating;
            if (routine.enabled) routine.enabled = false;
            nextRouteAt = 0;
            MovementStatus = "Evacuating to refuge";
        }

        public void ClearSafeZone(SafeZone zone)
        {
            if (safeZone != zone || refugeAssignment != RefugeAssignment.Evacuating) return;
            safeZone = null;
            refugeAssignment = RefugeAssignment.None;
            ResumeRoutineIfSafe();
        }

        public void ContinueEvacuation(float simulationSpeed)
        {
            if (safeZone == null || refugeAssignment != RefugeAssignment.Evacuating || !CanNavigate || simulationPaused) return;
            fleeing = false;
            if (!safeZone.TryGetDestination(this, out Vector3 destination))
            {
                nav.ResetPath();
                MovementStatus = VisibleSymptoms ? "Quarantine full" : "Refuge full";
                return;
            }
            Vector3 flat = transform.position - destination; flat.y = 0;
            if (flat.magnitude <= 0.65f)
            {
                safeZone.TryAdmit(this);
                return;
            }
            wantsBurst = false;
            SetMovementSpeed(3.1f, simulationSpeed);
            nav.stoppingDistance = 0.3f;
            if (Time.time < nextRouteAt) return;
            nextRouteAt = Time.time + 0.5f / Mathf.Max(1, simulationSpeed);
            Vector3 feet = destination;
            if (NavMesh.SamplePosition(feet, out NavMeshHit hit, 0.65f, nav.areaMask) &&
                nav.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete && nav.SetPath(path))
                MovementStatus = "Evacuating to refuge";
            else MovementStatus = "Refuge route blocked";
        }

        public void CompleteAdmission(SafeZone zone, RefugeAssignment assignment, Vector3 position)
        {
            if (safeZone != zone) return;
            refugeAssignment = assignment;
            fleeing = false;
            wantsBurst = false;
            routine.enabled = false;
            if (CanNavigate)
            {
                nav.ResetPath();
                if (NavMesh.SamplePosition(position, out NavMeshHit hit, 1, nav.areaMask)) nav.Warp(hit.position);
            }
            isolated = assignment == RefugeAssignment.Quarantined;
            MovementStatus = assignment == RefugeAssignment.Quarantined ? "Held in quarantine" : "Safe in refuge";
        }

        public void HoldInQuarantine()
        {
            isolated = true;
            if (CanNavigate) nav.ResetPath();
            MovementStatus = "Turned; contained in quarantine";
        }

        public void ReleaseFromZone(SafeZone zone, string reason)
        {
            if (safeZone != zone) return;
            safeZone = null;
            refugeAssignment = RefugeAssignment.None;
            isolated = false;
            MovementStatus = reason;
        }

        public void SetSimulationPaused(bool paused)
        {
            simulationPaused = paused;
            if (CanNavigate) nav.isStopped = paused || isolated || State == InfectionState.Symptomatic;
        }

        public void FleeFrom(Vector3 threat, float simulationSpeed, IReadOnlyList<OutbreakAgent> population = null)
        {
            if (!CanFlee || IsProtected || !CanNavigate || simulationPaused) return;
            if (!fleeing)
            {
                // OnDisable resets the routine's path; disable before issuing the escape route.
                routine.enabled = false; fleeing = true; nextRouteAt = 0;
            }
            Vector3 separation = transform.position - threat; separation.y = 0;
            wantsBurst = separation.magnitude <= 5;
            SetMovementSpeed(PursuitSpeed, simulationSpeed);
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

        public void AdvanceMovement(float simulationSeconds, float simulationSpeed)
        {
            if (simulationPaused || State == InfectionState.Neutralized || !isActiveAndEnabled) return;
            bool travelling = CanNavigate && !nav.isStopped && !nav.pathPending && nav.hasPath &&
                nav.remainingDistance > nav.stoppingDistance + 0.1f && nav.velocity.sqrMagnitude > 0.01f;
            Stamina.Advance(simulationSeconds, wantsBurst && travelling && !isolated &&
                (fleeing || State == InfectionState.Turned));
            if (travelling && (fleeing || State == InfectionState.Turned))
                SetMovementSpeed(PursuitSpeed, simulationSpeed);
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
            if (IsProtected) return;
            if (refugeAssignment == RefugeAssignment.Sheltered)
            {
                if (!defenseWorking && CanNavigate) nav.ResetPath();
                fleeing = false; wantsBurst = false; routine.enabled = false;
                MovementStatus = "Sheltered; perimeter protection only";
                return;
            }
            if (refugeAssignment == RefugeAssignment.Evacuating)
            {
                fleeing = false;
                ContinueEvacuation(1);
                return;
            }
            if (fleeing && CanNavigate) nav.ResetPath();
            fleeing = false; wantsBurst = false; nextRouteAt = 0;
            if (CanNavigate) nav.stoppingDistance = 0.25f;
            routine.enabled = true;
            MovementStatus = "Routine";
        }

        private void ApplyState()
        {
            if (isolated || State == InfectionState.Turned || State == InfectionState.Neutralized) IsWithdrawing = false;
            bool canRoutine = !isolated && (State == InfectionState.Healthy || State == InfectionState.Exposed);
            if (!canRoutine) { fleeing = false; wantsBurst = false; nextRouteAt = 0; }
            routine.enabled = canRoutine && !fleeing && refugeAssignment == RefugeAssignment.None;
            MovementStatus = isolated ? "Isolated" : State == InfectionState.Symptomatic ? "Symptomatic; stopped" : MovementStatus;
            if (!canRoutine && nav.isActiveAndEnabled && nav.isOnNavMesh) nav.ResetPath();
            if (State == InfectionState.Neutralized)
            {
                MovementStatus = "Neutralized";
                gameObject.SetActive(false);
            }
            if (State == InfectionState.Turned && zombieClass == ZombieClass.Brute)
            {
                var human = GetComponentInChildren<HumanVisual>();
                if (human != null) human.transform.localScale = new Vector3(1.5f, 1.35f, 1.5f);
            }
            if (body == null) return;
            body.material.color = State == InfectionState.Exposed ? healthyColor :
                State == InfectionState.Symptomatic ? new Color(0.85f, 0.75f, 0.15f) :
                State == InfectionState.Turned ? new Color(0.3f, 0.7f, 0.25f) :
                isolated ? new Color(0.4f, 0.75f, 1f) : healthyColor;
        }
    }
}
