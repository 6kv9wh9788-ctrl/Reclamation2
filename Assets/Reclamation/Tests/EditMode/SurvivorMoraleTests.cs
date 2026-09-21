using NUnit.Framework;
using Reclamation.Prototype;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class SurvivorMoraleTests
    {
        private GameObject survivor;
        private GameObject recreationObject;

        [TearDown]
        public void Cleanup()
        {
            if (survivor != null) Object.DestroyImmediate(survivor);
            if (recreationObject != null) Object.DestroyImmediate(recreationObject);
        }

        [Test]
        public void UnmetNeedsAndInjuryApplyDeterministicMoralePressure()
        {
            survivor = new GameObject("survivor");
            var needs = survivor.AddComponent<SurvivorNeeds>();
            needs.Configure(75, 0); needs.ConfigureEnergy(20, 0, 25);
            var medical = survivor.AddComponent<MedicalCondition>(); medical.Configure(50);
            var morale = survivor.AddComponent<SurvivorMorale>(); morale.Configure(70);

            morale.Advance(60, needs, medical);

            Assert.That(morale.Morale, Is.EqualTo(40).Within(0.001f));
            Assert.That(morale.Panicked, Is.False);
            morale.ApplyEvent(-25);
            Assert.That(morale.Panicked, Is.True);
            Assert.That(morale.WorkSpeedMultiplier, Is.EqualTo(0.65f));
        }

        [Test]
        public void RecreationCapacityIsExclusiveAndReleasable()
        {
            recreationObject = new GameObject("gathering spot");
            var spot = recreationObject.AddComponent<RecreationSpot>(); spot.Configure(2);
            Assert.That(spot.TryReserve("A"), Is.True);
            Assert.That(spot.TryReserve("B"), Is.True);
            Assert.That(spot.TryReserve("C"), Is.False);
            spot.Release("A");
            Assert.That(spot.TryReserve("C"), Is.True);
            Assert.That(spot.Occupancy, Is.EqualTo(2));
        }
    }
}
