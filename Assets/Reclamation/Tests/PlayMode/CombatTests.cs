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
            f.ConfigureProgression(false, veteran ? ProgressionRules.VeteranExperience : 0);
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

        private Combatant BruteAt(Vector3 position)
        {
            var brute = Actor("Brute", position, true);
            brute.Person.SetZombieClass(ZombieClass.Brute);
            return brute;
        }

        private void FinishSweep(Combatant brute, params Combatant[] people)
        {
            for (int i = 0; i < 20 && brute.Action == CombatAction.Sweep; i++) Tick(0.1f, people);
            Assert.That(brute.SweepRecovery, Is.True, brute.Status);
        }

        [UnityTest] public IEnumerator SweepHasWindupHitsMultipleSurvivorsAndDoesNotInfect()
        {
            director.ConfigureAwareness(true);
            var brute = BruteAt(Vector3.zero);
            var a = Actor("Alpha", new Vector3(-0.65f, 0, 1.4f));
            var b = Actor("Bravo", new Vector3(0.65f, 0, 1.4f));
            a.TrySpend(a.Energy); b.TrySpend(b.Energy);
            yield return null; Tick(0.1f, brute, a, b);
            Assert.That(brute.Action, Is.EqualTo(CombatAction.Sweep));
            Assert.That(brute.Remaining, Is.EqualTo(CombatDirector.SweepWindup));
            Assert.That(a.Health, Is.EqualTo(100)); Assert.That(b.Health, Is.EqualTo(100));
            Assert.That(a.Awareness, Is.EqualTo(ThreatAwareness.Alerted));
            float remaining = brute.Remaining;
            Tick(0, brute, a, b); Assert.That(brute.Remaining, Is.EqualTo(remaining));
            FinishSweep(brute, brute, a, b);
            Assert.That(a.Health, Is.EqualTo(90)); Assert.That(b.Health, Is.EqualTo(90));
            Assert.That(a.Person.Infectable, Is.True); Assert.That(b.Person.Infectable, Is.True);
            Assert.That(a.Action, Is.EqualTo(CombatAction.KnockedBack));
            Assert.That(brute.SweepCooldown, Is.GreaterThan(0));
        }

        [UnityTest] public IEnumerator SweepDoesNotTrackVictimWhoMovesBehindBrute()
        {
            var brute = BruteAt(Vector3.zero);
            var a = Actor("Alpha", new Vector3(-0.65f, 0, 1.4f));
            var b = Actor("Bravo", new Vector3(0.65f, 0, 1.4f));
            a.TrySpend(a.Energy); b.TrySpend(b.Energy);
            yield return null; Tick(0.1f, brute, a, b);
            Vector3 facing = brute.SweepForward;
            a.GetComponent<NavMeshAgent>().Warp(-facing * 2);
            FinishSweep(brute, brute, a, b);
            Assert.That(Vector3.Dot(brute.transform.forward, facing), Is.GreaterThan(0.99f));
            Assert.That(a.Health, Is.EqualTo(100)); Assert.That(b.Health, Is.EqualTo(90));
        }

        [UnityTest] public IEnumerator RestedSurvivorsReserveSeparateSweepEscapePoints()
        {
            var brute = BruteAt(Vector3.zero);
            var a = Actor("Alpha", new Vector3(-0.65f, 0, 1.4f));
            var b = Actor("Bravo", new Vector3(0.65f, 0, 1.4f));
            yield return null; Tick(0.1f, brute, a, b);
            Assert.That(a.Action, Is.EqualTo(CombatAction.Ready), "Civilian must first read the windup.");
            float reaction = a.SweepReactionRemaining;
            Assert.That(reaction, Is.GreaterThan(0));
            Tick(0, brute, a, b); Assert.That(a.SweepReactionRemaining, Is.EqualTo(reaction));
            Tick(reaction + 0.01f, brute, a, b);
            Assert.That(a.Action, Is.EqualTo(CombatAction.Dodge)); Assert.That(b.Action, Is.EqualTo(CombatAction.Dodge));
            Assert.That(a.MovementGoal.magnitude, Is.GreaterThan(CombatDirector.SweepRadius));
            Assert.That(b.MovementGoal.magnitude, Is.GreaterThan(CombatDirector.SweepRadius));
            Assert.That(Vector3.Distance(a.MovementGoal, b.MovementGoal), Is.GreaterThanOrEqualTo(0.95f));
            Assert.That(a.Energy, Is.LessThan(a.Attributes.MaximumStamina));
        }

        [UnityTest] public IEnumerator RepeatedHitsDamageBruteWithoutResettingLungeAndBiteStillCanBeRescued()
        {
            var human = Actor("Civilian", Vector3.zero);
            var brute = BruteAt(Vector3.forward * 1.2f);
            yield return null;
            for (int i = 0; i < 20 && brute.Action != CombatAction.Stagger; i++) Tick(0.1f, human, brute);
            Assert.That(brute.StaggerResistanceRemaining, Is.GreaterThan(0));
            Assert.That(brute.Health, Is.EqualTo(158));
            for (int i = 0; i < 20 && brute.Health == 158; i++) Tick(0.1f, human, brute);
            Assert.That(brute.Health, Is.EqualTo(136));
            Assert.That(brute.Action, Is.EqualTo(CombatAction.Lunge), "Second hit must not restart stagger.");
            for (int i = 0; i < 10 && human.Grabber == null; i++) Tick(0.1f, human, brute);
            Assert.That(human.Grabber, Is.EqualTo(brute));
            Assert.That(brute.StaggerResistanceRemaining, Is.GreaterThan(0));
            var ally = Actor("Rescuer", new Vector3(1.1f, 0, 1.2f), false, true);
            for (int i = 0; i < 6 && human.Grabber != null; i++) Tick(0.1f, human, brute, ally);
            Assert.That(human.Grabber, Is.Null); Assert.That(human.Person.Infectable, Is.True);
            Assert.That(brute.Action, Is.EqualTo(CombatAction.Stagger));
        }

        [UnityTest] public IEnumerator RecoveryGivesBonusDamageWithoutShorteningCounterattackWindow()
        {
            var brute = BruteAt(Vector3.zero);
            var a = Actor("Alpha", new Vector3(-0.65f, 0, 1.4f));
            var b = Actor("Bravo", new Vector3(0.65f, 0, 1.4f));
            a.TrySpend(a.Energy); b.TrySpend(b.Energy);
            yield return null; Tick(0.1f, brute, a, b); FinishSweep(brute, brute, a, b);
            var attacker = Actor("Counterattacker", Vector3.forward * 1.3f);
            for (int i = 0; i < 9 && brute.Health == 180; i++) Tick(0.1f, brute, attacker);
            Assert.That(brute.Health, Is.EqualTo(180 - attacker.Attributes.Damage * 1.25f));
            Assert.That(brute.SweepRecovery, Is.True); Assert.That(brute.Action, Is.EqualTo(CombatAction.Recover));
            Assert.That(brute.Duration, Is.EqualTo(CombatDirector.SweepRecoverySeconds));
        }

        [UnityTest] public IEnumerator KnockbackStopsAtNavigationWallAndHonorsPause()
        {
            var wall = Make("Wall"); wall.SetActive(false); wall.transform.position = Vector3.right * 2;
            var section = wall.AddComponent<DefenseSection>();
            section.Configure(new Vector3(0.5f, 2.4f, 30), Vector3.right, false, null);
            wall.SetActive(true); section.FinishBuild();
            var human = Actor("Human", Vector3.zero);
            yield return null; yield return null; yield return null;
            human.Person.CombatPush(Vector3.right * 8);
            yield return null;
            Assert.That(human.Person.FeetPosition.x, Is.GreaterThan(0));
            Assert.That(human.Person.FeetPosition.x, Is.LessThan(2));
            human.Person.SetSimulationPaused(true); Vector3 before = human.Person.FeetPosition;
            human.Person.CombatPush(Vector3.back * 2); yield return null;
            Assert.That(Vector3.Distance(human.Person.FeetPosition, before), Is.LessThan(0.05f));
        }

        [UnityTest] public IEnumerator CrowdedSurvivorsChooseSeparateMeleePositions()
        {
            var a = Actor("Alpha", Vector3.zero);
            var b = Actor("Bravo", Vector3.right * 0.95f);
            var zombie = Actor("Zombie", new Vector3(0.5f, 0, 2.5f), true);
            yield return null; Tick(0.1f, a, b, zombie);
            Assert.That(a.Action, Is.EqualTo(CombatAction.Reposition));
            Assert.That(b.Action, Is.EqualTo(CombatAction.Reposition));
            Assert.That(Vector3.Distance(a.MovementGoal, b.MovementGoal), Is.GreaterThanOrEqualTo(1.15f));
            Assert.That(Vector3.Distance(a.MovementGoal, a.Anchor), Is.LessThanOrEqualTo(4));
            Assert.That(Vector3.Distance(b.MovementGoal, b.Anchor), Is.LessThanOrEqualTo(4));
        }

        [UnityTest] public IEnumerator BruteWaitsForCloseCrowdBeforeCommittingSweep()
        {
            var brute = BruteAt(Vector3.zero);
            var a = Actor("Alpha", new Vector3(-1, 0, 1.8f));
            var b = Actor("Bravo", new Vector3(1, 0, 1.8f));
            yield return null; Tick(0.1f, brute, a, b);
            Assert.That(brute.Action, Is.Not.EqualTo(CombatAction.Sweep));
            Assert.That(brute.SweepCooldown, Is.Zero);
        }

        [UnityTest] public IEnumerator MissedSweepHasShortRecoveryWithoutBonusDamage()
        {
            var brute = BruteAt(Vector3.zero);
            var a = Actor("Alpha", new Vector3(-0.65f, 0, 1.4f));
            var b = Actor("Bravo", new Vector3(0.65f, 0, 1.4f));
            a.TrySpend(a.Energy); b.TrySpend(b.Energy);
            yield return null; Tick(0.1f, brute, a, b);
            Vector3 facing = brute.SweepForward;
            Vector3 side = Vector3.Cross(Vector3.up, facing);
            a.GetComponent<NavMeshAgent>().Warp(-facing * 2 + side * 0.6f);
            b.GetComponent<NavMeshAgent>().Warp(-facing * 2 - side * 0.6f);
            FinishSweep(brute, brute, a, b);
            Assert.That(brute.LastSweepHits, Is.Zero);
            Assert.That(brute.Duration, Is.EqualTo(CombatDirector.MissedSweepRecoverySeconds));
            Assert.That(brute.SweepCooldown, Is.LessThanOrEqualTo(2));
            var counter = Actor("Veteran", Vector3.forward * 1.3f, false, true);
            for (int i = 0; i < 6 && brute.Health == 180; i++) Tick(0.1f, brute, counter);
            Assert.That(brute.Health, Is.EqualTo(180 - counter.Attributes.Damage));
        }

        [UnityTest] public IEnumerator EffectiveCombatAwardsXpButFailedActionsDoNot()
        {
            var human = Actor("Learner", Vector3.zero);
            var zombie = Actor("Zombie", Vector3.forward * 1.3f, true);
            yield return null;
            int before = human.Experience;
            for (int i = 0; i < 8 && zombie.Health == 80; i++) Tick(0.1f, human, zombie);
            Assert.That(human.Experience, Is.EqualTo(before + 3));
            int afterHit = human.Experience;
            zombie.GetComponent<NavMeshAgent>().Warp(Vector3.forward * 8);
            for (int i = 0; i < 10; i++) Tick(0.1f, human, zombie);
            Assert.That(human.Experience, Is.EqualTo(afterHit), "Movement and missed actions do not award XP.");
        }

        [UnityTest] public IEnumerator BiteRescueAndVictoryProduceAttributedSummary()
        {
            var victim = Actor("Victim", Vector3.zero); victim.TrySpend(victim.Energy);
            var zombie = Actor("Zombie", Vector3.forward * 1.1f, true);
            yield return null; UntilGrab(victim, zombie);
            var rescuer = Actor("Rescuer", new Vector3(1.1f, 0, 1.1f), false, true);
            int before = rescuer.Experience;
            for (int i = 0; i < 30 && zombie.Person.State != InfectionState.Neutralized; i++) Tick(0.1f, victim, zombie, rescuer);
            Assert.That(victim.Person.Infectable, Is.True);
            Assert.That(rescuer.Experience, Is.GreaterThan(before + 15));
            Assert.That(rescuer.SessionExperienceSummary, Does.Contain("Bite rescue"));
            Assert.That(rescuer.SessionExperienceSummary, Does.Contain("Threat defeated"));
            Assert.That(rescuer.SessionExperienceSummary, Does.Contain("Survived encounter"));
        }

        [UnityTest] public IEnumerator RankGrowthChangesAttributesButNotHumanHealth()
        {
            var human = Actor("Learner", Vector3.zero);
            human.AwardExperience("Test training", ProgressionRules.VeteranExperience);
            Assert.That(human.Rank, Is.EqualTo(SurvivorRank.Veteran));
            Assert.That(human.Attributes.strength, Is.EqualTo(12));
            Assert.That(human.Health, Is.EqualTo(100));
            Assert.That(human.ExperienceStatus, Does.StartWith("Veteran"));
            yield return null;
        }

        [UnityTest] public IEnumerator ValidationLabSwitchesPopulationWithoutActivatingOtherScenarios()
        {
            var firstRoot = Make("First scenario"); var secondRoot = Make("Second scenario");
            var first = Actor("First", Vector3.zero); var second = Actor("Second", Vector3.right * 3);
            first.transform.SetParent(firstRoot.transform); second.transform.SetParent(secondRoot.transform);
            var outbreak = Make("Outbreak").AddComponent<OutbreakDirector>();
            var lab = outbreak.gameObject.AddComponent<SystemsValidationLab>();
            lab.Configure(new[] { firstRoot, secondRoot }, new[] { "First", "Second" }, outbreak);
            lab.Select(0);
            yield return null;
            Assert.That(firstRoot.activeSelf, Is.True); Assert.That(secondRoot.activeSelf, Is.False);
            Assert.That(outbreak.Population.Count, Is.EqualTo(1));
            Assert.That(lab.Select(1), Is.True);
            Assert.That(firstRoot.activeSelf, Is.False); Assert.That(secondRoot.activeSelf, Is.True);
            Assert.That(outbreak.Population[0], Is.EqualTo(second.Person));
            Assert.That(lab.Select(99), Is.False);
        }
    }
}
