using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using Stopwatch=System.Diagnostics.Stopwatch;

namespace Reclamation.Blight
{
    public sealed partial class BattleCrowdPerformanceLab : MonoBehaviour
    {
        public CrowdAnimationLibrary library;
        public Camera View {get;private set;}
        public CrowdBattleSimulation Simulation {get;private set;}
        public bool Automatic=true;
        private bool activeCombat, measuring, busy, forceNear;
        private float clock, accumulator;
        private int oldVsync,oldCap, count=64, nearCount,farCount,drawCalls;
        private string status="Choose a count and workload. Results export automatically.";
        private string output;
        private long lastStamp;
        private double elapsed,warmup=5,duration=20,dropped,warmupDropped;
        private readonly List<Sample> samples=new List<Sample>(20000);
        private readonly Matrix4x4[][] matrices=new Matrix4x4[192][];
        private readonly int[] sizes=new int[192];
        private Material floorMaterial;
        private GameObject flatFloor;
        public CrowdBattlefieldEnvironment EnvironmentLayer {get;private set;}
        public bool TerrainLayer {get;private set;}
        public bool ShadowLayer {get;private set;}
        public bool EquipmentLayer {get;private set;}
        private bool oldBackground;
        public bool SquadMode {get;private set;}
        public bool NavigationMode {get;private set;}
        public bool PerceptionMode {get;private set;}
        public bool CommunicationMode {get;private set;}
        public void OpenAlertDemo()
        {
            CommunicationMode=PerceptionMode=true;OpenNavigation(CrowdNavigation.Layout.Wall);observer=760;
            Simulation.Units[760].position=Simulation.Units[760].home=new Vector3(13.8f,0,-4.5f);
            Simulation.Units[780].position=Simulation.Units[780].home=new Vector3(16,0,-3.9f);
            Simulation.Units[1].position=Simulation.Units[1].home=new Vector3(16,0,4.5f);
            status="Scout sees around the wall; observer receives a location while holding behind cover.";
        }
        private bool noiseClick;
        private int observer=760;
        private LineRenderer visionLine,memoryLine;
        private Material senseMaterial;
        private void DrawSenses()
        {
            if(!visionLine)
            {
                senseMaterial=new Material(library.friendly);senseMaterial.color=Color.yellow;
                LineRenderer Line(string name){var go=new GameObject(name);go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=senseMaterial;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;line.startWidth=line.endWidth=.1f;return line;}
                visionLine=Line("Observer vision cone");memoryLine=Line("Observer remembered location");
            }
            visionLine.enabled=memoryLine.enabled=PerceptionMode && Simulation.Units[observer].fighter.Alive && !busy && !measuring;
            if(!visionLine.enabled)return;
            var u=Simulation.Units[observer];Vector3 p=u.position+Vector3.up*.3f;
            visionLine.positionCount=15;visionLine.SetPosition(0,p);for(int i=0;i<=12;i++)visionLine.SetPosition(i+1,p+Quaternion.Euler(0,-60+i*10,0)*u.forward*12);visionLine.SetPosition(14,p);
            memoryLine.enabled=Simulation.Perception.TryKnownPosition(observer,out Vector3 known);
            if(memoryLine.enabled){memoryLine.positionCount=2;memoryLine.SetPositions(new[]{p,known+Vector3.up*.3f});}
        }
        private CrowdNavigation.Layout navigationLayout;
        private GameObject navigationVisual;
        private readonly List<Material> navigationMaterials=new List<Material>();
        public void OpenNavigation(CrowdNavigation.Layout layout)
        { NavigationMode=true;navigationLayout=layout;OpenSquads(); }
        private void NavigationVisuals()
        {
            if(navigationVisual)Destroy(navigationVisual);
            foreach(var material in navigationMaterials)Destroy(material);navigationMaterials.Clear();
            if(!NavigationMode)return;
            navigationVisual=new GameObject("Navigation obstacles");navigationVisual.transform.SetParent(transform,false);
            void Box(string name,Vector3 position,Vector3 size,Color color)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(navigationVisual.transform,false);
                go.transform.position=position;go.transform.localScale=size;go.GetComponent<Collider>().enabled=false;
                var mat=new Material(library.friendly);mat.color=color;go.GetComponent<Renderer>().sharedMaterial=mat;navigationMaterials.Add(mat);
            }
            foreach(var rect in Simulation.Navigation.Obstacles)
                Box(navigationLayout==CrowdNavigation.Layout.Bridge?"Impassable river":"Wall",new Vector3(rect.center.x,navigationLayout==CrowdNavigation.Layout.Bridge?.08f:1,rect.center.y),new Vector3(rect.width,navigationLayout==CrowdNavigation.Layout.Bridge?.12f:2,rect.height),navigationLayout==CrowdNavigation.Layout.Bridge?new Color(.05f,.35f,.65f):Color.gray);
            if(navigationLayout==CrowdNavigation.Layout.Bridge)Box("Bridge",new Vector3(0,-.07f,0),new Vector3(6,.14f,6.8f),new Color(.45f,.3f,.13f));
        }
        private int selectedSquad;
        private CrowdSquadCommands.Command pendingCommand=CrowdSquadCommands.Command.Move;
        public void OpenSquads() { SquadMode=true;Reset(1280,true);status="Select squad 1-8 or All; choose Move/Attack, then right-click ground. Hold stops immediately."; }
        private int trial;
        public string Profile => TerrainLayer && ShadowLayer && EquipmentLayer?"combined":!TerrainLayer && !ShadowLayer && !EquipmentLayer?"baseline":TerrainLayer && !ShadowLayer && !EquipmentLayer?"terrain":!TerrainLayer && ShadowLayer && !EquipmentLayer?"shadows":!TerrainLayer && !ShadowLayer && EquipmentLayer?"equipment":"custom";
        public void SetLayers(bool terrain,bool shadows,bool equipment)
        {if(NavigationMode)terrain=false;TerrainLayer=terrain;ShadowLayer=shadows;EquipmentLayer=equipment;flatFloor.SetActive(!terrain);EnvironmentLayer.Set(terrain,shadows);}

        private readonly int[] counts={64,256,640,1280};
        [Serializable] public struct Sample {public double frameMs,simulationMs,submissionMs;public int near,far,calls,steps;}
        [Serializable] public sealed class Result
        {
            public string profile;
            public int trial,terrainVertices,propInstances,equipmentNearVertices,equipmentFarVertices,shadowMapSize,shadowCascades;
            public float shadowDistance;
            public bool terrain,equipment,squadCommands,navigation;
            public string navigationScenario;
            public bool perception,communication;
            public long alertBroadcasts,alertsDelivered,alertCandidateChecks;
            public long sightChecks,hearingChecks,sightAcquisitions,heardEvents;
            public int navigationFields;
            public double navigationPlanningMs;
            public string mode,cpu,gpu,unity,graphicsApi,quality,utc,limitations;
            public int count,friendly,enemy,width,height,frames,nearVertices,farVertices;
            public bool editor,development,allNear,shadows,focusedAtEnd;
            public double warmupDroppedSimulationSeconds;
            public double seconds,meanMs,p95Ms,p99Ms,averageFps,onePercentLowFps,simulationMeanMs,submissionMeanMs,droppedSimulationSeconds;
            public long unityAllocatedBytes,managedBytes,hits,attacks,respawns,simulationSteps,candidateChecks;
            public float meanNear,meanFar,meanDrawCalls;
        }
        private void Awake()
        {
            oldBackground=Application.runInBackground;oldVsync=QualitySettings.vSyncCount;oldCap=Application.targetFrameRate;
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;Application.runInBackground=true;
            if(!library)library=Resources.Load<CrowdAnimationLibrary>("CrowdPerformance/AnimationLibrary");
            if(!library || !SystemInfo.supportsInstancing)throw new InvalidOperationException("Crowd library and GPU instancing are required.");
            for(int i=0;i<matrices.Length;i++)matrices[i]=new Matrix4x4[1023];
            output=Argument("--output",Path.Combine(Application.persistentDataPath,"CrowdPerformance",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")));
            warmup=Number("--warmup",5);duration=Number("--seconds",20);
            var cam=new GameObject("Crowd benchmark camera");cam.transform.SetParent(transform,false);View=cam.AddComponent<Camera>();
            View.transform.position=new Vector3(0,42,-52);View.transform.LookAt(new Vector3(0,0,2));View.fieldOfView=60;View.farClipPlane=200;
            View.clearFlags=CameraClearFlags.SolidColor;View.backgroundColor=new Color(.045f,.065f,.09f);View.allowDynamicResolution=false;
            var sun=new GameObject("Crowd key light").AddComponent<Light>();sun.transform.SetParent(transform,false);sun.type=LightType.Directional;sun.intensity=1.3f;sun.transform.rotation=Quaternion.Euler(40,-25,0);sun.shadows=LightShadows.None;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);flatFloor=floor;floor.transform.SetParent(transform,false);floor.transform.localPosition=Vector3.down*.15f;floor.transform.localScale=new Vector3(100,.3f,100);floor.GetComponent<Collider>().enabled=false;
            floorMaterial=new Material(library.friendly);floorMaterial.color=new Color(.12f,.15f,.17f);floor.GetComponent<Renderer>().sharedMaterial=floorMaterial;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.gray;
            EnvironmentLayer=gameObject.AddComponent<CrowdBattlefieldEnvironment>();EnvironmentLayer.Build(sun);SetLayers(true,true,true);
            lastStamp=Stopwatch.GetTimestamp();Reset(64,false);
        }
        private IEnumerator Start()
        {
            if(Has("--objective") || Has("--objective-preview")){OpenObjective();if(Has("--objective-preview"))yield return ObjectivePreview();yield break;}
            if(Has("--communication-suite"))
            {
                busy=true;SquadMode=true;NavigationMode=true;PerceptionMode=true;navigationLayout=CrowdNavigation.Layout.Bridge;SetLayers(false,true,true);
                for(trial=1;trial<=2;trial++)foreach(bool enabled in trial==1?new[]{false,true}:new[]{true,false})
                {
                    CommunicationMode=enabled;Begin(1280,true);
                    for(int s=0;s<8;s++)Simulation.Squads.Issue(s,CrowdSquadCommands.Command.Attack,new Vector3((s%4-1.5f)*11,0,18+s/4*8));
                    while(measuring)yield return null;yield return null;
                }
                Debug.Log("COMMUNICATION_SUITE_PASSED: "+output);Application.Quit(0);yield break;
            }
            if(Has("--communication-preview"))
            {
                OpenAlertDemo();yield return new WaitForSeconds(.2f);Directory.CreateDirectory(output);ScreenCapture.CaptureScreenshot(Path.Combine(output,"communication-preview.png"));
                yield return new WaitForSeconds(.3f);Debug.Log("COMMUNICATION_PREVIEW_PASSED");Application.Quit(0);yield break;
            }
            if(Has("--communication")){CommunicationMode=PerceptionMode=true;OpenNavigation(CrowdNavigation.Layout.Wall);yield break;}
            if(Has("--perception-suite"))
            {
                busy=true;SquadMode=true;NavigationMode=true;navigationLayout=CrowdNavigation.Layout.Bridge;SetLayers(false,true,true);
                for(trial=1;trial<=2;trial++)foreach(bool enabled in trial==1?new[]{false,true}:new[]{true,false})
                {
                    PerceptionMode=enabled;Begin(1280,true);
                    // Equal facing in both workloads; only awareness gating changes.
                    foreach(var unit in Simulation.Units)unit.forward=unit.team==0?Vector3.forward:Vector3.back;
                    for(int s=0;s<8;s++)Simulation.Squads.Issue(s,CrowdSquadCommands.Command.Attack,new Vector3((s%4-1.5f)*11,0,18+s/4*8));
                    while(measuring)yield return null;yield return null;
                }
                Debug.Log("PERCEPTION_SUITE_PASSED: "+output);Application.Quit(0);yield break;
            }
            if(Has("--perception-preview"))
            {
                PerceptionMode=true;OpenNavigation(CrowdNavigation.Layout.Wall);
                Simulation.Perception.Emit(-1,Simulation.Units[observer].position+Vector3.back*4);
                yield return new WaitForSeconds(.2f);Directory.CreateDirectory(output);ScreenCapture.CaptureScreenshot(Path.Combine(output,"perception-preview.png"));
                yield return new WaitForSeconds(.3f);Debug.Log("PERCEPTION_PREVIEW_PASSED");Application.Quit(0);yield break;
            }
            if(Has("--perception")){PerceptionMode=true;OpenNavigation(CrowdNavigation.Layout.Wall);yield break;}
            if(Has("--navigation-suite"))
            {
                busy=true;SquadMode=true;SetLayers(false,true,true);
                for(trial=1;trial<=2;trial++) foreach(bool enabled in trial==1?new[]{false,true}:new[]{true,false})
                {
                    NavigationMode=enabled;navigationLayout=CrowdNavigation.Layout.Bridge;Begin(1280,true);
                    for(int s=0;s<8;s++)Simulation.Squads.Issue(s,CrowdSquadCommands.Command.Attack,new Vector3((s%4-1.5f)*11,0,18+s/4*8));
                    while(measuring)yield return null;yield return null;
                }
                Debug.Log("NAVIGATION_SUITE_PASSED: "+output);Application.Quit(0);yield break;
            }
            if(Has("--navigation")){OpenNavigation(CrowdNavigation.Layout.Wall);yield break;}
            if(Has("--squad-suite"))
            {
                busy=true;SetLayers(true,true,true);
                for(trial=1;trial<=2;trial++) foreach(bool enabled in trial==1?new[]{false,true}:new[]{true,false})
                {
                    SquadMode=enabled;Begin(1280,true);
                    if(enabled)for(int s=0;s<Simulation.Squads.Squads.Length;s++)Simulation.Squads.Issue(s,CrowdSquadCommands.Command.Attack,new Vector3((s%4-1.5f)*11,0,12+s/4*9));
                    while(measuring)yield return null;
                    yield return null;
                }
                Debug.Log("SQUAD_SUITE_PASSED: "+output);Application.Quit(0);yield break;
            }
            if(Has("--squads")){OpenSquads();yield break;}
            if(Has("--layer-suite"))
            {
                busy=true;int repeats=Mathf.Clamp((int)Number("--repeats",2),1,5);
                int[] profiles={0,1,2,4,7};
                for(trial=1;trial<=repeats;trial++)for(int slot=0;slot<profiles.Length;slot++)
                {
                    int flags=profiles[trial%2==1?slot:profiles.Length-1-slot];
                    SetLayers((flags&1)!=0,(flags&2)!=0,(flags&4)!=0);Begin(1280,true);
                    while(measuring)yield return null;
                    yield return null;
                }
                Debug.Log("BATTLEFIELD_SUITE_PASSED: "+output);Application.Quit(0);yield break;
            }
            if(!Has("--crowd-suite"))yield break;
            SetLayers(false,false,false);
            busy=true;
            int requested=(int)Number("--count",0);
            if(requested!=0 && Array.IndexOf(counts,requested)<0)throw new ArgumentOutOfRangeException("--count");
            foreach(int n in counts)
            {
                if(requested>0 && n!=requested)continue;
                foreach(bool combat in new[]{false,true})
                {
                    Begin(n,combat);
                    while(measuring)yield return null;
                    yield return null;
                }
            }
            Debug.Log("CROWD_SUITE_PASSED: "+output);Application.Quit(0);
        }
        public void Reset(int n,bool combat)
        {
            Objective=null;if(objectiveVisual)Destroy(objectiveVisual);
            Simulation=new CrowdBattleSimulation(n);if(SquadMode)Simulation.EnableSquads();if(NavigationMode)Simulation.EnableNavigation(navigationLayout);if(PerceptionMode)Simulation.EnablePerception();if(CommunicationMode)Simulation.EnableCommunication();NavigationVisuals();if(NavigationMode)SetLayers(false,ShadowLayer,EquipmentLayer);count=n;activeCombat=combat;clock=0;accumulator=0;dropped=0;warmupDropped=0;
        }
        public void Begin(int n,bool combat)
        {
            Reset(n,combat);samples.Clear();elapsed=-warmup;measuring=true;lastStamp=Stopwatch.GetTimestamp();
            status="Warming up "+n+" units / "+(combat?"active combat stress":"render + animation only");
        }
        private static double Milliseconds(long start)=>(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;
        private void Update()
        {
            if(!Automatic)return;
            long now=Stopwatch.GetTimestamp();double frame=(now-lastStamp)/(double)Stopwatch.Frequency;lastStamp=now;
            ReadSquadInput();DrawSenses();
            if(Objective==null || Objective.Outcome==CrowdObjectiveScenario.State.Running)clock+=(float)frame;long simStart=Stopwatch.GetTimestamp();int steps=0;
            if(activeCombat)
            {
                accumulator+=(float)frame;
                while(accumulator>=1f/60f && steps<8){if(Objective==null || Objective.Outcome==CrowdObjectiveScenario.State.Running){Simulation.Step(1f/60f);if(Objective!=null)Objective.Tick(1f/60f);}accumulator-=1f/60f;steps++;}
                if(accumulator>=1f/60f){dropped+=accumulator;if(measuring && elapsed<0)warmupDropped+=accumulator;accumulator=0;}
            }
            double simMs=Milliseconds(simStart);long renderStart=Stopwatch.GetTimestamp();Submit();double renderMs=Milliseconds(renderStart);
            if(!measuring)return;
            bool wasWarming=elapsed<0;elapsed+=frame;
            if(wasWarming)return;
            samples.Add(new Sample{frameMs=frame*1000,simulationMs=simMs,submissionMs=renderMs,near=nearCount,far=farCount,calls=drawCalls,steps=steps});
            if(elapsed>=duration)Finish();
        }
        public void Submit()
        {
            Array.Clear(sizes,0,sizes.Length);nearCount=farCount=drawCalls=0;
            for(int i=0;i<Simulation.Units.Length;i++)
            {
                var unit=Simulation.Units[i];if(!unit.fighter.Alive && !Simulation.RespawnEnabled)continue;int lod=forceNear || (unit.position-View.transform.position).sqrMagnitude<65*65?0:1;
                int motion=0;float phase=Mathf.Repeat(clock*1.4f+i*.618034f,1);
                if(activeCombat)
                {
                    if(unit.fighter.Action==DuelAction.Windup){motion=1;phase=unit.fighter.Progress*.45f;}
                    else if(unit.fighter.Action==DuelAction.Recovery){motion=1;phase=.45f+unit.fighter.Progress*.55f;}
                    else if(!unit.moving)motion=2;
                }
                // Baked poses are quantized to 16 near / 8 far phases.
                int frame=motion==2?0:Mathf.Min(15,(int)(phase*16));if(lod==1)frame=(frame/2)*2;
                int key=(((unit.team*2+lod)*3+motion)*16)+frame;
                if(sizes[key]==1023){Draw(key);sizes[key]=0;}
                matrices[key][sizes[key]++]=Matrix4x4.TRS(unit.position+Vector3.up*(TerrainLayer?CrowdBattlefieldAssets.Height(unit.position.x,unit.position.z):0),Quaternion.LookRotation(activeCombat?unit.forward:Vector3.back),Vector3.one);
                if(lod==0)nearCount++;else farCount++;
            }
            for(int key=0;key<sizes.Length;key++)if(sizes[key]>0)Draw(key);
            drawCalls+=EnvironmentLayer.Submit(View);
        }
        private void Draw(int key)
        {
            int frame=key%16,motion=(key/16)%3,lod=(key/48)%2,team=key/96;
            Graphics.DrawMeshInstanced(library.Get(lod,motion,frame),0,team==0?library.friendly:library.enemy,matrices[key],sizes[key],null,ShadowLayer?ShadowCastingMode.On:ShadowCastingMode.Off,ShadowLayer,gameObject.layer,View,LightProbeUsage.Off);
            drawCalls++;
            if(EquipmentLayer)
            {
                Graphics.DrawMeshInstanced(EnvironmentLayer.Assets.Gear(team,lod,motion,frame),0,EnvironmentLayer.Assets.metal,matrices[key],sizes[key],null,ShadowLayer?ShadowCastingMode.On:ShadowCastingMode.Off,ShadowLayer,gameObject.layer,View,LightProbeUsage.Off);
                drawCalls++;
            }
        }
        public static double Percentile(double[] sorted,double fraction)=>sorted[Mathf.Clamp((int)Math.Ceiling(sorted.Length*fraction)-1,0,sorted.Length-1)];
        private void Finish()
        {
            measuring=false;if(samples.Count==0)throw new InvalidOperationException("No benchmark frames collected.");
            var times=new double[samples.Count];double sum=0,sim=0,submit=0,near=0,far=0,calls=0;
            for(int i=0;i<samples.Count;i++){var s=samples[i];times[i]=s.frameMs;sum+=s.frameMs;sim+=s.simulationMs;submit+=s.submissionMs;near+=s.near;far+=s.far;calls+=s.calls;}
            Array.Sort(times);int slow=Math.Max(1,(int)Math.Ceiling(times.Length*.01));double slowSum=0;for(int i=times.Length-slow;i<times.Length;i++)slowSum+=times[i];
            var result=new Result{communication=CommunicationMode,alertBroadcasts=Simulation.Communication==null?0:Simulation.Communication.Broadcasts,alertsDelivered=Simulation.Communication==null?0:Simulation.Communication.Delivered,alertCandidateChecks=Simulation.Communication==null?0:Simulation.Communication.CandidateChecks,perception=PerceptionMode,sightChecks=Simulation.Perception==null?0:Simulation.Perception.SightChecks,hearingChecks=Simulation.Perception==null?0:Simulation.Perception.HearingChecks,sightAcquisitions=Simulation.Perception==null?0:Simulation.Perception.SightAcquisitions,heardEvents=Simulation.Perception==null?0:Simulation.Perception.HeardEvents,navigation=NavigationMode,navigationFields=Simulation.Navigation==null?0:Simulation.Navigation.FieldsBuilt,navigationPlanningMs=Simulation.Navigation==null?0:Simulation.Navigation.PlanningMilliseconds,navigationScenario=navigationLayout.ToString(),squadCommands=SquadMode,profile=Profile,trial=trial,terrain=TerrainLayer,equipment=EquipmentLayer,terrainVertices=TerrainLayer?EnvironmentLayer.Assets.terrain.vertexCount:0,propInstances=EnvironmentLayer.PropInstances,equipmentNearVertices=EquipmentLayer?EnvironmentLayer.Assets.Gear(0,0,0,0).vertexCount:0,equipmentFarVertices=EquipmentLayer?EnvironmentLayer.Assets.Gear(0,1,0,0).vertexCount:0,shadowDistance=ShadowLayer?110:0,shadowMapSize=ShadowLayer?2048:0,shadowCascades=ShadowLayer?2:0,mode=activeCombat?"active-combat-stress":"render-animation",cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,unity=Application.unityVersion,graphicsApi=SystemInfo.graphicsDeviceType.ToString(),quality=QualitySettings.names[QualitySettings.GetQualityLevel()],utc=DateTime.UtcNow.ToString("o"),count=count,friendly=Simulation.FriendlyCount,enemy=count-Simulation.FriendlyCount,width=Screen.width,height=Screen.height,frames=times.Length,nearVertices=library.nearRun[0].vertexCount,farVertices=library.farRun[0].vertexCount,focusedAtEnd=Application.isFocused,editor=Application.isEditor,development=Debug.isDebugBuild,allNear=forceNear,shadows=ShadowLayer,seconds=sum/1000,meanMs=sum/times.Length,p95Ms=Percentile(times,.95),p99Ms=Percentile(times,.99),averageFps=1000*times.Length/sum,onePercentLowFps=1000*slow/slowSum,simulationMeanMs=sim/times.Length,submissionMeanMs=submit/times.Length,droppedSimulationSeconds=Math.Max(0,dropped-warmupDropped),warmupDroppedSimulationSeconds=warmupDropped,unityAllocatedBytes=Profiler.GetTotalAllocatedMemoryLong(),managedBytes=GC.GetTotalMemory(false),hits=Simulation.Hits,attacks=Simulation.Attacks,respawns=Simulation.Respawns,simulationSteps=Simulation.Steps,candidateChecks=Simulation.CandidateChecks,meanNear=(float)(near/times.Length),meanFar=(float)(far/times.Length),meanDrawCalls=(float)(calls/times.Length),limitations="Frame intervals, not GPU timings. CPU submission excludes GPU execution. Uncapped/VSync off; fixed camera; visual layers recorded per profile; no production terrain routing or Company AI, equipment gameplay changes, VFX, ragdolls or full battle UI. Terrain height adjusts visual roots only. Draw counts are instanced submissions, not GPU pass counts. Combat is an isolated DuelFighter stress harness; respawns sustain workload. Counters include warmup. Warmup stalls are reported separately. Nonzero measured dropped simulation time invalidates real-time simulation throughput claims."};
            Directory.CreateDirectory(output);string stem=Path.Combine(output,Profile+(CommunicationMode?"-alerts":"")+(PerceptionMode?"-perception":"")+(NavigationMode?"-navigation":"")+(SquadMode?"-squads":"")+"-trial"+trial+"-"+count+"-"+result.mode+"-"+Screen.width+"x"+Screen.height+"-"+DateTime.UtcNow.ToString("HHmmssfff"));
            File.WriteAllText(stem+".json",JsonUtility.ToJson(result,true));var csv=new StringBuilder("frame,frame_ms,simulation_ms,submission_ms,near,far,draw_calls,sim_steps\n");
            for(int i=0;i<samples.Count;i++){Sample s=samples[i];csv.Append(i).Append(',').Append(s.frameMs.ToString("F4",CultureInfo.InvariantCulture)).Append(',').Append(s.simulationMs.ToString("F4",CultureInfo.InvariantCulture)).Append(',').Append(s.submissionMs.ToString("F4",CultureInfo.InvariantCulture)).Append(',').Append(s.near).Append(',').Append(s.far).Append(',').Append(s.calls).Append(',').Append(s.steps).Append('\n');}
            File.WriteAllText(stem+".csv",csv.ToString());status=count+" | "+result.averageFps.ToString("F0")+" avg FPS | p95 "+result.p95Ms.ToString("F1")+" ms | 1% low "+result.onePercentLowFps.ToString("F0")+" FPS";
            Debug.Log("CROWD_RESULT: "+stem+".json | "+status);
            if(count==1280 && Has("--captures")) ScreenCapture.CaptureScreenshot(stem+".png");
        }
        private void OnGUI()
        {
            if(Objective!=null){ObjectiveGUI();return;}
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);Matrix4x4 old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            GUI.Box(new Rect(10,10,680,232),"BATTLE CROWD PERFORMANCE LAB");GUI.enabled=!busy && !measuring && !SquadMode;
            for(int i=0;i<counts.Length;i++)if(GUI.Button(new Rect(24+i*112,40,104,27),counts[i]+" units"))count=counts[i];
            if(GUI.Button(new Rect(24,76,196,28),"Render + animation"))Begin(count,false);
            if(GUI.Button(new Rect(230,76,196,28),"Active combat stress"))Begin(count,true);
            forceNear=GUI.Toggle(new Rect(478,44,192,24),forceNear,"Force near detail");
            if(GUI.Button(new Rect(452,76,100,28),"1080p"))Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            if(GUI.Button(new Rect(562,76,100,28),"1440p"))Screen.SetResolution(2560,1440,FullScreenMode.Windowed);
            GUI.enabled=!busy && !measuring;
            if(!SquadMode) foreach(CrowdBattleSimulation.Order order in Enum.GetValues(typeof(CrowdBattleSimulation.Order)))
                if(GUI.Button(new Rect(24+(int)order*136,112,128,25),order.ToString()))Simulation.FriendlyOrder=order;
            if(GUI.Button(new Rect(452,112,210,25),"Results folder")){Directory.CreateDirectory(output);Application.OpenURL(new Uri(output+Path.DirectorySeparatorChar).AbsoluteUri);}
            bool terrain=GUI.Toggle(new Rect(24,145,135,25),TerrainLayer,"Terrain + props");
            bool shadows=GUI.Toggle(new Rect(175,145,135,25),ShadowLayer,"Shadows");
            bool equipment=GUI.Toggle(new Rect(325,145,160,25),EquipmentLayer,"Equipment");
            if(terrain!=TerrainLayer || shadows!=ShadowLayer || equipment!=EquipmentLayer)SetLayers(terrain,shadows,equipment);
            GUI.enabled=true;GUI.Label(new Rect(24,183,640,24),measuring?(elapsed<0?"Warming up...":"Measuring "+elapsed.ToString("F0")+" / "+duration+" s") : status);
            GUI.Label(new Rect(24,207,640,25),"Blue: friendly / Orange: enemy | "+Profile+" | "+Screen.width+" x "+Screen.height+" | 60 FPS budget: 16.7 ms");
            GUI.enabled=!busy && !measuring;
            if(GUI.Button(new Rect(710,12,210,30),"Open / reset squad commands")){busy=false;measuring=false;OpenSquads();}
            if(GUI.Button(new Rect(930,12,180,30),"Return to stress lab")){CommunicationMode=false;PerceptionMode=false;NavigationMode=false;SquadMode=false;Reset(1280,true);}
            for(int n=0;n<3;n++)if(GUI.Button(new Rect(930,50+n*32,180,28),((CrowdNavigation.Layout)n).ToString()+" navigation"))OpenNavigation((CrowdNavigation.Layout)n);
            if(GUI.Button(new Rect(930,150,180,28),PerceptionMode?"Senses ON (reset)":"Senses OFF (reset)")){PerceptionMode=!PerceptionMode;if(!PerceptionMode)CommunicationMode=false;OpenSquads();}
            if(GUI.Button(new Rect(1120,150,150,28),CommunicationMode?"Alerts ON (reset)":"Alerts OFF (reset)")){CommunicationMode=!CommunicationMode;if(CommunicationMode)PerceptionMode=true;OpenSquads();}
            if(GUI.Button(new Rect(1120,182,150,28),"Scout report demo"))OpenAlertDemo();
            if(CommunicationMode)GUI.Label(new Rect(1120,216,155,28),"Reports: "+Simulation.Communication.Delivered);
            if(PerceptionMode)
            {
                if(GUI.Button(new Rect(930,182,180,28),"Noise click: "+(noiseClick?"ON":"OFF")))noiseClick=!noiseClick;
                if(GUI.Button(new Rect(930,214,180,28),"Observe "+(observer==760?"friendly":"enemy")))observer=observer==760?1:760;
                var awareness=Simulation.Perception.States[observer];
                GUI.Label(new Rect(710,248,540,42),"Observer: "+(awareness.visibleTarget>=0?"SEES enemy":Simulation.Perception.TryKnownPosition(observer,out _)?(awareness.reported?"ALLY REPORT":awareness.heard?"HEARD noise":"LAST SEEN memory"):"UNAWARE")+" | yellow cone: 120 degrees / 12 m");
            }
            if(SquadMode)
            {
                GUI.enabled=!busy && !measuring;
                for(int s=0;s<8;s++)if(GUI.Button(new Rect(710+(s%4)*52,50+(s/4)*30,48,27),(selectedSquad==s?">":"")+(s+1)))selectedSquad=s;
                if(GUI.Button(new Rect(710,114,208,26),selectedSquad<0?"> All squads":"All squads"))selectedSquad=-1;
                if(GUI.Button(new Rect(710,146,65,28),"Move"))pendingCommand=CrowdSquadCommands.Command.Move;
                if(GUI.Button(new Rect(780,146,65,28),"Hold"))IssueSquads(CrowdSquadCommands.Command.Hold,Vector3.zero);
                if(GUI.Button(new Rect(850,146,65,28),"Attack"))pendingCommand=CrowdSquadCommands.Command.Attack;
                GUI.Label(new Rect(710,182,210,50),"Right-click: "+pendingCommand+"\nHold = defend in place");
                GUI.enabled=true;
                for(int s=0;s<Simulation.Squads.Squads.Length;s++)
                {
                    Vector3 p=View.WorldToScreenPoint(Simulation.Squads.Squads[s].anchor+Vector3.up*2);
                    if(p.z>0)GUI.Label(new Rect(p.x/scale-20,(Screen.height-p.y)/scale,120,25),(selectedSquad==s || selectedSquad<0?"> ":"")+(s+1)+" "+Simulation.Squads.Squads[s].command);
                }
            }
            GUI.enabled=true;
            GUI.Box(new Rect(10,Screen.height/scale-72,880,60),"Prototype stress harness: not the full Company battle. All units simulated; no individual fingers.\nJSON/CSV results save automatically. Use Results folder to open them.");GUI.matrix=old;
        }
        public void IssueSquads(CrowdSquadCommands.Command command,Vector3 point)
        {
            if(!SquadMode)return;
            for(int s=0;s<Simulation.Squads.Squads.Length;s++)if(selectedSquad<0 || selectedSquad==s)
            {
                Vector3 destination=point;
                if(selectedSquad<0)destination+=new Vector3((s%4-1.5f)*11,0,(s/4-.5f)*9);
                Simulation.Squads.Issue(s,command,destination);
            }
        }
        private void ReadSquadInput()
        {
            if(Objective!=null && Objective.Outcome!=CrowdObjectiveScenario.State.Running)return;
            if(!SquadMode || busy || measuring || Mouse.current==null || !Application.isFocused)return;
            if(!Mouse.current.rightButton.wasPressedThisFrame)return;
            Vector2 mouse=Mouse.current.position.ReadValue();float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            Vector2 gui=new Vector2(mouse.x/scale,(Screen.height-mouse.y)/scale);
            if(Objective!=null ? (gui.y<190 || gui.y>Screen.height/scale-72) : new Rect(10,10,680,232).Contains(gui) || new Rect(700,10,580,282).Contains(gui) || gui.y>Screen.height/scale-72)return;
            Ray ray=View.ScreenPointToRay(mouse);
            if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance))
            {
                if(PerceptionMode && noiseClick)Simulation.Perception.Emit(-1,ray.GetPoint(distance));
                else IssueSquads(pendingCommand,ray.GetPoint(distance));
            }
        }
        private static bool Has(string key)=>Array.IndexOf(Environment.GetCommandLineArgs(),key)>=0;
        private static string Argument(string key,string fallback){string[] a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,key);return i>=0 && i+1<a.Length?a[i+1]:fallback;}
        private static double Number(string key,double fallback)=>double.TryParse(Argument(key,""),NumberStyles.Float,CultureInfo.InvariantCulture,out double n)?Math.Max(0,n):fallback;
        private void OnDestroy(){Application.runInBackground=oldBackground;QualitySettings.vSyncCount=oldVsync;Application.targetFrameRate=oldCap;if(floorMaterial)Destroy(floorMaterial);if(senseMaterial)Destroy(senseMaterial);foreach(var material in navigationMaterials)Destroy(material);}
    }
}
