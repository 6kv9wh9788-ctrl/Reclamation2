using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Tests
{
    public sealed class CrowdCommunicationTests
    {
        private static CrowdBattleSimulation Setup()
        {
            var sim=new CrowdBattleSimulation(320);sim.EnableSquads();sim.EnableCommunication();
            for(int i=0;i<320;i++)sim.Units[i].position=sim.Units[i].home=new Vector3(500+i*3,0,500);
            sim.Units[0].position=new Vector3(0,0,-10);sim.Units[0].forward=Vector3.forward;
            sim.Units[5].position=new Vector3(0,0,-12);sim.Units[5].forward=Vector3.back;
            sim.Units[1].position=new Vector3(0,0,-4);sim.Units[1].forward=Vector3.forward;
            return sim;
        }
        [Test] public void AllyGetsSnapshotWithoutTargetAndCannotRelayIt()
        {
            var sim=Setup();sim.Step(1f/60f);var state=sim.Perception.States[5];
            Assert.That(state.reported,Is.True);Assert.That(state.visibleTarget,Is.EqualTo(-1));Assert.That(sim.Units[5].target,Is.EqualTo(-1));
            Vector3 snapshot=state.knownPosition;sim.Units[1].position=new Vector3(100,0,100);
            long broadcasts=sim.Communication.Broadcasts;sim.Communication.ReportSight(5);
            Assert.That(sim.Communication.Broadcasts,Is.EqualTo(broadcasts));Assert.That(state.knownPosition,Is.EqualTo(snapshot));
            sim.Perception.Tick(3.1f);Assert.That(sim.Perception.TryKnownPosition(5,out _),Is.False);
        }
        [Test] public void ReportsStayWithinRangeFactionAndFriendlySquad()
        {
            var sim=Setup();sim.Units[10].position=new Vector3(0,0,-23);sim.Units[160].position=new Vector3(1,0,-12);
            sim.Units[2].position=new Vector3(-2,0,-12);sim.Step(1f/60f);
            Assert.That(sim.Perception.States[5].reported,Is.True);
            Assert.That(sim.Perception.States[10].reported,Is.False);
            Assert.That(sim.Perception.States[160].reported,Is.False);
            Assert.That(sim.Perception.States[2].reported,Is.False);
        }
        [Test] public void CooldownRequiresFreshSightAndPreventsBroadcastSpam()
        {
            var sim=Setup();sim.Step(1f/60f);long sent=sim.Communication.Broadcasts;
            for(int i=0;i<10;i++)sim.Communication.ReportSight(0);Assert.That(sim.Communication.Broadcasts,Is.EqualTo(sent));
            sim.Perception.Tick(1.1f);sim.Communication.ReportSight(0);Assert.That(sim.Communication.Broadcasts,Is.EqualTo(sent));
            sim.Perception.Sense(0,1);sim.Communication.ReportSight(0);Assert.That(sim.Communication.Broadcasts,Is.EqualTo(sent+1));
        }
        [Test] public void OwnVisualMemoryWinsOverReports()
        {
            var sim=Setup();sim.Perception.Sense(5,1);var own=sim.Perception.States[5].knownPosition;
            Assert.That(sim.Perception.ReceiveReport(5,new Vector3(20,0,20),sim.Perception.Time),Is.False);
            sim.Perception.Sense(5,-1);Assert.That(sim.Perception.ReceiveReport(5,new Vector3(20,0,20),sim.Perception.Time),Is.False);
            Assert.That(sim.Perception.States[5].knownPosition,Is.EqualTo(own));
        }
        [Test] public void HoldAndMoveRemainAuthoritativeAfterAlert()
        {
            var sim=Setup();Vector3 held=sim.Units[5].position;sim.Step(1f/60f);Assert.That(sim.Units[5].position,Is.EqualTo(held));
            sim.Units[1].position=new Vector3(500,0,900);sim.Squads.Issue(0,CrowdSquadCommands.Command.Move,new Vector3(0,0,-25));
            for(int i=0;i<120;i++)sim.Step(1f/60f);
            Assert.That(sim.Units[5].position.z,Is.LessThan(held.z));
        }
        [Test] public void SolidBarrierMufflesButWaterDoesNotBlockLocalReports()
        {
            foreach(var layout in new[]{CrowdNavigation.Layout.Wall,CrowdNavigation.Layout.Bridge})
            {
                var sim=Setup();sim.EnableNavigation(layout);
                sim.Units[0].position=new Vector3(0,0,-4);sim.Units[5].position=new Vector3(0,0,4);
                sim.Units[1].position=new Vector3(0,0,-10);sim.Units[0].forward=Vector3.back;
                sim.Step(1f/60f);
                Assert.That(sim.Perception.States[5].reported,Is.EqualTo(layout==CrowdNavigation.Layout.Bridge));
            }
        }
    }
}
