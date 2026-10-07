using System.Collections;
using NUnit.Framework;
using Reclamation.Atlas;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class AtlasSyntyTests
    {
        [UnityTest] public IEnumerator PosesPreserveCombatStateAndKeepElbowsOutsideTorso()
        {
            var root=new GameObject("Synty pose test");var visual=root.AddComponent<AtlasSyntyVisual>();
            try
            {
                visual.Build(null,false);
                Assert.That(visual.Ready,Is.True);Assert.That(visual.SkinVertices,Is.GreaterThan(1000));
                // The sample's left appendage is a prosthetic; the replacement must have a real skinned hand.
                var handSkin=System.Array.Find(visual.GetComponentsInChildren<SkinnedMeshRenderer>(),r=>r.name.Contains("15HNDL"));
                Assert.That(handSkin,Is.Not.Null);Assert.That(handSkin.sharedMesh.vertexCount,Is.GreaterThan(100));
                Assert.That(handSkin.bones,Does.Contain(visual.Bone(HumanBodyBones.LeftHand)));
                Vector3 neutralHand=visual.Bone(HumanBodyBones.RightHand).position;
                foreach(bool heavy in new[]{false,true})
                {
                    var fighter=new DuelFighter();fighter.Attack(BlightEquipment.Weapon(BlightWeapon.Sword,heavy));
                    for(int frame=0;frame<100;frame++)
                    {
                        Vector3 position=root.transform.position;Quaternion rotation=root.transform.rotation;
                        float health=fighter.Health,stamina=fighter.Stamina,remaining=fighter.Remaining;var action=fighter.Action;
                        visual.Sample(fighter,false,false,1f/60);
                        Assert.That(fighter.Health,Is.EqualTo(health));Assert.That(fighter.Stamina,Is.EqualTo(stamina));
                        Assert.That(fighter.Remaining,Is.EqualTo(remaining));Assert.That(fighter.Action,Is.EqualTo(action));
                        Assert.That(root.transform.position,Is.EqualTo(position));Assert.That(root.transform.rotation,Is.EqualTo(rotation));
                        Assert.That(visual.MinimumElbowClearance,Is.GreaterThan(.14f),heavy+" / frame "+frame);
                        if(frame==8)Assert.That(Vector3.Distance(neutralHand,visual.Bone(HumanBodyBones.RightHand).position),Is.GreaterThan(.05f));
                        fighter.Advance(1f/60);
                    }
                }
                var dodge=new DuelFighter();dodge.Dodge();dodge.Advance(.16f);visual.Sample(dodge,false,false,1f/60);
                Assert.That(dodge.Stamina,Is.EqualTo(75));Assert.That(visual.Bone(HumanBodyBones.Hips).position.y,Is.LessThan(.85f));
                var dead=new DuelFighter();dead.Receive(100,false,false);visual.Sample(dead,false,false,1f/60);
                Assert.That(root.transform.position,Is.EqualTo(Vector3.zero));Assert.That(root.transform.rotation,Is.EqualTo(Quaternion.identity));
            }
            finally{Object.Destroy(root);}yield return null;
        }
        [UnityTest] public IEnumerator TrialReplacesOnlyPlayerAndFirstRaiderAcrossBothMaps()
        {
            var go=new GameObject("Synty map acceptance");var d=go.AddComponent<WorldAtlasDemo>();yield return null;
            try
            {
                d.ResumeDefenseCheckpoint=false;d.SyntyTrial=true;d.AutomaticBattle=false;d.EnableAtlasDefense();
                foreach(var size in new[]{AtlasSize.Small,AtlasSize.Medium})
                {
                    d.LoadMap(size,size==AtlasSize.Small?4:8);yield return null;
                    Assert.That(go.GetComponentsInChildren<AtlasSyntyVisual>().Length,Is.EqualTo(1));
                    Assert.That(d.BeginAtlasDefense(true),Is.True);
                    for(int i=0;i<21*60;i++)d.SimulateAtlasBattle(1f/60);
                    Assert.That(go.GetComponentsInChildren<AtlasSyntyVisual>().Length,Is.EqualTo(2));
                    Assert.That(d.BattleSurvivors,Is.EqualTo(25));Assert.That(d.BattleEnemies,Is.EqualTo(6));
                    foreach(var v in go.GetComponentsInChildren<AtlasSyntyVisual>())
                    {
                        var old=v.transform.parent.GetComponent<AtlasCharacterVisual>();
                        foreach(var renderer in old.GetComponentsInChildren<Renderer>())
                            if(!renderer.transform.IsChildOf(v.transform))Assert.That(renderer.enabled,Is.False,"Overlapping prototype: "+renderer.name);
                    }
                    var player=go.GetComponentsInChildren<AtlasSyntyVisual>()[0];
                    d.SurveyPaused=true;Vector3 hand=player.Bone(HumanBodyBones.RightHand).position;
                    d.SimulateAtlasBattle(.5f);yield return null;
                    Assert.That(player.Bone(HumanBodyBones.RightHand).position,Is.EqualTo(hand));d.SurveyPaused=false;
                    player.enabled=false;d.SimulateAtlasBattle(1f/60);
                    foreach(var renderer in player.GetComponentsInChildren<Renderer>())Assert.That(renderer.enabled,Is.False);
                    Assert.That(player.transform.parent.Find("Prototype body").GetComponent<Renderer>().enabled,Is.True);
                    player.enabled=true;
                    var enemy=go.GetComponentsInChildren<AtlasSyntyVisual>()[1];
                    enemy.transform.parent.position=new Vector3(d.Model.Extent*.45f,0,d.Model.Extent*.45f);
                    d.SimulateAtlasBattle(1f/60);
                    foreach(var renderer in enemy.GetComponentsInChildren<Renderer>())Assert.That(renderer.enabled,Is.False,"Fog leaked Synty mesh");
                }
            }
            finally{Object.Destroy(go);}yield return null;
        }
        [UnityTest] public IEnumerator DisablingTrialVisualRestoresOriginalWithoutOverlap()
        {
            var root=new GameObject("Synty fallback");var old=root.AddComponent<AtlasCharacterVisual>();old.Build(0);
            var child=new GameObject("Synty");child.transform.SetParent(root.transform,false);var visual=child.AddComponent<AtlasSyntyVisual>();
            try
            {
                visual.Build(old,false);visual.enabled=false;
                foreach(var r in child.GetComponentsInChildren<Renderer>())Assert.That(r.enabled,Is.False);
                Assert.That(root.transform.Find("Prototype body").GetComponent<Renderer>().enabled,Is.True);
                visual.enabled=true;Assert.That(root.transform.Find("Prototype body").GetComponent<Renderer>().enabled,Is.False);
            }
            finally{Object.Destroy(root);}yield return null;
        }
    }
}
