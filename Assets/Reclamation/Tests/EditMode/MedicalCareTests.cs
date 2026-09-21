using NUnit.Framework;
using Reclamation.Prototype;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class MedicalCareTests
    {
        private GameObject conditionObject;
        private GameObject storeObject;

        [TearDown]
        public void Cleanup()
        {
            if (conditionObject != null) Object.DestroyImmediate(conditionObject);
            if (storeObject != null) Object.DestroyImmediate(storeObject);
        }

        [Test]
        public void TraumaPersistsUntilOneTreatmentRelievesIt()
        {
            conditionObject = new GameObject("survivor");
            var condition = conditionObject.AddComponent<MedicalCondition>();
            condition.Configure(50);
            condition.ApplyTrauma(25);
            Assert.That(condition.Injury, Is.EqualTo(75));
            Assert.That(condition.Critical, Is.True);
            Assert.That(condition.Treat(), Is.True);
            Assert.That(condition.Injury, Is.EqualTo(10));
            Assert.That(condition.NeedsTreatment, Is.False);
        }

        [Test]
        public void MedicalSupplyReservationPreventsDoubleTreatment()
        {
            storeObject = new GameObject("medicine");
            var store = storeObject.AddComponent<MedicineStore>(); store.Configure(1);
            Assert.That(store.TryReserve("Avery"), Is.True);
            Assert.That(store.TryReserve("Morgan"), Is.False);
            Assert.That(store.ConsumeReserved("Morgan"), Is.False);
            Assert.That(store.ConsumeReserved("Avery"), Is.True);
            Assert.That(store.Supplies, Is.Zero);
            Assert.That(store.ReservedSupplies, Is.Zero);
        }
    }
}
