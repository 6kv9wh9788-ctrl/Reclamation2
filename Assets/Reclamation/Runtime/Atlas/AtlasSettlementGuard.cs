using System.Collections.Generic;
using Reclamation.Blight;
using UnityEngine;

namespace Reclamation.Atlas
{
    public enum AtlasGuardDuty { Gate, TownPatrol, FarmPatrol, Reserve }

    // A bounded, peacetime company schedule. Combat and threat perception remain in the combat lab.
    public sealed partial class AtlasCompanySchedule
    {
        public const int Soldiers = 24;
        public readonly CompanyCommander Commander;
        public AtlasCompanySchedule(int seed)
        {
            InitializePlatoons(seed);
            Commander = new CompanyCommander(new[] { "Mara", "Rowan", "Idris", "Sera" }[(seed & int.MaxValue) % 4], CommanderRisk.Balanced);
        }
        public static AtlasGuardDuty Duty(float hour, int soldier)
            => (AtlasGuardDuty)((soldier / 6 + Mathf.FloorToInt(Mathf.Repeat(hour, 24) / 6)) % 4);
        public static Vector2[] Route(AtlasVillageLayout layout, AtlasGuardDuty duty, int member)
        {
            Vector2 R(float x, float z) => layout.Rotate(new Vector2(x, z));
            float lane = (member % 2 == 0 ? -1 : 1) * 1.25f;
            if (duty == AtlasGuardDuty.Gate) return new[] { R(0, 0), R(0, -35), R(lane, -35 - member / 2 * 2) };
            if (duty == AtlasGuardDuty.Reserve) return new[] { R(0, 0), R(0, 12), R(3 + member % 3 * 1.6f, 10 + member / 3 * 2) };
            if (duty == AtlasGuardDuty.TownPatrol) return new[] { R(0, 0), R(lane, -30), R(lane, 20), R(0, 20), R(0, 0) };
            var road = new List<Vector2>(layout.WorkRoad(0));
            road.RemoveRange(road.Count - 3, 3); // shared road ends at the field-edge lane
            road.Add(layout.CollectionGate(1));
            road.Add(layout.CollectionGate(4));
            for (int i = road.Count - 2; i >= 0; i--) road.Add(road[i]);
            return road.ToArray();
        }
    }

    public sealed partial class WorldAtlasDemo
    {
        private sealed class GuardVisual
        {
            public Transform root;
            public AtlasCharacterVisual rig;
            public Vector2 local;
            public int member, platoon;
            public AtlasGuardDuty duty;
            public Quaternion visualRotation;

        }
        private readonly List<GuardVisual> guards = new List<GuardVisual>();
        private readonly Dictionary<int, AtlasCompanySchedule> companies = new Dictionary<int, AtlasCompanySchedule>();
        private int guardSite = -1;
        
        private bool commanderPanel;
        public AtlasCompanySchedule ActiveCompany => guardSite < 0 ? null : companies[guardSite];
        public int ActiveGuardCount { get; private set; }
        public string CommanderName => guardSite < 0 ? "No local company" : companies[guardSite].Commander.Name;
        public Vector3 GuardVisualFacing(int index) => guards[index].rig.transform.forward;
        public float GuardAlertness(int index) => guards[index].rig.Alertness;
        public Vector3 GuardPosition(int index) => guards[index].root.position;
        public AtlasGuardDuty GuardDuty(int index) => guards[index + 1].duty;

        private void BuildSettlementGuards()
        {
            guards.Clear(); companies.Clear(); guardSite = -1; 
            foreach (int id in developedVillages) companies[id] = new AtlasCompanySchedule(LayoutFor(id).Seed);
            for (int i = 0; i <= AtlasCompanySchedule.Soldiers; i++)
            {
                var root = new GameObject(i == 0 ? "Company commander" : "Company soldier " + i).transform;
                root.SetParent(world, false);
                var model = new GameObject("Guard presentation");
                model.transform.SetParent(root, false);
                var visual = model.AddComponent<AtlasCharacterVisual>();
                visual.Build(i, i == 0, false, i > 0 && (i - 1) % 6 == 0);
                visual.UseGuardPoses();
                visual.Play("Idle");
                guards.Add(new GuardVisual { root = root, rig = visual, member = Mathf.Max(0, i - 1) % 6, platoon = i == 0 ? -1 : (i - 1) / 6 });
                root.gameObject.SetActive(false);
            }
        }
        private void UpdateSettlementGuards(float seconds)
        {
            if(defenseMode&&defenseActive)return;
            ActiveGuardCount = 0;
            if (guards.Count == 0) return;
            var site = defenseMode?Model.sites[Model.factions[0].capital]:scoutEncounter.Active?Model.sites[scoutSite]:Model.Nearest(new Vector2(hero.position.x, hero.position.z));
            bool active = companies.ContainsKey(site.id) && (defenseMode||scoutEncounter.Active||Vector2.Distance(site.point, new Vector2(hero.position.x, hero.position.z)) < 190);
            bool changed = guardSite != site.id;
            var layout = LayoutFor(site.id);
            if (active) companies[site.id].UpdateWatch(scoutEncounter.Active ? scoutWatchHour : clockHours);
            if (active && changed)
            {
                guardSite = site.id;
                InitializeMarches(layout);
            }
            if (!active) guardSite = -1;

            var before=new Vector2[guards.Count];for(int i=0;i<guards.Count;i++)before[i]=guards[i].local;
            if(active)
            {
                guards[0].local=layout.Rotate(new Vector2(4,18));
                float remaining=Mathf.Max(0,seconds)*clockRate;
                while(remaining>0){float dt=Mathf.Min(.1f,remaining);TickMarches(layout,dt);remaining-=dt;}
                if(seconds<=0)TickMarches(layout,0);
            }
            for(int i=0;i<guards.Count;i++)
            {
                var g=guards[i];g.root.gameObject.SetActive(active);if(!active)continue;ActiveGuardCount++;
                if(defenseMode&&battleActors.Count>=26&&!battleActors[i+1].fighter.Alive)continue;
                Vector2 at=site.point+g.local;g.root.position=new Vector3(at.x,Surface(at),at.y);
                Vector2 delta=g.local-before[i];bool moving=delta.sqrMagnitude>.000001f;
                if(moving)g.root.rotation=Quaternion.LookRotation(new Vector3(delta.x,0,delta.y));
                else if(i==0||changed)g.root.rotation=Quaternion.Euler(0,layout.Angle+180,0);
                if(i>0&&marches[g.platoon].response&&marches[g.platoon].next>=marches[g.platoon].path.Count)
                {
                    Vector2 forward=layout.Hinterland(Vector2.right);
                    g.root.rotation=Quaternion.LookRotation(new Vector3(forward.x,0,forward.y));
                }
                float visualStep = paused ? 0 : Mathf.Max(0, seconds) * clockRate;
                if(changed) g.visualRotation = g.root.rotation;
                else g.visualRotation = Quaternion.RotateTowards(g.visualRotation, g.root.rotation, 180 * visualStep);
                g.rig.transform.rotation = g.visualRotation;
                bool alert = i > 0 && marches[g.platoon].response;
                g.rig.SetGuardAlertness(Mathf.MoveTowards(g.rig.Alertness, alert ? 1 : 0, visualStep * 2));
                string clip=moving?"Walk":"Idle";if(g.rig.CurrentClip!=clip)g.rig.Play(clip);g.rig.PlaybackSpeed=paused?0:clockRate;
            }
            UpdateResponseGuide();
        }

        public void VisitCommander()
        {
            if (!companies.ContainsKey(selected)) { message = "Select an inhabited settlement to visit its commander."; return; }
            var site = Model.sites[selected]; var layout = LayoutFor(selected);
            Vector2 p = site.point + layout.Rotate(new Vector2(0, 13));
            hero.position = new Vector3(p.x, Surface(p), p.y); yaw = layout.Angle; pitch = 20; zoom = 11; mapOpen = false;
            UpdateSettlementGuards(0); UpdateCamera(true); commanderPanel = true;
            message = companies[selected].Commander.Name + ": Defend settlement. Four platoon commanders organize posts, town patrol, farm patrol and reserve. Relief every six game hours.";
        }
        private void DrawCommanderPanel()
        {
            if (!commanderPanel || mapOpen || guardSite < 0) return;
            Rect r = CommandHudRect;
            Panel(r);
            GUI.Label(new Rect(r.x+12,r.y+7,r.width-24,26), CommanderName + " / COMPANY", subheading);
            GUI.Label(new Rect(r.x+12,r.y+34,r.width-24,20), "DEFEND SETTLEMENT   /   4 PLATOONS", small);
            foreach (var p in ActiveCompany.Platoons)
            {
                float y = r.y + 60 + p.Id * 27;
                string duty = p.Order == AtlasPlatoonOrder.SecureApproach ? "Secure approach" : p.Duty == AtlasGuardDuty.Gate ? "Gate posts" : p.Duty == AtlasGuardDuty.TownPatrol ? "Town patrol" : p.Duty == AtlasGuardDuty.FarmPatrol ? "Farm patrol" : "Reserve";
                if(defenseMode&&defenseActive)duty=p.Id==reservePlatoon?(defenseState.progression.approach==1?"Intercept / reserve":"Defend stores"):"Hold current posts";
                bool response = ResponseGuideHighlighted && p.Id == ResponseGuidePlatoon;
                if(response) { ResponseOutline(new Rect(r.x+9,y-1,r.width-18,25)); duty=ResponseGuideStage; }
                Dot(new Vector2(r.x+16,y+9),3,response ? ResponseColor : new Color(.45f,.75f,.6f));
                GUI.Label(new Rect(r.x+26,y,118,24), "P"+(p.Id+1)+"  "+p.Name, small);
                GUI.Label(new Rect(r.x+149,y,r.width-158,24), duty, small);
            }
            GUI.Label(new Rect(r.x+12,r.y+171,r.width-24,20), "C closes  /  F3 diagnostics", small);
        }

        private void BuildFortCore(AtlasSite site)
        {
            var layout=LayoutFor(site.id);
            foreach(var entry in new[]{("CommandPost",new Vector2(-19,17)),("Barracks",new Vector2(19,17)),("Quartermaster",new Vector2(-19,-17)),("WatchHouse",new Vector2(19,-17))})
            {
                Vector2 p=site.point+layout.Rotate(entry.Item2);
                if(BlocksResidentRoute(site.id,p))continue;
                var rotation=Quaternion.Euler(0,layout.Angle+(entry.Item2.x<0?-90:90),0);
                Vector3 ground=HinterlandGround(p);
                if(Physics.CheckBox(ground+Vector3.up*4,new Vector3(6,4,6),rotation,ObstacleMask))continue;
                var building=art.Object(entry.Item1,footprintRoot,ground);building.rotation=rotation;
                Obstacle(entry.Item1,ground+Vector3.up*4,new Vector3(8.3f,8,7.3f),rotation,footprintRoot);
            }
            // Preview-only drill markers and command standard, removed when another footprint is selected.
            for(int i=0;i<4;i++)
            {
                Vector2 p=site.point+layout.Rotate(new Vector2(i%2==0?-6:6,i<2?-7:7));
                art.Object("CommandStandard",footprintRoot,HinterlandGround(p));
            }
        }
        private void BuildGuardBuildings(AtlasSite site, Material path)
        {
            var layout = LayoutFor(site.id);
            foreach (var entry in new[] { ("WatchHouse", -12f), ("Barracks", 5f), ("Quartermaster", 22f) })
            {
                var p = site.point + layout.Rotate(new Vector2(40, entry.Item2));
                PlaceSolid(entry.Item1, p, layout.Angle + 90);
                BuildLane(site.point + layout.Rotate(new Vector2(32, 0)), site.point + layout.Rotate(new Vector2(32, entry.Item2)), 1.8f, path);
                BuildLane(site.point + layout.Rotate(new Vector2(32, entry.Item2)), site.point + layout.Rotate(new Vector2(35, entry.Item2)), 1.8f, path);
            }
            art.Place("CommandStandard", HinterlandGround(site.point + layout.Rotate(new Vector2(6, 18))), layout.Angle);
        }
    }

    public sealed partial class AtlasArtKit
    {
        private void BuildGuardAssets()
        {
            Make("CommandPost", b => { House(b, 2); b.Beam(new Vector3(0, 7, 0), new Vector3(0, 10, 0), .12f, Timber); b.Box(new Vector3(.65f, 9.3f, 0), new Vector3(1.3f, 1.1f, .05f), new Color(.18f, .33f, .39f)); });
            Make("WatchHouse", b => { House(b, 0); b.Box(new Vector3(0, 3, -3.8f), new Vector3(1, 1, .1f), Iron); foreach (float x in new[] { -.3f, .3f }) b.Beam(new Vector3(x, 2.7f, -3.9f), new Vector3(-x, 3.3f, -3.9f), .09f, Straw); });
            Make("Barracks", b => { House(b, 2); for (int i = 0; i < 4; i++) { b.Beam(new Vector3(-2 + i, .4f, -4.5f), new Vector3(-2 + i, 2.6f, -4.5f), .065f, Timber); b.Oval(new Vector3(-2 + i, 2.65f, -4.5f), new Vector3(.1f, .28f, .07f), Iron); } });
            Make("Quartermaster", b => { House(b, 1); for (int i = 0; i < 3; i++) { b.Box(new Vector3(-2 + i * 1.5f, .45f, -4.5f), new Vector3(1.2f, .9f, .9f), Timber); b.Beam(new Vector3(-2.5f + i * 1.5f, .1f, -5), new Vector3(-1.5f + i * 1.5f, .8f, -5), .08f, Straw); } });
            Make("CommandStandard", b => { b.Beam(Vector3.zero, Vector3.up * 4, .12f, Timber); b.Box(new Vector3(.65f, 3.3f, 0), new Vector3(1.3f, 1.1f, .045f), new Color(.18f, .33f, .39f)); b.Box(new Vector3(.65f, 3.3f, -.035f), new Vector3(.24f, .7f, .025f), Straw); });
        }
    }
}





