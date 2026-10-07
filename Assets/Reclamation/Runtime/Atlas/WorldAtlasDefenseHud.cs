using Reclamation.Blight;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo
    {
        private Rect BattlePanelRect=>new Rect(16,165,410,410);
        private bool CompactBattleHud=>SyntyCompanyTrial&&!defensePanel&&!mapOpen&&!commanderPanel;
        public bool BattleHudContains(Vector2 p)
        {
            if(DuelPractice)return new Rect(16,16,430,90).Contains(p)||p.y>Height-90||debug&&new Rect(16,118,430,85).Contains(p);
            if(CompactBattleHud)return new Rect(16,16,Width-32,86).Contains(p)||new Rect(16,Height-58,450,46).Contains(p)||new Rect(Width-268,Height-422,252,284).Contains(p)||debug&&new Rect(450,165,350,140).Contains(p);
            return p.y<155||p.y>Height-90||defensePanel&&BattlePanelRect.Contains(p)||p.x>Width-340||mapOpen;
        }
        private bool BattlePointerOverHud()
        {
            if(Mouse.current==null)return false;Vector2 p=Mouse.current.position.ReadValue();p=new Vector2(p.x/UiScale,(Screen.height-p.y)/UiScale);
            return BattleHudContains(p);
        }
        private void DrawAtlasDefenseHud()
        {
            if(DuelPractice){DrawPracticeHud();return;}
            var p=defenseState.progression;
            string objective=!battlePlayer.fighter.Alive?"YOU FELL / Reload a checkpoint or start fresh":defenseActive?defenseWarning>0?"RAID WARNING / "+Mathf.CeilToInt(defenseWarning)+" seconds":"HOLD THE ROAD / "+BattleEnemies+" raiders remain":p.completed==2?"TWO RAIDS REPELLED / Save your progress":"COMMAND POST / B for orders, recovery and perks";
            if(CompactBattleHud)
            {
                Panel(new Rect(16,16,Width-32,86));
                GUI.Label(new Rect(30,23,Width-300,26),objective,subheading);
                GUI.Label(new Rect(30,53,Width-300,22),"Health "+battlePlayer.fighter.Health.ToString("0")+"   /   Stamina "+battlePlayer.fighter.Stamina.ToString("0")+"   /   Company "+BattleSurvivors+" / 25   /   Food "+defenseState.food,small);
                GUI.Label(new Rect(Width-260,27,230,44),Model.Name+"\nDay "+day+" / "+((int)clockHours).ToString("00")+":"+((int)(clockHours%1*60)).ToString("00"),small);
            }
            else
            {
            Panel(new Rect(16,16,Width-32,62));
            GUI.Label(new Rect(30,21,Width-380,27),Model.Name+(SyntyTrial?" / SYNTY CHARACTER TRIAL":" / VILLAGE DEFENSE"),subheading);
            GUI.Label(new Rect(30,49,Width-380,22),"Health "+battlePlayer.fighter.Health.ToString("0")+"   /   Stamina "+battlePlayer.fighter.Stamina.ToString("0")+"   /   Food "+defenseState.food+"   /   Company "+BattleSurvivors+" / 25",small);
            GUI.Label(new Rect(Width-335,23,310,44),"DAY "+day+" / "+((int)clockHours).ToString("00")+":"+((int)(clockHours%1*60)).ToString("00")+"\n"+(Model.Extent/1000).ToString("0.0")+" km / "+Model.factions.Count+" factions",small);
            Panel(new Rect(16,88,Width-32,63));
            GUI.Label(new Rect(30,94,Width-70,25),objective,subheading);
            GUI.Label(new Rect(30,123,Width-70,24),defenseActive?"P"+(reservePlatoon+1)+" reserve responding / Other platoons hold / Stores breached after 5 seconds: "+defenseBreach.ToString("0.0")+"s":battleReport,small);
            }
            if(mapOpen){GUI.enabled=!defenseActive;DrawAtlas();GUI.enabled=true;}
            else
            {
                DrawMinimap();DrawBattleBadges();DrawCommanderPanel();
                if(defensePanel)
                {
                    Rect r=BattlePanelRect;Panel(r);float x=r.x+14,y=r.y+10,w=r.width-28;
                    GUI.Label(new Rect(x,y,w,27),CommanderName+" / OPERATION "+Mathf.Min(2,p.completed+1),subheading);y+=31;
                    GUI.Label(new Rect(x,y,w,44),"Protect "+Model.sites[defenseHome].name+". Six raiders approach by the south road. Your company has four platoons.",body);y+=52;
                    GUI.enabled=!paused&&!defenseActive&&AtDefenseCouncil&&p.completed<2;
                    if(GUI.Button(new Rect(x,y,w/2-4,30),"Intercept road",button))BeginAtlasDefense(true);
                    if(GUI.Button(new Rect(x+w/2+4,y,w/2-4,30),"Defend stores",button))BeginAtlasDefense(false);y+=37;
                    GUI.enabled=true;GUI.Label(new Rect(x,y,w,23),"XP  Founder "+p.playerXp+" / "+CommanderName+" "+p.commanderXp+" / Perk at 100",small);y+=27;
                    GUI.enabled=!paused&&!defenseActive&&AtDefenseCouncil&&p.playerXp>=100&&p.playerPerk==0;
                    if(GUI.Button(new Rect(x,y,w/2-4,28),"Field dressing",button))ChooseAtlasFounder(FounderPerk.FieldDressing);
                    if(GUI.Button(new Rect(x+w/2+4,y,w/2-4,28),"Forward rally",button))ChooseAtlasFounder(FounderPerk.ForwardRally);y+=32;
                    GUI.enabled=true;GUI.Label(new Rect(x,y,w,34),"Dressing: aid a safe nearby ally / 1 food.\nRally: redirect the reserve within the village approach.",small);y+=39;
                    GUI.enabled=!paused&&!defenseActive&&AtDefenseCouncil&&p.commanderXp>=100&&p.commanderPerk==0;
                    if(GUI.Button(new Rect(x,y,w/2-4,28),"Watch captain",button))ChooseAtlasCaptain(CaptainPerk.WatchCaptain);
                    if(GUI.Button(new Rect(x+w/2+4,y,w/2-4,28),"Careful relief",button))ChooseAtlasCaptain(CaptainPerk.CarefulRelief);y+=32;
                    GUI.enabled=true;GUI.Label(new Rect(x,y,w,34),"Watch: two reserve soldiers reinforce the stores.\nRelief: withdraw wounded troops at 55 health, not 35.",small);y+=39;
                    GUI.enabled=!paused&&CanCheckpointAtlas();
                    if(GUI.Button(new Rect(x,y,w/2-4,28),"Save checkpoint",button))SaveAtlasDefense();
                    if(GUI.Button(new Rect(x+w/2+4,y,w/2-4,28),"Rest / 2 food",button))RestAtlasCompany();y+=34;
                    GUI.enabled=!paused&&defenseActive&&p.playerPerk!=0;
                    if(GUI.Button(new Rect(x,y,w,28),"Q  Use selected player perk",button))UseAtlasPerk();y+=32;GUI.enabled=true;
                    GUI.Label(new Rect(x,y,w,40),"Chosen: "+(p.playerPerk==0?"no player perk":p.playerPerk==1?"Field dressing":"Forward rally")+" / "+(p.commanderPerk==0?"no command perk":p.commanderPerk==1?"Watch captain":"Careful relief"),small);
                }
                if(debug)
                {
                    Rect r=new Rect(450,165,350,140);Panel(r);
                    GUI.Label(new Rect(r.x+12,r.y+8,330,23),"DEVELOPER DIAGNOSTICS / F3",subheading);
                    GUI.Label(new Rect(r.x+12,r.y+38,325,93),"Map seed "+LayoutFor(defenseHome).Seed+" / Site "+defenseHome+"\nPlayer hits "+battlePlayerHits+" / Contribution "+defenseContribution.ToString("0.0")+"s\nReserve P"+(reservePlatoon+1)+" / "+BattleSurvivors+" troops alive\nShared DuelFighter / local atlas combat\nFrame "+frameMs.ToString("0.0")+" ms (not a benchmark)",small);
                }
            }
            if(impactUntil>Time.unscaledTime)GUI.Label(new Rect(Width/2-70,Height*.6f,200,30),battleImpact,heading);
            if(CompactBattleHud)
            {
                float bottom=Height-50;Panel(new Rect(16,bottom-8,450,46));
                if(GUI.Button(new Rect(28,bottom,150,29),"B  Operations",button))defensePanel=true;
                if(GUI.Button(new Rect(186,bottom,125,29),"M  Atlas",button))mapOpen=true;
                if(GUI.Button(new Rect(319,bottom,135,29),"F3  Debug",button))debug=!debug;
            }
            else
            {
            float by=Height-76;Panel(new Rect(16,by-8,Width-32,72));
            if(GUI.Button(new Rect(28,by,130,29),"B  Operations",button))defensePanel=!defensePanel;
            if(GUI.Button(new Rect(166,by,110,29),"M  Atlas",button))mapOpen=!mapOpen;
            GUI.enabled=!defenseActive;
            if(GUI.Button(new Rect(284,by,170,29),"Small forest valley",button))LoadMap(AtlasSize.Small,4);
            if(GUI.Button(new Rect(462,by,200,29),"Medium mountain coast",button))LoadMap(AtlasSize.Medium,8);
            if(GUI.Button(new Rect(670,by,140,29),"Command post",button)){hero.position=DefenseCouncil;UpdateCamera(true);}
            if(GUI.Button(new Rect(818,by,140,29),"Load checkpoint",button))LoadAtlasDefense();
            if(GUI.Button(new Rect(966,by,130,29),"Fresh scenario",button)){defenseMode=false;LoadMap(Model.Size,Model.factions.Count);EnableAtlasDefense();defenseState=new AtlasDefenseState{size=(int)Model.Size,site=defenseHome,seed=LayoutFor(defenseHome).Seed};foreach(var a in battleActors){a.fighter=new DuelFighter();a.visual.transform.localRotation=Quaternion.identity;}paused=false;battleReport="Fresh unsaved scenario. Existing checkpoint is preserved.";}
            GUI.enabled=true;
            if(GUI.Button(new Rect(1104,by,Width-1132,29),debug?"Hide debug":"F3 Debug",button))debug=!debug;
            GUI.Label(new Rect(30,by+35,Width-60,22),"WASD / Shift sprint / LMB light / E heavy / Ctrl block / Space dodge / Tab lock / Q perk / RMB look / Esc pause",small);
            }
            if(paused){Panel(new Rect(Width/2-135,Height/2-35,270,70));GUI.Label(new Rect(Width/2-110,Height/2-18,235,40),"PAUSED / Esc",heading);}
        }
        private void DrawBattleBadges()
        {
            var occupied=new System.Collections.Generic.List<Rect>();
            if(debug)occupied.Add(new Rect(450,165,350,140));
            foreach(var a in battleActors)
            {
                if(a==battlePlayer||!a.fighter.Alive||!a.root.gameObject.activeSelf)continue;
                if(a.enemy&&!Knowledge.Visible(new Vector2(a.root.position.x,a.root.position.z)))continue;
                float distance=Vector3.Distance(hero.position,a.root.position);if(distance>32||!BattleSight(hero.position,a.root.position))continue;
                var screen=view.WorldToScreenPoint(a.root.position+Vector3.up*2.3f);if(screen.z<=0)continue;
                var rect=new Rect(screen.x/UiScale-45,(Screen.height-screen.y)/UiScale-18,90,26);
                if(rect.y<(CompactBattleHud?106:155)||rect.yMax>Height-90||defensePanel&&rect.Overlaps(BattlePanelRect)||rect.x>Width-340)continue;
                bool leader=a.guard==0||a.guard>0&&(a.guard-1)%6==0;
                if(!a.enemy&&!leader&&a.fighter.Health>=99&&distance>6)continue;
                bool overlap=false;foreach(var previous in occupied)if(previous.Overlaps(rect)){overlap=true;break;}
                if(overlap)continue;occupied.Add(rect);
                string name=a.enemy?"RAIDER":a.guard==0?"★★ "+CommanderName:(a.guard-1)%6==0?"★ P"+((a.guard-1)/6+1):"P"+((a.guard-1)/6+1);
                GUI.Label(rect,name+" "+a.fighter.Health.ToString("0"),small);
            }
        }
    }
}
