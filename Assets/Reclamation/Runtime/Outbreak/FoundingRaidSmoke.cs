using System.Collections;
using System.IO;
using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        public IEnumerator RunFoundingRaidSmoke(string directory)
        {
            Directory.CreateDirectory(directory); InputEnabled = AutomaticSimulation = false;
            bool failed=false, observed=false; int playerAttacks=0;
            Application.LogCallback observe=(message,stack,type)=> { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) failed=true; };
            Application.logMessageReceived += observe;
            // Set up a developed village; the raid itself uses real movement, AI and resolved combat.
            foreach(var a in actors) if(a.enemy) a.fighter.Receive(10000,false,false);
            Village.RecoverSupplies(); Village.DeliverSupplies(); Village.StartResearch(FoundingVillage.Technology.WorkCrews); Village.Tick(15);
            Village.StartStorehouse(); Village.Tick(20); Village.StartResearch(FoundingVillage.Technology.PatrolDoctrine); Village.Tick(15); Village.BuildWatchpost(); RefreshFoundingBuildings();
            failed |= !BeginFoundingRaid(); player.root.position=VillageCenter+new Vector3(-3,0,-2); villagePanel=1;
            yield return null; ScreenCapture.CaptureScreenshot(Path.Combine(directory,"prepare.png")); yield return new WaitForSeconds(.3f);
            for(int f=0;f<8400 && !FoundingRaidCompleted && player.fighter.Alive;f++)
            {
                Actor target=null; float distance=float.MaxValue;
                foreach(var a in foundingRaid) if(a.fighter.Alive) { float d=Vector3.Distance(a.root.position,player.root.position); if(d<distance) { target=a; distance=d; } }
                // Join the defense once Mara has actually observed the approaching group.
                if(DefenseAlert && target!=null)
                {
                    SetPlayerLocomotion(distance<1.8f ? Vector3.zero : Vector3.ClampMagnitude(target.root.position-player.root.position,1),false,false);
                    if(distance<2.2f && RequestPlayerAttack(false)) playerAttacks++;
                }
                else SetPlayerLocomotion(Vector3.zero,false,false);
                Simulate(1f/60);
                if(DefenseAlert && !observed)
                {
                    observed=true; villageDebug=true;
                    yield return null; ScreenCapture.CaptureScreenshot(Path.Combine(directory,"response.png")); yield return new WaitForSeconds(.3f);
                }
                if(f%6==0) yield return null;
            }
            SetPlayerLocomotion(Vector3.zero,false,false);
            for(int f=0;f<720;f++) Simulate(1f/60);
            failed |= !FoundingRaidCompleted || !observed || !player.fighter.Alive || playerAttacks==0;
            player.root.position=VillageCenter+Vector3.back*2;
            string file=Path.Combine(directory,"completed-raid.json"); failed |= !SaveVillage(file);
            if(!failed) { ResetFight(); failed |= !LoadVillage(file) || !FoundingRaidCompleted; }
            villagePanel=1; villageDebug=true;
            yield return null; ScreenCapture.CaptureScreenshot(Path.Combine(directory,"outcome.png")); yield return new WaitForSeconds(.3f);
            File.WriteAllText(Path.Combine(directory,"result.txt"),"Passed: "+!failed+"\nObserved response: "+observed+"\nPlayer attacks: "+playerAttacks+"\nSurvivors: "+LivingCompanions+"/8\n"+FoundingRaidStatus);
            Application.logMessageReceived -= observe;
            Debug.Log(failed ? "FOUNDING_RAID_SMOKE_FAILED" : "FOUNDING_RAID_SMOKE_PASSED"); Application.Quit(failed ? 1 : 0);
        }
    }
}
