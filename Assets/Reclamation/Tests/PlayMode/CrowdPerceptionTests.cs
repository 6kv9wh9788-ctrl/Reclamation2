using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class CrowdPerceptionTests
    {
        private static CrowdBattleSimulation Pair(bool wall=false)
        {
            var sim=new CrowdBattleSimulation(5);
            if(wall){sim.EnableSquads();sim.EnableNavigation(CrowdNavigation.Layout.Wall);}
            sim.EnablePerception();sim.Units[0].position=Vector3.zero;sim.Units[0].forward=Vector3.forward;
            for(int i=1;i<5;i++)sim.Units[i].position=sim.Units[i].home=new Vector3(100+i*10,0,100);
            return sim;
        }
        [Test] public void VisionRejectsBehindOutsideConeAndBeyondRange()
        {
            var sim=Pair();var p=sim.Perception;var enemy=sim.Units[1];
            enemy.position=Vector3.forward*8;Assert.That(p.CanSee(0,1),Is.True);
            enemy.position=Vector3.back*2;Assert.That(p.CanSee(0,1),Is.False);
            enemy.position=Vector3.right*4;Assert.That(p.CanSee(0,1),Is.False);
            enemy.position=Vector3.forward*13;Assert.That(p.CanSee(0,1),Is.False);
        }
        [Test] public void WallBlocksVisionButOpenSideDoesNot()
        {
            var sim=Pair(true);sim.Units[0].position=new Vector3(0,0,-5);sim.Units[1].position=new Vector3(0,0,5);
            Assert.That(sim.Perception.CanSee(0,1),Is.False);
            sim.Units[0].position=new Vector3(20,0,-5);sim.Units[1].position=new Vector3(20,0,5);
            Assert.That(sim.Perception.CanSee(0,1),Is.True);
        }
        [Test] public void RiverBlocksWalkingButNotVisionOrHearing()
        {
            var sim=new CrowdBattleSimulation(5);sim.EnableSquads();sim.EnableNavigation(CrowdNavigation.Layout.Bridge);sim.EnablePerception();
            sim.Units[0].position=new Vector3(15,0,-4);sim.Units[0].forward=Vector3.forward;sim.Units[1].position=new Vector3(15,0,4);
            Assert.That(sim.Navigation.Visible(sim.Units[0].position,sim.Units[1].position),Is.False);
            Assert.That(sim.Perception.CanSee(0,1),Is.True);
            sim.Perception.Emit(1,sim.Units[1].position);sim.Perception.Sense(0,-1);
            Assert.That(sim.Perception.States[0].heard,Is.True);
        }
        [Test] public void LostTargetMemoryKeepsSnapshotThenExpires()
        {
            var sim=Pair();sim.Units[1].position=Vector3.forward*5;sim.Perception.Sense(0,1);
            sim.Units[1].position=new Vector3(50,0,50);sim.Perception.Tick(.1f);sim.Perception.Sense(0,-1);
            Assert.That(sim.Perception.States[0].visibleTarget,Is.EqualTo(-1));
            Assert.That(sim.Perception.TryKnownPosition(0,out var remembered),Is.True);Assert.That(remembered,Is.EqualTo(Vector3.forward*5));
            sim.Perception.Tick(4);Assert.That(sim.Perception.TryKnownPosition(0,out _),Is.False);
        }
        [Test] public void HearingWorksBehindButDoesNotRevealTargetOrReplayExpiredNoise()
        {
            var sim=Pair();sim.Perception.Emit(1,Vector3.back*4);sim.Perception.Sense(0,-1);
            Assert.That(sim.Perception.States[0].heard,Is.True);Assert.That(sim.Perception.States[0].visibleTarget,Is.EqualTo(-1));
            Assert.That(sim.Perception.TryKnownPosition(0,out var known),Is.True);Assert.That(known,Is.EqualTo(Vector3.back*4));
            sim.Perception.Tick(3);sim.Perception.Sense(0,-1);Assert.That(sim.Perception.TryKnownPosition(0,out _),Is.False);
        }
        [Test] public void HearingHasRangeAndWallMuffling()
        {
            var sim=Pair();sim.Perception.Emit(1,Vector3.back*11);sim.Perception.Sense(0,-1);Assert.That(sim.Perception.TryKnownPosition(0,out _),Is.False);
            sim=Pair(true);sim.Units[0].position=new Vector3(0,0,-4);sim.Perception.Emit(1,new Vector3(0,0,4));sim.Perception.Sense(0,-1);
            Assert.That(sim.Perception.TryKnownPosition(0,out _),Is.False);
        }
        [Test] public void BehindEnemyNeedsSoundBeforeObserverTurnsAndSeesIt()
        {
            var sim=Pair();sim.FriendlyOrder=CrowdBattleSimulation.Order.Hold;sim.Units[1].position=sim.Units[1].home=Vector3.back*4;
            sim.Step(1f/60f);Assert.That(sim.Units[0].target,Is.EqualTo(-1));
            sim.Perception.Emit(1,Vector3.back*4);Vector3 start=sim.Units[0].position;
            for(int i=0;i<12;i++)sim.Step(1f/60f);
            Assert.That(sim.Units[0].target,Is.EqualTo(1));Assert.That(sim.Units[0].position,Is.EqualTo(start));
        }
        [Test] public void SquadMoveIgnoresNoiseAndRespawnForgets()
        {
            var sim=new CrowdBattleSimulation(160);sim.EnableSquads();sim.EnablePerception();
            sim.Squads.Issue(0,CrowdSquadCommands.Command.Move,sim.Squads.Squads[0].anchor+Vector3.forward*4);
            Vector3 start=sim.Units[0].position;sim.Perception.Emit(-1,start+Vector3.back*4);
            for(int i=0;i<60;i++)sim.Step(1f/60f);
            Assert.That(sim.Units[0].position.z,Is.GreaterThan(start.z));
            for(int i=0;i<10;i++)sim.Units[0].fighter.ReceiveAttack(BlightEquipment.Weapon(BlightWeapon.Sword,true),false);
            Assert.That(sim.Units[0].fighter.Alive,Is.False);sim.Step(1f/60f);
            Assert.That(sim.Units[0].fighter.Alive,Is.True);Assert.That(sim.Perception.TryKnownPosition(0,out _),Is.False);
        }
    }
}
