using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reclamation.Atlas
{
    public sealed partial class WorldAtlasDemo
    {
        private bool mapOpen, debug, simulation, paused, showArmies=false;
        private int selected;
        private float yaw=25,pitch=18,zoom=16,strategyClock,frameMs;
        private string message;
        private Transform footprintRoot;
        private float UiScale=>Mathf.Max(.3f,Mathf.Min(Screen.width/1400f,Screen.height/900f));
        private float Width=>Screen.width/UiScale;
        private float Height=>Screen.height/UiScale;
        private IEnumerator Start()
        {
            var launchArgs=Environment.GetCommandLineArgs();
            bool practice=Array.IndexOf(launchArgs,"--atlas-duel")>=0||Array.IndexOf(launchArgs,"--atlas-duel-smoke")>=0;
            SyntyTrial=Array.IndexOf(launchArgs,"--atlas-synty")>=0;
            SyntyRoleTrial=Array.IndexOf(launchArgs,"--atlas-synty-roles")>=0;
            SyntyCompanyTrial=Array.IndexOf(launchArgs,"--atlas-synty-company")>=0;
            if(practice){SyntyCompanyTrial=true;ResumeDefenseCheckpoint=false;}
            if(SyntyCompanyTrial)SyntyRoleTrial=true;
            if(SyntyRoleTrial)SyntyTrial=true;
            defenseMode=Array.IndexOf(launchArgs,"--atlas-defense")>=0||Array.IndexOf(launchArgs,"--atlas-defense-smoke")>=0||Array.IndexOf(launchArgs,"--atlas-defense-resume-smoke")>=0;
            if(defenseMode){Application.runInBackground=true;}
            if(practice){defenseMode=true;Application.runInBackground=true;}
            if(Array.IndexOf(launchArgs,"--formation-smoke")>=0||Array.IndexOf(launchArgs,"--platoon-smoke")>=0||Array.IndexOf(launchArgs,"--wildlife-smoke")>=0||Array.IndexOf(launchArgs,"--visibility-smoke")>=0||Array.IndexOf(launchArgs,"--scout-smoke")>=0||Array.IndexOf(launchArgs,"--village-smoke")>=0||Array.IndexOf(launchArgs,"--atlas-smoke")>=0)Application.runInBackground=true;
            LoadMap(AtlasSize.Small,4);if(!defenseMode)VisitVillage();
            if(practice)BeginDuelPractice();
            if(Array.IndexOf(launchArgs,"--atlas-duel-smoke")>=0){yield return DuelPracticeSmoke();yield break;}
            if(Array.IndexOf(launchArgs,"--atlas-synty-capture")>=0){yield return SyntyCapture();yield break;}
            if(Array.IndexOf(launchArgs,"--atlas-synty-profile")>=0){yield return SyntyProfile();yield break;}
            if(Array.IndexOf(launchArgs,"--atlas-defense-smoke")>=0){yield return AtlasDefenseSmoke();yield break;}
            if(Array.IndexOf(launchArgs,"--atlas-defense-resume-smoke")>=0)
            {
                AutomaticBattle=false;bool valid=true;
                foreach(var size in new[]{AtlasSize.Small,AtlasSize.Medium}){LoadMap(size,size==AtlasSize.Small?4:8);valid&=defenseState.progression.completed==2&&defenseState.progression.playerPerk!=0&&defenseState.progression.commanderPerk!=0;}
                Debug.Log(valid?"ATLAS_DEFENSE_RESUME_PASSED":"ATLAS_DEFENSE_RESUME_FAILED");Application.Quit(valid?0:1);yield break;
            }
            if(Array.IndexOf(launchArgs,"--formation-smoke")>=0)yield return FormationSmoke();
            else if(Array.IndexOf(launchArgs,"--platoon-smoke")>=0)yield return PlatoonSmoke();
            else if(Array.IndexOf(launchArgs,"--wildlife-smoke")>=0)yield return WildlifeSmoke();
            else if(Array.IndexOf(launchArgs,"--visibility-smoke")>=0)yield return VisibilitySmoke();
            else if(Array.IndexOf(Environment.GetCommandLineArgs(),"--scout-smoke")>=0)yield return ScoutSmoke();
            else if(Array.IndexOf(Environment.GetCommandLineArgs(),"--village-smoke")>=0)yield return VillageSmoke();
            else if(Array.IndexOf(Environment.GetCommandLineArgs(),"--atlas-smoke")>=0)yield return Smoke();
        }
        private float Surface(Vector2 p)
        {
            float step=Model.Extent/160,fx=(p.x+Model.Extent/2)/step,fz=(p.y+Model.Extent/2)/step;
            int ix=Mathf.FloorToInt(fx),iz=Mathf.FloorToInt(fz);float tx=fx-ix,tz=fz-iz,x=ix*step-Model.Extent/2,z=iz*step-Model.Extent/2;
            float a=Model.Height(x,z),b=Model.Height(x+step,z),c=Model.Height(x,z+step),d=Model.Height(x+step,z+step);
            return tx+tz<=1?a+(b-a)*tx+(c-a)*tz:d+(c-d)*(1-tx)+(b-d)*(1-tz);
        }
        public void Travel(int id)
        {
            if(id<0||id>=Model.sites.Count)return;selected=id;zoom=16;var p=Model.sites[id].point+Vector2.up*50;
            hero.position=new Vector3(p.x,Surface(p),p.y);message="Survey travel: "+Model.sites[id].name+". This shortcut is for comparing map scale.";
            rig.Play("Idle");UpdateCamera(true);
        }
        private void Update()
        {
            if(Model==null)return;frameMs=Mathf.Lerp(frameMs,Time.unscaledDeltaTime*1000,.03f);
            if(defenseMode){UpdateAtlasDefense();return;}
            var k=Keyboard.current;var m=Mouse.current;
            if(k!=null)
            {
                if(k.escapeKey.wasPressedThisFrame)paused=!paused;
                if(k.mKey.wasPressedThisFrame)mapOpen=!mapOpen;
                if(k.f3Key.wasPressedThisFrame)debug=!debug;
                if(k.jKey.wasPressedThisFrame&&!mapOpen&&!paused)VisitWildlife();
                if(k.hKey.wasPressedThisFrame&&!paused)VisitWorkplace();
                if(!mapOpen&&k.rKey.wasPressedThisFrame)BeginScoutEncounter();
                if(!mapOpen&&k.gKey.wasPressedThisFrame)VisitScoutContact();
                if(!mapOpen&&k.eKey.wasPressedThisFrame)ChallengeScouts();
                if(k.cKey.wasPressedThisFrame){if(commanderPanel)commanderPanel=false;else VisitCommander();}
            }
            if(!paused)
            {
                TickVillage(Time.deltaTime);
                if(k!=null&&k.tKey.wasPressedThisFrame)SetHour(clockHours+3);
                if(simulation){strategyClock+=Time.deltaTime;if(strategyClock>=10){strategyClock=0;Model.AdvanceStrategy();}}
                if(!mapOpen && m!=null)zoom=Mathf.Clamp(zoom-m.scroll.ReadValue().y*.04f,8,110);
                Vector3 motion=Vector3.zero;
                if(!mapOpen && k!=null)
                {
                    var raw=new Vector3((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),0,(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
                    motion=Quaternion.Euler(0,yaw,0)*raw.normalized;
                    bool sprint=k.leftShiftKey.isPressed;float speed=sprint?6.2f:3.8f;
                    Vector3 desired=ResolveVillageMove(hero.position,motion*speed*Mathf.Min(Time.deltaTime,.05f));
                    var p=new Vector2(desired.x,desired.z);
                    if(Model.Walkable(p)){hero.position=new Vector3(p.x,Surface(p),p.y);if(motion.sqrMagnitude>.01f)hero.rotation=Quaternion.Slerp(hero.rotation,Quaternion.LookRotation(motion),Time.deltaTime*10);}
                    else motion=Vector3.zero;
                    rig.Play(motion.sqrMagnitude>.01f?sprint?"Run":"Walk":"Idle");
                    if(k.fKey.wasPressedThisFrame)ClaimNearby();
                    if(m!=null && m.rightButton.isPressed && m.position.ReadValue().y>70*UiScale)
                    {Vector2 delta=m.delta.ReadValue();yaw+=delta.x*.16f;pitch=Mathf.Clamp(pitch-delta.y*.12f,12,65);}
                }
                else rig.Play("Idle");
            }
            foreach(var guard in guards)guard.rig.PlaybackSpeed=paused?0:clockRate;
            foreach(var scout in hostileScouts)if(scout)scout.PlaybackSpeed=paused?0:clockRate;
            rig.PlaybackSpeed=paused?0:1;UpdateCamera(false);UpdateRecon(Time.unscaledDeltaTime);DrawInstances();
        }
        private void UpdateCamera(bool snap)
        {
            if(!view||!hero)return;var rotation=Quaternion.Euler(pitch,yaw,0);Vector3 target=hero.position+Vector3.up*1.5f;
            Vector3 next=target+rotation*new Vector3(0,0,-zoom);
            next.y=Mathf.Max(next.y,Model.Height(next.x,next.z)+1.5f);
            Vector3 clear=ResolveVillageCamera(target,next);
            Vector3 eased=snap||Vector3.Distance(target,clear)<Vector3.Distance(target,view.transform.position)?clear:Vector3.Lerp(view.transform.position,clear,Time.unscaledDeltaTime*9);
            view.transform.position=ResolveVillageCamera(target,eased);view.transform.LookAt(target);
        }
        public bool ClaimNearby()
        {
            var nearest=Model.Nearest(new Vector2(hero.position.x,hero.position.z));
            if(Vector2.Distance(nearest.point,new Vector2(hero.position.x,hero.position.z))>65){message="Approach a settlement clearing to establish a camp.";return false;}
            if(!Model.Claim(nearest.id,0)){message="This site is already occupied.";return false;}
            selected=nearest.id;message="Your camp is established at "+nearest.name+". No economy costs in this survey prototype.";PreviewFootprint(0);return true;
        }
        private void PreviewFootprint(int kind)
        {
            if(footprintRoot){footprintRoot.gameObject.SetActive(false);Destroy(footprintRoot.gameObject);}
            footprintRoot=new GameObject("Settlement footprint preview").transform;footprintRoot.SetParent(world,false);
            var site=Model.sites[selected];site.footprint=kind;hero.position=Ground(site.point+Vector2.right*8);zoom=kind==1?100:kind==2?85:55;pitch=42;UpdateCamera(true);float radius=kind==1?58:kind==2?43:25;var material=banners[3];
            int count=kind==1?24:kind==2?8:4;
            for(int i=0;i<count;i++)
            {
                float angle=i*Mathf.PI*2/count;var layout=LayoutFor(selected);var random=new System.Random(layout.Seed+kind*3571+i*97);
                Vector2 offset=kind==2?new Vector2(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius):kind==1?new Vector2((i%4-1.5f)*28,(i/4-2.5f)*18):new Vector2(i%2==0?-30:30,i<2?-18:18);
                offset+=new Vector2((float)random.NextDouble()*4-2,(float)random.NextDouble()*4-2);Vector2 p=site.point+layout.Rotate(offset);
                bool blocked=false;foreach(var deposit in site.deposits)if(Vector2.Distance(p,deposit.point)<14)blocked=true;if(blocked||BlocksResidentRoute(selected,p))continue;
                Vector3 size=kind==2?new Vector3(7,9.5f,7):new Vector3(8.5f,8.5f,8.5f);
                if(Physics.CheckBox(new Vector3(p.x,Surface(p)+4,p.y),size*.5f,Quaternion.identity,ObstacleMask))continue;
                var building=art.Object(kind==2?"Tower":i%5==0?"Storehouse":"Cottage"+(i%3),footprintRoot,new Vector3(p.x,Surface(p),p.y));building.rotation=Quaternion.Euler(0,layout.Angle+(offset.x<0?-90:90),0);
                Obstacle("Preview building",building.position+Vector3.up*size.y*.5f,size,building.rotation,footprintRoot);
            }
            if(kind==2)BuildFortCore(site);
            Physics.SyncTransforms();
            message=(kind==0?"Village":kind==1?"City":"Fortress")+" footprint preview: "+(radius*2).ToString("0")+" m across. Layout only; no upgrade costs or production simulated.";
        }
        private IEnumerator Smoke()
        {
            string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--output");string output=index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Application.persistentDataPath,"AtlasSmoke");Directory.CreateDirectory(output);
            bool failed=false;Application.LogCallback observe=(message,stack,type)=>{if(type==LogType.Exception||type==LogType.Error)failed=true;};Application.logMessageReceived+=observe;
            foreach(var size in new[]{AtlasSize.Small,AtlasSize.Medium})
            {
                LoadMap(size,size==AtlasSize.Small?4:12);yield return null;yield return new WaitForSeconds(.3f);
                mapOpen=true;ScreenCapture.CaptureScreenshot(Path.Combine(output,size+"-atlas.png"));yield return new WaitForSeconds(.3f);
                mapOpen=false;debug=true;
                VisitVillage();yield return null;yield return new WaitForSeconds(.3f);
                failed|=ActiveVillagerCount!=18||SceneryInstanceCount<100;
                ScreenCapture.CaptureScreenshot(Path.Combine(output,size+"-village.png"));yield return new WaitForSeconds(.3f);
                var field=Model.sites[selected].deposits[0].point;hero.position=new Vector3(field.x,Surface(field),field.y-9);yaw=0;pitch=25;zoom=16;UpdateCamera(true);
                yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,size+"-resources.png"));yield return new WaitForSeconds(.3f);
                Travel(Model.factions[0].capital);

                if(size==AtlasSize.Medium){int high=0;for(int i=0;i<Model.sites.Count;i++)if(Model.sites[i].elevation>Model.sites[high].elevation)high=i;Travel(high);yaw=90;pitch=18;UpdateCamera(true);}
                yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,size+"-ground.png"));yield return new WaitForSeconds(.3f);
                PreviewFootprint(1);yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,size+"-footprint.png"));yield return new WaitForSeconds(.3f);
                Travel(Model.sites.FindIndex(s=>s.owner==-1));PreviewFootprint(2);yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,size+"-fortress.png"));yield return new WaitForSeconds(.3f);
                int available=Model.sites.FindIndex(s=>s.owner==-1);Travel(available);failed|=!ClaimNearby();
                for(int turn=0;turn<4;turn++)Model.AdvanceStrategy();
                failed|=Model.factions.Count!=(size==AtlasSize.Small?4:12)||Model.sites.Count<6*Model.factions.Count||!Model.Walkable(new Vector2(hero.position.x,hero.position.z));
                mapOpen=true;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,size+"-strategy.png"));yield return new WaitForSeconds(.3f);
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),"Passed: "+!failed+"\nSmall: 1600m, 4 slots, 36 sites\nMedium: 3200m, 12 slots, 80 sites\nClaim, travel, footprint, strategy and both map switches exercised.\nStatic army scale figures; no per-unit combat or networking.");
            Application.logMessageReceived-=observe;Debug.Log(failed?"ATLAS_SMOKE_FAILED":"ATLAS_SMOKE_PASSED");Application.Quit(failed?1:0);
        }
    }
}



