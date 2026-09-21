using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Prototype
{
    public sealed class MedicineStore : MonoBehaviour
    {
        [SerializeField, Min(0)] private int supplies = 4;
        private readonly HashSet<string> reservations = new();

        public int Supplies => supplies;
        public int ReservedSupplies => reservations.Count;
        public int AvailableSupplies => Mathf.Max(0, supplies - reservations.Count);

        public void Configure(int startingSupplies)
        {
            supplies = Mathf.Max(0, startingSupplies);
            reservations.Clear();
        }

        public bool TryReserve(string survivorId)
        {
            if (string.IsNullOrEmpty(survivorId)) return false;
            if (reservations.Contains(survivorId)) return true;
            if (AvailableSupplies <= 0) return false;
            reservations.Add(survivorId);
            return true;
        }

        public bool ConsumeReserved(string survivorId)
        {
            if (!reservations.Remove(survivorId) || supplies <= 0) return false;
            supplies--;
            return true;
        }

        public void Release(string survivorId) => reservations.Remove(survivorId);
    }
}
