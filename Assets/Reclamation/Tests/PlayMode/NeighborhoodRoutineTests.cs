using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Reclamation.Neighborhood;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class NeighborhoodRoutineTests
    {
        private readonly List<GameObject> objects = new();
        private NavMeshData data;
        private NavMeshDataInstance instance;
        private NeighborhoodClock clock;
        private CivilianRoutine civilian;
        private Transform cafe;

        private GameObject ObjectAt(string name, Vector3 position)
        {
            var go = new GameObject(name); go.transform.position = position;
            objects.Add(go); return go;
        }
        [SetUp] public void Setup()
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
            instance = NavMesh.AddNavMeshData(data);
            clock = ObjectAt("clock", Vector3.zero).AddComponent<NeighborhoodClock>();
            clock.SetSpeed(1); // These existing real-time deadlines exercise the original 1x scenario.
            var home = ObjectAt("home", new Vector3(-4, 0, 0)).transform;
            cafe = ObjectAt("cafe", new Vector3(4, 0, 0)).transform;
            var park = ObjectAt("park", new Vector3(0, 0, 4)).transform;
            civilian = ObjectAt("civilian", home.position).AddComponent<CivilianRoutine>();
            civilian.GetComponent<NavMeshAgent>().stoppingDistance = 0.25f;
            civilian.Configure("Tester", clock, home, cafe, park, 0);
        }
        [TearDown] public void Cleanup()
        {
            for (int i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]);
            objects.Clear(); instance.Remove();
            if (data != null) Object.DestroyImmediate(data);
        }
        private IEnumerator ArriveAtCafe()
        {
            float deadline = Time.realtimeSinceStartup + 15;
            while (!(civilian.Destination == RoutineDestination.Cafe && civilian.AtDestination)
                && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(civilian.Destination, Is.EqualTo(RoutineDestination.Cafe), civilian.Status);
            Assert.That(civilian.AtDestination, Is.True, civilian.Status);
            Assert.That(Vector3.Distance(civilian.transform.position, cafe.position), Is.LessThan(0.6f));
        }
        [UnityTest] public IEnumerator CivilianLeavesHomeAndArrivesAtCafe()
        {
            yield return ArriveAtCafe();
        }
        [UnityTest] public IEnumerator PauseStopsClockAndTravelThenResumes()
        {
            float deadline = Time.realtimeSinceStartup + 10;
            while (civilian.Destination != RoutineDestination.Cafe && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(civilian.Destination, Is.EqualTo(RoutineDestination.Cafe));
            clock.SetPaused(true);
            yield return null;
            double minute = clock.MinuteOfDay;
            Vector3 position = civilian.transform.position;
            yield return new WaitForSeconds(0.5f);
            Assert.That(clock.MinuteOfDay, Is.EqualTo(minute));
            Assert.That(Vector3.Distance(position, civilian.transform.position), Is.LessThan(0.05f));
            clock.SetPaused(false);
            yield return ArriveAtCafe();
        }
        [UnityTest] public IEnumerator UnreachableCafeRecoversAfterMovingBack()
        {
            cafe.position = new Vector3(100, 0, 0);
            yield return new WaitForSeconds(5);
            Assert.That(civilian.AtDestination, Is.False);
            Assert.That(Vector3.Distance(civilian.transform.position, new Vector3(-4, 0, 0)), Is.LessThan(0.6f));
            cafe.position = new Vector3(4, 0, 0);
            yield return ArriveAtCafe();
        }
    }
}
