using UnityEngine;

namespace Reclamation.Outbreak
{
    // Explicit authored starting enemies, never spawned in response to construction.
    public sealed class CombatEncounter : MonoBehaviour
    {
        [SerializeField] private OutbreakAgent[] startingZombies;
        public void Configure(OutbreakAgent[] zombies) => startingZombies = zombies;
        private void Start()
        {
            if (startingZombies == null) return;
            foreach (var zombie in startingZombies)
                if (zombie != null) { zombie.Expose(0, 1, 1); zombie.Simulate(2); }
        }
    }
}
