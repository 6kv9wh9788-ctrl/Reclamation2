using System;
using System.Collections;
using System.IO;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo
    {
        // Packaged-player visual evidence. This command is separate from combat smoke.
        private IEnumerator SyntyCapture()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--output");
            string output=at>=0&&at+1<args.Length?args[at+1]:Path.Combine(Application.temporaryCachePath,"SyntyCapture");
            Directory.CreateDirectory(output);AutomaticBattle=false;AutomaticClock=false;paused=false;defensePanel=false;debug=false;
            if(!battlePlayer.synty){Debug.LogError("Synty capture requires --atlas-defense --atlas-synty");Application.Quit(1);yield break;}
            hero.rotation=Quaternion.identity;zoom=3.4f;pitch=12;
            foreach(string pose in new[]{"idle-front","idle-side","idle-rear","block","light-load","light-contact","heavy-load","heavy-contact","run","dodge","relaxed","alert"})
            {
                var fighter=new DuelFighter();
                battlePlayer.synty.Readiness=pose=="relaxed"?SyntyReadiness.Relaxed:pose=="alert"?SyntyReadiness.Alert:SyntyReadiness.Combat;
                if(pose.StartsWith("light")||pose.StartsWith("heavy"))
                {fighter.Attack(BlightEquipment.Weapon(BlightWeapon.Sword,pose.StartsWith("heavy")));fighter.Advance(fighter.Strike.Windup*(pose.EndsWith("load")?.65f:1));}
                if(pose=="block")fighter.Blocking=true;
                if(pose=="dodge"){fighter.Dodge();fighter.Advance(.16f);}
                for(int i=0;i<20;i++)battlePlayer.synty.Sample(fighter,pose=="run",pose=="run",1f/60);
                yaw=pose=="idle-side"?90:pose=="idle-rear"?0:180;UpdateCamera(true);
                yield return null;yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(output,pose+".png"));yield return new WaitForSeconds(.25f);
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),"Synty pose captures complete. These are staged presentation poses, not combat outcomes.\nSkin vertices: "+battlePlayer.synty.SkinVertices);
            if(SyntyRoleTrial)
            {
                SetHour(12);TickVillage(1f/60);for(int i=0;i<30;i++)SimulateAtlasBattle(1f/60);
                Vector3 home=hero.position;
                foreach(var role in GetComponentsInChildren<AtlasSyntyVisual>(true))
                {
                    if(role.Role==SyntyRole.Player||!role.gameObject.activeInHierarchy)continue;
                    hero.position=role.transform.position;hero.rotation=Quaternion.identity;
                    battlePlayer.synty.SetVisible(false);
                    yaw=role.transform.eulerAngles.y+180;zoom=3.8f;pitch=12;UpdateCamera(true);
                    yield return null;yield return null;
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"role-"+role.Role+"-"+role.Variant+".png"));yield return new WaitForSeconds(.25f);
                }
                hero.position=home;SetHour(23);SimulateAtlasBattle(1f/60);
                var commander=battleActors.Find(a=>a.guard==0);
                hero.position=commander.root.position;battlePlayer.synty.SetVisible(false);
                yaw=commander.root.eulerAngles.y+90;UpdateCamera(true);yield return null;yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"night-commander.png"));yield return new WaitForSeconds(.25f);
                File.AppendAllText(Path.Combine(output,"result.txt"),"\nRole captures include staged camera placement around existing residents and guards. Full combat is verified separately.");
            }
            Application.Quit(0);
        }
    }
}
