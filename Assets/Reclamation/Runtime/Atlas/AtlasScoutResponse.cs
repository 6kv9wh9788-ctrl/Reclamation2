using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Atlas
{
    public enum ScoutEncounterPhase { Idle, Unseen, Reporting, Responding, Withdrawing, Secured, Escaped }

    // No damage or combat authority: this encounter exercises observation, command and deterrence.
    public sealed class AtlasScoutEncounter
    {
        public ScoutEncounterPhase Phase { get; private set; }
        public float Age { get; private set; }
        public bool PlayerAssisted { get; private set; }
        public bool Active => Phase >= ScoutEncounterPhase.Unseen && Phase <= ScoutEncounterPhase.Withdrawing;
        public bool Known { get; private set; }
        private float stageTime, supportedTime;
        private bool deterred;
        public bool Start()
        {
            if(Active)return false;
            Phase=ScoutEncounterPhase.Unseen;Age=stageTime=supportedTime=0;PlayerAssisted=Known=deterred=false;return true;
        }
        private void Enter(ScoutEncounterPhase phase){Phase=phase;stageTime=0;}
        public bool Challenge(float distance,int support)
        {
            if(Phase!=ScoutEncounterPhase.Responding||distance>14||support<3)return false;
            PlayerAssisted=deterred=true;Enter(ScoutEncounterPhase.Withdrawing);return true;
        }
        public void Tick(float seconds,bool observed,int support,bool withdrawn)
        {
            if(!Active||seconds<=0)return;
            Age+=seconds;stageTime+=seconds;
            switch(Phase)
            {
                case ScoutEncounterPhase.Unseen:
                    if(observed){Known=true;Enter(ScoutEncounterPhase.Reporting);}
                    else if(stageTime>=180)Enter(ScoutEncounterPhase.Withdrawing);
                    break;
                case ScoutEncounterPhase.Reporting:
                    if(stageTime>=3)Enter(ScoutEncounterPhase.Responding);
                    break;
                case ScoutEncounterPhase.Responding:
                    supportedTime=support>=3?supportedTime+seconds:0;
                    if(supportedTime>=60){deterred=true;Enter(ScoutEncounterPhase.Withdrawing);}
                    else if(stageTime>=300&&support<3)Enter(ScoutEncounterPhase.Withdrawing);
                    break;
                case ScoutEncounterPhase.Withdrawing:
                    if(withdrawn)Enter(deterred?ScoutEncounterPhase.Secured:ScoutEncounterPhase.Escaped);
                    break;
            }
        }
    }

    public sealed partial class WorldAtlasDemo
    {
        private AtlasScoutEncounter scoutEncounter=new AtlasScoutEncounter();
        private readonly List<AtlasCharacterVisual> hostileScouts=new List<AtlasCharacterVisual>();
        private int scoutSite=-1;
        private float scoutWatchHour;
        private bool reserveDispatched;
        private Vector2 scoutLocal,scoutExit;
        public ScoutEncounterPhase ScoutPhase=>scoutEncounter.Phase;
        public bool ScoutContactKnown=>scoutEncounter.Known;
        public bool ScoutPlayerAssisted=>scoutEncounter.PlayerAssisted;
        public float ScoutElapsed=>scoutEncounter.Age;
        public Vector3 ScoutContactPosition => scoutSite<0?Vector3.zero:HinterlandGround(Model.sites[scoutSite].point+scoutLocal);
        public int ScoutSupport { get; private set; }
        private void ResetScoutEncounter()
        {
            responseGuideComplete=false;scoutEncounter=new AtlasScoutEncounter();hostileScouts.Clear();scoutSite=-1;reserveDispatched=false;ScoutSupport=0;
        }
        public bool BeginScoutEncounter()
        {
            if(paused||scoutEncounter.Active||guardSite<0||Model.sites[guardSite].owner!=0)
            {message="Start the scout encounter near your inhabited town, while unpaused. Only one encounter can run.";return false;}
            scoutSite=guardSite;scoutWatchHour=clockHours;reserveDispatched=false;ScoutSupport=0;
            var layout=LayoutFor(scoutSite);scoutLocal=layout.CollectionGate(1)+layout.Hinterland(new Vector2(6,0));
            scoutExit=scoutLocal+layout.Hinterland(new Vector2(0,-22));
            // Validate the encounter lane instead of placing scouts on unsuitable future terrain.
            for(int i=0;i<=22;i++)
            {
                var p=Model.sites[scoutSite].point+Vector2.Lerp(scoutLocal,scoutExit,i/22f);
                if(!Model.Walkable(p)||IsSolidAt(HinterlandGround(p)))
                {message="This settlement has no clear scout encounter lane.";scoutSite=-1;return false;}
            }
            foreach(var scout in hostileScouts)if(scout)Destroy(scout.gameObject);hostileScouts.Clear();
            for(int i=0;i<3;i++)
            {
                var root=new GameObject("Hostile scout "+(i+1));root.transform.SetParent(world,false);
                var visual=root.AddComponent<AtlasCharacterVisual>();visual.Build(i+5,false,true);
                visual.Play("Idle");hostileScouts.Add(visual);
            }
            responseGuideComplete=false;companies[scoutSite].BeginContact();scoutEncounter.Start();PlaceHostileScouts();
            message="Scout encounter started. The farm patrol must observe contact before the commander receives a report.";return true;
        }
        private void PlaceHostileScouts()
        {
            if(scoutSite<0)return;
            var layout=LayoutFor(scoutSite);
            for(int i=0;i<hostileScouts.Count;i++)
            {
                var p=Model.sites[scoutSite].point+scoutLocal+layout.Hinterland(new Vector2(0,(i-1)*1.7f));
                var scout=hostileScouts[i];scout.transform.position=HinterlandGround(p);
                scout.transform.rotation=Quaternion.Euler(0,layout.HinterlandAngle+(ScoutPhase==ScoutEncounterPhase.Withdrawing?180:270),0);
                string clip=ScoutPhase==ScoutEncounterPhase.Withdrawing?"Walk":"Idle";
                if(scout.CurrentClip!=clip)scout.Play(clip);scout.PlaybackSpeed=paused?0:clockRate;
                scout.gameObject.SetActive(scoutEncounter.Active);
            }
        }
        public bool ScoutSight(Vector3 observer,Vector3 target)
        {
            if(Vector3.Distance(observer,target)>22)return false;
            var a=observer+Vector3.up*1.6f;var b=target+Vector3.up*1.4f;
            if(Physics.Linecast(a,b,ObstacleMask,QueryTriggerInteraction.Ignore))return false;
            int samples=Mathf.Max(2,Mathf.CeilToInt(Vector3.Distance(a,b)/2));
            for(int i=1;i<samples;i++){var p=Vector3.Lerp(a,b,i/(float)samples);if(Surface(new Vector2(p.x,p.z))>p.y)return false;}
            return true;
        }
        private void DispatchScoutReserve()
        {
            RetaskMarch(companies[scoutSite].RespondingPlatoon,LayoutFor(scoutSite),true);
            reserveDispatched=true;
        }
        private void TickScoutEncounter(float seconds)
        {
            if(!scoutEncounter.Active||seconds<=0)return;
            float dt=seconds*clockRate;
            bool observed=false;ScoutSupport=0;
            for(int i=1;i<guards.Count;i++)
            {
                var g=guards[i];
                if(g.duty==AtlasGuardDuty.FarmPatrol&&ScoutSight(g.root.position,ScoutContactPosition)){companies[scoutSite].ObserveContact(g.platoon,true);observed=companies[scoutSite].ReportingPlatoon>=0;}
                if(reserveDispatched&&g.duty==AtlasGuardDuty.Reserve&&Vector3.Distance(g.root.position,ScoutContactPosition)<=10)ScoutSupport++;
            }
            var previous=ScoutPhase;
            if(previous==ScoutEncounterPhase.Withdrawing)scoutLocal=Vector2.MoveTowards(scoutLocal,scoutExit,dt*2);
            scoutEncounter.Tick(dt,observed,ScoutSupport,Vector2.Distance(scoutLocal,scoutExit)<.05f);
            if(ScoutPhase==ScoutEncounterPhase.Responding&&!reserveDispatched&&companies[scoutSite].DelegateResponse())DispatchScoutReserve();
            companies[scoutSite].ReportSupport(ScoutSupport);
            if(previous!=ScoutPhase)
            {
                message=ScoutPhase==ScoutEncounterPhase.Reporting?companies[scoutSite].Platoons[companies[scoutSite].ReportingPlatoon].Name+": Three scouts sighted. Reporting to company.":
                    ScoutPhase==ScoutEncounterPhase.Responding?companies[scoutSite].Commander.Name+": "+companies[scoutSite].Platoons[companies[scoutSite].RespondingPlatoon].Name+", secure the farm approach. Fixed posts hold. G visits the reported contact.":
                    ScoutPhase==ScoutEncounterPhase.Withdrawing?"The scouts are withdrawing. Hold the approach; do not pursue.":
                    ScoutPhase==ScoutEncounterPhase.Secured?(ScoutPlayerAssisted?"Approach secured. Your challenge, backed by the reserve, drove the scouts away.":"Approach secured. The reserve deterred the scouts without your intervention."):
                    "Scouts slipped away. No confirmed defense success. Patrol watch resumes.";
                if(!scoutEncounter.Active)
                {
                    companies[scoutSite].CompleteContact(ScoutPhase==ScoutEncounterPhase.Secured);
                    if(reserveDispatched)RetaskMarch(companies[scoutSite].RespondingPlatoon,LayoutFor(scoutSite),false);
                    reserveDispatched=false;
                }
            }
            PlaceHostileScouts();
        }
        public bool ChallengeScouts()
        {
            if(paused||scoutSite<0)return false;
            if(!scoutEncounter.Challenge(Vector3.Distance(hero.position,ScoutContactPosition),ScoutSupport))
            {message="Challenge requires a reported contact within 14 m and at least three reserve soldiers supporting you.";return false;}
            message="You challenge the scouts with the reserve at your side. They withdraw.";return true;
        }
        public bool VisitScoutContact()
        {
            if(!scoutEncounter.Known||!scoutEncounter.Active||paused)return false;
            var layout=LayoutFor(scoutSite);var p=Model.sites[scoutSite].point+layout.CollectionGate(1)+layout.Hinterland(new Vector2(-4,0));
            hero.position=HinterlandGround(p);selected=scoutSite;yaw=layout.HinterlandAngle+90;pitch=22;zoom=15;mapOpen=false;UpdateCamera(true);return true;
        }
        private void DrawScoutPanel()
        {
            if(mapOpen)return;
            if(ScoutPhase==ScoutEncounterPhase.Idle)
            {GUI.Label(new Rect(28,92,365,25),"R: start optional farm-scout encounter",small);return;}
            if(ResponseGuidePlatoon>=0){DrawResponseCard();return;}
            Panel(ScoutHudRect);
            GUI.Label(new Rect(28,98,341,25),ScoutPhase==ScoutEncounterPhase.Unseen?"PATROL WATCH":ScoutPhase==ScoutEncounterPhase.Reporting?"CONTACT REPORT":ScoutPhase==ScoutEncounterPhase.Responding?(ScoutSupport>=3?"RESERVE IN POSITION":"RESERVE RESPONDING"):ScoutPhase==ScoutEncounterPhase.Withdrawing?"SCOUTS WITHDRAWING":ScoutPhase==ScoutEncounterPhase.Secured?"APPROACH SECURED":"CONTACT LOST",subheading);
            string detail=ScoutPhase==ScoutEncounterPhase.Unseen?"Awaiting a patrol sighting.\nTown posts remain covered.":ScoutPhase==ScoutEncounterPhase.Reporting?"Farm patrol sighted three hostile scouts.\nReport en route to the commander.":ScoutPhase==ScoutEncounterPhase.Responding?"Reserve support nearby: "+ScoutSupport+" / 6\nG: visit report location / E: challenge with support":ScoutPhase==ScoutEncounterPhase.Withdrawing?"Holding the approach. No pursuit.":ScoutPhase==ScoutEncounterPhase.Secured?(ScoutPlayerAssisted?"Player and reserve deterred the scouts.":"Commander-led reserve deterred the scouts.")+"\nRoutine watch resumes. R: replay":"Scouts escaped; no victory credited. R: replay";
            GUI.Label(new Rect(28,128,341,58),detail,small);
        }

        private IEnumerator ScoutSmoke()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--output");string output=index>=0?args[index+1]:Path.Combine(Application.persistentDataPath,"ScoutSmoke");Directory.CreateDirectory(output);
            bool failed=false;Application.LogCallback observe=(text,stack,type)=>{if(type==LogType.Error||type==LogType.Exception)failed=true;};Application.logMessageReceived+=observe;
            AutomaticClock=false;
            foreach(var config in new[]{(AtlasSize.Small,4),(AtlasSize.Medium,8),(AtlasSize.Medium,12)})
            {
                LoadMap(config.Item1,config.Item2);VisitCommander();SetHour(9);commanderPanel=false;debug=true;
                failed|=!BeginScoutEncounter();string label=config.Item1+"-"+config.Item2;
                for(int i=0;i<1500&&ScoutPhase!=ScoutEncounterPhase.Responding&&scoutEncounter.Active;i++)TickVillage(.2f);
                failed|=ScoutPhase!=ScoutEncounterPhase.Responding||!VisitScoutContact();
                UpdateCamera(true);yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-contact.png"));yield return new WaitForSeconds(.3f);
                for(int i=0;i<1800&&ScoutPhase==ScoutEncounterPhase.Responding&&ScoutSupport<3;i++)TickVillage(.2f);
                failed|=ScoutSupport<3;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-reserve.png"));yield return new WaitForSeconds(.3f);
                failed|=!ChallengeScouts();
                for(int i=0;i<100&&scoutEncounter.Active;i++)TickVillage(.2f);
                failed|=ScoutPhase!=ScoutEncounterPhase.Secured||!ScoutPlayerAssisted;
                yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-secured.png"));yield return new WaitForSeconds(.3f);
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),"Passed: "+!failed+"\nScout sighting, delayed report, reserve response, player challenge and withdrawal on small/4, medium/8, medium/12.\nDeterrence only; no combat, damage or rewards.");
            Application.logMessageReceived-=observe;Application.Quit(failed?1:0);
        }
    }
}


