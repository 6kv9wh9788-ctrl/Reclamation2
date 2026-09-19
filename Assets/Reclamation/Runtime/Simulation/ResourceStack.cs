using System;

namespace Reclamation.Simulation
{
    public sealed class ResourceStack
    {
        public ResourceStack(int initialAmount = 0)
        {
            if (initialAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialAmount));
            }

            Amount = initialAmount;
        }

        public int Amount { get; private set; }

        public bool TryTakeOne()
        {
            if (Amount <= 0)
            {
                return false;
            }

            Amount--;
            return true;
        }

        public void Deposit(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Amount += amount;
        }
    }
}
