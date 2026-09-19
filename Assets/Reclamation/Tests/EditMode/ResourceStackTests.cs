using System;
using NUnit.Framework;
using Reclamation.Simulation;

namespace Reclamation.Tests
{
    public sealed class ResourceStackTests
    {
        [Test]
        public void ResourceCannotBeTakenBelowZero()
        {
            var stack = new ResourceStack(1);

            Assert.That(stack.TryTakeOne(), Is.True);
            Assert.That(stack.TryTakeOne(), Is.False);
            Assert.That(stack.Amount, Is.Zero);
        }

        [Test]
        public void DepositRejectsNonPositiveAmounts()
        {
            var stack = new ResourceStack();

            Assert.Throws<ArgumentOutOfRangeException>(() => stack.Deposit(0));
        }
    }
}
