using System;
using System.Collections.Generic;
using System.IO;
using Reclamation.Blight;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reclamation.Atlas
{
    // Atlas collision and presentation adapt the shared, unchanged DuelFighter rules.
    // Only the local company participates; this is not a world-wide battle simulation.
    public sealed partial class WorldAtlasDemo
    {
        private sealed class BattleActor
        {
            public Transform root;
            public AtlasCharacterVisual visual;
            public AtlasSyntyVisual synty;
            public DuelFighter fighter=new DuelFighter();
            public bool enemy,moving;
            public int guard=-1;
            public Vector3 post,dodge;
            public float delay;
        }
        private readonly List<BattleActor> battleActors=new List<BattleActor>();
        private readonly List<BattleActor> battleHits=new List<BattleActor>();
        private BattleActor battlePlayer, battleLock;
        private AtlasDefenseState defenseState;
        private int defenseHome, reservePlatoon;
        private bool defenseMode, defenseActive, defensePanel=true, battleSprintExhausted, defenseDressingUsed, defenseRally;
        private float defenseWarning, defenseBreach, defenseContribution, battleAccumulator;
        private Vector3 battleMove, rallyPoint;
        private bool battleBlock,battleSprint,battleAttack,battleHeavy,battleDodge;
        private string battleReport="Choose a defense plan at the company command post.", battleImpact="";
        private float impactUntil;
        private int battlePlayerHits;
        public bool DefenseMode=>defenseMode;
        public bool DefenseActive=>defenseActive;
        public AtlasDefenseState DefenseState=>defenseState;
        public DuelFighter AtlasPlayerFighter=>battlePlayer?.fighter;
        public int BattlePlayerHits=>battlePlayerHits;
        public int BattleEnemies { get { int n=0;foreach(var a in battleActors)if(a.enemy&&a.fighter.Alive)n++;return n; } }
        public int BattleSurvivors { get { int n=0;foreach(var a in battleActors)if(a.guard>=0&&a.fighter.Alive)n++;return n; } }
        public float BattleWarning=>defenseWarning;
        public float BattleBreach=>defenseBreach;
        public int BattleReservePlatoon=>reservePlatoon;
        public bool AutomaticBattle {get;set;}=true;
        public bool ResumeDefenseCheckpoint {get;set;}=true;
        public bool SyntyTrial {get;set;}
        public bool SyntyRoleTrial {get;set;}
        public bool SyntyCompanyTrial {get;set;}
        private void InstallSynty(BattleActor actor)
        {
            if(!SyntyTrial||actor.synty)return;
            var go=new GameObject("Synty art trial");go.transform.SetParent(actor.root,false);
            var role=!SyntyRoleTrial?SyntyRole.Player:actor.enemy?SyntyRole.Raider:actor.guard==0?SyntyRole.CompanyCommander:actor.guard>0?(actor.guard-1)%6==0?SyntyRole.PlatoonCommander:SyntyRole.Soldier:SyntyRole.Player;
            actor.synty=go.AddComponent<AtlasSyntyVisual>();actor.synty.Build(actor.visual,actor.enemy,role,actor.enemy?battleActors.Count-27:Mathf.Max(0,(actor.guard-1)/6));
            if(actor.guard>=0)actor.synty.BindTorch(actor.visual);
        }
        private Vector3 DefensePoint(float x,float z)
        {
            Vector2 p=Model.sites[defenseHome].point+LayoutFor(defenseHome).Rotate(new Vector2(x,z));
            return new Vector3(p.x,Surface(p),p.y);
        }
        public Vector3 DefenseCouncil=>DefensePoint(0,13);
        public Vector3 DefenseApproachPoint=>DefensePoint(-3,-45);
        private bool AtlasGuardAlive(Transform root)
        {
            if(!defenseMode||battleActors.Count<26)return true;
            foreach(var a in battleActors)if(a.root==root)return a.fighter.Alive;
            return true;
        }
        private bool AtDefenseCouncil=>Vector3.Distance(hero.position,DefenseCouncil)<5;
        public void EnableAtlasDefense()
        {
            defenseMode=true;InitializeAtlasDefense();
        }
        private void ResetAtlasDefense()
        {
            foreach(var a in battleActors)if(a.synty){a.synty.gameObject.SetActive(false);Destroy(a.synty.gameObject);}
            battleActors.Clear();battleHits.Clear();battlePlayer=battleLock=null;defenseActive=false;
            defenseState=null;battleAccumulator=0;defensePanel=!SyntyCompanyTrial;battlePlayerHits=0;
            battleMove=Vector3.zero;battleAttack=battleDodge=battleSprint=battleBlock=false;
        }
        private void InitializeAtlasDefense()
        {
            if(!defenseMode)return;
            ResetAtlasDefense();defenseHome=Model.factions[0].capital;selected=defenseHome;
            VisitCommander();commanderPanel=false;clockRate=1;
            defenseState=new AtlasDefenseState{size=(int)Model.Size,site=defenseHome,seed=LayoutFor(defenseHome).Seed};
            battlePlayer=new BattleActor{root=hero,visual=rig};battleActors.Add(battlePlayer);
            InstallSynty(battlePlayer);
            for(int i=0;i<guards.Count;i++)
            {
                var actor=new BattleActor{root=guards[i].root,visual=guards[i].rig,guard=i,post=guards[i].root.position};battleActors.Add(actor);
                if(SyntyCompanyTrial||SyntyRoleTrial&&(i==0||(i-1)%6==0||(i-1)%6==1))InstallSynty(actor);
            }
            battleReport=CommanderName+": The reserve is ready. Choose whether to meet the raiders on the road or defend our stores.";
            if(ResumeDefenseCheckpoint&&File.Exists(AtlasDefensePath))LoadAtlasDefense();
        }
        private string AtlasDefensePath
        {
            get
            {
                string[] args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--defense-save-directory");
                string folder=i>=0&&i+1<args.Length?args[i+1]:Path.Combine(Application.persistentDataPath,"AtlasDefense");
                return Path.Combine(folder,Model.Size+"-defense-v1.json");
            }
        }
        public bool BeginAtlasDefense(bool intercept)
        {
            if(!defenseMode||defenseActive||paused||!AtDefenseCouncil||!battlePlayer.fighter.CanAct||!battleActors[1].fighter.Alive||
                defenseState.progression.completed>=2||scoutEncounter.Active)return false;
            var p=defenseState.progression;if(p.completed>0&&(p.playerPerk==0||p.commanderPerk==0))return false;
            for(int i=battleActors.Count-1;i>=26;i--){Destroy(battleActors[i].root.gameObject);battleActors.RemoveAt(i);}
            battleLock=null;reservePlatoon=-1;
            foreach(var platoon in ActiveCompany.Platoons)if(platoon.Duty==AtlasGuardDuty.Reserve)reservePlatoon=platoon.Id;
            if(reservePlatoon<0)return false;
            p.approach=intercept?1:0;defenseActive=true;defenseWarning=20;defenseBreach=defenseContribution=0;
            defenseDressingUsed=defenseRally=false;clockRate=1;defensePanel=false;commanderPanel=false;yaw=LayoutFor(defenseHome).Angle+180;pitch=20;zoom=11;UpdateCamera(true);
            foreach(var a in battleActors){a.post=a.root.position;a.delay=0;}
            battleReport=CommanderName+": P"+(reservePlatoon+1)+(intercept?" will intercept at the south road. Join their line.":" will defend the stores. Let the raiders come to us.")+" Other platoons hold their current posts.";
            return true;
        }
        private void SpawnAtlasRaiders(int count=6)
        {
            for(int i=0;i<count;i++)
            {
                var root=new GameObject("Raider "+(i+1)).transform;root.SetParent(world,false);root.position=DefensePoint((i%3-1)*2.2f,-65-i/3*3);
                var visual=root.gameObject.AddComponent<AtlasCharacterVisual>();visual.Build(i,false,true);
                var actor=new BattleActor{root=root,visual=visual,enemy=true,post=root.position,delay=i*.12f};
                battleActors.Add(actor);if(i==0||SyntyRoleTrial)InstallSynty(actor);
            }
        }
        public void SetAtlasBattleInput(Vector3 direction,bool block=false,bool sprint=false)
        {battleMove=Vector3.ClampMagnitude(new Vector3(direction.x,0,direction.z),1);battleBlock=block;battleSprint=sprint;}
        public bool RequestAtlasAttack(bool heavy)
        {
            if(paused||!defenseMode||!battlePlayer.fighter.CanAct)return false;
            return battlePlayer.fighter.Attack(BlightEquipment.Weapon(BlightWeapon.Sword,heavy));
        }
        public bool RequestAtlasDodge()
        {
            if(paused||!defenseMode||!battlePlayer.fighter.Dodge())return false;
            battlePlayer.dodge=battleMove.sqrMagnitude>.001f?battleMove:-hero.forward;return true;
        }
        public void SimulateAtlasBattle(float seconds)
        {
            if(!defenseMode||paused||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            float remaining=Mathf.Min(seconds,1);
            while(remaining>0){float dt=Mathf.Min(remaining,1f/60);BattleStep(dt);remaining-=dt;}
            SyncBattleVisuals(Mathf.Min(seconds,1));
        }
        private bool BattleSight(Vector3 a,Vector3 b)=>!Physics.Linecast(a+Vector3.up,b+Vector3.up,ObstacleMask,QueryTriggerInteraction.Ignore);
        private bool Mobile(BattleActor a)=>a.guard>0&&(a.guard-1)/6==reservePlatoon;
        private void BattleStep(float dt)
        {
            if(!battlePlayer.fighter.Alive)return;
            if(DuelPractice)practiceAttackSpacing=Mathf.Max(0,practiceAttackSpacing-dt);
            if(defenseActive&&defenseWarning>0){defenseWarning=Mathf.Max(0,defenseWarning-dt);if(defenseWarning==0)SpawnAtlasRaiders();}
            foreach(var actor in battleActors)actor.moving=false;
            var f=battlePlayer.fighter;
            if(!battleSprint&&f.Stamina>=20)battleSprintExhausted=false;
            if(f.CanAct)
            {
                f.Blocking=battleBlock;
                Vector3 facing=battleLock!=null&&battleLock.fighter.Alive?battleLock.root.position-hero.position:battleMove;
                BattleFace(battlePlayer,facing);
                if(battleDodge)RequestAtlasDodge();else if(battleAttack)RequestAtlasAttack(battleHeavy);
                if(f.CanAct)
                {
                    bool wants=battleSprint&&!battleBlock&&battleMove.sqrMagnitude>.01f&&!battleSprintExhausted;
                    bool running=wants&&f.TrySprint(dt);if(wants&&!running)battleSprintExhausted=true;
                    BattleMoveActor(battlePlayer,battleMove*(running?6.2f:battleBlock?1.8f:3.8f)*dt);
                }
            }
            battleAttack=battleDodge=false;
            battleHits.Clear();
            foreach(var a in battleActors)
            {
                a.delay=Mathf.Max(0,a.delay-dt);
                if(defenseActive&&a!=battlePlayer&&a.fighter.Alive&&(!DuelPractice||a.enemy))
                {
                    if(DuelPractice&&practiceShield){BattleFace(a,hero.position-a.root.position);a.fighter.Blocking=a.fighter.CanAct;}
                    else BattleAI(a,dt);
                }
                if(a.fighter.Action==DuelAction.Dodge)BattleMoveActor(a,a.dodge*8*Mathf.Min(dt,a.fighter.Remaining));
                if(a.fighter.Advance(dt))battleHits.Add(a);
            }
            foreach(var source in battleHits)ResolveAtlasHit(source);
            if(!battlePlayer.fighter.Alive){defenseActive=false;defensePanel=true;battleReport="You fell. Reload your previous checkpoint or start a fresh scenario.";return;}
            if(defenseActive&&defenseWarning==0&&!DuelPractice)
            {
                bool breach=false,contribution=false;
                foreach(var a in battleActors)if(a.enemy&&a.fighter.Alive)
                {
                    breach|=Vector3.Distance(a.root.position,DefensePoint(0,-12))<6;
                    contribution|=Vector3.Distance(a.root.position,hero.position)<10&&BattleSight(hero.position,a.root.position);
                }
                if(breach)defenseBreach+=dt;if(contribution)defenseContribution+=dt;
                if(BattleEnemies==0)CompleteAtlasDefense();
            }
        }
        private static float BattleDistance(Vector3 a,Vector3 b){Vector3 delta=a-b;delta.y=0;return delta.magnitude;}
        private void BattleAI(BattleActor a,float dt)
        {
            if(!a.fighter.CanAct)return;
            if(TryPracticePairAI(a,dt))return;
            bool mobile=Mobile(a);float best=a.enemy?13:mobile?11:5;BattleActor target=null;
            float relief=defenseState.progression.commanderPerk==(int)CaptainPerk.CarefulRelief?55:35;
            if(!a.enemy&&a.fighter.Health<=relief){a.fighter.Blocking=true;BattleApproach(a,DefensePoint(4,12),2.2f,dt,1);return;}
            foreach(var b in battleActors)
            {
                if(b.enemy==a.enemy||!b.fighter.Alive||!b.root.gameObject.activeSelf||DuelPractice&&b.guard>=0)continue;
                float d=BattleDistance(a.root.position,b.root.position);
                if(d<best&&BattleSight(a.root.position,b.root.position)){best=d;target=b;}
            }
            a.fighter.Blocking=false;
            if(target!=null)
            {
                BattleFace(a,target.root.position-a.root.position);
                var strike=a.enemy?BlightEquipment.Enemy(BlightEnemy.Thrall,a.fighter.AttackSequence):BlightEquipment.Weapon(BlightWeapon.Sword,false);
                if(best>strike.Reach-.50f){if(a.guard!=0)BattleApproach(a,target.root.position,a.enemy?2.2f:2.5f,dt,strike.Reach-.55f);else a.fighter.Blocking=true;}
                else if(a.delay<=0)
                {
                    int committed=0;foreach(var other in battleActors)if(other.enemy&&(other.fighter.Action==DuelAction.Windup||other.fighter.Action==DuelAction.Recovery))committed++;
                    if((!a.enemy||committed<2)&&a.fighter.Attack(strike))a.delay=strike.Windup+strike.Recovery+.45f;
                }
                return;
            }
            Vector3 goal=a.post;
            if(a.enemy)goal=DefensePoint(0,-12);
            else if(mobile)
            {
                int member=(a.guard-1)%6;
                bool watch=defenseState.progression.commanderPerk==(int)CaptainPerk.WatchCaptain;
                goal=watch&&member>=4?DefensePoint((member==4?-1:1)*2,-16):
                    defenseRally?rallyPoint+new Vector3((member%3-1)*1.2f,0,member/3*1.2f):DefensePoint((member%3-1)*2.2f,defenseState.progression.approach==1?-44-member/3*2:-22-member/3*2);
            }
            BattleApproach(a,goal,a.enemy?2.2f:3,dt,.25f);
        }
        private static void BattleFace(BattleActor actor,Vector3 direction)
        {direction.y=0;if(direction.sqrMagnitude>.001f)actor.root.rotation=Quaternion.LookRotation(direction);}
        private void BattleApproach(BattleActor a,Vector3 target,float speed,float dt,float stop)
        {
            Vector3 d=target-a.root.position;d.y=0;if(d.magnitude<=stop)return;
            BattleFace(a,d);BattleMoveActor(a,d.normalized*Mathf.Min(speed*dt,d.magnitude-stop));
        }
        private void BattleMoveActor(BattleActor a,Vector3 delta)
        {
            if(delta.sqrMagnitude<.000001f)return;
            Vector3 from=a.root.position;
            int attempts=a==battlePlayer?1:7;
            for(int attempt=0;attempt<attempts;attempt++)
            {
                float angle=attempt==0?0:(attempt+1)/2*35*(attempt%2==1?1:-1);
                Vector3 motion=Quaternion.Euler(0,angle,0)*delta;
                Vector3 next=ResolveVillageMove(from,motion);Vector2 p=new Vector2(next.x,next.z);
                if(!Model.Walkable(p))continue;
                bool clear=true;
                foreach(var b in battleActors)
                {
                    if(b==a||!b.fighter.Alive||!b.root.gameObject.activeSelf)continue;
                    Vector3 old=from-b.root.position,near=next-b.root.position;old.y=near.y=0;
                    if(near.sqrMagnitude<.86f*.86f&&near.sqrMagnitude<old.sqrMagnitude-.00001f){clear=false;break;}
                }
                if(!clear)continue;
                a.root.position=new Vector3(p.x,Surface(p),p.y);a.moving=(a.root.position-from).sqrMagnitude>.000001f;return;
            }
        }
        private void ResolveAtlasHit(BattleActor source)
        {
            BattleActor target=null;float nearest=float.MaxValue;var strike=source.fighter.Strike;
            foreach(var a in battleActors)
            {
                if(a.enemy==source.enemy||!a.fighter.Alive||!a.root.gameObject.activeSelf||DuelPractice&&a.guard>=0||!BattleSight(source.root.position,a.root.position))continue;
                if(!DuelFighter.InReach(source.root.position,source.root.forward,a.root.position,strike.Reach,strike.HalfAngle))continue;
                float d=(a.root.position-source.root.position).sqrMagnitude;if(d<nearest){nearest=d;target=a;}
            }
            if(target==null)return;
            float before=target.fighter.Health;
            string result=target.fighter.ReceiveAttack(strike,Vector3.Angle(target.root.forward,source.root.position-target.root.position)<70);
            if(target.synty)target.synty.ReceiveContact(result);
            if(source.synty)source.synty.ReceiveWeaponContact(result);
            if(source==battlePlayer&&target.fighter.Health<before)battlePlayerHits++;
            if(source==battlePlayer||target==battlePlayer){battleImpact=result;impactUntil=Time.unscaledTime+1.5f;}
        }
        private void CompleteAtlasDefense()
        {
            defenseActive=false;
            for(int i=1;i<26;i++){Vector3 at=battleActors[i].root.position;guards[i-1].local=new Vector2(at.x,at.z)-Model.sites[defenseHome].point;}
            var p=defenseState.progression;p.completed++;
            p.lastProtected=defenseBreach<5;p.lastParticipated=defenseContribution>=3;
            p.lastFoodLost=p.lastProtected?0:Mathf.Min(5,defenseState.food);defenseState.food-=p.lastFoodLost;
            // The shared compact progression DTO retains its eight-person lab field; atlas roster is stored separately.
            p.lastSurvivors=0;
            if(battlePlayer.fighter.Alive)p.playerXp+=100+(p.lastProtected?50:0)+(p.lastParticipated?25:0);
            if(battleActors[1].fighter.Alive)p.commanderXp+=100+(p.lastProtected?50:0);
            p.outcome="Raid repelled / "+BattleSurvivors+" of 25 company troops survived / "+(p.lastProtected?"stores protected":"lost "+p.lastFoodLost+" food");
            battleReport=p.outcome+". Return to the command post for perks, recovery and a checkpoint.";defensePanel=!SyntyCompanyTrial;
            CaptureAtlasHealth();
        }
        private void CaptureAtlasHealth(){for(int i=0;i<26;i++)defenseState.health[i]=battleActors[i].fighter.Health;}
        public bool ChooseAtlasFounder(FounderPerk perk)=>defenseMode&&!defenseActive&&!paused&&AtDefenseCouncil&&defenseState.progression.ChooseFounder(perk);
        public bool ChooseAtlasCaptain(CaptainPerk perk)=>defenseMode&&!defenseActive&&!paused&&AtDefenseCouncil&&battleActors[1].fighter.Alive&&defenseState.progression.ChooseCaptain(perk);
        public bool RestAtlasCompany()
        {
            if(!CanCheckpointAtlas()||defenseState.food<2)return false;
            defenseState.food-=2;foreach(var a in battleActors)if(!a.enemy&&a.fighter.Alive)a.fighter.Recover(100f/18);return true;
        }
        public bool UseAtlasPerk()
        {
            if(!defenseActive||paused||!battlePlayer.fighter.CanAct)return false;
            if(defenseState.progression.playerPerk==(int)FounderPerk.ForwardRally)
            {
                if(!battleActors[1].fighter.Alive||Vector3.Distance(hero.position,DefensePoint(0,-28))>35)return false;
                rallyPoint=hero.position;defenseRally=true;battleReport="Reserve rally acknowledged. Fixed platoons hold their posts.";return true;
            }
            if(defenseState.progression.playerPerk!=(int)FounderPerk.FieldDressing||defenseDressingUsed||defenseState.food<1)return false;
            BattleActor patient=null;foreach(var a in battleActors)if(a.guard>=0&&a.fighter.CanAct&&a.fighter.Health<100&&Vector3.Distance(a.root.position,hero.position)<3&&(patient==null||a.fighter.Health<patient.fighter.Health))patient=a;
            if(patient==null)return false;
            foreach(var a in battleActors)if(a.enemy&&a.fighter.Alive&&(Vector3.Distance(a.root.position,hero.position)<8||Vector3.Distance(a.root.position,patient.root.position)<8))return false;
            patient.fighter.Recover(20f/18);defenseDressingUsed=true;defenseState.food--;battleReport="Dressing applied. One food used.";return true;
        }
        private bool CanCheckpointAtlas()
        {
            if(!defenseMode||defenseActive||paused||!AtDefenseCouncil||!battlePlayer.fighter.Alive)return false;
            foreach(var a in battleActors)if(!a.enemy&&a.fighter.Alive&&(!a.fighter.CanAct||a.fighter.Stamina<99.9f))return false;
            return true;
        }
        public bool SaveAtlasDefense(string path=null)
        {
            if(!CanCheckpointAtlas()){battleReport="Return to command after combat and let stamina recover before saving.";return false;}
            try{CaptureAtlasHealth();defenseState.Validate(Model,defenseHome,LayoutFor(defenseHome).Seed);defenseState.Write(path??AtlasDefensePath);battleReport="Map defense checkpoint saved. Wounds, casualties and perks will resume.";return true;}
            catch(Exception ex) when(ex is InvalidDataException||ex is IOException||ex is UnauthorizedAccessException||ex is ArgumentException){battleReport="Save failed: "+ex.Message;return false;}
        }
        public bool LoadAtlasDefense(string path=null)
        {
            if(defenseActive)return false;
            try
            {
                path=path??AtlasDefensePath;if(!File.Exists(path)||new FileInfo(path).Length>100000)return false;
                var saved=JsonUtility.FromJson<AtlasDefenseState>(File.ReadAllText(path));if(saved==null)return false;
                saved.Validate(Model,defenseHome,LayoutFor(defenseHome).Seed);
                defenseState=saved;for(int i=0;i<26;i++){battleActors[i].fighter=new DuelFighter();battleActors[i].fighter.Receive(100-saved.health[i],false,false);battleActors[i].visual.transform.localRotation=Quaternion.identity;}
                hero.position=DefenseCouncil;battleReport="Resumed this map's defense checkpoint. "+saved.progression.outcome;SyncBattleVisuals();return true;
            }
            catch(Exception ex) when(ex is InvalidDataException||ex is IOException||ex is ArgumentException||ex is UnauthorizedAccessException){battleReport="Checkpoint rejected: "+ex.Message;return false;}
        }
        private void SyncBattleVisuals(float delta=0)
        {
            foreach(var a in battleActors)
            {
                bool moving=a.guard>=0&&!defenseActive?a.visual.CurrentClip!="Idle":a.moving;
                if(a.guard>=0&&defenseActive)a.visual.transform.localRotation=Quaternion.identity;
                a.visual.SetCombatState(a.fighter,moving,battleSprint&&a==battlePlayer);
                if(a.enemy)foreach(var renderer in a.root.GetComponentsInChildren<Renderer>())
                    if(!a.synty||!renderer.transform.IsChildOf(a.synty.transform))renderer.enabled=Knowledge.Visible(new Vector2(a.root.position.x,a.root.position.z));
                if(!a.fighter.Alive&&!(a.synty&&a.synty.isActiveAndEnabled))a.visual.transform.localRotation=Quaternion.Euler(0,0,80);
                if(a.synty&&a.synty.isActiveAndEnabled)
                {
                    a.synty.transform.rotation=a.guard>=0&&!defenseActive?a.visual.transform.rotation:a.root.rotation;
                    a.synty.Readiness=DuelPractice&&a.guard>=0?SyntyReadiness.Relaxed:defenseActive?(defenseWarning>0?SyntyReadiness.Alert:SyntyReadiness.Combat):SyntyReadiness.Relaxed;
                    a.synty.CombatMotion=DuelPractice&&(a==battlePlayer||a.enemy);
                    a.synty.Sample(a.fighter,moving,battleSprint&&a==battlePlayer,delta);
                    a.synty.SetVisible(!a.enemy||Knowledge.Visible(new Vector2(a.root.position.x,a.root.position.z)));
                }
            }
        }
        private void UpdateAtlasDefense()
        {
            var k=Keyboard.current;var m=Mouse.current;
            if(k!=null)
            {
                if(k.escapeKey.wasPressedThisFrame)paused=!paused;
                if(DuelPractice)UpdatePracticeInput(k);
                if(!DuelPractice&&k.mKey.wasPressedThisFrame)mapOpen=!mapOpen;
                if(!DuelPractice&&k.bKey.wasPressedThisFrame)defensePanel=!defensePanel;
                if(k.f3Key.wasPressedThisFrame)debug=!debug;
                if(!DuelPractice&&k.cKey.wasPressedThisFrame)commanderPanel=!commanderPanel;
            }
            if(!paused&&AutomaticBattle)
            {
                TickVillage(Time.deltaTime);
                bool input=!mapOpen&&Application.isFocused;
                Vector3 raw=input&&k!=null?new Vector3((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),0,(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0)):Vector3.zero;
                SetAtlasBattleInput(Quaternion.Euler(0,yaw,0)*raw.normalized,input&&k!=null&&k.leftCtrlKey.isPressed,input&&k!=null&&k.leftShiftKey.isPressed);
                if(input&&k!=null)
                {
                    bool ui=BattlePointerOverHud();
                    battleAttack=(m!=null&&m.leftButton.wasPressedThisFrame&&!ui)||k.eKey.wasPressedThisFrame;battleHeavy=k.eKey.wasPressedThisFrame;battleDodge=k.spaceKey.wasPressedThisFrame;
                    if(k.tabKey.wasPressedThisFrame){battleLock=null;float best=25;foreach(var a in battleActors)if(a.enemy&&a.fighter.Alive&&Vector3.Distance(a.root.position,hero.position)<best){best=Vector3.Distance(a.root.position,hero.position);battleLock=a;}}
                    if(k.qKey.wasPressedThisFrame)UseAtlasPerk();
                    if(m!=null&&!ui){zoom=Mathf.Clamp(zoom-m.scroll.ReadValue().y*.04f,5,40);if(m.rightButton.isPressed){Vector2 d=m.delta.ReadValue();yaw+=d.x*.16f;pitch=Mathf.Clamp(pitch-d.y*.12f,12,65);}}
                }
                battleAccumulator+=Mathf.Min(Time.deltaTime,.1f);while(battleAccumulator>=1f/60){SimulateAtlasBattle(1f/60);battleAccumulator-=1f/60;}
            }
            foreach(var a in battleActors)a.visual.PlaybackSpeed=paused?0:1;
            UpdateCamera(false);UpdateRecon(Time.unscaledDeltaTime);DrawInstances();
        }
        public string AtlasBattleDiagnostic()
        {
            string text="warning "+defenseWarning+" / breach "+defenseBreach;
            foreach(var a in battleActors){Vector3 p=a.root.position;var local=Quaternion.Inverse(Quaternion.Euler(0,LayoutFor(defenseHome).Angle,0))*(p-DefensePoint(0,0));text+="\n"+a.root.name+" local="+local+" hp="+a.fighter.Health+" walk="+Model.Walkable(new Vector2(p.x,p.z))+" solid="+IsSolidAt(p);}
            return text;
        }
        private void OnApplicationFocus(bool focus){if(defenseMode&&!focus&&AutomaticBattle){paused=true;SetAtlasBattleInput(Vector3.zero);battleAttack=battleDodge=false;}}
    }
}
