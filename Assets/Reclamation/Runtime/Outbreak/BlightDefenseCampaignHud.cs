using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        private void DrawCampaignPanel(GUIStyle heading,GUIStyle body,GUIStyle detail,GUIStyle eyebrow)
        {
            Rect r=VillagePanelRect;GUI.Box(r,GUIContent.none);float x=r.x+14,y=r.y+10,w=r.width-28;
            GUI.Label(new Rect(x,y,w,27),"NORTH ROAD / OPERATION "+Mathf.Min(2,Campaign.completed+1),heading);y+=31;
            GUI.Label(new Rect(x,y,w,44),FoundingRaidActive?"Garrison holds. Join the reserve or cover home. Five total seconds of enemy presence risks food stores.":Campaign.outcome,detail);y+=48;
            GUI.enabled=CanBeginDefenseOperation;
            if(GUI.Button(new Rect(x,y,w/2-3,30),"Intercept road",button))BeginDefenseOperation(DefenseApproach.InterceptRoad);
            if(GUI.Button(new Rect(x+w/2+3,y,w/2-3,30),"Defend village",button))BeginDefenseOperation(DefenseApproach.HoldVillage);
            GUI.enabled=true;y+=35;
            GUI.Label(new Rect(x,y,w,20),"XP  Founder "+Campaign.playerXp+"   /   Mara "+Campaign.commanderXp+"   /   Perk at 100",eyebrow);y+=25;
            GUI.Label(new Rect(x,y,w,20),"FOUNDER / "+(FounderPerk)Campaign.playerPerk,eyebrow);y+=23;
            GUI.enabled=AtFoundingCouncil&&!FoundingRaidActive&&Campaign.playerXp>=100&&Campaign.playerPerk==0;
            if(GUI.Button(new Rect(x,y,w/2-3,28),"Field dressing",button))ChooseFounderPerk(FounderPerk.FieldDressing);
            if(GUI.Button(new Rect(x+w/2+3,y,w/2-3,28),"Forward rally",button))ChooseFounderPerk(FounderPerk.ForwardRally);
            GUI.enabled=true;y+=31;
            GUI.Label(new Rect(x,y,w,36),"Dressing: restore ally health/stamina, 1 food, once per raid, clear of enemies. Rally: redirect reserve; posts hold.",detail);y+=40;
            GUI.Label(new Rect(x,y,w,20),"MARA / "+(CaptainPerk)Campaign.commanderPerk,eyebrow);y+=23;
            GUI.enabled=AtFoundingCouncil&&!FoundingRaidActive&&Campaign.commanderXp>=100&&Campaign.commanderPerk==0&&foundingCommander.fighter.Alive;
            if(GUI.Button(new Rect(x,y,w/2-3,28),"Watch captain",button))ChooseCaptainPerk(CaptainPerk.WatchCaptain);
            if(GUI.Button(new Rect(x+w/2+3,y,w/2-3,28),"Careful relief",button))ChooseCaptainPerk(CaptainPerk.CarefulRelief);
            GUI.enabled=true;y+=31;
            GUI.Label(new Rect(x,y,w,35),"Watch: 3 fixed guards, fewer mobile troops. Relief: withdraw soldiers at 55 health instead of 35.",detail);y+=38;
            GUI.enabled=FoundingRaidActive&&!paused&&Campaign.playerPerk==(int)FounderPerk.FieldDressing&&!dressingUsed;
            if(GUI.Button(new Rect(x,y,w/2-3,28),"Use dressing",button))UseFieldDressing();
            GUI.enabled=FoundingRaidActive&&!paused&&Campaign.playerPerk==(int)FounderPerk.ForwardRally;
            if(GUI.Button(new Rect(x+w/2+3,y,w/2-3,28),"Rally at my position",button))PlaceForwardRally();
            GUI.enabled=true;y+=32;
            GUI.enabled=CanSaveVillage;if(GUI.Button(new Rect(x,y,w/2-3,27),"Save at council",button))SaveVillage();GUI.enabled=CanSaveVillage&&Village.Food>=2;
            if(GUI.Button(new Rect(x+w/2+3,y,w/2-3,27),"Rest / 2 food",button))RestDefenseParty();GUI.enabled=true;
            GUI.Label(new Rect(x,y+30,w,36),Campaign.completed>=2?"Two-operation slice complete. Save to keep outcomes and perks.":FoundingRaidActive ? (Campaign.playerPerk==(int)FounderPerk.FieldDressing?"Dressing: ready wounded ally within 3 m; enemies at least 8 m away.":Campaign.playerPerk==(int)FounderPerk.ForwardRally?"Rally: within 12 m of Mara, on the central road approach.":"Fight alongside the defense. Perks unlock after this operation.") : "After victory, choose both perks at council before the follow-up raid.",detail);
        }
    }
}
