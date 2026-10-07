using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Reclamation.Tests
{
    public sealed class DefenseCampaignTests
    {
        private GameObject root;private BlightCombatLab lab;private string path;
        [UnitySetUp] public IEnumerator Setup()
        {
            path=Path.Combine(Path.GetTempPath(),"DefenseCampaign-"+Guid.NewGuid().ToString("N")+".json");
            root=new GameObject("Defense campaign acceptance");lab=root.AddComponent<BlightCombatLab>();lab.InputEnabled=lab.AutomaticSimulation=false;
            yield return null;lab.SelectScenario(BlightScenario.Founding);Assert.That(lab.PrepareDefenseCampaign(),Is.True);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {Object.Destroy(root);yield return null;foreach(string p in new[]{path,path+".bak"})if(File.Exists(p))File.Delete(p);}
        private void Step(float seconds){for(int i=0;i<Mathf.CeilToInt(seconds*60);i++)lab.Simulate(1f/60);}
        private void FinishRaid()
        {
            Step(21);foreach(Transform t in lab.transform)if(t.name.StartsWith("Village raider")){var fighter=lab.GetFighter(t);if(fighter!=null)fighter.Receive(10000,false,false);}
            lab.SidekickPlayerRoot.position=lab.CampPosition+Vector3.back*2;Step(12);
        }
        [UnityTest] public IEnumerator TwoOperationsRewardOnceAndPersistChoices()
        {
            Assert.That(lab.ChooseFounderPerk(FounderPerk.ForwardRally),Is.False);
            Assert.That(lab.BeginDefenseOperation(DefenseApproach.HoldVillage),Is.True);Assert.That(lab.BeginDefenseOperation(DefenseApproach.InterceptRoad),Is.False);
            Assert.That(lab.SaveVillage(path),Is.False);FinishRaid();
            Assert.That(lab.Campaign.completed,Is.EqualTo(1));int xp=lab.Campaign.playerXp;Step(5);Assert.That(lab.Campaign.playerXp,Is.EqualTo(xp));
            Assert.That(lab.BeginDefenseOperation(DefenseApproach.HoldVillage),Is.False,"Choose progression before follow-up");
            Assert.That(lab.ChooseFounderPerk(FounderPerk.ForwardRally),Is.True);Assert.That(lab.ChooseFounderPerk(FounderPerk.FieldDressing),Is.False);
            Assert.That(lab.ChooseCaptainPerk(CaptainPerk.WatchCaptain),Is.True);Assert.That(lab.DefenseGarrison,Is.EqualTo(3));
            Assert.That(lab.SaveVillage(path),Is.True,lab.VillageSaveStatus);lab.ResetFight();Assert.That(lab.LoadVillage(path),Is.True);
            Assert.That(lab.Campaign.playerPerk,Is.EqualTo((int)FounderPerk.ForwardRally));Assert.That(lab.DefenseGarrison,Is.EqualTo(3));
            Assert.That(lab.BeginDefenseOperation(DefenseApproach.InterceptRoad),Is.True);FinishRaid();
            Assert.That(lab.Campaign.completed,Is.EqualTo(2));Assert.That(lab.BeginDefenseOperation(DefenseApproach.HoldVillage),Is.False);
            Assert.That(lab.SaveVillage(path),Is.True,lab.VillageSaveStatus);yield return null;
        }
        [UnityTest] public IEnumerator InterceptionKeepsPostsAndPauseFreezesPreparation()
        {
            Assert.That(lab.BeginDefenseOperation(DefenseApproach.InterceptRoad),Is.True);
            lab.SetPaused(true);Step(30);Assert.That(lab.FoundingRaidRemaining,Is.Zero);lab.SetPaused(false);Step(5);
            int garrison=0,forward=0;
            foreach(Transform t in lab.transform)if(t.name.StartsWith("Mara"))
            {if(lab.DefenseDuty(t)=="Garrison")garrison++;else if(t.position.z>-5)forward++;}
            Assert.That(garrison,Is.EqualTo(2));Assert.That(forward,Is.GreaterThanOrEqualTo(2));
            Assert.That(lab.IssueFoundingDirective(FoundingDirective.Accompany),Is.False,"Essential posts cannot be abandoned by a conflicting order");
            yield return null;
        }
        [UnityTest] public IEnumerator PerksProvideBoundedAidAndDifferentReliefPolicy()
        {
            lab.Campaign.playerXp=lab.Campaign.commanderXp=150;lab.Campaign.completed=1;
            Assert.That(lab.ChooseFounderPerk(FounderPerk.FieldDressing),Is.True);Assert.That(lab.ChooseCaptainPerk(CaptainPerk.CarefulRelief),Is.True);
            Assert.That(lab.SoldierReliefHealth,Is.EqualTo(55));Assert.That(lab.BeginDefenseOperation(DefenseApproach.HoldVillage),Is.True);
            Transform patient=null;foreach(Transform t in lab.transform)if(t.name=="Mara's soldier 1")patient=t;
            patient.position=lab.SidekickPlayerRoot.position+Vector3.right;lab.GetFighter(patient).Receive(50,false,false);Step(.5f);
            Assert.That(lab.DefenseDuty(patient),Is.EqualTo("Wounded reserve"));patient.position=lab.SidekickPlayerRoot.position+Vector3.right;
            int food=lab.Village.Food;float health=lab.GetFighter(patient).Health;
            Assert.That(lab.UseFieldDressing(),Is.True);Assert.That(lab.GetFighter(patient).Health,Is.EqualTo(health+20).Within(.01f));
            Assert.That(lab.Village.Food,Is.EqualTo(food-1));Assert.That(lab.UseFieldDressing(),Is.False);yield return null;
        }
        [UnityTest] public IEnumerator RallyChangesReserveGoalWithoutMovingFixedPosts()
        {
            lab.Campaign.playerXp=lab.Campaign.commanderXp=150;lab.Campaign.completed=1;
            lab.ChooseFounderPerk(FounderPerk.ForwardRally);lab.ChooseCaptainPerk(CaptainPerk.WatchCaptain);
            Assert.That(lab.BeginDefenseOperation(DefenseApproach.HoldVillage),Is.True);
            lab.SidekickPlayerRoot.position=new Vector3(-3,0,-6);Assert.That(lab.PlaceForwardRally(),Is.True);Step(3);
            Assert.That(lab.DefenseGarrison,Is.EqualTo(3));Assert.That(lab.FoundingCommander.position.z,Is.GreaterThan(-9));yield return null;
        }
        [UnityTest] public IEnumerator BreachedStoresHavePersistentConsequences()
        {
            Assert.That(lab.BeginDefenseOperation(DefenseApproach.HoldVillage),Is.True);
            foreach(Transform t in lab.transform)if(t.name.StartsWith("Mara"))lab.GetFighter(t).Receive(10000,false,false);
            lab.SidekickPlayerRoot.position=new Vector3(-13,0,-22);Step(65);
            Assert.That(lab.DefenseBreachSeconds,Is.GreaterThanOrEqualTo(5));
            foreach(Transform t in lab.transform)if(t.name.StartsWith("Village raider")){var fighter=lab.GetFighter(t);if(fighter!=null)fighter.Receive(10000,false,false);}
            lab.Simulate(1f/60);
            Assert.That(lab.Campaign.lastProtected,Is.False);Assert.That(lab.Campaign.lastFoodLost,Is.EqualTo(5));
            Assert.That(lab.Campaign.commanderXp,Is.Zero);Assert.That(lab.Campaign.lastSurvivors,Is.Zero);
            lab.SidekickPlayerRoot.position=lab.CampPosition+Vector3.back*2;Step(12);
            Assert.That(lab.SaveVillage(path),Is.True,lab.VillageSaveStatus);Assert.That(lab.LoadVillage(path),Is.True);
            Assert.That(lab.Campaign.lastFoodLost,Is.EqualTo(5));Assert.That(lab.CanBeginDefenseOperation,Is.False);yield return null;
        }
        [UnityTest] public IEnumerator CouncilRecoveryCostsFoodAndNeverRevivesCasualties()
        {
            Transform wounded=null,fallen=null;
            foreach(Transform t in lab.transform){if(t.name=="Mara's soldier 1")wounded=t;if(t.name=="Mara's soldier 2")fallen=t;}
            lab.GetFighter(wounded).Receive(50,false,false);lab.GetFighter(fallen).Receive(10000,false,false);Step(1);
            int food=lab.Village.Food;Assert.That(lab.RestDefenseParty(),Is.True);
            Assert.That(lab.Village.Food,Is.EqualTo(food-2));Assert.That(lab.GetFighter(wounded).Health,Is.EqualTo(100));
            Assert.That(lab.GetFighter(fallen).Alive,Is.False);
            Assert.That(lab.BeginDefenseOperation(DefenseApproach.HoldVillage),Is.True);Assert.That(lab.RestDefenseParty(),Is.False);
            yield return null;
        }
        [Test] public void InvalidProgressionIsRejected()
        {
            Assert.Throws<InvalidDataException>(()=>new DefenseCampaign{playerPerk=1}.Validate());
            Assert.Throws<InvalidDataException>(()=>new DefenseCampaign{completed=3}.Validate());
            Assert.Throws<InvalidDataException>(()=>new DefenseCampaign{completed=1,playerXp=176}.Validate());
        }
    }
}
