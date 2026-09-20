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
    public sealed class CrowdEscapeTests
    {
        private readonly List<GameObject> objects = new();
        private NavMeshData data;
        private NavMeshDataInstance instance;

        [SetUp]
        public void Setup()
        {
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(20, 0.2f, 20),
                transform = Matrix4x4.TRS(new Vector3(0, -0.1f, 0), Quaternion.identity, Vector3.one)
            };
            data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),
                new List<NavMeshBuildSource> { source }, new Bounds(Vector3.zero, new Vector3(24, 10, 24)),
                Vector3.zero, Quaternion.identity);
            instance = NavMesh.AddNavMeshData(data);
        }

        private OutbreakAgent Person(string name, Vector3 position)
        {
            var go = new GameObject(name); objects.Add(go);
            go.transform.position = position;
            var person = go.AddComponent<OutbreakAgent>(); person.Configure(name);
            Assert.That(go.GetComponent<NavMeshAgent>().isOnNavMesh, Is.True);
            return person;
        }

        [TearDown]
        public void Cleanup()
        {
            for (int i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]);
            objects.Clear(); instance.Remove();
            if (data != null) Object.DestroyImmediate(data);
        }

        [UnityTest]
        public IEnumerator HumanSprintChangesSpeedAndPauseFreezesStamina()
        {
            var person = Person("Sprinter", Vector3.zero);
            var nav = person.GetComponent<NavMeshAgent>();
            yield return null;
            person.FleeFrom(new Vector3(-2, 0, 0), 1);
            float deadline = Time.realtimeSinceStartup + 2;
            while (nav.velocity.sqrMagnitude <= 0.01f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(nav.velocity.sqrMagnitude, Is.GreaterThan(0.01f));
            person.AdvanceMovement(1, 1);
            Assert.That(person.IsSprinting, Is.True);
            Assert.That(nav.speed, Is.EqualTo(5));
            Assert.That(person.StaminaFraction, Is.EqualTo(0.75f).Within(0.001f));
            person.SetSimulationPaused(true);
            person.AdvanceMovement(10, 12);
            Assert.That(person.StaminaFraction, Is.EqualTo(0.75f).Within(0.001f));
            person.SetSimulationPaused(false);
            deadline = Time.realtimeSinceStartup + 2;
            while (nav.velocity.sqrMagnitude <= 0.01f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(nav.velocity.sqrMagnitude, Is.GreaterThan(0.01f));
            person.AdvanceMovement(3, 1);
            Assert.That(person.IsSprinting, Is.False);
            Assert.That(nav.speed, Is.EqualTo(2.8f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator ZombieBurstRunsOutAndCannotRestartImmediately()
        {
            var hunter = Person("Bursting hunter", new Vector3(-2, 0, 0));
            var prey = Person("Prey", new Vector3(2, 0, 0));
            hunter.Expose(0, 1, 1); hunter.Simulate(2);
            var nav = hunter.GetComponent<NavMeshAgent>();
            yield return null;
            hunter.SetThreatTarget(prey, 1);
            float deadline = Time.realtimeSinceStartup + 2;
            while (nav.velocity.sqrMagnitude <= 0.01f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(nav.velocity.sqrMagnitude, Is.GreaterThan(0.01f));
            hunter.AdvanceMovement(1, 1);
            Assert.That(hunter.IsSprinting, Is.True);
            Assert.That(nav.speed, Is.EqualTo(5.2f).Within(0.001f));
            hunter.AdvanceMovement(1, 1);
            Assert.That(hunter.IsSprinting, Is.False);
            Assert.That(nav.speed, Is.EqualTo(3.3f).Within(0.001f));
            hunter.AdvanceMovement(1, 1);
            Assert.That(hunter.IsSprinting, Is.False);
            Assert.That(hunter.StaminaFraction, Is.EqualTo(0.1f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator ElevatedLabCivilianFindsEscapeAtFootLevel()
        {
            var person = Person("Elevated civilian", Vector3.zero);
            var nav = person.GetComponent<NavMeshAgent>();
            nav.baseOffset = 1;
            yield return null;
            yield return null;
            Vector3 feet = person.transform.position - Vector3.up * nav.baseOffset;
            // Reproduce the lab, unlike the earlier tests whose roots sat on the mesh.
            Assert.That(person.transform.position.y, Is.GreaterThan(0.8f));
            Assert.That(NavMesh.SamplePosition(person.transform.position, out _, 0.65f, nav.areaMask), Is.False);
            Assert.That(NavMesh.SamplePosition(feet, out _, 0.65f, nav.areaMask), Is.True);
            Vector3 start = person.transform.position;
            float until = Time.realtimeSinceStartup + 1;
            while (Time.realtimeSinceStartup < until)
            {
                person.FleeFrom(new Vector3(-2, 1, 0), 1);
                yield return null;
            }
            Assert.That(person.MovementStatus, Is.EqualTo("Escaping"));
            Assert.That(Vector3.Distance(start, person.transform.position), Is.GreaterThan(1));
        }

        [UnityTest]
        public IEnumerator ElevatedLabPursuerReachesElevatedPrey()
        {
            var hunter = Person("Elevated hunter", new Vector3(-4, 0, 0));
            var prey = Person("Elevated prey", new Vector3(2, 0, 0));
            hunter.GetComponent<NavMeshAgent>().baseOffset = 1;
            prey.GetComponent<NavMeshAgent>().baseOffset = 1;
            hunter.Expose(0, 1, 1); hunter.Simulate(2);
            yield return null;
            yield return null;
            Assert.That(prey.transform.position.y, Is.GreaterThan(0.8f));
            float until = Time.realtimeSinceStartup + 4;
            while (Time.realtimeSinceStartup < until)
            {
                hunter.SetThreatTarget(prey, 1);
                yield return null;
            }
            Assert.That(hunter.MovementStatus, Is.EqualTo("Pursuing"));
            Assert.That(Vector3.Distance(hunter.transform.position, prey.transform.position), Is.InRange(0.75f, 1.4f));
        }

        [UnityTest]
        public IEnumerator CivilianEscapesAlongBoundaryInsteadOfPushingOutside()
        {
            var person = Person("Edge civilian", new Vector3(8.5f, 0, 0));
            Vector3 threat = new Vector3(6, 0, 0);
            Vector3 start = person.transform.position;
            yield return null;
            float until = Time.realtimeSinceStartup + 1.2f;
            while (Time.realtimeSinceStartup < until)
            {
                person.FleeFrom(threat, 1);
                yield return null;
            }
            Assert.That(person.IsFleeing, Is.True);
            Assert.That(Mathf.Abs(person.transform.position.z - start.z), Is.GreaterThan(1));
            Assert.That(Vector3.Distance(person.transform.position, threat), Is.GreaterThan(3));
            Assert.That(person.GetComponent<NavMeshAgent>().isOnNavMesh, Is.True);
        }

        [UnityTest]
        public IEnumerator CornerRouteStaysOnMeshAndAvoidsRunningThroughThreat()
        {
            var person = Person("Corner civilian", new Vector3(8.5f, 0, 8.5f));
            var nav = person.GetComponent<NavMeshAgent>();
            Vector3 threat = new Vector3(6, 0, 6);
            yield return null;
            var planner = new EscapeRoutePlanner();
            Assert.That(planner.TryFind(nav, threat, null, out Vector3 destination), Is.True);
            Assert.That(Vector3.Distance(destination, threat), Is.GreaterThan(Vector3.Distance(person.transform.position, threat)));
            var path = new NavMeshPath();
            Assert.That(nav.CalculatePath(destination, path), Is.True);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
            for (int i = 1; i < path.corners.Length; i++)
                for (int step = 0; step <= 20; step++)
                    Assert.That(Vector3.Distance(Vector3.Lerp(path.corners[i - 1], path.corners[i], step / 20f), threat),
                        Is.GreaterThan(1.6f));
        }

        [UnityTest]
        public IEnumerator LatentCivilianCanEscapeAndResumeRoutine()
        {
            var person = Person("Latent civilian", Vector3.zero);
            yield return null;
            person.Expose(0, 20, 10);
            person.FleeFrom(new Vector3(-2, 0, 0), 1);
            Assert.That(person.IsFleeing, Is.True);
            Assert.That(person.GetComponent<CivilianRoutine>().enabled, Is.False);
            Assert.That(person.PublicStatus, Is.EqualTo("appears healthy"));
            person.ResumeRoutineIfSafe();
            Assert.That(person.IsFleeing, Is.False);
            Assert.That(person.GetComponent<CivilianRoutine>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator PauseAndIsolationPreventEscapeMovement()
        {
            var person = Person("Held civilian", Vector3.zero);
            yield return null;
            person.SetSimulationPaused(true);
            person.FleeFrom(new Vector3(-2, 0, 0), 1);
            Assert.That(person.IsFleeing, Is.False);
            Assert.That(person.GetComponent<NavMeshAgent>().hasPath, Is.False);
            person.SetSimulationPaused(false);
            person.SetIsolated(true);
            person.FleeFrom(new Vector3(-2, 0, 0), 1);
            Assert.That(person.IsFleeing, Is.False);
            Assert.That(person.GetComponent<NavMeshAgent>().hasPath, Is.False);
        }

        [UnityTest]
        public IEnumerator PursuerStopsWithinContactRangeWithoutEnteringPreyCenter()
        {
            var hunter = Person("Hunter", new Vector3(-4, 0, 0));
            var prey = Person("Prey", new Vector3(2, 0, 0));
            hunter.Expose(0, 1, 1); hunter.Simulate(2);
            yield return null;
            float until = Time.realtimeSinceStartup + 4;
            while (Time.realtimeSinceStartup < until)
            {
                hunter.SetThreatTarget(prey, 1);
                yield return null;
            }
            float separation = Vector3.Distance(hunter.transform.position, prey.transform.position);
            Assert.That(separation, Is.InRange(0.75f, 1.4f));
            prey.gameObject.SetActive(false);
            hunter.SetThreatTarget(prey, 1);
            Assert.That(hunter.GetComponent<NavMeshAgent>().hasPath, Is.False);
            hunter.SetThreatTarget(null, 1);
            Assert.That(hunter.GetComponent<NavMeshAgent>().hasPath, Is.False);
        }

        [UnityTest]
        public IEnumerator EscapeRouteAccountsForASecondPursuer()
        {
            var civilian = Person("Civilian", Vector3.zero);
            var first = Person("West threat", new Vector3(-2, 0, 0));
            var second = Person("East threat", new Vector3(3, 0, 0));
            first.Expose(0, 1, 1); first.Simulate(2);
            second.Expose(0, 1, 1); second.Simulate(2);
            yield return null;
            var planner = new EscapeRoutePlanner();
            Assert.That(planner.TryFind(civilian.GetComponent<NavMeshAgent>(), first.transform.position,
                new[] { civilian, first, second }, out Vector3 destination), Is.True);
            Assert.That(Mathf.Abs(destination.z), Is.GreaterThan(2));
            var path = new NavMeshPath();
            Assert.That(civilian.GetComponent<NavMeshAgent>().CalculatePath(destination, path), Is.True);
            for (int i = 1; i < path.corners.Length; i++)
                for (int step = 0; step <= 20; step++)
                {
                    Vector3 point = Vector3.Lerp(path.corners[i - 1], path.corners[i], step / 20f);
                    Assert.That(Vector3.Distance(point, first.transform.position), Is.GreaterThan(1.6f));
                    Assert.That(Vector3.Distance(point, second.transform.position), Is.GreaterThan(1.6f));
                }
        }
    }
}
