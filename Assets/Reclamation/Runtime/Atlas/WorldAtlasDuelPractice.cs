using System;
using System.Collections;
using System.IO;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo
    {
        public bool DuelPractice {get;private set;}
        private bool practiceShield;
        private int practiceCount=1;
        private bool practiceResultLabels;
        private Vector3 practiceForward;
        private float practiceAttackSpacing;
        public void BeginDuelPractice(int opponents=1,bool shieldDrill=false)
        {
            if(!defenseMode)EnableAtlasDefense();
            DuelPractice=true;practiceCount=Mathf.Clamp(opponents,1,2);practiceShield=shieldDrill;
            for(int i=battleActors.Count-1;i>=26;i--){battleActors[i].root.gameObject.SetActive(false);Destroy(battleActors[i].root.gameObject);battleActors.RemoveAt(i);}
            paused=false;mapOpen=defensePanel=commanderPanel=false;defenseActive=true;defenseWarning=0;
            battleAccumulator=0;battleMove=Vector3.zero;battleAttack=battleDodge=battleBlock=battleSprint=false;
            battlePlayer.fighter=new DuelFighter();battlePlayerHits=0;battleLock=null;
            hero.position=DefensePoint(0,-43);BattleFace(battlePlayer,DefensePoint(0,-60)-hero.position);
            practiceForward=hero.forward;practiceAttackSpacing=0;
            // Keep the normal company visible in town, clear of the practice camera and exchange.
            for(int i=1;i<26;i++)battleActors[i].root.position=DefensePoint(((i-1)%5-2)*1.6f,10+(i-1)/5*1.6f);
            SpawnAtlasRaiders(practiceCount);
            for(int i=26;i<battleActors.Count;i++)
            {
                var a=battleActors[i];a.root.position=hero.position+hero.forward*2.05f+hero.right*(practiceCount==1?0:(i==26?-.75f:.75f));
                BattleFace(a,hero.position-a.root.position);a.fighter.Blocking=shieldDrill;
            }
            battleLock=battleActors[26];yaw=hero.eulerAngles.y;pitch=18;zoom=5;SyncBattleVisuals(.1f);UpdateCamera(true);
        }
        // Stable approach lanes follow player translation, not camera orbit or target-lock turns.
        // Only the two-opponent drill uses this policy; committed actions are never redirected.
        private bool TryPracticePairAI(BattleActor actor,float dt)
        {
            if(!DuelPractice||practiceShield||practiceCount!=2||!actor.enemy||BattleEnemies!=2)return false;
            if(!actor.fighter.CanAct)return true;
            if(!BattleSight(actor.root.position,hero.position)||BattleDistance(actor.root.position,hero.position)>13)return false;
            actor.fighter.Blocking=false;
            var strike=BlightEquipment.Enemy(BlightEnemy.Thrall,actor.fighter.AttackSequence);
            int side=battleActors.IndexOf(actor)==26?-1:1;
            Vector3 lane=Quaternion.AngleAxis(side*32,Vector3.up)*practiceForward;
            Vector3 goal=hero.position+lane*(strike.Reach-.55f);
            BattleApproach(actor,goal,2.2f,dt,.18f);
            BattleFace(actor,hero.position-actor.root.position);
            if(BattleDistance(actor.root.position,goal)>.36f||actor.delay>0||practiceAttackSpacing>0)return true;
            if(!DuelFighter.InReach(actor.root.position,actor.root.forward,hero.position,strike.Reach-.10f,strike.HalfAngle))return true;
            if(actor.fighter.Attack(strike))
            {
                actor.delay=strike.Windup+strike.Recovery+.45f;
                practiceAttackSpacing=.32f;
            }
            return true;
        }
        private void UpdatePracticeInput(Keyboard keyboard)
        {
            if(keyboard.rKey.wasPressedThisFrame)BeginDuelPractice(practiceCount,practiceShield);
            if(keyboard.digit1Key.wasPressedThisFrame)BeginDuelPractice(1,false);
            if(keyboard.digit2Key.wasPressedThisFrame)BeginDuelPractice(2,false);
            if(keyboard.gKey.wasPressedThisFrame)BeginDuelPractice(1,true);
            if(keyboard.f4Key.wasPressedThisFrame)practiceResultLabels=!practiceResultLabels;
        }
        private void DrawPracticeHud()
        {
            Panel(new Rect(16,16,430,90));
            string state=!battlePlayer.fighter.Alive?"DEFEATED / R to retry":BattleEnemies==0?"EXCHANGE WON / R to retry":practiceShield?"SHIELD DRILL / Opponent guards":"VILLAGE PRACTICE / "+practiceCount+" opponent"+(practiceCount>1?"s":"");
            GUI.Label(new Rect(30,23,405,27),state,subheading);
            DrawPracticeMeter(30,56,"Health",battlePlayer.fighter.Health,new Color(.72f,.22f,.18f));
            DrawPracticeMeter(238,56,"Stamina",battlePlayer.fighter.Stamina,new Color(.68f,.57f,.22f));
            GUI.Label(new Rect(30,82,400,20),"Practice only / No campaign rewards or saves",small);
            float y=Height-75;Panel(new Rect(16,y-8,Width-32,72));
            if(GUI.Button(new Rect(28,y,140,28),"1  Duel",button))BeginDuelPractice(1);
            if(GUI.Button(new Rect(176,y,140,28),"2  Two opponents",button))BeginDuelPractice(2);
            if(GUI.Button(new Rect(324,y,140,28),"G  Shield drill",button))BeginDuelPractice(1,true);
            if(GUI.Button(new Rect(472,y,140,28),"R  Restart",button))BeginDuelPractice(practiceCount,practiceShield);
            if(GUI.Button(new Rect(620,y,140,28),"Switch map",button)){var size=Model.Size==AtlasSize.Small?AtlasSize.Medium:AtlasSize.Small;LoadMap(size,size==AtlasSize.Small?4:8);BeginDuelPractice(practiceCount,practiceShield);}
            if(GUI.Button(new Rect(768,y,140,28),"F3  Debug",button))debug=!debug;
            if(GUI.Button(new Rect(916,y,180,28),practiceResultLabels?"F4  Hide hit labels":"F4  Show hit labels",button))practiceResultLabels=!practiceResultLabels;
            GUI.Label(new Rect(30,y+33,Width-60,22),"WASD / Shift run / LMB light / E heavy / Ctrl block / Space dodge / RMB orbit / Tab lock / Esc pause",small);
            DrawBattleBadges();
            if(debug){Panel(new Rect(16,118,430,85));GUI.Label(new Rect(28,126,405,70),"DEVELOPER DIAGNOSTICS / F3\nShared DuelFighter / hits "+battlePlayerHits+" / "+battlePlayer.fighter.Action+"\nFrame "+frameMs.ToString("0.0")+" ms (not a benchmark)",small);}
            if(practiceResultLabels&&impactUntil>Time.unscaledTime)GUI.Label(new Rect(Width/2-70,Height*.6f,200,30),battleImpact,heading);
            if(paused)GUI.Label(new Rect(Width/2-100,Height/2,240,35),"PAUSED / Esc",heading);
        }
        private void DrawPracticeMeter(float x,float y,string name,float value,Color color)
        {
            GUI.Label(new Rect(x,y,185,20),name+" "+value.ToString("0"),small);
            var previous=GUI.color;
            GUI.color=new Color(.12f,.14f,.14f);GUI.DrawTexture(new Rect(x,y+20,185,4),Texture2D.whiteTexture);
            GUI.color=color;GUI.DrawTexture(new Rect(x,y+20,185*Mathf.Clamp01(value/100),4),Texture2D.whiteTexture);
            GUI.color=previous;
        }
        private IEnumerator DuelPracticeSmoke()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--output");string output=at>=0&&at+1<args.Length?args[at+1]:Path.Combine(Application.temporaryCachePath,"DuelPractice");Directory.CreateDirectory(output);
            AutomaticBattle=false;AutomaticClock=false;bool passed=true;string report="Real shared fighter exchanges; no invulnerability or synthetic damage.\n";
            foreach(var size in new[]{AtlasSize.Small,AtlasSize.Medium})
            {
                LoadMap(size,size==AtlasSize.Small?4:8);BeginDuelPractice(1,true);yield return null;
                // Real practice input, captured at two step phases from front and side.
                foreach(int angle in new[]{90,180})
                foreach(int direction in new[]{-1,1})
                {
                    BeginDuelPractice(1,true);yaw=hero.eulerAngles.y+angle;UpdateCamera(true);
                    SetAtlasBattleInput(direction<0?-hero.forward:hero.right,true);
                    for(int frame=0;frame<24;frame++)
                    {
                        SimulateAtlasBattle(1f/60);UpdateCamera(true);
                        if(frame==6||frame==18)
                        {
                            yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,size+"-motion-"+angle+"-"+direction+"-"+frame+".png"));
                            yield return new WaitForSeconds(.1f);
                        }
                    }
                }
                BeginDuelPractice(1,true);yield return null;
                RequestAtlasAttack(false);SimulateAtlasBattle(.26f);
                var opponent=battleActors[26];passed&=opponent.fighter.Health==100&&opponent.synty.LastContact=="Blocked";
                SimulateAtlasBattle(.08f);yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,size+"-blocked.png"));yield return new WaitForSeconds(.2f);
                report+=size+" shield: "+opponent.synty.LastContact+" / health "+opponent.fighter.Health+" / stamina "+opponent.fighter.Stamina+"\n";
                foreach(int count in new[]{1,2})
                {
                    BeginDuelPractice(count);int frames=0;
                    while(frames++<2400&&battlePlayer.fighter.Alive&&BattleEnemies>0)
                    {
                        battleLock=battleActors.Find(a=>a.enemy&&a.fighter.Alive);
                        if(battlePlayer.fighter.CanAct)RequestAtlasAttack(false);
                        SimulateAtlasBattle(1f/60);
                        if(frames%30==0)yield return null;
                        if(frames==120){ScreenCapture.CaptureScreenshot(Path.Combine(output,size+"-"+count+"-combat.png"));yield return new WaitForSeconds(.2f);}
                    }
                    passed&=battlePlayerHits>0&&(!battlePlayer.fighter.Alive||BattleEnemies==0)&&defenseState.progression.completed==0&&defenseState.progression.playerXp==0;
                    report+=size+" / "+count+" enemies / player hits "+battlePlayerHits+" / health "+battlePlayer.fighter.Health+" / enemies left "+BattleEnemies+" / campaign XP "+defenseState.progression.playerXp+"\n";
                }
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),"Passed: "+passed+"\n"+report);Application.Quit(passed?0:1);
        }
    }
}
