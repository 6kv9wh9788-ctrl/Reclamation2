using NUnit.Framework;
using Reclamation.Prototype;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class SurvivorNeedsTests
    {
        [Test]
        public void HungerAdvancesDeterministicallyAndMealRelievesIt()
        {
            var go = new GameObject("survivor");
            try
            {
                var needs = go.AddComponent<SurvivorNeeds>();
                needs.Configure(64, 12);
                needs.Advance(30);
                Assert.That(needs.Hunger, Is.EqualTo(70).Within(0.001f));
                Assert.That(needs.NeedsMeal, Is.True);
                needs.Eat();
                Assert.That(needs.Hunger, Is.EqualTo(5).Within(0.001f));
                Assert.That(needs.NeedsMeal, Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void FoodReservationsPreventDoubleConsumption()
        {
            var go = new GameObject("food");
            try
            {
                var store = go.AddComponent<FoodStore>(); store.Configure(1);
                Assert.That(store.TryReserve("A"), Is.True);
                Assert.That(store.TryReserve("B"), Is.False);
                Assert.That(store.Servings, Is.EqualTo(1));
                Assert.That(store.ConsumeReserved("B"), Is.False);
                Assert.That(store.ConsumeReserved("A"), Is.True);
                Assert.That(store.Servings, Is.Zero);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void ReleasedMealCanBeClaimedByAnotherSurvivor()
        {
            var go = new GameObject("food");
            try
            {
                var store = go.AddComponent<FoodStore>(); store.Configure(1);
                Assert.That(store.TryReserve("A"), Is.True);
                store.Release("A");
                Assert.That(store.TryReserve("B"), Is.True);
                Assert.That(store.AvailableServings, Is.Zero);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
