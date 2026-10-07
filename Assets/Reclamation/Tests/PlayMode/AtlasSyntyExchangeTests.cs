using System.Collections;
using System.Linq;
using NUnit.Framework;
using Reclamation.Atlas;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class AtlasSyntyExchangeTests
    {
        [UnityTest] public IEnumerator CombatMotionPreservesRootAndFighterWhileSteppingAndPausing()
        {
            var go=new GameObject("Combat locomotion");var visual=go.AddComponent<AtlasSyntyVisual>();
            try
            {
                visual.Build(null,false);visual.CombatMotion=true;var fighter=new DuelFighter{Blocking=true};
                foreach(var direction in new[]{Vector3.right,Vector3.left,Vector3.back,Vector3.forward})
                {
                    float lift=0;
                    for(int frame=0;frame<45;frame++)
                    {
                        go.transform.position+=direction*.03f;
                        var position=go.transform.position;var rotation=go.transform.rotation;
                        visual.Sample(fighter,true,false,1f/60);
                        var left=go.transform.InverseTransformPoint(visual.Bone(HumanBodyBones.LeftFoot).position);
                        var right=go.transform.InverseTransformPoint(visual.Bone(HumanBodyBones.RightFoot).position);
                        Assert.That(left.x,Is.LessThan(0));Assert.That(right.x,Is.GreaterThan(0));
                        lift=Mathf.Max(lift,Mathf.Abs(left.y-right.y));
                        Assert.That(go.transform.position,Is.EqualTo(position));Assert.That(go.transform.rotation,Is.EqualTo(rotation));
                        Assert.That(fighter.Action,Is.EqualTo(DuelAction.Ready));Assert.That(fighter.Stamina,Is.EqualTo(100));
                        Assert.That(visual.MinimumElbowClearance,Is.GreaterThan(.14f));
                        var foot=visual.Bone(HumanBodyBones.LeftFoot).position;var hand=visual.Bone(HumanBodyBones.RightHand).position;
                        visual.Sample(fighter,true,false,0);
                        Assert.That(Vector3.Distance(foot,visual.Bone(HumanBodyBones.LeftFoot).position),Is.LessThan(.0001f));
                        Assert.That(Vector3.Distance(hand,visual.Bone(HumanBodyBones.RightHand).position),Is.LessThan(.0001f));
                    }
                    Assert.That(lift,Is.GreaterThan(.02f),"Travel must visibly lift alternating feet");
                }
                for(int i=0;i<30;i++){go.transform.Rotate(0,2,0);visual.Sample(fighter,false,false,1f/60);}
                var hip=visual.Bone(HumanBodyBones.Hips).rotation;
                visual.Sample(fighter,false,false,0);
                Assert.That(Quaternion.Angle(hip,visual.Bone(HumanBodyBones.Hips).rotation),Is.LessThan(.01f));
                fighter.Blocking=false;fighter.Attack(BlightEquipment.Weapon(BlightWeapon.Sword,true));
                for(int i=0;i<100;i++)
                {
                    float remaining=fighter.Remaining;visual.Sample(fighter,false,false,1f/60);
                    Assert.That(fighter.Remaining,Is.EqualTo(remaining));Assert.That(visual.MinimumElbowClearance,Is.GreaterThan(.14f));
                    fighter.Advance(1f/60);
                }
            }
            finally{Object.Destroy(go);}yield return null;
        }
        [UnityTest] public IEnumerator PairUsesSeparateLanesAndStaggeredStartsWithoutRedirectingWindups()
        {
            var go=new GameObject("Pair readability");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                demo.ResumeDefenseCheckpoint=false;demo.AutomaticBattle=false;demo.EnableAtlasDefense();
                foreach(var size in new[]{AtlasSize.Small,AtlasSize.Medium})
                {
                    demo.LoadMap(size,size==AtlasSize.Small?4:8);demo.BeginDuelPractice(2);yield return null;
                    // Inspect the existing actors without adding a mutable combat API for tests.
                    var field=typeof(WorldAtlasDemo).GetField("battleActors",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                    var actors=(System.Collections.IList)field.GetValue(demo);
                    var fighters=new DuelFighter[2];var roots=new Transform[2];
                    for(int i=0;i<2;i++)
                    {
                        var actor=actors[26+i];var type=actor.GetType();
                        fighters[i]=(DuelFighter)type.GetField("fighter").GetValue(actor);
                        roots[i]=(Transform)type.GetField("root").GetValue(actor);
                    }
                    demo.SetAtlasBattleInput(Vector3.zero,true);
                    var sequences=new int[2];float lastStart=-10;bool overlappingWindups=false;
                    for(int frame=0;frame<180&&demo.AtlasPlayerFighter.Alive;frame++)
                    {
                        var positions=roots.Select(r=>r.position).ToArray();
                        var actions=fighters.Select(f=>f.Action).ToArray();
                        demo.SimulateAtlasBattle(1f/60);
                        for(int i=0;i<2;i++)
                        {
                            if(actions[i]==DuelAction.Windup)Assert.That(roots[i].position,Is.EqualTo(positions[i]),"Lane seeking moved a committed attacker");
                            if(fighters[i].AttackSequence>sequences[i])
                            {
                                Assert.That(frame/60f-lastStart,Is.GreaterThanOrEqualTo(.31f),"Attack preparations started together");
                                lastStart=frame/60f;sequences[i]=fighters[i].AttackSequence;
                                var expected=BlightEquipment.Enemy(BlightEnemy.Thrall,sequences[i]-1);
                                Assert.That(fighters[i].Strike.Windup,Is.EqualTo(expected.Windup));
                                Assert.That(fighters[i].Strike.Recovery,Is.EqualTo(expected.Recovery));
                            }
                        }
                        overlappingWindups|=fighters.All(f=>f.Action==DuelAction.Windup);
                        Assert.That(Vector3.Distance(roots[0].position,roots[1].position),Is.GreaterThan(1.1f),"Pair collapsed into one silhouette");
                    }
                    Assert.That(sequences[0],Is.GreaterThan(0));Assert.That(sequences[1],Is.GreaterThan(0));
                    Assert.That(overlappingWindups,Is.True,"The pair must remain simultaneous threats, not take exclusive turns");
                    demo.SurveyPaused=true;var frozen=roots.Select(r=>r.position).ToArray();
                    demo.SimulateAtlasBattle(.5f);
                    for(int i=0;i<2;i++)Assert.That(roots[i].position,Is.EqualTo(frozen[i]));
                    demo.SurveyPaused=false;demo.BeginDuelPractice(1,true);
                    Assert.That(demo.BattleEnemies,Is.EqualTo(1));Assert.That(demo.RequestAtlasAttack(false),Is.True);
                    demo.SimulateAtlasBattle(.26f);Assert.That(demo.BattlePlayerHits,Is.Zero,"Reset must preserve shield drill behavior");
                }
            }
            finally{Object.Destroy(go);}yield return null;
        }
        [UnityTest] public IEnumerator FeetStayPlantedAndContactDoesNotModifyFighter()
        {
            var go=new GameObject("Exchange pose");var visual=go.AddComponent<AtlasSyntyVisual>();
            try
            {
                visual.Build(null,false);var fighter=new DuelFighter();visual.Sample(fighter,false,false,0);
                var left=visual.Bone(HumanBodyBones.LeftFoot).position;var right=visual.Bone(HumanBodyBones.RightFoot).position;
                foreach(bool heavy in new[]{false,true})
                {
                    fighter=new DuelFighter();fighter.Attack(BlightEquipment.Weapon(BlightWeapon.Sword,heavy));
                    for(int i=0;i<100;i++)
                    {
                        float remaining=fighter.Remaining,stamina=fighter.Stamina;visual.Sample(fighter,false,false,1f/60);
                        Assert.That(fighter.Remaining,Is.EqualTo(remaining));Assert.That(fighter.Stamina,Is.EqualTo(stamina));
                        Assert.That(Vector3.Distance(left,visual.Bone(HumanBodyBones.LeftFoot).position),Is.LessThan(.035f));
                        Assert.That(Vector3.Distance(right,visual.Bone(HumanBodyBones.RightFoot).position),Is.LessThan(.035f));
                        Assert.That(go.transform.position,Is.EqualTo(Vector3.zero));fighter.Advance(1f/60);
                    }
                }
                fighter=new DuelFighter{Blocking=true};visual.Sample(fighter,false,false,0);
                var shield=visual.transform.Find("Faction shield");var before=shield.position;
                visual.ReceiveContact("Blocked");
                Assert.That(visual.ContactRecoil,Is.EqualTo(1),"Contact must react on the resolved impact frame");
                visual.ReceiveWeaponContact("Hit");Assert.That(visual.WeaponRebound,Is.Zero,"A clean hit must not bounce off an imaginary shield");
                visual.ReceiveWeaponContact("Blocked");Assert.That(visual.WeaponRebound,Is.EqualTo(1));
                visual.Sample(fighter,false,false,0);Assert.That(visual.WeaponRebound,Is.EqualTo(1),"Pause freezes the attacker's rebound too");
                visual.Sample(fighter,false,false,.1f);
                Assert.That(visual.ContactRecoil,Is.GreaterThan(.5f));Assert.That(Vector3.Distance(before,shield.position),Is.GreaterThan(.02f));
                Assert.That(fighter.Health,Is.EqualTo(100));Assert.That(fighter.Stamina,Is.EqualTo(100));Assert.That(fighter.Action,Is.EqualTo(DuelAction.Ready));
                float frozen=visual.ContactRecoil;visual.Sample(fighter,false,false,0);Assert.That(visual.ContactRecoil,Is.EqualTo(frozen));
                for(int i=0;i<30;i++)visual.Sample(fighter,false,false,1f/60);Assert.That(visual.ContactRecoil,Is.EqualTo(0).Within(.001));Assert.That(visual.WeaponRebound,Is.Zero);
            }
            finally{Object.Destroy(go);}yield return null;
        }
        [UnityTest] public IEnumerator VillagePracticeBlocksActualStrikesWithoutCampaignProgress()
        {
            var go=new GameObject("Village duel");var demo=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                demo.ResumeDefenseCheckpoint=false;demo.SyntyTrial=true;demo.SyntyRoleTrial=true;demo.SyntyCompanyTrial=true;demo.AutomaticBattle=false;demo.EnableAtlasDefense();
                foreach(var size in new[]{AtlasSize.Small,AtlasSize.Medium})
                {
                    demo.LoadMap(size,size==AtlasSize.Small?4:8);demo.BeginDuelPractice(1,true);yield return null;
                    var roles=go.GetComponentsInChildren<AtlasSyntyVisual>();var player=roles.Single(v=>v.Role==SyntyRole.Player);
                    foreach(var guard in roles.Where(v=>v.Role==SyntyRole.CompanyCommander||v.Role==SyntyRole.PlatoonCommander||v.Role==SyntyRole.Soldier))
                        Assert.That(Vector3.Distance(guard.transform.position,player.transform.position),Is.GreaterThan(30),"Spectator obstructs practice area");
                    Assert.That(demo.BattleEnemies,Is.EqualTo(1));Assert.That(demo.RequestAtlasAttack(false),Is.True);demo.SimulateAtlasBattle(.26f);
                    Assert.That(demo.BattleHudContains(new Vector2(40,40)),Is.True);
                    Assert.That(demo.BattleHudContains(new Vector2(600,40)),Is.False,"Empty sky beside the compact practice panel must remain interactive");
                    Assert.That(player.WeaponRebound,Is.GreaterThan(0),"The real blocked result must reach the attacking visual");
                    var opponent=go.GetComponentsInChildren<AtlasSyntyVisual>().Single(v=>v.Role==SyntyRole.Raider);
                    Assert.That(opponent.LastContact,Is.EqualTo("Blocked"));Assert.That(demo.BattlePlayerHits,Is.EqualTo(0));
                    Assert.That(demo.DefenseState.progression.completed,Is.EqualTo(0));Assert.That(demo.DefenseState.progression.playerXp,Is.EqualTo(0));
                    demo.BeginDuelPractice(2);yield return null;Assert.That(demo.BattleEnemies,Is.EqualTo(2));
                    Assert.That(demo.RequestAtlasAttack(false),Is.True);demo.SimulateAtlasBattle(.26f);Assert.That(demo.BattlePlayerHits,Is.GreaterThan(0));
                    demo.SurveyPaused=true;float remaining=demo.AtlasPlayerFighter.Remaining;demo.SimulateAtlasBattle(.3f);Assert.That(demo.AtlasPlayerFighter.Remaining,Is.EqualTo(remaining));demo.SurveyPaused=false;
                }
            }
            finally{Object.Destroy(go);}yield return null;
        }
    }
}
