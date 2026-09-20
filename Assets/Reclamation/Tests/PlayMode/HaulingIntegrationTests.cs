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
        private readonly List<HaulWorker> crew = new();
        private NavMeshData data;
        private NavMeshDataInstance instance;
        private Stockpile stock;
        private ResourcePile pile;
        private HaulWorker worker;
        private HaulJobBoard board;
        private int expectedWood;

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
            expectedWood = 4;
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
            crew.Add(worker);
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
            crew.Clear();
            instance.Remove();
            if (data != null) Object.DestroyImmediate(data);
        }

        private void AssertConserved()
        {
            int delivered = board.Shelter == null ? 0 : board.Shelter.DeliveredWood;
            int carried = 0;
            foreach (HaulWorker survivor in crew) if (survivor.Carrying) carried++;
            Assert.That(pile.Amount + stock.StoredUnits + carried + delivered,
                Is.EqualTo(expectedWood));
        }

        private IEnumerator FinishDelivery()
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (stock.StoredUnits < expectedWood && Time.realtimeSinceStartup < deadline)
            {
                AssertConserved();
                yield return null;
            }
            Assert.That(stock.StoredUnits, Is.EqualTo(expectedWood), worker.DecisionExplanation);
            AssertConserved();
        }

        [UnityTest]
        public IEnumerator HaulingConservesResourcesAcrossEveryFrame()
        {
            yield return FinishDelivery();
        }

        private ShelterBlueprint AddShelter(bool fromStorage)
        {
            expectedWood = 8;
            pile.Configure(fromStorage ? 0 : 8);
            if (fromStorage) for (int i = 0; i < 8; i++) stock.DepositOne();
            var site = Make<ShelterBlueprint>("shelter", new Vector3(4, 0, 5));
            board.SetShelter(site);
            return site;
        }

        private IEnumerator FinishShelter(ShelterBlueprint site)
        {
            float deadline = Time.realtimeSinceStartup + 45f;
            while (!site.Complete && Time.realtimeSinceStartup < deadline)
            {
                AssertConserved();
                yield return null;
            }
            Assert.That(site.Complete, Is.True, worker.DecisionExplanation);
            AssertConserved();
            Assert.That(site.DeliveredWood, Is.EqualTo(8));
        }

        [UnityTest] public IEnumerator ShelterUsesLooseWoodWithoutLoss()
        {
            yield return FinishShelter(AddShelter(false));
        }

        [UnityTest] public IEnumerator ThreeWorkersBuildWithoutOverdelivery()
        {
            var site = AddShelter(false);
            expectedWood = 16;
            pile.Configure(16);
            for (int i = 0; i < 2; i++)
            {
                var extra = Make<HaulWorker>($"helper-{i}", new Vector3(0, 0, 2 + i * 2));
                extra.Configure($"Helper {i}", board);
                extra.GetComponent<NavMeshAgent>().speed = 12;
                extra.GetComponent<NavMeshAgent>().acceleration = 100;
                crew.Add(extra);
            }
            yield return FinishShelter(site);
            float deadline = Time.realtimeSinceStartup + 25f;
            while (stock.StoredUnits < 8 && Time.realtimeSinceStartup < deadline)
            {
                AssertConserved();
                yield return null;
            }
            Assert.That(stock.StoredUnits, Is.EqualTo(8));
            Assert.That(site.DeliveryClaims, Is.Zero);
            AssertConserved();
        }

        [UnityTest] public IEnumerator ShelterUsesStoredWoodAndBuilderResumes()
        {
            var site = AddShelter(true);
            float deadline = Time.realtimeSinceStartup + 30f;
            while (site.Progress < 0.1f && Time.realtimeSinceStartup < deadline)
            {
                AssertConserved();
                yield return null;
            }
            Assert.That(site.Progress, Is.GreaterThanOrEqualTo(0.1f));
            worker.enabled = false;
            float before = site.Progress;
            yield return new WaitForSeconds(1);
            Assert.That(site.Progress, Is.EqualTo(before));
            AssertConserved();
            worker.enabled = true;
            yield return FinishShelter(site);
        }

        [UnityTest] public IEnumerator CancellingBlueprintReturnsDeliveredAndCarriedWood()
        {
            var site = AddShelter(true);
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!(site.DeliveredWood > 0 && worker.Carrying)
                && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(site.DeliveredWood, Is.GreaterThan(0));
            Assert.That(worker.Carrying, Is.True);
            Assert.That(site.TryCancel(stock), Is.True);
            AssertConserved();
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

        [UnityTest]
        public IEnumerator UrgentHungerConsumesOneReservedMealThenWorkResumes()
        {
            var food = Make<FoodStore>("food", new Vector3(0, 0, 3));
            food.Configure(1);
            worker.ConfigureNeeds(food, 75, 0);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (food.Servings > 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(food.Servings, Is.Zero, worker.DecisionExplanation);
            Assert.That(worker.Needs.Hunger, Is.EqualTo(10).Within(0.1f));
            Assert.That(worker.Needs.NeedsMeal, Is.False);
            Assert.That(food.ReservedServings, Is.Zero);

            deadline = Time.realtimeSinceStartup + 15f;
            while (!worker.Carrying && stock.StoredUnits == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(worker.Carrying || stock.StoredUnits > 0, Is.True,
                "The survivor should resume hauling after eating.");
            AssertConserved();
        }
    }
}
