using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Prototype
{
    public sealed class FoodStore : MonoBehaviour
    {
        [SerializeField, Min(0)] private int servings = 9;
        private readonly HashSet<string> reservations = new();

        public int Servings => servings;
        public int ReservedServings => reservations.Count;
        public int AvailableServings => Mathf.Max(0, servings - reservations.Count);

        public void Configure(int startingServings)
        {
            servings = Mathf.Max(0, startingServings);
            reservations.Clear();
        }

        public bool TryReserve(string survivorId)
        {
            if (string.IsNullOrEmpty(survivorId)) return false;
            if (reservations.Contains(survivorId)) return true;
            if (AvailableServings <= 0) return false;
            reservations.Add(survivorId);
            return true;
        }

        public bool ConsumeReserved(string survivorId)
        {
            if (!reservations.Remove(survivorId) || servings <= 0) return false;
            servings--;
            return true;
        }

        public void Release(string survivorId) => reservations.Remove(survivorId);
        public void AddServings(int amount) => servings = Mathf.Max(0, servings + amount);
    }
}
