using Reclamation.Simulation;
using UnityEngine;
using UnityEngine.AI;

namespace Reclamation.Outbreak
{
    public sealed class PerimeterDefense : MonoBehaviour
    {
        [SerializeField] private DefenseSection[] sections;
        private readonly ResourceStack supply = new(36);
        private SafeZone zone;
        private DefenseSection job;
        private OutbreakAgent worker;
        private bool repair;
        private int reserved;
        private float work;
        public int Wood => supply.Amount;
        public int ReservedWood => reserved;
        public int SpentWood { get; private set; }
        public string Status { get; private set; } = "Reserve a survivor in the refuge, then build sections.";
        public DefenseSection[] Sections => sections;
        public bool HasJob => job != null;

        public void Configure(SafeZone refuge, DefenseSection[] perimeter)
        {
            zone = refuge; sections = perimeter;
        }
        private void Awake() { if (zone == null) zone = GetComponentInParent<SafeZone>(); }

        public bool RequestWork(bool repairDamage)
        {
            if (job != null || sections == null) return false;
            foreach (var section in sections)
            {
                if (section == null || (repairDamage ? !section.Built || section.Health >= DefenseSection.MaximumHealth : section.Built)) continue;
                int cost = repairDamage ? 2 : 4;
                if (Wood < cost) { Status = "Not enough defense wood."; return false; }
                for (int i = 0; i < cost; i++) supply.TryTakeOne();
                reserved = cost; job = section; repair = repairDamage; work = 0;
                Status = $"Queued {(repair ? "repair" : "build")}: {job.name}";
                return true;
            }
            Status = repairDamage ? "No damaged standing section." : "All sections built.";
            return false;
        }

        public void CancelWork()
        {
            if (reserved > 0) supply.Deposit(reserved);
            reserved = 0; job = null; work = 0;
            if (worker != null) worker.EndDefenseWork();
            worker = null; Status = "Work cancelled; reserved wood refunded.";
        }

        public bool IsWorker(OutbreakAgent person) => worker == person && job != null;

        public void Tick(OutbreakAgent[] population, float speed, bool paused)
        {
            if (paused || job == null) return;
            if (repair && !job.Built) { CancelWork(); Status = "Section destroyed; repair refunded. Rebuild required."; return; }
            if (!Eligible(worker))
            {
                if (worker != null) worker.EndDefenseWork();
                worker = null;
                foreach (var person in population)
                    if (Eligible(person) && person.CanReachPoint(job.InsidePoint)) { worker = person; break; }
            }
            if (worker == null) { Status = "Waiting for an available sheltered worker with a complete route."; return; }
            foreach (var threat in population)
                if (threat != null && threat.Contagious && threat.State == InfectionState.Turned &&
                    Vector3.Distance(worker.transform.position, threat.transform.position) < 6 && worker.CanReachPoint(threat.FeetPosition))
                {
                    worker.EndDefenseWork(); worker = null; Status = "Work paused: reachable zombie nearby."; return;
                }
            if (!worker.WorkAtDefense(job.InsidePoint, speed)) { Status = $"Worker approaching {job.name}"; return; }
            work += Time.deltaTime * speed;
            Status = $"{(repair ? "Repairing" : "Building")} {job.name}: {Mathf.Min(100, work / 5 * 100):0}%";
            if (work < 5) return;
            // Avoid materializing a barrier through a passing person (or zombie).
            if (!repair && !job.CanCloseSafely(population)) { Status = "Waiting for section footprint to clear."; return; }
            if (repair) job.Repair(); else job.FinishBuild();
            SpentWood += reserved; reserved = 0; job = null;
            worker.EndDefenseWork(); worker = null; Status = "Work complete.";
        }

        private bool Eligible(OutbreakAgent person) => person != null && person.isActiveAndEnabled &&
            !person.Isolated && !person.VisibleSymptoms && person.State != InfectionState.Neutralized &&
            zone != null && zone.GetAssignment(person) == RefugeAssignment.Sheltered;

        public bool TrySiege(OutbreakAgent attacker, float speed)
        {
            if (sections == null) return false;
            DefenseSection best = null; Vector3 point = default; float score = float.MaxValue;
            foreach (var section in sections)
            {
                if (section == null || !section.Blocking || DefenseRules.Damage(attacker.Class, section.Material) <= 0) continue;
                foreach (Vector3 candidate in new[] { section.OutsidePoint, section.InsidePoint })
                {
                    float distance = Vector3.Distance(attacker.FeetPosition, candidate);
                    if (distance >= score || !attacker.CanReachPoint(candidate)) continue;
                    best = section; point = candidate; score = distance;
                }
            }
            if (best == null) return false;
            return attacker.AttackDefense(best, point, speed);
        }

        public void ToggleGate(OutbreakAgent[] population)
        {
            if (sections == null) return;
            foreach (var section in sections)
                if (section != null && section.IsGate && section.Built)
                {
                    if (section.Open && !section.CanCloseSafely(population)) { Status = "Gate occupied; wait before closing."; return; }
                    section.ToggleGate(); Status = section.Open ? "Gate open: anyone can enter." : "Gate closed: arrivals must wait.";
                    return;
                }
            Status = "Build the gate first.";
        }

        public bool BlocksContact(Vector3 a, Vector3 b)
        {
            if (sections == null) return false;
            foreach (var section in sections)
                if (section != null && section.Blocking && section.GetComponent<BoxCollider>().Raycast(
                    new Ray(a, b - a), out _, Vector3.Distance(a, b))) return true;
            return false;
        }
    }
}
