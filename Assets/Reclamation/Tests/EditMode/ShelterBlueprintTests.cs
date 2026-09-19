using System.Collections.Generic;
using NUnit.Framework;
using Reclamation.Prototype;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class ShelterBlueprintTests
    {
        private readonly List<GameObject> objects = new();
        private T Make<T>() where T : Component
        {
            var go = new GameObject(typeof(T).Name);
            objects.Add(go);
            return go.AddComponent<T>();
        }
        [TearDown] public void Cleanup()
        {
            foreach (var go in objects) Object.DestroyImmediate(go);
            objects.Clear();
        }
        private static void Fund(ShelterBlueprint site)
        {
            for (int i = 0; i < ShelterBlueprint.WoodCost; i++)
            {
                Assert.That(site.TryReserveDelivery("worker"), Is.True);
                Assert.That(site.TryDeliver("worker"), Is.True);
            }
        }

        [Test] public void ClaimsCannotOverbookRemainingMaterials()
        {
            var site = Make<ShelterBlueprint>();
            for (int i = 0; i < 8; i++) Assert.That(site.TryReserveDelivery($"worker-{i}"), Is.True);
            Assert.That(site.TryReserveDelivery("extra"), Is.False);
            Assert.That(site.TryReserveDelivery("worker-0"), Is.True);
            Assert.That(site.DeliveryClaims, Is.EqualTo(8));
        }

        [Test] public void UnreservedOrDuplicateDeliveryIsRejected()
        {
            var site = Make<ShelterBlueprint>();
            Assert.That(site.TryDeliver("worker"), Is.False);
            site.TryReserveDelivery("worker");
            Assert.That(site.TryDeliver("worker"), Is.True);
            Assert.That(site.TryDeliver("worker"), Is.False);
            Assert.That(site.DeliveredWood, Is.EqualTo(1));
        }

        [Test] public void BuilderRequiresMaterialsAndExclusiveOwnership()
        {
            var site = Make<ShelterBlueprint>();
            Assert.That(site.TryReserveBuild("builder"), Is.False);
            Fund(site);
            Assert.That(site.TryReserveBuild("builder"), Is.True);
            Assert.That(site.TryReserveBuild("other"), Is.False);
            Assert.That(site.TryWork("other", 1), Is.False);
            Assert.That(site.TryWork("builder", 3), Is.True);
            site.Release("builder");
            Assert.That(site.Progress, Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(site.TryReserveBuild("other"), Is.True);
        }

        [Test] public void CancelRefundsExactlyOnceAndRejectsIncomingDelivery()
        {
            var site = Make<ShelterBlueprint>();
            var stock = Make<Stockpile>();
            site.TryReserveDelivery("worker");
            site.TryDeliver("worker");
            site.TryReserveDelivery("incoming");
            Assert.That(site.TryCancel(stock), Is.True);
            Assert.That(site.TryCancel(stock), Is.False);
            Assert.That(stock.StoredUnits, Is.EqualTo(1));
            Assert.That(site.DeliveredWood, Is.Zero);
            Assert.That(site.DeliveryClaims, Is.Zero);
            Assert.That(site.TryDeliver("incoming"), Is.False);
        }

        [Test] public void CompletedShelterRetainsEmbodiedWoodAndCannotRefund()
        {
            var site = Make<ShelterBlueprint>();
            Fund(site);
            site.TryReserveBuild("builder");
            site.TryWork("builder", 100);
            Assert.That(site.Complete, Is.True);
            Assert.That(site.Progress, Is.EqualTo(1));
            Assert.That(site.DeliveredWood, Is.EqualTo(8));
            Assert.That(site.TryCancel(Make<Stockpile>()), Is.False);
            Assert.That(site.TryReserveBuild("another"), Is.False);
        }

        [Test] public void StockpileWithdrawalCannotUnderflow()
        {
            var stock = Make<Stockpile>();
            Assert.That(stock.TryTakeOne(), Is.False);
            stock.DepositOne();
            Assert.That(stock.TryTakeOne(), Is.True);
            Assert.That(stock.TryTakeOne(), Is.False);
            Assert.That(stock.StoredUnits, Is.Zero);
        }
    }
}
