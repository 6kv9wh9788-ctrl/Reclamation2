using System;
using System.Collections;
using System.IO;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo
    {
        public DuelFighter AtlasDefenseFighter(int index)=>battleActors[index].fighter;
        private IEnumerator AtlasDefenseSmoke()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--output");
            string directory=index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Application.temporaryCachePath,"AtlasDefenseSmoke");Directory.CreateDirectory(directory);
            AutomaticBattle=false;bool failed=false;
            Application.LogCallback observe=(s,t,k)=>{if(k==LogType.Error||k==LogType.Exception||k==LogType.Assert)failed=true;};Application.logMessageReceived+=observe;
            string results="";
            foreach(var size in new[]{AtlasSize.Small,AtlasSize.Medium})
            {
                LoadMap(size,size==AtlasSize.Small?4:8);AutomaticBattle=false;AutomaticClock=false;paused=false;debug=true;
                yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(directory,size+"-briefing.png"));yield return new WaitForSeconds(.3f);
                for(int operation=0;operation<2&&!failed;operation++)
                {
                    hero.position=DefenseCouncil;SetAtlasBattleInput(Vector3.zero);SimulateAtlasBattle(1);
                    failed|=!BeginAtlasDefense(operation==0);int hitsBefore=battlePlayerHits;
                    for(int frame=0;frame<18000&&defenseActive&&battlePlayer.fighter.Alive;frame++)
                    {
                        BattleActor target=null;float distance=float.MaxValue;
                        foreach(var a in battleActors)if(a.enemy&&a.fighter.Alive){float d=Vector3.Distance(hero.position,a.root.position);if(d<distance){distance=d;target=a;}}
                        if(battlePlayer.fighter.Health<60)
                        {
                            Vector3 retreat=DefensePoint(-8,7)-hero.position;
                            SetAtlasBattleInput(retreat.magnitude>.5f?retreat:Vector3.zero);
                            if(distance<2.8f&&battlePlayer.fighter.Stamina>40)RequestAtlasDodge();
                        }
                        else if(target!=null&&distance<10f)
                        {
                            battleLock=target;
                            bool tired=battlePlayer.fighter.Stamina<22;
                            SetAtlasBattleInput(tired?hero.position-target.root.position:distance>1.75f?target.root.position-hero.position:Vector3.zero,tired);
                            if(distance<2.2f&&!tired)RequestAtlasAttack(false);
                        }
                        else
                        {
                            Vector3 destination=DefensePoint(-8,-61); // Join the outer watch instead of charging beyond friendly support.
                            Vector3 delta=destination-hero.position;SetAtlasBattleInput(delta.magnitude>.4f?delta:Vector3.zero);
                        }
                        TickVillage(1f/60);SimulateAtlasBattle(1f/60);
                        if(frame==1320){defensePanel=false;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(directory,size+"-combat-"+operation+".png"));yield return new WaitForSeconds(.3f);}
                        if(frame%6==0)yield return null;
                    }
                    failed|=defenseActive||!battlePlayer.fighter.Alive||defenseState.progression.completed!=operation+1||battlePlayerHits==hitsBefore;
                    SetAtlasBattleInput(Vector3.zero);hero.position=DefenseCouncil;for(int i=0;i<1200;i++)SimulateAtlasBattle(1f/60);
                    if(operation==0)
                    {
                        failed|=!ChooseAtlasFounder(size==AtlasSize.Small?FounderPerk.FieldDressing:FounderPerk.ForwardRally);
                        failed|=!ChooseAtlasCaptain(size==AtlasSize.Small?CaptainPerk.WatchCaptain:CaptainPerk.CarefulRelief);
                        failed|=!RestAtlasCompany();
                    }
                    string checkpoint=Path.Combine(directory,size+"-defense-v1.json");failed|=!SaveAtlasDefense(checkpoint)||!LoadAtlasDefense(checkpoint);
                    defensePanel=true;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(directory,size+"-outcome-"+operation+".png"));yield return new WaitForSeconds(.3f);
                }
                if(defenseState.progression.completed==2)failed|=BeginAtlasDefense(false);else failed=true;
                results+=size+": "+defenseState.progression.completed+" operations / "+battlePlayerHits+" player damaging hits / "+BattleSurvivors+" survivors / "+defenseState.progression.playerXp+" player XP / health "+battlePlayer.fighter.Health+" / active "+defenseActive+" / enemies "+BattleEnemies+"\n";
                mapOpen=true;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(directory,size+"-map.png"));yield return new WaitForSeconds(.3f);mapOpen=false;
                if(failed)break;
            }
            File.WriteAllText(Path.Combine(directory,"result.txt"),"Passed: "+!failed+"\n"+results);
            Application.logMessageReceived-=observe;Application.Quit(failed?1:0);
        }
    }
}
