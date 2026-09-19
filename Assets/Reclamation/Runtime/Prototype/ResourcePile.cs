using UnityEngine;

namespace Reclamation.Prototype
{
    public sealed class ResourcePile : MonoBehaviour
    {
        [SerializeField, Min(0)] private int amount = 4;
        [SerializeField, Min(0)] private int policyPriority = 1;

        public int Amount => amount;
        public int PolicyPriority => policyPriority;
        public string ReservationId => $"resource:{GetInstanceID()}";

        public bool TryTakeOne()
        {
            if (amount <= 0)
            {
                return false;
            }

            amount--;
            if (amount == 0)
            {
                gameObject.SetActive(false);
            }

            return true;
        }

        public void Configure(int startingAmount, int priority = 1)
        {
            amount = Mathf.Max(0, startingAmount);
            policyPriority = Mathf.Max(0, priority);
        }
    }
}
