using UnityEngine;

namespace Reclamation.Prototype
{
    public sealed class Stockpile : MonoBehaviour
    {
        [SerializeField, Min(0)] private int storedUnits;

        public int StoredUnits => storedUnits;

        public bool TryTakeOne()
        {
            if (storedUnits <= 0) return false;
            storedUnits--;
            return true;
        }

        public void DepositOne()
        {
            storedUnits++;
        }
    }
}
