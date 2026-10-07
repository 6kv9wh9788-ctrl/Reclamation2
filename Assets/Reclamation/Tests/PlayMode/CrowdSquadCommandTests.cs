using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class CrowdSquadCommandTests
    {
        private static CrowdBattleSimulation Create(bool clearEnemies = true)
        {
            var sim = new CrowdBattleSimulation(1280); sim.EnableSquads();
            if (clearEnemies) foreach (var u in sim.Units) if (u.team == 1) u.position = u.home = new Vector3(1000,0,1000);
            return sim;
        }
        private static void Run(CrowdBattleSimulation sim, int frames)
        { for (int i=0;i<frames;i++) sim.Step(1f/60f); }
        [Test] public void EightSquadsHaveDistinctSlotsAndThirtyTwoMembers()
        {
            var sim=Create(); Assert.That(sim.Squads.Squads.Length,Is.EqualTo(8));
            foreach(var s in sim.Squads.Squads)Assert.That(s.count,Is.EqualTo(32));
            for(int i=0;i<sim.Units.Length;i+=5)for(int j=i+5;j<sim.Units.Length;j+=5)
                Assert.That(Vector3.Distance(sim.Units[i].position,sim.Units[j].position),Is.GreaterThan(1));
        }
        [Test] public void AllSquadsReachTranslatedSlotsWithoutPersistentClumping()
        {
            var sim=Create();
            for(int s=0;s<8;s++)sim.Squads.Issue(s,CrowdSquadCommands.Command.Move,sim.Squads.Squads[s].anchor+Vector3.forward*8);
            Run(sim,600);
            for(int i=0;i<sim.Units.Length;i+=5)
            {
                Assert.That(Vector3.Distance(sim.Units[i].position,sim.Squads.Slot(i)),Is.LessThan(.2f),"slot "+i);
                for(int j=i+5;j<sim.Units.Length;j+=5)Assert.That(Vector3.Distance(sim.Units[i].position,sim.Units[j].position),Is.GreaterThan(.65f));
            }
            Assert.That(sim.Attacks,Is.Zero);
        }
        [Test] public void HoldStopsMarchAndNeverChases()
        {
            var sim=Create();sim.Squads.Issue(0,CrowdSquadCommands.Command.Move,Vector3.zero);Run(sim,120);
            sim.Squads.Issue(0,CrowdSquadCommands.Command.Hold,Vector3.zero);Vector3 stopped=sim.Units[0].position;
            sim.Units[1].position=sim.Units[1].home=stopped+Vector3.forward*5;
            // Keep the opponent out of reach by pinning its position after each step.
            for(int i=0;i<180;i++){sim.Units[1].position=stopped+Vector3.forward*5;sim.Step(1f/60f);}
            Assert.That(sim.Units[0].position,Is.EqualTo(stopped));
        }
        [Test] public void AttackEngagesAndNewMoveWaitsForCommittedStrike()
        {
            var sim=Create();var u=sim.Units[0];sim.Units[1].position=sim.Units[1].home=u.position+Vector3.forward*1.5f;
            sim.Squads.Issue(0,CrowdSquadCommands.Command.Attack,sim.Squads.Squads[0].anchor);
            Run(sim,1);Assert.That(u.fighter.Action,Is.EqualTo(DuelAction.Windup));
            Vector3 before=u.position;sim.Squads.Issue(0,CrowdSquadCommands.Command.Move,Vector3.back*30);
            Run(sim,5);Assert.That(u.position,Is.EqualTo(before));Run(sim,90);Assert.That(sim.Hits,Is.GreaterThan(0));
        }
        [Test] public void CommandAffectsOnlySelectedSquadAndMoveDoesNotAttack()
        {
            var sim=Create();Vector3 other=sim.Squads.Squads[1].anchor;
            sim.Squads.Issue(0,CrowdSquadCommands.Command.Move,sim.Squads.Squads[0].anchor+Vector3.back*4);
            Run(sim,240);Assert.That(sim.Squads.Squads[1].anchor,Is.EqualTo(other));
            Assert.That(sim.Attacks,Is.Zero);
        }
        [Test] public void MoveIgnoresNearbyEnemyWhileHoldDefendsWithoutMoving()
        {
            foreach(bool hold in new[]{false,true})
            {
                var sim=Create();var u=sim.Units[0];Vector3 before=u.position;
                sim.Units[1].position=sim.Units[1].home=before+Vector3.forward*1.5f;
                sim.Squads.Issue(0,hold?CrowdSquadCommands.Command.Hold:CrowdSquadCommands.Command.Move,
                    sim.Squads.Squads[0].anchor+Vector3.back*4);
                Run(sim,1);
                Assert.That(u.fighter.AttackSequence,Is.EqualTo(hold?1:0));
                if(hold)Assert.That(u.position,Is.EqualTo(before));
            }
        }
    }
}
