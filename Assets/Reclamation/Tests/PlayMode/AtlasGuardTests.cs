using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Reclamation.Atlas;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class AtlasGuardTests
    {
        [Test] public void EveryWatchCoversAllDutiesAndEverySoldierRotates()
        {
            for(int watch=0;watch<4;watch++)
            {
                var counts=new int[4];
                for(int soldier=0;soldier<24;soldier++)counts[(int)AtlasCompanySchedule.Duty(watch*6,soldier)]++;
                CollectionAssert.AreEqual(new[]{6,6,6,6},counts);
            }
            for(int soldier=0;soldier<24;soldier++)
            {
                var duties=new HashSet<AtlasGuardDuty>();
                for(int watch=0;watch<4;watch++)duties.Add(AtlasCompanySchedule.Duty(watch*6,soldier));
                Assert.That(duties.Count,Is.EqualTo(4));
                Assert.That(AtlasCompanySchedule.Duty(24,soldier),Is.EqualTo(AtlasCompanySchedule.Duty(0,soldier)));
            }
        }
        [Test] public void FieldsVaryDeterministicallyAndKeepWorkersWithinBounds()
        {
            var sizes=new HashSet<Vector2>();
            for(int seed=0;seed<25;seed++)
            {
                var a=new AtlasVillageLayout(seed);var b=new AtlasVillageLayout(seed);
                for(int i=0;i<3;i++)
                {
                    sizes.Add(a.FieldSize(i));Assert.That(a.FieldSize(i),Is.EqualTo(b.FieldSize(i)));
                    Assert.That(a.FieldSize(i).x*a.FieldSize(i).y,Is.GreaterThanOrEqualTo(484));
                }
            }
            Assert.That(sizes.Count,Is.GreaterThan(10));
        }
        [UnityTest] public IEnumerator CompanyRoutesReliefAndNightWatchRemainClearOnAllUiPresets()
        {
            var go=new GameObject("Guard integration test");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                demo.AutomaticClock=false;
                foreach(var setting in new[]{(AtlasSize.Small,4),(AtlasSize.Medium,8),(AtlasSize.Medium,12)})
                {
                    demo.LoadMap(setting.Item1,setting.Item2);yield return null;demo.VisitCommander();demo.SetHour(9);
                    Assert.That(demo.ActiveGuardCount,Is.EqualTo(25));
                    foreach(var site in demo.Model.sites)
                    {
                        if(!(site.owner==-2||site.owner>=0&&demo.Model.factions[site.owner].family==0))continue;
                        var layout=demo.LayoutFor(site.id);
                        for(int duty=0;duty<4;duty++)for(int member=0;member<6;member++)
                        {
                            var route=AtlasCompanySchedule.Route(layout,(AtlasGuardDuty)duty,member);
                            for(int segment=1;segment<route.Length;segment++)for(int sample=0;sample<=8;sample++)
                            {
                                Vector2 p=site.point+Vector2.Lerp(route[segment-1],route[segment],sample/8f);
                                Assert.That(demo.Model.Walkable(p),Is.True,$"Unwalkable {setting}, site {site.id}, duty {duty}, {p}");
                                Assert.That(demo.IsSolidAt(new Vector3(p.x,demo.Model.Height(p.x,p.y),p.y)),Is.False,$"Blocked {setting}, site {site.id}, duty {duty}, {p}");
                            }
                        }
                    }
                    Vector3 initial=demo.GuardPosition(7);
                    for(int step=0;step<200;step++)demo.TickVillage(.2f);
                    Assert.That(Vector3.Distance(demo.GuardPosition(7),initial),Is.GreaterThan(1));
                    var before=new Vector3[25];for(int i=0;i<25;i++)before[i]=demo.GuardPosition(i);
                    demo.SetHour(12);demo.TickVillage(.1f);
                    for(int i=0;i<25;i++)Assert.That(Vector3.Distance(before[i],demo.GuardPosition(i)),Is.LessThan(.3f),"Relief must walk, not teleport");
                    for(int jump=0;jump<6;jump++){demo.SetHour(12+jump*3);for(int step=0;step<15;step++){for(int i=0;i<25;i++)before[i]=demo.GuardPosition(i);demo.TickVillage(.1f);for(int i=0;i<25;i++){Assert.That(Vector3.Distance(before[i],demo.GuardPosition(i)),Is.LessThan(.3f));Assert.That(demo.IsSolidAt(demo.GuardPosition(i)),Is.False);}}}
                    demo.SurveyPaused=true;initial=demo.GuardPosition(7);demo.TickVillage(10);Assert.That(demo.GuardPosition(7),Is.EqualTo(initial));
                    demo.SurveyPaused=false;demo.SetHour(23);Assert.That(demo.ActiveGuardCount,Is.EqualTo(25));Assert.That(demo.ActiveVillagerCount,Is.Zero);
                }
            }
            finally{Object.Destroy(go);}yield return null;
        }
    }
}

