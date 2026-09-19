using System.Collections.Generic;
using NUnit.Framework;
using Reclamation.Prototype;
using UnityEditor;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class HaulingRegressionTests
    {
        private readonly List<GameObject> objects = new();

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            objects.Add(go);
            return go.AddComponent<T>();
}
        [TearDown]
        public void Cleanup()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test]
        public void ActualPileCannotBeCollectedTwiceWhenEmpty()
        {
            var pile = Make<ResourcePile>("wood");
            pile.Configure(1);
            Assert.That(pile.TryTakeOne(), Is.True);
            Assert.That(pile.TryTakeOne(), Is.False);
            Assert.That(pile.Amount, Is.Zero);
            Assert.That(pile.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void BoardSkipsIneligibleHigherPrioritySource()
        {
            var high = Make<ResourcePile>("blocked");
            high.Configure(4, 10);
            var low = Make<ResourcePile>("accessible");
            low.Configure(4, 1);
            var board = Make<HaulJobBoard>("board");
            board.Configure(Make<Stockpile>("store"), new[] { high, low });
            Assert.That(board.TryClaimBest("worker", Vector3.zero, out var selected,
                out _, out _, source => source != high), Is.True);
            Assert.That(selected, Is.SameAs(low));
            Assert.That(board.OpenReservationCount, Is.EqualTo(1));
        }

        [Test]
        public void OwnerCleanupReleasesDestroyedResource()
        {
            var pile = Make<ResourcePile>("wood");
            var board = Make<HaulJobBoard>("board");
            board.Configure(Make<Stockpile>("store"), new[] { pile });
            Assert.That(board.TryClaimBest("worker", Vector3.zero, out _, out _, out _), Is.True);
            Object.DestroyImmediate(pile.gameObject);
            board.ReleaseAll("worker");
            Assert.That(board.OpenReservationCount, Is.Zero);
        }

        [Test]
        public void DisableHandlerReleasesClaimButRetainsCargo()
        {
            var pile = Make<ResourcePile>("wood");
            var board = Make<HaulJobBoard>("board");
            board.Configure(Make<Stockpile>("store"), new[] { pile });
            var worker = Make<HaulWorker>("worker");
            worker.Configure("Test worker", board);
            // Arrange a survivor already carrying one unit.
            var serialized = new SerializedObject(worker);
            serialized.FindProperty("carrying").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            board.TryClaimBest(worker.WorkerId, Vector3.zero, out _, out _, out _);

            // Explicitly exercise the handler: EditMode does not simulate the
            // normal Play Mode component enable/disable lifecycle.
            worker.SendMessage("OnDisable");

            Assert.That(worker.Carrying, Is.True);
            Assert.That(board.OpenReservationCount, Is.Zero);
        }

        [Test]
        public void ActualPileAndStockpileConserveWoodDuringTransfers()
        {
            var pile = Make<ResourcePile>("wood");
            var stock = Make<Stockpile>("store");
            pile.Configure(4);
            for (int i = 0; i < 4; i++)
            {
                Assert.That(pile.TryTakeOne(), Is.True);
                stock.DepositOne();
                Assert.That(pile.Amount + stock.StoredUnits, Is.EqualTo(4));
            }
        }
    }
}
