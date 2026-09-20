using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Reclamation.Outbreak;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class PerimeterTests
    {
        private readonly List<GameObject> objects = new();
        private NavMeshData data;
        private NavMeshDataInstance mesh;
        private GameObject Make(string name)
        {
            var go = new GameObject(name); objects.Add(go); return go;
        }
        private DefenseSection Section(bool gate = false)
        {
            var go = Make("Test section"); go.SetActive(false);
            var section = go.AddComponent<DefenseSection>();
            section.Configure(new Vector3(20, 2.4f, 0.5f), Vector3.forward, gate, null);
            go.SetActive(true); return section;
        }
        [SetUp] public void Setup()
        {
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box, size = new Vector3(12, 0.2f, 12),
                transform = Matrix4x4.TRS(new Vector3(0, -0.1f, 0), Quaternion.identity, Vector3.one)
            };
            data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),
                new List<NavMeshBuildSource> { source }, new Bounds(Vector3.zero, new Vector3(30, 10, 30)),
                Vector3.zero, Quaternion.identity);
            mesh = NavMesh.AddNavMeshData(data);
        }
        [TearDown] public void Cleanup()
        {
            for (int i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]);
            objects.Clear(); mesh.Remove(); if (data != null) Object.DestroyImmediate(data);
        }

        [UnityTest] public IEnumerator DestroyedFenceReopensNavigation()
        {
            var section = Section(); section.FinishBuild();
            yield return null; yield return null; yield return null;
            var path = new NavMeshPath();
            NavMesh.CalculatePath(new Vector3(0, 0, -3), new Vector3(0, 0, 3), NavMesh.AllAreas, path);
            Assert.That(path.status, Is.Not.EqualTo(NavMeshPathStatus.PathComplete));
            section.TakeDamage(ZombieClass.Ordinary, 30);
            Assert.That(section.Blocking, Is.False);
            Assert.That(section.GetComponent<BoxCollider>().enabled, Is.False);
            yield return null; yield return null; yield return null;
            Assert.That(NavMesh.CalculatePath(new Vector3(0, 0, -3), new Vector3(0, 0, 3), NavMesh.AllAreas, path), Is.True);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
        }

        [UnityTest] public IEnumerator GateOnlyBlocksWhenClosed()
        {
            var gate = Section(true); gate.FinishBuild();
            Assert.That(gate.Open, Is.True); Assert.That(gate.Blocking, Is.False);
            gate.ToggleGate();
            Assert.That(gate.Blocking, Is.True);
            Assert.That(gate.GetComponent<NavMeshObstacle>().enabled, Is.True);
            gate.ToggleGate();
            Assert.That(gate.Blocking, Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator CancelledWorkRefundsReservedWood()
        {
            var section = Section();
            var defense = Make("Defense").AddComponent<PerimeterDefense>();
            defense.Configure(null, new[] { section });
            Assert.That(defense.RequestWork(false), Is.True);
            Assert.That(defense.Wood, Is.EqualTo(32)); Assert.That(defense.ReservedWood, Is.EqualTo(4));
            Assert.That(defense.RequestWork(false), Is.False);
            defense.Tick(new OutbreakAgent[0], 12, true);
            Assert.That(section.Built, Is.False);
            defense.CancelWork(); defense.CancelWork();
            Assert.That(defense.Wood, Is.EqualTo(36)); Assert.That(defense.ReservedWood, Is.Zero);
            yield return null;
        }

        [UnityTest] public IEnumerator BruteBreaksWoodFasterAndDamageIsTimeScaled()
        {
            var ordinary = Section(); var brute = Section(); ordinary.FinishBuild(); brute.FinishBuild();
            ordinary.TakeDamage(ZombieClass.Ordinary, 5); brute.TakeDamage(ZombieClass.Brute, 5);
            Assert.That(ordinary.Health, Is.EqualTo(100)); Assert.That(brute.Health, Is.EqualTo(60));
            ordinary.TakeDamage(ZombieClass.Ordinary, 0); ordinary.TakeDamage(ZombieClass.Ordinary, float.NaN);
            Assert.That(ordinary.Health, Is.EqualTo(100));
            brute.TakeDamage(ZombieClass.Brute, 5); Assert.That(brute.Built, Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator FenceBlocksContactUntilBroken()
        {
            var section = Section(); section.FinishBuild();
            var defense = Make("Defense").AddComponent<PerimeterDefense>(); defense.Configure(null, new[] { section });
            yield return null;
            Assert.That(defense.BlocksContact(new Vector3(0, 1, -2), new Vector3(0, 1, 2)), Is.True);
            section.TakeDamage(ZombieClass.Brute, 10);
            Assert.That(defense.BlocksContact(new Vector3(0, 1, -2), new Vector3(0, 1, 2)), Is.False);
        }

        [UnityTest] public IEnumerator ShelteredWorkerBuildsAndRepairsWithAccountedWood()
        {
            var section = Section();
            var zone = Make("Refuge").AddComponent<SafeZone>();
            var entry = Make("Entry").transform; entry.position = section.InsidePoint;
            zone.Configure(entry, entry, 4, 2);
            var personObject = Make("Builder"); personObject.transform.position = entry.position;
            var person = personObject.AddComponent<OutbreakAgent>();
            yield return null;
            Assert.That(zone.RequestEvacuation(person), Is.True); Assert.That(zone.TryAdmit(person), Is.True);
            var defense = Make("Defense").AddComponent<PerimeterDefense>();
            defense.Configure(zone, new[] { section }); zone.AttachPerimeter(defense);
            var people = new[] { person };
            Assert.That(defense.RequestWork(false), Is.True);
            float deadline = Time.realtimeSinceStartup + 4;
            while (defense.HasJob && Time.realtimeSinceStartup < deadline) { defense.Tick(people, 4, false); yield return null; }
            Assert.That(section.Built, Is.True, defense.Status);
            Assert.That(defense.Wood + defense.ReservedWood + defense.SpentWood, Is.EqualTo(36));
            Assert.That(defense.SpentWood, Is.EqualTo(4));
            section.TakeDamage(ZombieClass.Ordinary, 5);
            Assert.That(defense.RequestWork(true), Is.True);
            deadline = Time.realtimeSinceStartup + 4;
            while (defense.HasJob && Time.realtimeSinceStartup < deadline) { defense.Tick(people, 4, false); yield return null; }
            Assert.That(section.Health, Is.EqualTo(120), defense.Status);
            Assert.That(defense.SpentWood, Is.EqualTo(6));
            Assert.That(defense.Wood + defense.ReservedWood + defense.SpentWood, Is.EqualTo(36));
        }

        [UnityTest] public IEnumerator BruteAttacksReachableFenceAndHonorsPause()
        {
            var section = Section(); section.FinishBuild();
            var defense = Make("Defense").AddComponent<PerimeterDefense>(); defense.Configure(null, new[] { section });
            var go = Make("Brute"); go.transform.position = section.OutsidePoint;
            var person = go.AddComponent<OutbreakAgent>(); person.SetZombieClass(ZombieClass.Brute);
            person.Expose(0, 1, 1); person.Simulate(2);
            yield return null; yield return null; yield return null;
            person.SetSimulationPaused(true);
            defense.TrySiege(person, 12);
            Assert.That(section.Health, Is.EqualTo(120));
            person.SetSimulationPaused(false);
            float deadline = Time.realtimeSinceStartup + 4;
            while (section.Built && Time.realtimeSinceStartup < deadline) { defense.TrySiege(person, 12); yield return null; }
            Assert.That(section.Built, Is.False, person.MovementStatus);
        }
    }
}
