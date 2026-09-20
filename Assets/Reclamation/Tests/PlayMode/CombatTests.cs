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
    public sealed class CombatTests
    {
        private readonly List<GameObject> objects = new();
        private NavMeshData data;
        private NavMeshDataInstance mesh;
        private CombatDirector director;
        private GameObject Make(string name) { var go = new GameObject(name); objects.Add(go); return go; }
        private Combatant Actor(string name, Vector3 feet, bool zombie = false, bool veteran = false)
        {
            var go = Make(name); go.transform.position = feet;
            var person = go.AddComponent<OutbreakAgent>(); person.Configure(name);
            var f = go.AddComponent<Combatant>(); f.Configure(veteran, CombatOrder.Hold);
            if (zombie) { person.Expose(0, 1, 1); person.Simulate(2); }
            return f;
        }
        [SetUp] public void Setup()
        {
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box, size = new Vector3(24, 0.2f, 24),
                transform = Matrix4x4.TRS(new Vector3(0, -0.1f, 0), Quaternion.identity, Vector3.one)
            };
            data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), new List<NavMeshBuildSource> { source },
                new Bounds(Vector3.zero, new Vector3(30, 10, 30)), Vector3.zero, Quaternion.identity);
            mesh = NavMesh.AddNavMeshData(data); director = Make("Combat director").AddComponent<CombatDirector>();
            director.ConfigureAwareness(false);
        }
        [TearDown] public void Cleanup()
        {
            for (int i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]);
            objects.Clear(); mesh.Remove(); if (data != null) Object.DestroyImmediate(data);
        }
        private void Tick(float seconds, params Combatant[] actors)
        {
            var people = new OutbreakAgent[actors.Length];
            for (int i = 0; i < actors.Length; i++) people[i] = actors[i].Person;
            director.Tick(people, seconds, 1, 500);
        }
        private void UntilGrab(Combatant human, Combatant zombie)
        {
            human.TrySpend(human.Energy);
            for (int i = 0; i < 15 && human.Grabber == null; i++) Tick(0.1f, human, zombie);
            Assert.That(human.Grabber, Is.EqualTo(zombie));
        }

        [UnityTest] public IEnumerator ProximityDoesNotInfectBeforeBiteContact()
        {
            var human = Actor("Civilian", Vector3.zero); var zombie = Actor("Zombie", Vector3.forward * 1.1f, true);
            yield return null;
            UntilGrab(human, zombie);
            Assert.That(human.Person.State, Is.EqualTo(InfectionState.Healthy));
            Assert.That(zombie.Action, Is.EqualTo(CombatAction.Bite));
            for (int i = 0; i < 14; i++) Tick(0.1f, human, zombie);
            Assert.That(human.Person.State, Is.EqualTo(InfectionState.Exposed));
            Assert.That(human.Health, Is.EqualTo(75)); Assert.That(human.Grabber, Is.Null);
        }

        [UnityTest] public IEnumerator ZeroTimeAndNewOrderCannotSkipGrab()
        {
            var human = Actor("Civilian", Vector3.zero); var zombie = Actor("Zombie", Vector3.forward * 1.1f, true);
            yield return null; UntilGrab(human, zombie);
            float remaining = zombie.Remaining; float energy = human.Energy;
            human.GiveOrder(CombatOrder.Disengage);
            for (int i = 0; i < 20; i++) Tick(0, human, zombie);
            Assert.That(zombie.Remaining, Is.EqualTo(remaining)); Assert.That(human.Energy, Is.EqualTo(energy));
            Assert.That(human.Action, Is.EqualTo(CombatAction.Grabbed)); Assert.That(human.Person.Infectable, Is.True);
        }

        [UnityTest] public IEnumerator TeammateStrikeInterruptsBiteWithoutInfection()
        {
            var human = Actor("Civilian", Vector3.zero); var zombie = Actor("Zombie", Vector3.forward * 1.1f, true);
            yield return null; UntilGrab(human, zombie);
            var ally = Actor("Veteran", new Vector3(1.1f, 0, 1.1f), false, true);
            for (int i = 0; i < 6; i++) Tick(0.1f, human, zombie, ally);
            Assert.That(human.Grabber, Is.Null); Assert.That(human.Person.Infectable, Is.True);
            Assert.That(zombie.Health, Is.LessThan(80));
        }

        [UnityTest] public IEnumerator NeutralizingGrabberReleasesVictim()
        {
            var human = Actor("Civilian", Vector3.zero); var zombie = Actor("Zombie", Vector3.forward * 1.1f, true);
            yield return null; UntilGrab(human, zombie);
            zombie.Person.Neutralize();
            Assert.That(human.Grabber, Is.Null); Assert.That(human.Person.Infectable, Is.True);
        }

        [UnityTest] public IEnumerator IsolationInterruptsAnExistingBite()
        {
            var human = Actor("Civilian", Vector3.zero); var zombie = Actor("Zombie", Vector3.forward * 1.1f, true);
            yield return null; UntilGrab(human, zombie);
            human.Person.SetIsolated(true); Tick(0.1f, human, zombie);
            Assert.That(human.Grabber, Is.Null); Assert.That(zombie.Action, Is.Not.EqualTo(CombatAction.Bite));
            Assert.That(human.Person.State, Is.EqualTo(InfectionState.Healthy));
        }

        [UnityTest] public IEnumerator HoldDoesNotChaseEnemiesOutsideAnchorRadius()
        {
            var human = Actor("Veteran", Vector3.zero, false, true); var zombie = Actor("Zombie", Vector3.forward * 6, true);
            yield return null; Tick(0.1f, human, zombie);
            Assert.That(human.Handled, Is.True); Assert.That(human.Action, Is.EqualTo(CombatAction.Ready));
            Assert.That(human.Person.GetComponent<NavMeshAgent>().destination.z, Is.EqualTo(human.Anchor.z).Within(0.1f));
        }

        [UnityTest] public IEnumerator DisengageDoesNotStartAnOffensiveStrike()
        {
            var human = Actor("Veteran", Vector3.zero, false, true); var zombie = Actor("Zombie", Vector3.forward * 1.4f, true);
            yield return null; human.GiveOrder(CombatOrder.Disengage); Tick(0.1f, human, zombie);
            Assert.That(human.Action, Is.EqualTo(CombatAction.Ready)); Assert.That(human.Person.IsFleeing, Is.True);
        }

        [UnityTest] public IEnumerator IntactFencePreventsMeleeThroughIt()
        {
            var wall = Make("Fence"); wall.SetActive(false);
            var section = wall.AddComponent<DefenseSection>();
            section.Configure(new Vector3(20, 2.4f, 0.5f), Vector3.forward, false, null);
            wall.SetActive(true); section.FinishBuild();
            var human = Actor("Veteran", Vector3.back * 0.8f, false, true);
            var zombie = Actor("Zombie", Vector3.forward * 0.8f, true);
            yield return null; yield return null; yield return null;
            for (int i = 0; i < 40; i++) Tick(0.1f, human, zombie);
            Assert.That(human.Person.Infectable, Is.True); Assert.That(zombie.Health, Is.EqualTo(80));
            Assert.That(zombie.Action, Is.EqualTo(CombatAction.Ready));
        }

        [UnityTest] public IEnumerator StaminaCannotBeOverspentOrCreatedByNegativeCosts()
        {
            var human = Actor("Civilian", Vector3.zero);
            Assert.That(human.TrySpend(-1), Is.False); Assert.That(human.TrySpend(float.NaN), Is.False);
            Assert.That(human.TrySpend(human.Energy + 1), Is.False);
            Assert.That(human.TrySpend(human.Energy), Is.True); Assert.That(human.Energy, Is.Zero);
            yield return null;
        }

        [UnityTest] public IEnumerator RestedVeteranBreaksGrabBeforeBite()
        {
            var human = Actor("Veteran", Vector3.zero, false, true);
            var zombie = Actor("Zombie", Vector3.forward * 1.1f, true);
            yield return null; human.GiveOrder(CombatOrder.Disengage);
            // No yielded movement frames: force close contact to test the escape action,
            // independently of navigation successfully dodging the original lunge.
            for (int i = 0; i < 15 && human.Grabber == null; i++) Tick(0.1f, human, zombie);
            Assert.That(human.Grabber, Is.EqualTo(zombie));
            for (int i = 0; i < 8; i++) Tick(0.1f, human, zombie);
            Assert.That(human.Grabber, Is.Null); Assert.That(human.Person.Infectable, Is.True);
            Assert.That(human.Energy, Is.LessThan(human.Attributes.MaximumStamina));
        }

        [UnityTest] public IEnumerator PausedOutbreakDirectorDoesNotAdvanceCombat()
        {
            var human = Actor("Civilian", Vector3.zero); var zombie = Actor("Zombie", Vector3.forward * 1.1f, true);
            yield return null; UntilGrab(human, zombie);
            var clock = Make("Clock").AddComponent<NeighborhoodClock>(); clock.SetPaused(true);
            director.gameObject.AddComponent<OutbreakDirector>().Configure(clock,
                new[] { human.Person, zombie.Person }, null, 614);
            float remaining = zombie.Remaining;
            yield return null; yield return null; yield return null;
            Assert.That(zombie.Remaining, Is.EqualTo(remaining));
            Assert.That(human.Person.State, Is.EqualTo(InfectionState.Healthy));
        }

        [UnityTest] public IEnumerator UnawareSurvivorDoesNotAttackOnSight()
        {
            director.ConfigureAwareness(true);
            var human = Actor("Civilian", Vector3.zero);
            var zombie = Actor("Zombie", Vector3.forward * 4, true);
            yield return null; Tick(0.1f, human, zombie);
            Assert.That(human.Awareness, Is.EqualTo(ThreatAwareness.Suspicious));
            Assert.That(human.Action, Is.EqualTo(CombatAction.Ready));
            Assert.That(director.RecognizesThreat(human.Person, zombie.Person), Is.False);
            Assert.That(zombie.Health, Is.EqualTo(80));
        }

        [UnityTest] public IEnumerator LungeAlertsVictimAndLocalWitnessButNotDistantSurvivor()
        {
            director.ConfigureAwareness(true);
            var victim = Actor("Victim", Vector3.zero);
            var zombie = Actor("Zombie", Vector3.forward * 1.1f, true);
            var nearby = Actor("Witness", new Vector3(3, 0, 1));
            var distant = Actor("Distant", new Vector3(10, 0, 1));
            yield return null; Tick(0.1f, victim, zombie, nearby, distant);
            Assert.That(zombie.Action, Is.EqualTo(CombatAction.Lunge));
            Assert.That(victim.Person.Infectable, Is.True, "Recognition precedes bite contact.");
            Assert.That(victim.Awareness, Is.EqualTo(ThreatAwareness.Alerted));
            Assert.That(nearby.Awareness, Is.EqualTo(ThreatAwareness.Alerted));
            Assert.That(distant.Awareness, Is.EqualTo(ThreatAwareness.Unaware));
        }

        [UnityTest] public IEnumerator WitnessBehindWallDoesNotReceiveGlobalAlert()
        {
            director.ConfigureAwareness(true);
            var wall = Make("Sight blocking fence"); wall.SetActive(false); wall.transform.position = Vector3.right * 2;
            var section = wall.AddComponent<DefenseSection>();
            section.Configure(new Vector3(0.5f, 2.4f, 30), Vector3.right, false, null);
            wall.SetActive(true); section.FinishBuild();
            var victim = Actor("Victim", Vector3.zero);
            var zombie = Actor("Zombie", Vector3.forward * 1.1f, true);
            var hidden = Actor("Behind wall", new Vector3(4, 0, 1));
            yield return null; yield return null; yield return null;
            Tick(0.1f, victim, zombie, hidden);
            Assert.That(victim.Awareness, Is.EqualTo(ThreatAwareness.Alerted));
            Assert.That(hidden.Awareness, Is.EqualTo(ThreatAwareness.Unaware));
        }

        [UnityTest] public IEnumerator SymptomsCreateSuspicionWithoutPermissionToAttack()
        {
            director.ConfigureAwareness(true);
            var human = Actor("Civilian", Vector3.zero);
            var sick = Actor("Sick neighbor", Vector3.forward * 4);
            sick.Person.Expose(0, 1, 20);
            yield return null; Tick(0.1f, human, sick);
            Assert.That(human.Awareness, Is.EqualTo(ThreatAwareness.Unaware), "Hidden infection alone is not evidence.");
            sick.Person.Simulate(1); Tick(0.1f, human, sick);
            Assert.That(human.Awareness, Is.EqualTo(ThreatAwareness.Suspicious));
            Assert.That(human.Action, Is.EqualTo(CombatAction.Ready));
            Assert.That(sick.Health, Is.EqualTo(100));
        }

        [UnityTest] public IEnumerator WithdrawalStartsLateInIncubationAndUsesReachableGround()
        {
            var sick = Actor("Visitor", Vector3.zero);
            var crowd = Actor("Neighbor", Vector3.forward);
            sick.Person.Expose(0, 20, 10);
            yield return null;
            Assert.That(sick.Person.ShouldWithdraw(10), Is.False);
            Assert.That(sick.Person.ShouldWithdraw(12), Is.True);
            var people = new[] { sick.Person, crowd.Person };
            Assert.That(sick.Person.TryWithdraw(12, 0.1f, 0.5f, people), Is.True);
            var nav = sick.GetComponent<NavMeshAgent>();
            Assert.That(sick.Person.IsWithdrawing, Is.True);
            Assert.That(nav.hasPath, Is.True); Assert.That(nav.pathStatus, Is.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(nav.speed, Is.EqualTo(1.05f).Within(0.01f));
            Assert.That(Vector3.Distance(nav.destination, crowd.Person.FeetPosition), Is.GreaterThan(1));
            sick.Person.SetSimulationPaused(true);
            Assert.That(sick.Person.TryWithdraw(12, 1, 0.5f, people), Is.False);
            sick.Person.SetSimulationPaused(false); sick.Person.SetIsolated(true);
            Assert.That(sick.Person.ShouldWithdraw(12), Is.False);
            Assert.That(sick.Person.IsWithdrawing, Is.False);
        }

        [UnityTest] public IEnumerator EvacuationOverridesWithdrawal()
        {
            var sick = Actor("Visitor", Vector3.zero); sick.Person.Expose(0, 20, 10);
            var zone = Make("Refuge").AddComponent<SafeZone>();
            yield return null; sick.Person.AssignSafeZone(zone);
            Assert.That(sick.Person.ShouldWithdraw(12), Is.False);
        }

        [UnityTest] public IEnumerator HalfSpeedIsDefaultAndQuarterSpeedAdvancesClockProportionally()
        {
            var clock = Make("Slow clock").AddComponent<NeighborhoodClock>();
            Assert.That(clock.Speed, Is.EqualTo(0.5f));
            clock.SetSpeed(0.25f); Assert.That(clock.Speed, Is.EqualTo(0.25f));
            clock.SetSpeed(float.NaN); clock.SetSpeed(-1); Assert.That(clock.Speed, Is.EqualTo(0.25f));
            double before = clock.MinuteOfDay;
            float elapsed = 0;
            for (int i = 0; i < 6; i++) { yield return null; elapsed += Time.deltaTime; }
            Assert.That(clock.MinuteOfDay - before, Is.EqualTo(elapsed).Within(0.05), "4 minutes/sec times 0.25x = 1 minute/sec.");
            clock.SetPaused(true); before = clock.MinuteOfDay;
            yield return null; yield return null;
            Assert.That(clock.MinuteOfDay, Is.EqualTo(before));
        }

        [UnityTest] public IEnumerator ExistingCombatEncounterStartsWithKnownThreats()
        {
            director.ConfigureAwareness(true);
            var human = Actor("Veteran", Vector3.zero, false, true);
            var zombie = Actor("Starting zombie", Vector3.forward * 4);
            director.gameObject.AddComponent<CombatEncounter>().Configure(new[] { zombie.Person });
            yield return null; Tick(0.1f, human, zombie);
            Assert.That(director.RequiresWitness, Is.False);
            Assert.That(zombie.Person.State, Is.EqualTo(InfectionState.Turned));
            Assert.That(human.Awareness, Is.EqualTo(ThreatAwareness.Alerted));
        }
    }
}
