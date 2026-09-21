using UnityEngine;

namespace Reclamation.Prototype
{
    public sealed class FarmPlot : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] private float growth;
        [SerializeField, Min(0)] private float growthPerMinute = 0.25f;
        [SerializeField, Min(1)] private int harvestServings = 3;
        private string reservedBy;

        public float Growth => growth;
        public bool Mature => growth >= 1f;
        public bool Reserved => !string.IsNullOrEmpty(reservedBy);
        public string Status => Mature ? Reserved ? "Harvest reserved" : "Ready to harvest" : $"Growing {growth:P0}";

        public void Configure(float startingGrowth, float minutesToMature = 4f, int servings = 3)
        {
            growth = Mathf.Clamp01(startingGrowth);
            growthPerMinute = minutesToMature > 0 ? 1f / minutesToMature : 0;
            harvestServings = Mathf.Max(1, servings);
            reservedBy = null;
        }

        private void Update() => Advance(Time.deltaTime);

        public void Advance(float seconds)
        {
            if (Mature || !(seconds > 0)) return;
            growth = Mathf.Clamp01(growth + seconds * growthPerMinute / 60f);
        }

        public bool TryReserve(string workerId)
        {
            if (!Mature || string.IsNullOrEmpty(workerId)) return false;
            if (reservedBy == workerId) return true;
            if (Reserved) return false;
            reservedBy = workerId; return true;
        }

        public bool TryHarvest(string workerId, out int servings)
        {
            servings = 0;
            if (!Mature || reservedBy != workerId) return false;
            servings = harvestServings;
            growth = 0; reservedBy = null;
            return true;
        }

        public void Release(string workerId)
        {
            if (reservedBy == workerId) reservedBy = null;
        }
    }
}
