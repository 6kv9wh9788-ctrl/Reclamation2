using UnityEngine;

namespace Reclamation.Prototype
{
    public sealed class SurvivorNeeds : MonoBehaviour
    {
        public const float UrgentHunger = 70f;
        public const float UrgentEnergy = 30f;
        public const float WakeEnergy = 85f;
        [SerializeField, Range(0, 100)] private float hunger = 20f;
        [SerializeField, Min(0)] private float hungerPerMinute = 12f;
        [SerializeField, Range(0, 100)] private float energy = 100f;
        [SerializeField, Min(0)] private float energyDrainPerMinute = 8f;
        [SerializeField, Min(0)] private float sleepRecoveryPerSecond = 25f;

        public float Hunger => hunger;
        public float Energy => energy;
        public bool NeedsMeal => hunger >= UrgentHunger;
        public bool NeedsSleep => energy <= UrgentEnergy;
        public bool Rested => energy >= WakeEnergy;
        public float MealUrgency => NeedsMeal ? Mathf.InverseLerp(UrgentHunger, 100, hunger) : -1;
        public float SleepUrgency => NeedsSleep ? Mathf.InverseLerp(UrgentEnergy, 0, energy) : -1;

        public void Configure(float startingHunger, float growthPerMinute = 12f)
        {
            hunger = Mathf.Clamp(startingHunger, 0, 100);
            hungerPerMinute = Mathf.Max(0, growthPerMinute);
        }

        public void ConfigureEnergy(float startingEnergy, float drainPerMinute = 8f,
            float recoveryPerSecond = 25f)
        {
            energy = Mathf.Clamp(startingEnergy, 0, 100);
            energyDrainPerMinute = Mathf.Max(0, drainPerMinute);
            sleepRecoveryPerSecond = Mathf.Max(0, recoveryPerSecond);
        }

        public void Advance(float seconds, bool sleeping = false)
        {
            seconds = Mathf.Max(0, seconds);
            hunger = Mathf.Clamp(hunger + seconds * hungerPerMinute / 60f, 0, 100);
            energy = Mathf.Clamp(energy + seconds * (sleeping
                ? sleepRecoveryPerSecond : -energyDrainPerMinute / 60f), 0, 100);
        }

        public void Eat(float relief = 65f) => hunger = Mathf.Clamp(hunger - Mathf.Max(0, relief), 0, 100);
    }
}
