using NUnit.Framework;
using Reclamation.AI;

namespace Reclamation.Tests
{
    public sealed class ReservationRegistryTests
    {
        [Test]
        public void TargetCannotBeReservedByTwoWorkers()
        {
            var registry = new ReservationRegistry();

            Assert.That(registry.TryReserve("wood-1", "avery"), Is.True);
            Assert.That(registry.TryReserve("wood-1", "morgan"), Is.False);
            Assert.That(registry.IsReservedByOther("wood-1", "morgan"), Is.True);
        }

        [Test]
        public void OnlyOwnerCanReleaseReservation()
        {
            var registry = new ReservationRegistry();
            registry.TryReserve("wood-1", "avery");

            Assert.That(registry.Release("wood-1", "morgan"), Is.False);
            Assert.That(registry.Release("wood-1", "avery"), Is.True);
            Assert.That(registry.Count, Is.Zero);
        }

        [Test]
        public void ReleaseAllClearsOnlyOwnersReservations()
        {
            var registry = new ReservationRegistry();
            registry.TryReserve("wood-1", "avery");
            registry.TryReserve("wood-2", "avery");
            registry.TryReserve("wood-3", "morgan");

            registry.ReleaseAll("avery");

            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.IsReservedByOther("wood-3", "avery"), Is.True);
        }
    }
}
