using System.Collections;
using System.Linq;
using NUnit.Framework;
using Reclamation.Atlas;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class AtlasSyntyRoleTests
    {
        [UnityTest] public IEnumerator RoleRosterIsBoundedAcrossBothMapsAndFogHidesRaiders()
        {
            var go=new GameObject("Synty role roster");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                demo.ResumeDefenseCheckpoint=false;demo.SyntyTrial=true;demo.SyntyRoleTrial=true;demo.AutomaticBattle=false;demo.EnableAtlasDefense();
                foreach(var size in new[]{AtlasSize.Small,AtlasSize.Medium})
                {
                    demo.LoadMap(size,size==AtlasSize.Small?4:8);yield return null;
                    var roles=go.GetComponentsInChildren<AtlasSyntyVisual>(true);
                    Assert.That(roles.Length,Is.EqualTo(13));
                    Assert.That(roles.Count(v=>v.Role==SyntyRole.CompanyCommander),Is.EqualTo(1));
                    Assert.That(roles.Count(v=>v.Role==SyntyRole.PlatoonCommander),Is.EqualTo(4));
                    Assert.That(roles.Count(v=>v.Role==SyntyRole.Soldier),Is.EqualTo(4));
                    Assert.That(roles.Count(v=>v.Civilian),Is.EqualTo(3));
                    Assert.That(roles.All(v=>v.Ready),Is.True);
                    Assert.That(demo.BeginAtlasDefense(true),Is.True);
                    for(int frame=0;frame<21*60;frame++)demo.SimulateAtlasBattle(1f/60);
                    roles=go.GetComponentsInChildren<AtlasSyntyVisual>(true);
                    Assert.That(roles.Length,Is.EqualTo(19));
                    Assert.That(demo.BattleSurvivors,Is.EqualTo(25));Assert.That(demo.BattleEnemies,Is.EqualTo(6));
                    var raiders=roles.Where(v=>v.Role==SyntyRole.Raider).ToArray();Assert.That(raiders.Length,Is.EqualTo(6));
                    Assert.That(raiders.Select(v=>v.Variant%3).Distinct().Count(),Is.EqualTo(3));
                    foreach(var raider in raiders)raider.transform.parent.position=new Vector3(demo.Model.Extent*.45f,0,demo.Model.Extent*.45f);
                    demo.SimulateAtlasBattle(1f/60);
                    foreach(var raider in raiders)Assert.That(raider.GetComponentsInChildren<Renderer>().All(r=>!r.enabled),Is.True,"Fog revealed a raider");
                }
            }
            finally{Object.Destroy(go);}yield return null;
        }
        [UnityTest] public IEnumerator CiviliansKeepSchedulesAndGuardsKeepNightTorches()
        {
            var go=new GameObject("Synty schedules");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                demo.ResumeDefenseCheckpoint=false;demo.SyntyTrial=true;demo.SyntyRoleTrial=true;demo.AutomaticBattle=false;demo.AutomaticClock=false;demo.EnableAtlasDefense();demo.LoadMap(AtlasSize.Small,4);yield return null;
                var roles=go.GetComponentsInChildren<AtlasSyntyVisual>(true);var civilians=roles.Where(v=>v.Civilian).ToArray();
                demo.SetHour(12);demo.TickVillage(1f/60);yield return null;
                Assert.That(civilians.Any(v=>v.gameObject.activeInHierarchy),Is.True);
                foreach(var v in civilians)
                {
                    Assert.That(v.Sword.gameObject.activeSelf,Is.False);
                    Assert.That(v.transform.Find("Faction shield").gameObject.activeSelf,Is.False);
                    foreach(var renderer in v.transform.parent.GetComponentsInChildren<Renderer>())
                        if(!renderer.transform.IsChildOf(v.transform)&&renderer.name=="Torso")Assert.That(renderer.enabled,Is.False);
                }
                demo.SetHour(23);yield return null;
                Assert.That(civilians.All(v=>!v.gameObject.activeInHierarchy),Is.True,"Residents should sleep indoors");
                int torches=0;
                foreach(var v in roles.Where(v=>v.Role==SyntyRole.CompanyCommander||v.Role==SyntyRole.PlatoonCommander||v.Role==SyntyRole.Soldier))
                {
                    var old=v.transform.parent.GetComponentInChildren<AtlasCharacterVisual>();
                    if(!old.TorchLit)continue;
                    torches++;var torch=old.transform.Find("Night watch torch");
                    Assert.That(torch.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),Is.True);
                    Assert.That(Vector3.Distance(torch.position,v.Bone(HumanBodyBones.LeftHand).position),Is.LessThan(.2f));
                    Assert.That(v.transform.Find("Faction shield").gameObject.activeSelf,Is.False);
                }
                Assert.That(torches,Is.GreaterThan(0));
                demo.SetHour(12);demo.TickVillage(1f/60);yield return null;
                Assert.That(civilians.Any(v=>v.gameObject.activeInHierarchy),Is.True);
            }
            finally{Object.Destroy(go);}yield return null;
        }
    }
}
