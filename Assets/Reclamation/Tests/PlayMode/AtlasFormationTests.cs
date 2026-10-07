using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reclamation.Atlas;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class AtlasFormationTests
    {
        private static float Planar(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));
        [UnityTest] public IEnumerator PlatoonTravelsDeploysAndReturnsWithoutTeleporting()
        {
            var go=new GameObject("Formation acceptance");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                demo.AutomaticClock=false;
                foreach(var config in new[]{(AtlasSize.Small,4),(AtlasSize.Medium,8),(AtlasSize.Medium,12)})
                {
                    demo.LoadMap(config.Item1,config.Item2);yield return null;demo.VisitCommander();demo.SetHour(9);
                    for(int i=0;i<25;i++)for(int j=i+1;j<25;j++)Assert.That(Planar(demo.GuardPosition(i),demo.GuardPosition(j)),Is.GreaterThan(.7f),$"Initial body overlap {i}/{j}");
                    Assert.That(demo.BeginScoutEncounter(),Is.True);
                    for(int step=0;step<1500&&demo.ScoutPhase!=ScoutEncounterPhase.Responding;step++)demo.TickVillage(.2f);
                    Assert.That(demo.ScoutPhase,Is.EqualTo(ScoutEncounterPhase.Responding));
                    int p=demo.ActiveCompany.RespondingPlatoon,first=1+p*6;
                    bool sawColumn=false,sawLine=false;
                    var previous=new Vector3[25];
                    for(int step=0;step<2300&&demo.ScoutPhase!=ScoutEncounterPhase.Secured;step++)
                    {
                        for(int i=0;i<25;i++)previous[i]=demo.GuardPosition(i);
                        demo.TickVillage(.2f);
                        sawColumn|=demo.FormationPhase(p)==AtlasFormationPhase.Column;
                        if(demo.FormationPhase(p)==AtlasFormationPhase.Column)Assert.That(demo.ResponseGuideStage,Is.EqualTo("Travelling"));
                        if(demo.FormationPhase(p)==AtlasFormationPhase.Deploying)Assert.That(demo.ResponseGuideStage,Is.EqualTo("Deploying"));
                        if(demo.FormationPhase(p)==AtlasFormationPhase.Line)
                        {
                            sawLine=true;
                            Assert.That(demo.ResponseGuideStage,Is.EqualTo("Holding"));
                            var layout=demo.LayoutFor(demo.Model.factions[0].capital);Vector2 forward=layout.Hinterland(Vector2.right);
                            for(int i=0;i<6;i++)Assert.That(Vector3.Dot(demo.GuardFacing(first+i),new Vector3(forward.x,0,forward.y)),Is.GreaterThan(.99f));
                            for(int i=1;i<6;i++)for(int j=i+1;j<6;j++)Assert.That(Planar(demo.GuardPosition(first+i),demo.GuardPosition(first+j)),Is.GreaterThan(1.5f));
                        }
                        for(int i=1;i<25;i++)
                        {
                            Assert.That(Planar(previous[i],demo.GuardPosition(i)),Is.LessThanOrEqualTo(.271f),"No warp or formation snap");
                            Assert.That(demo.IsSolidAt(demo.GuardPosition(i)),Is.False,"Guard inside a building");
                        }
                        for(int i=0;i<6;i++)for(int j=i+1;j<6;j++)Assert.That(Planar(demo.GuardPosition(first+i),demo.GuardPosition(first+j)),Is.GreaterThan(.70f),$"Reserve spacing {config}, step {step}, pair {i}/{j}, phase {demo.FormationPhase(p)}");
                    }
                    Assert.That(sawColumn,Is.True);Assert.That(sawLine,Is.True,"All six must occupy their defensive slots");
                    Assert.That(demo.ScoutPhase,Is.EqualTo(ScoutEncounterPhase.Secured));
                    Assert.That(demo.FormationPhase(p),Is.EqualTo(AtlasFormationPhase.Returning));
                    Assert.That(demo.ResponseGuideStage,Is.EqualTo("Returning"));
                    Assert.That(demo.ResponseGuideHighlighted,Is.True);
                    for(int step=0;step<2400&&demo.FormationPhase(p)==AtlasFormationPhase.Returning;step++)
                    {
                        for(int i=0;i<25;i++)previous[i]=demo.GuardPosition(i);
                        demo.TickVillage(.2f);
                        for(int i=0;i<6;i++)
                        {
                            Assert.That(Planar(previous[first+i],demo.GuardPosition(first+i)),Is.LessThanOrEqualTo(.271f));
                            Assert.That(demo.IsSolidAt(demo.GuardPosition(first+i)),Is.False);
                            for(int j=i+1;j<6;j++)Assert.That(Planar(demo.GuardPosition(first+i),demo.GuardPosition(first+j)),Is.GreaterThan(.7f),$"Return spacing {config}, {step}, {i}/{j}");
                        }
                    }
                    Assert.That(demo.FormationPhase(p),Is.EqualTo(AtlasFormationPhase.Posted),"Reserve must finish the trip home: "+demo.FormationDiagnostic(p));
                    Assert.That(demo.ResponseGuideStage,Is.EqualTo("Watch resumed"));
                    Assert.That(demo.ResponseGuideHighlighted,Is.False);
                    demo.SurveyPaused=true;var stopped=demo.GuardPosition(first);demo.TickVillage(30);Assert.That(demo.GuardPosition(first),Is.EqualTo(stopped));demo.SurveyPaused=false;
                }
            }
            finally{Object.Destroy(go);}yield return null;
        }

        [UnityTest] public IEnumerator FarmWatchHandoverCanReportAfterRapidClockChanges()
        {
            var go=new GameObject("Watch handover report");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                demo.AutomaticClock=false;
                foreach(var config in new[]{(AtlasSize.Small,4),(AtlasSize.Medium,8),(AtlasSize.Medium,12)})
                {
                demo.LoadMap(config.Item1,config.Item2);yield return null;demo.VisitCommander();
                demo.SetHour(12);demo.TickVillage(.3f);demo.SetHour(23);demo.TickVillage(.3f);demo.SetHour(9);
                Assert.That(demo.BeginScoutEncounter(),Is.True);
                for(int step=0;step<1500&&demo.ScoutPhase!=ScoutEncounterPhase.Responding&&demo.ScoutPhase!=ScoutEncounterPhase.Escaped;step++)demo.TickVillage(.2f);
                Assert.That(demo.ScoutPhase,Is.EqualTo(ScoutEncounterPhase.Responding));
                Assert.That(demo.ActiveCompany.ReportingPlatoon,Is.InRange(0,3));
                Assert.That(demo.ActiveCompany.RespondingPlatoon,Is.InRange(0,3));
                Assert.That(demo.ActiveCompany.ReportDelivered,Is.True);
                Assert.That(demo.VisitScoutContact(),Is.True);
                }
            }
            finally{Object.Destroy(go);}yield return null;
        }

        [UnityTest] public IEnumerator AcceleratedWatchKeepsPatrolsMovingAndSeparatesMembers()
        {
            var go=new GameObject("Accelerated formation acceptance");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                demo.AutomaticClock=false;demo.VisitCommander();demo.SetHour(9);
                typeof(WorldAtlasDemo).GetField("clockRate",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(demo,60);
                var start=demo.GuardPosition(1);
                for(int step=0;step<120;step++)
                {
                    var before=Enumerable.Range(0,25).Select(demo.GuardPosition).ToArray();demo.TickVillage(1f/60);
                    for(int i=1;i<25;i++)
                    {
                        Assert.That(Planar(before[i],demo.GuardPosition(i)),Is.LessThanOrEqualTo(1.351f));
                        Assert.That(demo.IsSolidAt(demo.GuardPosition(i)),Is.False);
                    }
                }
                Assert.That(Planar(start,demo.GuardPosition(1)),Is.GreaterThan(5));
                for(int p=0;p<4;p++)for(int i=0;i<6;i++)for(int j=i+1;j<6;j++)
                    Assert.That(Planar(demo.GuardPosition(1+p*6+i),demo.GuardPosition(1+p*6+j)),Is.GreaterThan(.7f));
            }
            finally{Object.Destroy(go);}yield return null;
        }
    }
}
