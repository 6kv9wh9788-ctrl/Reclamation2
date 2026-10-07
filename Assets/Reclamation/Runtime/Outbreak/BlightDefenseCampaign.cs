using System;
using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        public DefenseCampaign Campaign { get; private set; }
        private bool startingCampaign, campaignRaid, dressingUsed, rallyPlaced;
        private float breachSeconds, participationSeconds;
        private Vector3 campaignRally;
        private bool DefenseLoopLaunch => Array.IndexOf(Environment.GetCommandLineArgs(),"--defense-loop")>=0;
        public float DefenseBreachSeconds => breachSeconds;
        public bool FieldDressingUsed => dressingUsed;
        public float SoldierReliefHealth => Campaign!=null&&Campaign.commanderPerk==(int)CaptainPerk.CarefulRelief?55:35;
        public string CampaignStatus => Campaign==null?"":FoundingRaidActive?
            (raidPreparing?"SCOUT WARNING / "+Mathf.CeilToInt(raidCountdown)+"s":"RAID / "+FoundingRaidRemaining+" remain"):
            Campaign.completed==0?"Choose interception or village defense":Campaign.completed==2?"Two raids repelled":"Raid repelled / choose progression";

        public bool PrepareDefenseCampaign()
        {
            if(!Founding||Campaign!=null||FoundingRaidActive)return false;
            // A declared developed-village starting scenario, not a reward or a saved-world upgrade.
            foreach(var a in actors)if(a.enemy){a.fighter.Receive(10000,false,false);a.root.gameObject.SetActive(false);}
            Village.RecoverSupplies();Village.DeliverSupplies();Village.StartResearch(FoundingVillage.Technology.WorkCrews);Village.Tick(15);
            Village.StartStorehouse();Village.Tick(20);Village.StartResearch(FoundingVillage.Technology.PatrolDoctrine);Village.Tick(15);Village.BuildWatchpost();
            for(int i=0;i<10;i++)Village.DeliverFood();
            Campaign=new DefenseCampaign();campaignRaid=dressingUsed=rallyPlaced=false;breachSeconds=participationSeconds=0;
            FoundingRaidCompleted=false;CommanderDirective=FoundingDirective.DefendVillage;RefreshFoundingBuildings();RefreshMaraAssignments();ReviewDefense();
            villagePanel=4;CommanderReport=Campaign.outcome;return true;
        }
        public bool CanBeginDefenseOperation => Campaign!=null&&Campaign.completed<2&&!FoundingRaidActive&&CanSaveVillage&&
            foundingCommander.fighter.Alive&&LivingEnemies==0&&CommanderDirective==FoundingDirective.DefendVillage&&
            (Campaign.completed==0||Campaign.playerPerk!=0&&Campaign.commanderPerk!=0);
        public bool BeginDefenseOperation(DefenseApproach approach)
        {
            if(!CanBeginDefenseOperation||approach<DefenseApproach.HoldVillage||approach>DefenseApproach.InterceptRoad)return false;
            Campaign.approach=(int)approach;FoundingRaidCompleted=false;
            startingCampaign=true;bool began;try{began=BeginFoundingRaid();}finally{startingCampaign=false;}
            if(!began)return false;
            campaignRaid=true;breachSeconds=participationSeconds=0;dressingUsed=rallyPlaced=false;
            CommanderReport=approach==DefenseApproach.InterceptRoad?
                "Mara: Garrison holds. I will take the reserve to the north road. Join us or cover home.":
                "Mara: Hold the village approaches. Garrison stays posted; reserve responds to observed contact.";
            villagePanel=0;ReviewDefense();return true;
        }
        private void TrackDefenseOperation(float dt)
        {
            if(!campaignRaid||Campaign==null)return;
            bool breach=false,near=false;
            foreach(var a in foundingRaid)if(a.fighter.Alive)
            {
                breach|=Vector3.Distance(a.root.position,VillageCenter)<6;
                near|=player.fighter.Alive&&Vector3.Distance(a.root.position,player.root.position)<10&&TerrainSight(player.root.position,a.root.position);
            }
            if(breach)breachSeconds+=dt;
            if(near)participationSeconds+=dt;
        }
        private void CompleteDefenseOperation()
        {
            if(!campaignRaid||Campaign==null)return;
            campaignRaid=false;rallyPlaced=false;
            Campaign.completed++;Campaign.lastSurvivors=LivingCompanions;Campaign.lastProtected=breachSeconds<5;
            Campaign.lastParticipated=participationSeconds>=3;
            Campaign.lastFoodLost=Campaign.lastProtected?0:Village.RemoveFood(5);
            if(player.fighter.Alive)Campaign.playerXp+=100+(Campaign.lastProtected?50:0)+(Campaign.lastParticipated?25:0);
            if(foundingCommander.fighter.Alive)Campaign.commanderXp+=100+(Campaign.lastProtected?50:0);
            Campaign.outcome="Raid repelled / "+LivingCompanions+" of 8 survived / "+(Campaign.lastProtected?"stores protected":"stores breached: "+Campaign.lastFoodLost+" food lost")+".";
            CommanderReport=Campaign.outcome+" Return to council for progression and save.";villagePanel=4;
        }
        public bool ChooseFounderPerk(FounderPerk perk) => Campaign!=null&&!FoundingRaidActive&&AtFoundingCouncil&&Campaign.ChooseFounder(perk);
        public bool ChooseCaptainPerk(CaptainPerk perk)
        {
            if(Campaign==null||FoundingRaidActive||!AtFoundingCouncil||!foundingCommander.fighter.Alive||!Campaign.ChooseCaptain(perk))return false;
            ReviewDefense();return true;
        }
        public bool RestDefenseParty()
        {
            if(Campaign==null||!CanSaveVillage||Village.Food<2)return false;
            Village.RemoveFood(2);
            foreach(var a in actors)if(!a.enemy&&a.fighter.Alive)a.fighter.Recover(100f/18);
            CommanderReport="Two food used for recovery at the council. Living fighters rested; casualties remain.";
            return true;
        }
        public bool UseFieldDressing()
        {
            if(Campaign==null||Campaign.playerPerk!=(int)FounderPerk.FieldDressing||!campaignRaid||dressingUsed||paused||!player.fighter.CanAct||Village.Food<1)return false;
            Actor patient=null;float health=100;
            foreach(var a in foundingSoldiers)if(a.fighter.CanAct&&a.fighter.Health<health&&Vector3.Distance(a.root.position,player.root.position)<=3){patient=a;health=a.fighter.Health;}
            if(patient==null)return false;
            foreach(var enemy in actors)if(enemy.enemy&&enemy.fighter.Alive&&(Vector3.Distance(enemy.root.position,patient.root.position)<8||Vector3.Distance(enemy.root.position,player.root.position)<8))return false;
            Village.RemoveFood(1);patient.fighter.Recover(20f/18);dressingUsed=true;
            CommanderReport="Field dressing: "+patient.root.name+" recovered up to 20 health. One food used.";return true;
        }
        public bool PlaceForwardRally()
        {
            if(Campaign==null||Campaign.playerPerk!=(int)FounderPerk.ForwardRally||!campaignRaid||paused||!player.fighter.CanAct||!foundingCommander.fighter.Alive||Vector3.Distance(player.root.position,foundingCommander.root.position)>12)return false;
            Vector3 p=player.root.position;if(p.z < -10||p.z>7||Mathf.Abs(p.x)>6)return false;
            campaignRally=p;rallyPlaced=true;CommanderReport="Mara: Reserve rally acknowledged. Fixed garrison remains on post.";return true;
        }
        private bool CampaignGoal(Actor a,int rank,out Vector3 goal)
        {
            goal=Vector3.zero;
            if(!campaignRaid||Campaign==null||rank<DefenseGarrison&&a!=foundingCommander)return false;
            if(rallyPlaced)goal=campaignRally;
            else if(Campaign.approach==(int)DefenseApproach.InterceptRoad)goal=new Vector3(0,0,4);
            else return false;
            if(a!=foundingCommander)goal+=new Vector3((a.slot%3-1)*1.6f,0,-a.slot/3f);
            return true;
        }
    }
}
