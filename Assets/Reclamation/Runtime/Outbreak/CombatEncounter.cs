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
            // Authored fight labs begin with known enemies, including previously saved labs.
            var combat = GetComponent<CombatDirector>();
            if (combat != null) combat.ConfigureAwareness(false);
            foreach (var fighter in FindObjectsByType<Combatant>(FindObjectsSortMode.None))
            {
                // Old saved labs predate the explicit persistence flag; infer their
                // authored veteran preset once, then keep all lab progression session-only.
                bool veteran = fighter.Attributes.strength >= 10;
                fighter.ConfigureProgression(false, veteran ? ProgressionRules.VeteranExperience : 0);
            }
            if (startingZombies == null) return;
            foreach (var zombie in startingZombies)
                if (zombie != null) { zombie.Expose(0, 1, 1); zombie.Simulate(2); }
        }
    }
}
