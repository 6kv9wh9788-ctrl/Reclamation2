using UnityEngine;

namespace Reclamation.Prototype
{
    public sealed class SurvivorNeeds : MonoBehaviour
    {
        public const float UrgentHunger = 70f;
        [SerializeField, Range(0, 100)] private float hunger = 20f;
        [SerializeField, Min(0)] private float hungerPerMinute = 12f;

        public float Hunger => hunger;
        public bool NeedsMeal => hunger >= UrgentHunger;

        public void Configure(float startingHunger, float growthPerMinute = 12f)
        {
            hunger = Mathf.Clamp(startingHunger, 0, 100);
            hungerPerMinute = Mathf.Max(0, growthPerMinute);
        }

        public void Advance(float seconds)
        {
            hunger = Mathf.Clamp(hunger + Mathf.Max(0, seconds) * hungerPerMinute / 60f, 0, 100);
        }

        public void Eat(float relief = 65f) => hunger = Mathf.Clamp(hunger - Mathf.Max(0, relief), 0, 100);
    }
}
