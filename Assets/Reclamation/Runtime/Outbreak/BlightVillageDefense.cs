using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        [SerializeField] private bool villageDefense;
        private bool villageStarted, villageWon;
        private float villageElapsed, villageIntegrity = 100;
        private int villageWave;
        private readonly Dictionary<Actor,int> villageRoutes = new Dictionary<Actor,int>();
        public bool VillageDefense => villageDefense && Scenario == BlightScenario.Company;
        public bool VillageStarted => villageStarted;
        public bool VillageWon => VillageDefense && villageWon;
        public bool VillageLost => VillageDefense && (villageIntegrity <= 0 || player != null && !player.fighter.Alive);
        public int VillageWave => villageWave;
        public float VillageIntegrity => villageIntegrity;
        public int CompanyCapacity(int index) => company[index].members.Count;
        private int CompanyInitialStrength => VillageDefense ? 24 : 6;
        public void SetVillageDefense(bool enabled)
        {
            villageDefense = enabled;
            if (enabled) expandedCompany = true;
            SelectScenario(BlightScenario.Company);
        }
        private void ClearVillageDefense()
        {
            villageStarted = villageWon = false; villageElapsed = 0; villageWave = 0;
            villageIntegrity = 100; villageRoutes.Clear();
        }
        private Vector3 VillageSite(CompanySite site) => site == CompanySite.Gate ? new Vector3(0,0,-2) :
            site == CompanySite.West ? new Vector3(-21,0,8) : site == CompanySite.East ? new Vector3(21,0,8) : new Vector3(0,0,18);
        private string CompanySiteName(CompanySite site) => !VillageDefense ? site.ToString() :
            site == CompanySite.Gate ? "Main gate" : site == CompanySite.West ? "West gate" : site == CompanySite.East ? "East gate" : "Square";
        private Vector3 CompanyFormationOffset(Actor actor)
        {
            if (!VillageDefense) return Vector3.right * (actor.slot == 0 ? -.85f : .85f);
            Vector3 offset = new Vector3((actor.slot % 4 - 1.5f) * 1.35f,0,(actor.slot / 4) * 1.45f);
            CompanySite site = company[actor.platoon].site;
            if (company[actor.platoon].order == CompanyOrder.Defend)
                for (int i=0;i<actor.platoon;i++)
                    if (CompanyLiving(i)>0 && company[i].order==CompanyOrder.Defend && company[i].site==site && company[i].phase!=CompanyPhase.Returning)
                        offset.z+=3.1f;
            if (company[actor.platoon].phase == CompanyPhase.Returning) return offset;
            return Quaternion.Euler(0,site == CompanySite.West ? 90 : site == CompanySite.East ? -90 : 0,0) * offset;
        }
        private void BuildVillageDefense()
        {
            Color stone = new Color(.37f,.39f,.37f), roof = new Color(.35f,.26f,.2f);
            // Solid perimeter with three actual gaps. Walls do not breach in this milestone.
            FieldObstacle("South wall west",new Rect(-25,-7,20,1.5f),3,stone);
            FieldObstacle("South wall east",new Rect(5,-7,20,1.5f),3,stone);
            FieldObstacle("West wall south",new Rect(-25,-7,1.5f,11),3,stone);
            FieldObstacle("West wall north",new Rect(-25,12,1.5f,19),3,stone);
            FieldObstacle("East wall south",new Rect(23.5f,-7,1.5f,11),3,stone);
            FieldObstacle("East wall north",new Rect(23.5f,12,1.5f,19),3,stone);
            FieldObstacle("North wall",new Rect(-25,29.5f,50,1.5f),3,stone);
            FieldObstacle("West homes",new Rect(-16,15,6,8),3.5f,roof);
            FieldObstacle("East homes",new Rect(10,15,6,8),3.5f,roof);
            FieldObstacle("West granary",new Rect(-15,0,5,6),3,roof);
            FieldObstacle("East granary",new Rect(10,0,5,6),3,roof);
            Part(companyScenery,"Village square",new Vector3(0,.015f,18),new Vector3(13,.03f,10),Material(new Color(.4f,.4f,.35f)));
            for (int group=0;group<3;group++)
            {
                CommandPlatoon p=company[group];
                for (int slot=p.members.Count;slot<8;slot++)
                {
                    Actor a=CreateActor(p.commander.Name+" soldier "+(slot+1),camp,false);
                    a.platoon=group; a.slot=slot; Equip(a,slot%2==0 ? BlightWeapon.Sword : BlightWeapon.Spear);
                    InstallHumanVisual(a,slot%2==0 ? BlightHumanLook.Mara : BlightHumanLook.Bren); p.members.Add(a);
                }
            }
            DeployVillageDefense();
            player.root.position=camp+Vector3.forward*6;
            foreach (CommandPlatoon p in company) foreach (Actor a in p.members)
            { a.root.position=p.goal+CompanyFormationOffset(a); a.spawn=a.root.position; a.anchor=a.root.position; a.root.rotation=Quaternion.LookRotation(Vector3.back); }
            for (int i=0;i<4;i++)
            {
                Transform marker=Marker(CompanySiteName((CompanySite)i),new Color(.65f,.65f,.4f),2);
                marker.SetParent(companyScenery,true); marker.position=VillageSite((CompanySite)i);
            }
            companySelectionMarker.position=CompanyCenter(company[0]);
            CompanyLog("Village prepared: 24 soldiers. Set orders, then Start waves. The east gate needs a decision.");
        }
        public bool DeployVillageDefense()
        {
            if (!VillageDefense || paused || player == null || !player.fighter.Alive) return false;
            companyPlan=false;
            for (int i=0;i<company.Count;i++) if (CompanyLiving(i)>0)
                AssignCompanyOrder(i,CompanyOrder.Defend,i==0 ? CompanySite.Gate : i==1 ? CompanySite.West : CompanySite.Fortress);
            CompanyLog("Defense: Mara main gate / Ivo west gate / Tess square reserve."); return true;
        }
        public bool StartVillageDefense()
        {
            if (!VillageDefense || paused || villageStarted || player == null || !player.fighter.Alive) return false;
            villageStarted=true; villageElapsed=0; CompanyLog("Defense begun. First assault in 12 seconds."); return true;
        }
        private void SpawnVillageWave()
        {
            villageWave++;
            int count=villageWave==1 ? 8 : villageWave==2 ? 12 : 16;
            for (int i=0;i<count;i++)
            {
                int route=villageWave==1 ? 0 : villageWave==2 ? (i<6 ? 0 : 2) : i%3;
                Vector3 origin=route==0 ? new Vector3((i%4-1.5f)*2.2f,0,-32-(i/4)*2.5f) :
                    new Vector3(route==1 ? -30 : 30,0,3+(i%6)*2);
                // Avoid spawning on another body when waves overlap.
                for (int attempt=0;attempt<8 && actors.Exists(a=>a.fighter.Alive && Vector3.Distance(a.root.position,origin)<1.8f);attempt++)
                    origin+=route==0 ? Vector3.back*1.8f : Vector3.forward*1.8f;
                origin=ClampCompanionGoal(origin);
                bool hulk=villageWave==3 && i>=14;
                Actor enemy=CreateActor("Wave "+villageWave+" "+(hulk ? "Hulk " : "thrall ")+(i+1),origin,true,hulk);
                villageRoutes.Add(enemy,route);
            }
            CompanyLog("Assault wave "+villageWave+" has begun. Scout reports locate the attackers.");
        }
        private bool ControlVillageEnemyIdle(Actor actor,float dt)
        {
            if (!VillageDefense || !villageRoutes.TryGetValue(actor,out int route)) return false;
            Vector3 goal=VillageSite(CompanySite.Fortress);
            // Route to the chosen opening until inside the perimeter.
            if (route==0 && actor.root.position.z < -5) goal=new Vector3(0,0,-3);
            if (route==1 && actor.root.position.x < -23) goal=new Vector3(-21,0,8);
            if (route==2 && actor.root.position.x > 23) goal=new Vector3(21,0,8);
            actor.intent="Advancing on village";
            MoveToward(actor,goal,actor.hulk ? 1.7f : 2.1f,dt,1); return true;
        }
        private void UpdateVillageDefense(float dt)
        {
            if (!VillageDefense || !villageStarted || villageWon || VillageLost) return;
            villageElapsed+=dt;
            float next=villageWave==0 ? 12 : villageWave==1 ? 75 : 150;
            if (villageWave<3 && villageElapsed>=next && LivingEnemies<=12) SpawnVillageWave();
            Vector3 square=VillageSite(CompanySite.Fortress);
            bool defended=false; int occupiers=0;
            foreach (Actor a in actors) if (a.fighter.Alive)
            {
                float distance=Vector3.Distance(a.root.position,square);
                if (!a.enemy && distance<=6) defended=true;
                if (a.enemy && distance<=4) occupiers++;
            }
            if (!defended && occupiers>0) villageIntegrity=Mathf.Max(0,villageIntegrity-dt*4*Mathf.Min(3,occupiers));
            if (villageIntegrity<=0) { CompanyLog("Village square overrun. Defense lost."); return; }
            if (villageWave==3 && LivingEnemies==0)
            { villageWon=true; CompanyLog("Village secured. Survivors: "+LivingCompanions+"/24. Recall to review."); }
        }
        private string VillageObjective() => VillageLost ? "VILLAGE LOST — R to retry" : VillageWon ?
            "VILLAGE SECURED | "+LivingCompanions+"/24 soldiers\nRecall to square; F reviews the operation." :
            !villageStarted ? "VILLAGE DEFENSE | 24 soldiers\nSet orders, then Start waves. Protect the square." :
            "Wave "+villageWave+"/3 | Square "+villageIntegrity.ToString("0")+"%\n"+LivingCompanions+"/24 soldiers | 4: fallback to square";
    }
}
