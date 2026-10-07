using System.Collections;
using System.Linq;
using NUnit.Framework;
using Reclamation.Atlas;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class AtlasSyntyPresentationTests
    {
        [UnityTest] public IEnumerator ReadinessLowersWeaponsButNeverDelaysCommittedActions()
        {
            var go=new GameObject("Readiness");var visual=go.AddComponent<AtlasSyntyVisual>();
            try
            {
                visual.Build(null,false,SyntyRole.PlatoonCommander);var fighter=new DuelFighter();
                Assert.That(go.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Officer crest"),Is.False);
                visual.Readiness=SyntyReadiness.Relaxed;
                for(int i=0;i<30;i++)visual.Sample(fighter,true,false,1f/60);
                Assert.That(visual.WeaponReadiness,Is.EqualTo(0).Within(.001));
                Assert.That(visual.Sword.Find("Patrol scabbard").gameObject.activeSelf,Is.True);
                Assert.That(visual.Bone(HumanBodyBones.RightHand).position.y,Is.LessThan(1));
                visual.Readiness=SyntyReadiness.Alert;for(int i=0;i<30;i++)visual.Sample(fighter,false,false,1f/60);
                Assert.That(visual.WeaponReadiness,Is.EqualTo(.55f).Within(.001));
                visual.Readiness=SyntyReadiness.Relaxed;
                fighter.Attack(BlightEquipment.Weapon(BlightWeapon.Sword,false));float remaining=fighter.Remaining,stamina=fighter.Stamina;
                visual.Sample(fighter,false,false,1f/60);
                Assert.That(visual.WeaponReadiness,Is.EqualTo(1));Assert.That(fighter.Remaining,Is.EqualTo(remaining));Assert.That(fighter.Stamina,Is.EqualTo(stamina));
                Assert.That(visual.Sword.Find("Patrol scabbard").gameObject.activeSelf,Is.False);
                Assert.That(go.transform.position,Is.EqualTo(Vector3.zero));
                fighter=new DuelFighter{Blocking=true};visual.Sample(fighter,false,false,1f/60);Assert.That(visual.WeaponReadiness,Is.EqualTo(1));
                fighter=new DuelFighter();fighter.Dodge();visual.Sample(fighter,false,false,1f/60);Assert.That(visual.WeaponReadiness,Is.EqualTo(1));Assert.That(fighter.Stamina,Is.EqualTo(75));
            }
            finally{Object.Destroy(go);}yield return null;
        }
        [UnityTest] public IEnumerator CompanyRosterAndCompactHudWorkOnBothMaps()
        {
            var go=new GameObject("Company presentation");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                demo.ResumeDefenseCheckpoint=false;demo.SyntyTrial=true;demo.SyntyRoleTrial=true;demo.SyntyCompanyTrial=true;demo.AutomaticBattle=false;demo.EnableAtlasDefense();
                foreach(var size in new[]{AtlasSize.Small,AtlasSize.Medium})
                {
                    demo.LoadMap(size,size==AtlasSize.Small?4:8);yield return null;
                    var roles=go.GetComponentsInChildren<AtlasSyntyVisual>(true);
                    Assert.That(roles.Length,Is.EqualTo(29));Assert.That(roles.Count(v=>v.Role==SyntyRole.Soldier),Is.EqualTo(20));
                    Assert.That(roles.Count(v=>v.Role==SyntyRole.PlatoonCommander),Is.EqualTo(4));
                    for(int i=0;i<30;i++)demo.SimulateAtlasBattle(1f/60);
                    foreach(var v in roles.Where(v=>!v.Civilian))Assert.That(v.WeaponReadiness,Is.EqualTo(0).Within(.001));
                    Assert.That(demo.BattleHudContains(new Vector2(500,120)),Is.False,"Former banner area should allow world input");
                    Assert.That(demo.BattleHudContains(new Vector2(500,40)),Is.True);
                    Assert.That(demo.BeginAtlasDefense(true),Is.True);demo.SimulateAtlasBattle(.5f);
                    foreach(var v in roles.Where(v=>!v.Civilian))Assert.That(v.Readiness,Is.EqualTo(SyntyReadiness.Alert));
                    for(int frame=0;frame<21*60;frame++)demo.SimulateAtlasBattle(1f/60);
                    roles=go.GetComponentsInChildren<AtlasSyntyVisual>(true);Assert.That(roles.Length,Is.EqualTo(35));
                    foreach(var v in roles.Where(v=>!v.Civilian))Assert.That(v.Readiness,Is.EqualTo(SyntyReadiness.Combat));
                    foreach(var v in roles.Where(v=>v.Role==SyntyRole.Soldier||v.Role==SyntyRole.PlatoonCommander||v.Role==SyntyRole.CompanyCommander))
                    {
                        var old=v.transform.parent.GetComponentInChildren<AtlasCharacterVisual>();
                        Assert.That(old.GetComponentsInChildren<Renderer>().Where(r=>!r.transform.IsChildOf(v.transform)).All(r=>!r.enabled),Is.True,"Prototype overlaps company member");
                    }
                }
            }
            finally{Object.Destroy(go);}yield return null;
        }
    }
}
