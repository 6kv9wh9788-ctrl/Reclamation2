using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Reclamation.Atlas;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class AtlasVillageTests
    {
        [Test] public void SeedsAreRepeatableAndProduceDifferentPlans()
        {
            var signatures=new HashSet<string>();
            for(int seed=0;seed<20;seed++)
            {
                var a=new AtlasVillageLayout(seed);var b=new AtlasVillageLayout(seed);
                Assert.That(a.House(0),Is.EqualTo(b.House(0)));Assert.That(a.Angle,Is.EqualTo(b.Angle));
                signatures.Add(a.Angle+"/"+a.Name+"/"+a.House(0));
                for(int i=0;i<a.HouseCount;i++)for(int j=i+1;j<a.HouseCount;j++)Assert.That(Vector2.Distance(a.House(i),a.House(j)),Is.GreaterThan(10));
            }
            Assert.That(signatures.Count,Is.GreaterThan(15));
        }
        [Test] public void HinterlandHasMultipleLargeFieldsAndWorkCarryReturnSequences()
        {
            var layout=new AtlasVillageLayout(42);
            for(int i=0;i<3;i++){Assert.That(layout.Field(i).magnitude,Is.GreaterThan(70));for(int j=i+1;j<3;j++)Assert.That(Vector2.Distance(layout.Field(i),layout.Field(j)),Is.GreaterThan(25));}
            foreach(int resident in new[]{1,4})
            {
                var road=layout.WorkRoad(resident);
                for(int segment=1;segment<road.Length;segment++)for(int step=0;step<=20;step++)
                {
                    Vector2 point=Vector2.Lerp(road[segment-1],road[segment],step/20f);
                    for(int field=0;field<3;field++){var d=point-layout.Field(field);Assert.That(Mathf.Abs(Vector2.Dot(d,layout.Hinterland(Vector2.right)))<12&&Mathf.Abs(Vector2.Dot(d,layout.Hinterland(Vector2.up)))<12,Is.False,"Logging/mining access must not cross cultivated plots");}
                }
            }
            var motions=new HashSet<VillageMotion>();var positions=new HashSet<Vector2>();
            for(float hour=9;hour<16;hour+=.01f){motions.Add(AtlasVillageRoutine.PresentationMotion(hour,0));positions.Add(AtlasVillageRoutine.Position(hour,0,layout));}
            Assert.That(motions,Does.Contain(VillageMotion.Farming));Assert.That(motions,Does.Contain(VillageMotion.Carrying));Assert.That(motions,Does.Contain(VillageMotion.Resting));Assert.That(motions,Does.Contain(VillageMotion.Walking));Assert.That(positions.Count,Is.GreaterThan(100));
            for(float hour=9;hour<16;hour+=.01f)Assert.That(Vector2.Distance(AtlasVillageRoutine.Position(hour,0,layout),AtlasVillageRoutine.Position(hour+.0001f,0,layout)),Is.LessThan(.1f),"Work cycle must not teleport");
        }
        [Test] public void ActivitiesHaveDistinctRolesAndCommutesAreStaggered()
        {
            Assert.That(AtlasVillageRoutine.Motion(9,0),Is.EqualTo(VillageMotion.Farming));
            Assert.That(AtlasVillageRoutine.Motion(9,1),Is.EqualTo(VillageMotion.Chopping));
            Assert.That(AtlasVillageRoutine.Motion(9,2),Is.EqualTo(VillageMotion.Trading));
            Assert.That(AtlasVillageRoutine.Motion(9,4),Is.EqualTo(VillageMotion.Mining));
            Assert.That(AtlasVillageRoutine.Motion(19,0),Is.EqualTo(VillageMotion.Drinking));
            Assert.That(AtlasVillageRoutine.Motion(19,1),Is.EqualTo(VillageMotion.Eating));
            var offsets=new HashSet<float>();var durations=new HashSet<float>();
            float earliest=1,latest=0;
            for(int i=0;i<18;i++){float offset=AtlasVillageRoutine.DepartureOffset(i);offsets.Add(offset);durations.Add(AtlasVillageRoutine.TravelDuration(i));earliest=Mathf.Min(earliest,offset);latest=Mathf.Max(latest,offset);}
            Assert.That(offsets.Count,Is.EqualTo(18));Assert.That(durations.Count,Is.EqualTo(18));Assert.That(latest-earliest,Is.GreaterThan(.45f));
            foreach(int resident in new[]{0,1,4,8,17})
            {
                float arrival=7+AtlasVillageRoutine.DepartureOffset(resident)+AtlasVillageRoutine.TravelDuration(resident);
                Assert.That(Vector2.Distance(AtlasVillageRoutine.Position(arrival-.0001f,resident),AtlasVillageRoutine.Position(arrival+.0001f,resident)),Is.LessThan(.1f));
            }
        }
        [UnityTest] public IEnumerator ToolsFollowActivitiesAndPauseFreezesPoses()
        {
            var go=new GameObject("Activity visual test");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                demo.AutomaticClock=false;demo.VisitVillage();demo.SetHour(9);
                int Count(string name){int count=0;foreach(var t in go.GetComponentsInChildren<Transform>())if(t.name==name)count++;return count;}
                int Expected(VillageMotion motion){int count=0;for(int i=0;i<18;i++)if(AtlasVillageRoutine.PresentationMotion(9,i)==motion)count++;return count;}
                Assert.That(Count("Hoe"),Is.EqualTo(Expected(VillageMotion.Farming)));Assert.That(Count("Axe"),Is.EqualTo(Expected(VillageMotion.Chopping)));Assert.That(Count("Pick"),Is.EqualTo(Expected(VillageMotion.Mining)));Assert.That(Count("Mug"),Is.Zero);
                Transform arm=null;foreach(var t in go.GetComponentsInChildren<Transform>(true))if(t.name=="Hoe"){arm=t.parent;break;}
                Assert.That(arm,Is.Not.Null);demo.SetHour(19);Assert.That(Count("Hoe"),Is.Zero);Assert.That(Count("Mug"),Is.EqualTo(9));Assert.That(Count("Bread"),Is.EqualTo(9));
                var before=arm.localRotation;demo.TickVillage(1);Assert.That(Quaternion.Angle(before,arm.localRotation),Is.GreaterThan(1));
                demo.SurveyPaused=true;before=arm.localRotation;demo.TickVillage(5);Assert.That(arm.localRotation,Is.EqualTo(before));
                demo.SetHour(23);Assert.That(Count("Mug"),Is.Zero);Assert.That(demo.ActiveVillagerCount,Is.Zero);
            }
            finally{Object.Destroy(go);}yield return null;
        }
        [UnityTest] public IEnumerator MarketAwningRetractsCameraAboveCounter()
        {
            var go=new GameObject("Awning test");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                int id=demo.Model.factions[0].capital;var layout=demo.LayoutFor(id);var p=demo.Model.sites[id].point+layout.Market;
                var target=new Vector3(p.x,demo.Model.Height(p.x,p.y)+2.3f,p.y);var desired=target+Vector3.up*4;
                Assert.That(Vector3.Distance(demo.ResolveVillageCamera(target,desired),target),Is.LessThan(2));
            }
            finally{Object.Destroy(go);}yield return null;
        }
        [Test] public void RoutinesCoverWorkEveningSleepAndWrapAtMidnight()
        {
            for(int resident=0;resident<18;resident++)
            {
                Assert.That(AtlasVillageRoutine.Activity(9,resident),Is.EqualTo(VillageActivity.Working));
                Assert.That(AtlasVillageRoutine.Activity(19,resident),Is.EqualTo(VillageActivity.Socializing));
                Assert.That(AtlasVillageRoutine.Activity(23,resident),Is.EqualTo(VillageActivity.Sleeping));
                Assert.That(AtlasVillageRoutine.Activity(2,resident),Is.EqualTo(VillageActivity.Sleeping));
                Assert.That(AtlasVillageRoutine.Position(9,resident),Is.EqualTo(AtlasVillageRoutine.Position(33,resident)));
            }
        }
        [UnityTest] public IEnumerator ClockPauseSleepAndDawnAreConsistent()
        {
            var go=new GameObject("Village clock test");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                demo.VisitVillage();demo.SetHour(23);Assert.That(demo.ActiveVillagerCount,Is.Zero);
                demo.SurveyPaused=true;demo.TickVillage(120);Assert.That(demo.Hour,Is.EqualTo(23));
                demo.SurveyPaused=false;demo.TickVillage(120);Assert.That(demo.Hour,Is.EqualTo(1).Within(.01f));
                demo.SetHour(9);Assert.That(demo.ActiveVillagerCount,Is.EqualTo(18));
            }
            finally{Object.Destroy(go);}yield return null;
        }
        [UnityTest] public IEnumerator HomesBlockMovementAndHallDoorwayAllowsEntry()
        {
            var go=new GameObject("Village collision test");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                int id=demo.Model.factions[0].capital;var site=demo.Model.sites[id];var layout=demo.LayoutFor(id);
                Vector3 P(Vector2 p){p+=site.point;return new Vector3(p.x,site.elevation,p.y);}
                Vector3 home=P(layout.House(0)),door=P(layout.Door(0));
                Assert.That(demo.IsSolidAt(home),Is.True);Assert.That(demo.IsSolidAt(door),Is.False);
                Vector3 stopped=demo.ResolveVillageMove(door,(home-door)*2);Assert.That(Vector3.Distance(stopped,home),Is.GreaterThan(3.5f));Assert.That(demo.IsSolidAt(stopped),Is.False);
                Vector3 entry=P(layout.Rotate(new Vector2(0,30))),inside=P(layout.Rotate(new Vector2(0,36)));
                Assert.That(Vector3.Distance(demo.ResolveVillageMove(entry,inside-entry),inside),Is.LessThan(.01f));
                Vector3 roofTarget=P(layout.Hall)+Vector3.up*1.5f;Assert.That(Vector3.Distance(demo.ResolveVillageCamera(roofTarget,roofTarget+Vector3.up*12),roofTarget),Is.LessThan(5));
                Vector3 wall=P(layout.Rotate(new Vector2(5,33)));Assert.That(demo.IsSolidAt(wall),Is.True);
                Vector3 camera=demo.ResolveVillageCamera(door+Vector3.up*1.5f,home+Vector3.up*1.5f);Assert.That(Vector3.Distance(camera,door+Vector3.up*1.5f),Is.LessThan(Vector3.Distance(home,door)));
            }
            finally{Object.Destroy(go);}yield return null;
        }
        [UnityTest] public IEnumerator ScheduledRoutesAvoidBuildingsInBothMaps()
        {
            var go=new GameObject("Village route test");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                foreach(var size in new[]{AtlasSize.Small,AtlasSize.Medium})
                {
                    demo.LoadMap(size,size==AtlasSize.Small?4:12);yield return null;
                    foreach(var site in demo.Model.sites)
                    {
                        if(!(site.owner==-2||site.owner>=0&&demo.Model.factions[site.owner].family==0))continue;
                        var layout=demo.LayoutFor(site.id);
                        for(int field=0;field<3;field++)for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
                            Assert.That(demo.Model.Walkable(site.point+layout.Field(field)+layout.Hinterland(new Vector2(x*12,z*12))),Is.True,$"Field {field} must be on accessible terrain at {size} site {site.id}");
                        for(int resident=0;resident<18;resident++)for(float hour=6;hour<22;hour+=.1f)
                        {
                            Vector2 p=site.point+AtlasVillageRoutine.Position(hour,resident,layout);var ground=new Vector3(p.x,demo.Model.Height(p.x,p.y),p.y);
                            Assert.That(demo.Model.Walkable(p),Is.True,$"Unwalkable {size} site {site.id}, resident {resident}, hour {hour}, at {p}");
                            Assert.That(demo.IsSolidAt(ground),Is.False,$"{size} site {site.id}, seed {layout.Seed}, resident {resident}, hour {hour:F2}, at {p}");
                        }
                    }
                }
            }
            finally{Object.Destroy(go);}yield return null;
        }
    }
}
