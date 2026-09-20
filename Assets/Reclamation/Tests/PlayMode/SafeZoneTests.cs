using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Reclamation.Neighborhood;
using Reclamation.Outbreak;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class SafeZoneTests
    {
        private readonly List<GameObject> objects = new();
        private NavMeshData data;
        private NavMeshDataInstance navMesh;
        private SafeZone zone;
        private Transform shelter;
        private Transform quarantine;

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
                new List<NavMeshBuildSource> { source }, new Bounds(Vector3.zero, new Vector3(34, 10, 34)),
                Vector3.zero, Quaternion.identity);
            navMesh = NavMesh.AddNavMeshData(data);
            var root = Make("Safe zone", Vector3.zero);
            shelter = Make("Shelter", new Vector3(5, 0, 0)).transform;
            quarantine = Make("Quarantine", new Vector3(0, 0, 5)).transform;
            zone = root.AddComponent<SafeZone>();
            zone.Configure(shelter, quarantine, 4, 2);
        }

        private GameObject Make(string name, Vector3 position)
        {
            var go = new GameObject(name); go.transform.position = position; objects.Add(go); return go;
        }

        private OutbreakAgent Person(string name, Vector3 position)
        {
            var go = Make(name, position);
            var person = go.AddComponent<OutbreakAgent>();
            person.Configure(name);
            Assert.That(go.GetComponent<NavMeshAgent>().isOnNavMesh, Is.True);
            return person;
        }

        [TearDown]
        public void Cleanup()
        {
            for (int i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]);
            objects.Clear(); navMesh.Remove();
            if (data != null) Object.DestroyImmediate(data);
        }

        [UnityTest]
        public IEnumerator ShelterCapacityReservesSpaceForEvacuees()
        {
            yield return null;
            for (int i = 0; i < 4; i++) Assert.That(zone.RequestEvacuation(Person($"P{i}", Vector3.zero)), Is.True);
            var rejected = Person("No bed", Vector3.zero);
            Assert.That(zone.RequestEvacuation(rejected), Is.False);
            Assert.That(zone.EvacuatingCount, Is.EqualTo(4));
            Assert.That(rejected.ZoneAssignment, Is.EqualTo(RefugeAssignment.None));
        }

        [UnityTest]
        public IEnumerator VisibleSymptomsRouteToQuarantine()
        {
            var person = Person("Symptomatic", Vector3.zero);
            yield return null;
            person.Expose(0, 1, 10); person.Simulate(1);
            Assert.That(person.VisibleSymptoms, Is.True);
            Assert.That(zone.RequestEvacuation(person), Is.True);
            Assert.That(zone.TryGetDestination(person, out Vector3 destination), Is.True);
            Assert.That(destination, Is.EqualTo(quarantine.position));
            Assert.That(zone.TryAdmit(person), Is.True);
            Assert.That(person.ZoneAssignment, Is.EqualTo(RefugeAssignment.Quarantined));
            Assert.That(person.Isolated, Is.True);
        }

        [UnityTest]
        public IEnumerator LatentCaseCanEnterShelterThenCauseBreach()
        {
            var person = Person("Latent", Vector3.zero);
            var bystander = Person("Bystander", Vector3.zero);
            yield return null;
            person.Expose(0, 1, 1);
            Assert.That(person.PublicStatus, Is.EqualTo("appears healthy"));
            Assert.That(zone.RequestEvacuation(person), Is.True);
            Assert.That(zone.RequestEvacuation(bystander), Is.True);
            Assert.That(zone.TryAdmit(person), Is.True);
            Assert.That(zone.TryAdmit(bystander), Is.True);
            Assert.That(person.ZoneAssignment, Is.EqualTo(RefugeAssignment.Sheltered));
            person.Simulate(2);
            yield return null;
            Assert.That(zone.Breached, Is.True);
            Assert.That(person.IsProtected, Is.False);
            Assert.That(bystander.IsProtected, Is.False);
            Assert.That(person.MovementStatus, Is.EqualTo("Refuge breached"));
            Assert.That(bystander.MovementStatus, Is.EqualTo("Refuge breached"));
        }

        [UnityTest]
        public IEnumerator TurnedQuarantineCaseRemainsContained()
        {
            var person = Person("Contained", Vector3.zero);
            yield return null;
            person.Expose(0, 1, 1); person.Simulate(1);
            zone.RequestEvacuation(person); zone.TryAdmit(person);
            person.Simulate(2);
            yield return null;
            Assert.That(person.State, Is.EqualTo(InfectionState.Turned));
            Assert.That(person.IsProtected, Is.True);
            Assert.That(person.Isolated, Is.True);
            Assert.That(person.Contagious, Is.False);
            Assert.That(zone.Breached, Is.False);
        }

        [UnityTest]
        public IEnumerator ShelteredSymptomsCanBeTransferredBeforeTurning()
        {
            var person = Person("Detected latent case", Vector3.zero);
            yield return null;
            person.Expose(0, 1, 2);
            zone.RequestEvacuation(person); zone.TryAdmit(person);
            person.Simulate(1);
            Assert.That(person.VisibleSymptoms, Is.True);
            Assert.That(zone.TryTransferToQuarantine(person), Is.True);
            Assert.That(person.ZoneAssignment, Is.EqualTo(RefugeAssignment.Quarantined));
            Assert.That(person.Isolated, Is.True);
            person.Simulate(3);
            yield return null;
            Assert.That(zone.Breached, Is.False);
            Assert.That(person.Contagious, Is.False);
        }

        [UnityTest]
        public IEnumerator EvacueePhysicallyReachesShelter()
        {
            var person = Person("Evacuee", new Vector3(-5, 0, 0));
            yield return null;
            Assert.That(zone.RequestEvacuation(person), Is.True);
            float deadline = Time.realtimeSinceStartup + 7;
            while (person.ZoneAssignment == RefugeAssignment.Evacuating && Time.realtimeSinceStartup < deadline)
            {
                person.ContinueEvacuation(1);
                yield return null;
            }
            Assert.That(person.ZoneAssignment, Is.EqualTo(RefugeAssignment.Sheltered), person.MovementStatus);
            Assert.That(Vector3.Distance(person.transform.position, shelter.position), Is.LessThan(0.8f));
        }

        [UnityTest]
        public IEnumerator CancelledEvacuationReleasesReservation()
        {
            var first = Person("First", Vector3.zero);
            yield return null;
            Assert.That(zone.RequestEvacuation(first), Is.True);
            Assert.That(zone.CancelEvacuation(first), Is.True);
            Assert.That(first.ZoneAssignment, Is.EqualTo(RefugeAssignment.None));
            Assert.That(zone.EvacuatingCount, Is.Zero);
        }
    }
}
