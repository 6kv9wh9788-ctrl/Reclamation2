using NUnit.Framework;
using Reclamation.Prototype;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class FarmPlotTests
    {
        private GameObject farmObject;
        private FarmPlot farm;

        [SetUp]
        public void Setup()
        {
            farmObject = new GameObject("farm");
            farm = farmObject.AddComponent<FarmPlot>();
        }

        [TearDown]
        public void Cleanup() => Object.DestroyImmediate(farmObject);

        [Test]
        public void GrowthMaturesAtConfiguredRate()
        {
            farm.Configure(0, 1, 3);
            farm.Advance(30);
            Assert.That(farm.Growth, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(farm.Mature, Is.False);
            farm.Advance(30);
            Assert.That(farm.Mature, Is.True);
            Assert.That(farm.Status, Is.EqualTo("Ready to harvest"));
        }

        [Test]
        public void ReservedCropCanOnlyBeHarvestedByItsWorker()
        {
            farm.Configure(1, 4, 3);
            Assert.That(farm.TryReserve("Avery"), Is.True);
            Assert.That(farm.TryReserve("Morgan"), Is.False);
            Assert.That(farm.TryHarvest("Morgan", out _), Is.False);
            Assert.That(farm.TryHarvest("Avery", out int servings), Is.True);
            Assert.That(servings, Is.EqualTo(3));
            Assert.That(farm.Growth, Is.Zero);
            Assert.That(farm.Reserved, Is.False);
        }
    }
}
