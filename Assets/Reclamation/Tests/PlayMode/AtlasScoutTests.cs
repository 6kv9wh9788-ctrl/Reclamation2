using System.Collections;
using NUnit.Framework;
using Reclamation.Atlas;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class AtlasScoutTests
    {
        [Test] public void ContactMustBeSeenAndReportMustArriveBeforeResponse()
        {
            var e=new AtlasScoutEncounter();Assert.That(e.Start(),Is.True);Assert.That(e.Start(),Is.False);
            e.Tick(30,false,6,false);Assert.That(e.Phase,Is.EqualTo(ScoutEncounterPhase.Unseen));Assert.That(e.Known,Is.False);
            Assert.That(e.Challenge(1,6),Is.False);e.Tick(.1f,true,0,false);Assert.That(e.Phase,Is.EqualTo(ScoutEncounterPhase.Reporting));
            e.Tick(2,false,0,false);Assert.That(e.Phase,Is.EqualTo(ScoutEncounterPhase.Reporting));
            e.Tick(1,false,0,false);Assert.That(e.Phase,Is.EqualTo(ScoutEncounterPhase.Responding));
            Assert.That(e.Challenge(20,6),Is.False);Assert.That(e.Challenge(5,2),Is.False);Assert.That(e.Challenge(5,3),Is.True);
            e.Tick(1,false,0,false);Assert.That(e.Phase,Is.EqualTo(ScoutEncounterPhase.Withdrawing));
            e.Tick(1,false,0,true);Assert.That(e.Phase,Is.EqualTo(ScoutEncounterPhase.Secured));Assert.That(e.PlayerAssisted,Is.True);
        }
        [Test] public void CommanderCanDeterScoutsWithoutPlayerAndLostContactIsNotVictory()
        {
            var e=new AtlasScoutEncounter();e.Start();e.Tick(1,true,0,false);e.Tick(3,false,0,false);
            e.Tick(59,false,3,false);Assert.That(e.Phase,Is.EqualTo(ScoutEncounterPhase.Responding));
            e.Tick(1,false,3,false);e.Tick(1,false,0,true);Assert.That(e.Phase,Is.EqualTo(ScoutEncounterPhase.Secured));Assert.That(e.PlayerAssisted,Is.False);
            e.Start();e.Tick(180,false,6,false);e.Tick(1,false,0,true);Assert.That(e.Phase,Is.EqualTo(ScoutEncounterPhase.Escaped));Assert.That(e.Known,Is.False);
        }
        [Test] public void LateReserveGetsFullDeterrenceWindowOnceInPosition()
        {
            var e=new AtlasScoutEncounter();e.Start();e.Tick(1,true,0,false);e.Tick(3,false,0,false);
            e.Tick(299,false,0,false);e.Tick(59,false,3,false);
            Assert.That(e.Phase,Is.EqualTo(ScoutEncounterPhase.Responding));
            e.Tick(1,false,3,false);e.Tick(1,false,0,true);
            Assert.That(e.Phase,Is.EqualTo(ScoutEncounterPhase.Secured));
        }
        [UnityTest] public IEnumerator ReportReserveChallengeAndPauseWorkOnEveryDemoPreset()
        {
            var go=new GameObject("Scout integration test");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                demo.AutomaticClock=false;
                foreach(var setting in new[]{(AtlasSize.Small,4),(AtlasSize.Medium,8),(AtlasSize.Medium,12)})
                {
                    demo.LoadMap(setting.Item1,setting.Item2);yield return null;demo.VisitCommander();demo.SetHour(9);
                    for(int i=0;i<200;i++)demo.TickVillage(.2f);
                    Assert.That(demo.BeginScoutEncounter(),Is.True);Assert.That(demo.VisitScoutContact(),Is.False);Assert.That(demo.ChallengeScouts(),Is.False);
                    var gatePositions=new Vector3[24];var gates=new bool[24];
                    for(int i=0;i<24;i++){gates[i]=demo.GuardDuty(i)==AtlasGuardDuty.Gate;gatePositions[i]=demo.GuardPosition(i+1);}
                    demo.SurveyPaused=true;float age=demo.ScoutElapsed;Vector3 contact=demo.ScoutContactPosition;
                    demo.TickVillage(50);Assert.That(demo.ScoutElapsed,Is.EqualTo(age));Assert.That(demo.ScoutContactPosition,Is.EqualTo(contact));demo.SurveyPaused=false;
                    for(int i=0;i<1500&&demo.ScoutPhase!=ScoutEncounterPhase.Responding;i++)demo.TickVillage(.2f);
                    Assert.That(demo.ScoutPhase,Is.EqualTo(ScoutEncounterPhase.Responding));Assert.That(demo.VisitScoutContact(),Is.True);
                    demo.SetHour(18); // relief stays frozen during the response
                    for(int step=0;step<1800&&demo.ScoutSupport<3&&demo.ScoutPhase==ScoutEncounterPhase.Responding;step++)
                    {
                        demo.TickVillage(.2f);
                        for(int i=0;i<24;i++)if(gates[i])Assert.That(Vector3.Distance(gatePositions[i],demo.GuardPosition(i+1)),Is.LessThan(.01f));
                        for(int i=1;i<25;i++)Assert.That(demo.IsSolidAt(demo.GuardPosition(i)),Is.False,"Response route must avoid buildings");
                    }
                    Assert.That(demo.ScoutSupport,Is.GreaterThanOrEqualTo(3));Assert.That(demo.ChallengeScouts(),Is.True);
                    for(int i=0;i<100&&demo.ScoutPhase==ScoutEncounterPhase.Withdrawing;i++)demo.TickVillage(.2f);
                    Assert.That(demo.ScoutPhase,Is.EqualTo(ScoutEncounterPhase.Secured));Assert.That(demo.ScoutPlayerAssisted,Is.True);
                    demo.LoadMap(setting.Item1,setting.Item2);yield return null;Assert.That(demo.ScoutPhase,Is.EqualTo(ScoutEncounterPhase.Idle));
                }
            }
            finally{Object.Destroy(go);}yield return null;
        }
        [UnityTest] public IEnumerator SightRejectsWallsAndRangeAndUnattendedResponseCompletes()
        {
            var go=new GameObject("Scout sight test");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                demo.AutomaticClock=false;demo.VisitCommander();demo.SetHour(9);
                int id=demo.Model.factions[0].capital;var site=demo.Model.sites[id];var layout=demo.LayoutFor(id);
                var a=site.point+layout.House(0)+layout.Rotate(Vector2.right*7);var b=site.point+layout.House(0)-layout.Rotate(Vector2.right*7);
                Assert.That(demo.ScoutSight(new Vector3(a.x,site.elevation,a.y),new Vector3(b.x,site.elevation,b.y)),Is.False);
                Assert.That(demo.ScoutSight(demo.PlayerPosition,demo.PlayerPosition+Vector3.right*30),Is.False);
                Assert.That(demo.BeginScoutEncounter(),Is.True);
                for(int i=0;i<2500&&demo.ScoutPhase!=ScoutEncounterPhase.Secured&&demo.ScoutPhase!=ScoutEncounterPhase.Escaped;i++)demo.TickVillage(.2f);
                Assert.That(demo.ScoutPhase,Is.EqualTo(ScoutEncounterPhase.Secured));Assert.That(demo.ScoutPlayerAssisted,Is.False);
            }
            finally{Object.Destroy(go);}yield return null;
        }
    }
}


