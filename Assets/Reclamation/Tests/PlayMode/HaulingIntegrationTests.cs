using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Reclamation.Prototype;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class HaulingIntegrationTests
    {
        private readonly List<GameObject> objects = new();
        private NavMeshData data;
        private NavMeshDataInstance instance;
        private Stockpile stock;
        private ResourcePile pile;
        private HaulWorker worker;
        private HaulJobBoard board;

        private T Make<T>(string name, Vector3 position) where T : Component
        {
            var go = new GameObject(name);
            go.transform.position = position;
            objects.Add(go);
            return go.AddComponent<T>();
        }

        [SetUp]
        public void Setup()
        {
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(30, 0.2f, 30),
                transform = Matrix4x4.TRS(new Vector3(0, -0.1f, 0), Quaternion.identity, Vector3.one)
            };
            data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),
                new List<NavMeshBuildSource> { source },
                new Bounds(Vector3.zero, new Vector3(40, 10, 40)), Vector3.zero, Quaternion.identity);
            Assert.That(data, Is.Not.Null);
            instance = NavMesh.AddNavMeshData(data);
            stock = Make<Stockpile>("store", new Vector3(4, 0, 0));
            pile = Make<ResourcePile>("wood", new Vector3(-4, 0, 0));
            pile.Configure(4);
            board = Make<HaulJobBoard>("board", Vector3.zero);
            board.Configure(stock, new[] { pile });
            worker = Make<HaulWorker>("worker", Vector3.zero);
            worker.Configure("Tester", board);
            var agent = worker.GetComponent<NavMeshAgent>();
            agent.speed = 12;
            agent.acceleration = 100;
            agent.stoppingDistance = 0.25f;
        }

        [TearDown]
        public void Cleanup()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
            instance.Remove();
            if (data != null) Object.DestroyImmediate(data);
        }

        private void AssertConserved()
        {
            Assert.That(pile.Amount + stock.StoredUnits + (worker.Carrying ? 1 : 0), Is.EqualTo(4));
        }

        private IEnumerator FinishDelivery()
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (stock.StoredUnits < 4 && Time.realtimeSinceStartup < deadline)
            {
                AssertConserved();
                yield return null;
            }
            Assert.That(stock.StoredUnits, Is.EqualTo(4), worker.DecisionExplanation);
            AssertConserved();
        }

        [UnityTest]
        public IEnumerator HaulingConservesResourcesAcrossEveryFrame()
        {
            yield return FinishDelivery();
        }

        [UnityTest]
        public IEnumerator DisabledStockpileRetainsCargoAndResumes()
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!worker.Carrying && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(worker.Carrying, Is.True);
            stock.enabled = false;
            yield return new WaitForSeconds(1);
            Assert.That(worker.Carrying, Is.True);
            AssertConserved();
            stock.enabled = true;
            yield return FinishDelivery();
        }

        [UnityTest]
        public IEnumerator DisablingWorkerReleasesReservationAndPreservesCargo()
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (board.OpenReservationCount == 0 && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(board.OpenReservationCount, Is.EqualTo(1));

            worker.enabled = false;
            yield return null;
            Assert.That(board.OpenReservationCount, Is.Zero);
            AssertConserved();
            worker.enabled = true;

            deadline = Time.realtimeSinceStartup + 10f;
            while (!worker.Carrying && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(worker.Carrying, Is.True);
            int storedBeforePause = stock.StoredUnits;
            worker.enabled = false;
            yield return new WaitForSeconds(1);
            Assert.That(worker.Carrying, Is.True);
            Assert.That(stock.StoredUnits, Is.EqualTo(storedBeforePause));
            Assert.That(board.OpenReservationCount, Is.Zero);
            AssertConserved();
            worker.enabled = true;
            yield return FinishDelivery();
        }

        [UnityTest]
        public IEnumerator UnreachableSourceIsSkippedThenRetried()
        {
            pile.transform.position = new Vector3(100, 0, 0);
            yield return new WaitForSeconds(1);
            Assert.That(pile.Amount, Is.EqualTo(4));
            Assert.That(worker.Carrying, Is.False);
            pile.transform.position = new Vector3(-4, 0, 0);
            yield return FinishDelivery();
        }
    }
}
