using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class CrowdObjectiveTests
    {
        private static void ClearZone(CrowdObjectiveScenario round)
        {
            foreach (var unit in round.Simulation.Units) unit.position = new Vector3(30, 0, 30);
            for (int i = 0; i < 4; i++) round.Simulation.Units[i].position = round.Center;
        }
        [Test] public void RequiresFourFriendliesAndTenConsecutiveUncontestedSeconds()
        {
            var round = new CrowdObjectiveScenario(); ClearZone(round);
            round.Simulation.Units[3].position = Vector3.zero; round.Tick(10);
            Assert.That(round.Progress, Is.Zero);
            round.Simulation.Units[3].position = round.Center; round.Tick(9);
            Assert.That(round.Outcome, Is.EqualTo(CrowdObjectiveScenario.State.Running));
            round.Simulation.Units[64].position = round.Center; round.Tick(1);
            Assert.That(round.Progress, Is.Zero);
            round.Simulation.Units[64].position = Vector3.zero; round.Tick(10);
            Assert.That(round.Outcome, Is.EqualTo(CrowdObjectiveScenario.State.Secured));
            round.Tick(200); Assert.That(round.Outcome, Is.EqualTo(CrowdObjectiveScenario.State.Secured));
        }
        [Test] public void TimeoutAndRestartHaveFreshState()
        {
            var round = new CrowdObjectiveScenario(); round.Tick(180);
            Assert.That(round.Outcome, Is.EqualTo(CrowdObjectiveScenario.State.TimedOut));
            round = new CrowdObjectiveScenario();
            Assert.That(round.Elapsed, Is.Zero); Assert.That(round.FriendlyAlive, Is.EqualTo(64));
            Assert.That(round.EnemyAlive, Is.EqualTo(32)); Assert.That(round.Simulation.Communication.Delivered, Is.Zero);
        }
        [Test] public void FallenUnitsStayDeadAndDoNotCapture()
        {
            var round = new CrowdObjectiveScenario();
            foreach (var unit in round.Simulation.Units) if (unit.team == 0) unit.fighter.Receive(10000, false, false);
            round.Simulation.Step(1f / 60); round.Tick(1f / 60);
            Assert.That(round.Outcome, Is.EqualTo(CrowdObjectiveScenario.State.Defeated));
            Assert.That(round.Simulation.Respawns, Is.Zero);
        }
        [Test] public void LegacyStressStillRespawnsByDefault()
        {
            var sim = new CrowdBattleSimulation(64); sim.Units[0].fighter.Receive(10000, false, false); sim.Step(1f / 60);
            Assert.That(sim.Respawns, Is.EqualTo(1)); Assert.That(sim.FriendlyCount, Is.EqualTo(13));
        }
        [Test] public void TwoFlanksCanSecureRelayThroughRealSimulation()
        {
            var round = new CrowdObjectiveScenario(); var sim = round.Simulation;
            sim.Squads.Issue(0, CrowdSquadCommands.Command.Attack, new Vector3(-18, 0, 8));
            sim.Squads.Issue(1, CrowdSquadCommands.Command.Attack, new Vector3(18, 0, 8));
            for (int frame = 0; frame < 10801 && round.Outcome == CrowdObjectiveScenario.State.Running; frame++)
            {
                if (frame == 1200)
                {
                    sim.Squads.Issue(0, CrowdSquadCommands.Command.Attack, round.Center + Vector3.left * 2);
                    sim.Squads.Issue(1, CrowdSquadCommands.Command.Attack, round.Center + Vector3.right * 2);
                }
                sim.Step(1f / 60); round.Tick(1f / 60);
            }
            Assert.That(round.Outcome, Is.EqualTo(CrowdObjectiveScenario.State.Secured), "Survivors: " + round.FriendlyAlive + "/" + round.EnemyAlive);
            Assert.That(sim.Communication.Delivered, Is.GreaterThan(0));
            Assert.That(sim.Hits, Is.GreaterThan(0)); Assert.That(sim.Respawns, Is.Zero);
        }
    }
}

