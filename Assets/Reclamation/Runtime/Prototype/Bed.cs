using UnityEngine;

namespace Reclamation.Prototype
{
    public sealed class Bed : MonoBehaviour
    {
        private string occupant;

        public bool Occupied => !string.IsNullOrEmpty(occupant);
        public string Occupant => occupant;
        public Vector3 RestPoint => transform.position;

        public bool TryReserve(string survivorId)
        {
            if (string.IsNullOrEmpty(survivorId)) return false;
            if (occupant == survivorId) return true;
            if (Occupied) return false;
            occupant = survivorId;
            return true;
        }

        public void Release(string survivorId)
        {
            if (occupant == survivorId) occupant = null;
        }
    }
}
