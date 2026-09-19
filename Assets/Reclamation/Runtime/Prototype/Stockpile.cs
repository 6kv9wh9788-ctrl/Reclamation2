using UnityEngine;

namespace Reclamation.Prototype
{
    public sealed class Stockpile : MonoBehaviour
    {
        [SerializeField, Min(0)] private int storedUnits;

        public int StoredUnits => storedUnits;

        public void DepositOne()
        {
            storedUnits++;
        }
    }
}
