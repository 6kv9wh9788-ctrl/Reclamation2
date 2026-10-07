using System.Collections;
using System.IO;
using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        public IEnumerator RunDefenseCampaignSmoke(string directory,bool intercept)
        {
            Directory.CreateDirectory(directory);InputEnabled=AutomaticSimulation=false;
            bool failed=false;Application.LogCallback observe=(s,trace,kind)=>{if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)failed=true;};
            Application.logMessageReceived+=observe;
            if(Campaign==null)failed|=!PrepareDefenseCampaign();
            villagePanel=4;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(directory,"briefing.png"));yield return new WaitForSeconds(.3f);
            int attacks=0;
            for(int operation=0;operation<2&&!failed;operation++)
            {
                player.root.position=VillageCenter+new Vector3(-3,0,-2);
                failed|=!BeginDefenseOperation(intercept?DefenseApproach.InterceptRoad:DefenseApproach.HoldVillage);
                bool captured=false;
                for(int frame=0;frame<10800&&FoundingRaidActive&&player.fighter.Alive;frame++)
                {
                    Actor target=null;float distance=float.MaxValue;
                    foreach(var a in foundingRaid)if(a.fighter.Alive){float d=Vector3.Distance(a.root.position,player.root.position);if(d<distance){distance=d;target=a;}}
                    if(target!=null)
                    {
                        SetPlayerLocomotion(distance<1.8f?Vector3.zero:Vector3.ClampMagnitude(target.root.position-player.root.position,1),false,false);
                        if(distance<2.2f&&RequestPlayerAttack(false))attacks++;
                    }
                    else SetPlayerLocomotion(Vector3.zero,false,false);
                    Simulate(1f/60);
                    if(!captured&&foundingRaid.Count>0)
                    {
                        captured=true;villageDebug=true;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(directory,"operation-"+operation+".png"));yield return new WaitForSeconds(.3f);
                    }
                    if(frame%6==0)yield return null;
                }
                failed|=FoundingRaidActive||!player.fighter.Alive||Campaign.completed!=operation+1;
                SetPlayerLocomotion(Vector3.zero,false,false);player.root.position=VillageCenter+Vector3.back*2;
                for(int frame=0;frame<900;frame++)Simulate(1f/60);
                villagePanel=4;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(directory,"outcome-"+operation+".png"));yield return new WaitForSeconds(.3f);
                if(operation==0)
                {
                    failed|=!ChooseFounderPerk(intercept?FounderPerk.ForwardRally:FounderPerk.FieldDressing);
                    failed|=!ChooseCaptainPerk(intercept?CaptainPerk.CarefulRelief:CaptainPerk.WatchCaptain);
                }
                if(operation==0)failed|=!RestDefenseParty();
                string checkpoint=Path.Combine(directory,"checkpoint.json");failed|=!SaveVillage(checkpoint);
                if(!failed)failed|=!LoadVillage(checkpoint)||Campaign.completed!=operation+1;
            }
            failed|=attacks==0||Campaign.completed!=2||BeginDefenseOperation(DefenseApproach.HoldVillage);
            villagePanel=4;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(directory,"complete.png"));yield return new WaitForSeconds(.3f);
            File.WriteAllText(Path.Combine(directory,"result.txt"),"Passed: "+!failed+"\nOperations: "+Campaign.completed+"\nPlayer attacks: "+attacks+"\nFounder XP: "+Campaign.playerXp+"\nMara XP: "+Campaign.commanderXp+"\n"+Campaign.outcome);
            Application.logMessageReceived-=observe;Application.Quit(failed?1:0);
        }
    }
}
