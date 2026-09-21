using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Prototype
{
    public sealed class RecreationSpot : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 2;
        private readonly Dictionary<string, int> occupants = new();

        public int Capacity => capacity;
        public int Occupancy => occupants.Count;
        public bool Full => Occupancy >= capacity;
        public string Status => $"{Occupancy}/{capacity} social spaces occupied";

        public void Configure(int availableSpaces)
        {
            capacity = Mathf.Max(1, availableSpaces);
            occupants.Clear();
        }

        public bool TryReserve(string survivorId)
        {
            if (string.IsNullOrEmpty(survivorId)) return false;
            if (occupants.ContainsKey(survivorId)) return true;
            if (Full) return false;
            int slot = 0;
            while (occupants.ContainsValue(slot)) slot++;
            occupants.Add(survivorId, slot);
            return true;
        }

        public void Release(string survivorId) => occupants.Remove(survivorId);

        public Vector3 ActivityPoint(string survivorId)
        {
            if (capacity <= 1 || !occupants.TryGetValue(survivorId, out int slot)) return transform.position;
            float angle = slot * Mathf.PI * 2f / capacity;
            return transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 0.9f;
        }
    }
}
