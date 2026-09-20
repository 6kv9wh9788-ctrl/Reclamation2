using UnityEngine;
using UnityEngine.AI;

namespace Reclamation.Outbreak
{
    [RequireComponent(typeof(BoxCollider), typeof(NavMeshObstacle))]
    public sealed class DefenseSection : MonoBehaviour
    {
        [SerializeField] private DefenseMaterial material;
        [SerializeField] private bool gate;
        [SerializeField] private Vector3 outward = Vector3.forward;
        [SerializeField] private Transform visual;
        [SerializeField] private Vector3 size = new Vector3(3, 2.4f, 0.5f);
        private BoxCollider box;
        private NavMeshObstacle obstacle;
        public const float MaximumHealth = 120;
        public float Health { get; private set; }
        public bool Built => Health > 0;
        public bool IsGate => gate;
        public bool Open { get; private set; }
        public bool Blocking => Built && !Open && isActiveAndEnabled;
        public DefenseMaterial Material => material;
        public Vector3 InsidePoint => transform.position - outward * 1.6f;
        public Vector3 OutsidePoint => transform.position + outward * 1.6f;

        public void Configure(Vector3 dimensions, Vector3 normal, bool isGate, Transform mesh)
        {
            size = dimensions; outward = normal.normalized; gate = isGate; visual = mesh;
        }

        private void Awake()
        {
            box = GetComponent<BoxCollider>(); obstacle = GetComponent<NavMeshObstacle>();
            box.size = size; box.center = Vector3.up * size.y / 2;
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = size; obstacle.center = box.center;
            obstacle.carving = true; obstacle.carveOnlyStationary = false;
            Refresh();
        }

        public void FinishBuild()
        {
            Health = MaximumHealth; Open = gate; Refresh();
        }

        private void OnEnable() => Refresh();
        private void OnDisable() => Refresh();

        public void Repair()
        {
            if (Built) Health = MaximumHealth;
        }

        public void ToggleGate()
        {
            if (!gate || !Built) return;
            Open = !Open; Refresh();
        }

        public void TakeDamage(ZombieClass attacker, float seconds)
        {
            if (!Blocking || !(seconds > 0) || float.IsInfinity(seconds)) return;
            Health = Mathf.Max(0, Health - DefenseRules.Damage(attacker, material) * seconds);
            if (!Built) { Open = false; Refresh(); }
        }

        public bool CanCloseSafely(OutbreakAgent[] people)
        {
            var volume = new Bounds(transform.position + Vector3.up * size.y / 2, size + Vector3.one * 1.3f);
            foreach (var person in people)
                if (person != null && person.gameObject.activeInHierarchy && volume.Contains(person.transform.position)) return false;
            return true;
        }

        private void Refresh()
        {
            if (box == null) return;
            box.enabled = Blocking; obstacle.enabled = Blocking;
            if (visual != null)
            {
                visual.localScale = new Vector3(size.x, Blocking ? size.y : 0.08f, size.z);
                visual.localPosition = Vector3.up * (Blocking ? size.y / 2 : 0.04f);
            }
        }
    }
}
