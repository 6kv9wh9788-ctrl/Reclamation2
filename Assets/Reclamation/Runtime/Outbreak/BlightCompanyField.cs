using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        [SerializeField] private bool expandedCompany;
        public bool ExpandedCompany => (expandedCompany || villageDefense) && Scenario == BlightScenario.Company;
        private float CompanyScale => ExpandedCompany ? 2.2f : 1;
        public Vector3 CompanySitePosition(CompanySite site) => VillageDefense ? VillageSite(site) : CompanyDestination(site) * CompanyScale;
        private sealed class FieldRoute
        {
            public Actor actor;
            public Vector3[] points;
            public int next;
            public bool worker;
            public float work;
        }
        private readonly List<FieldRoute> fieldRoutes = new List<FieldRoute>();
        private int fieldSupplies, fieldReinforcements;
        private float fieldIntelAt = -1;
        private string fieldIntel = "No report on hostile supply activity.";
        public int CompanyReinforcements => fieldReinforcements;
        public string CompanySupplyReport => fieldIntelAt < 0 ? fieldIntel : fieldIntel + " | " + Mathf.FloorToInt(simulationTime - fieldIntelAt) + "s ago";
        public void SetExpandedCompany(bool expanded)
        {
            villageDefense = false; expandedCompany = expanded;
            if (Scenario == BlightScenario.Company) ResetFight();
        }
        private void ClearCompanyField()
        { fieldRoutes.Clear(); fieldSupplies = fieldReinforcements = 0; fieldIntelAt = -1; fieldIntel = "No report on hostile supply activity."; }
        private void ConfigureCompanyGround()
        {
            terrain.Bounds = new Rect(-15 * CompanyScale, -23 * CompanyScale, 30 * CompanyScale, 43 * CompanyScale);
            Transform ground = transform.Find("Training ground"), road = transform.Find("Patrol road");
            if (ground != null) { ground.localScale = new Vector3(42 * CompanyScale, .4f, 58 * CompanyScale); }
            if (road != null) road.localScale = new Vector3(7, .005f, 42 * CompanyScale);
            foreach (Transform child in transform) if (child.name == "Boundary ruin") child.gameObject.SetActive(!ExpandedCompany);
        }
        private void FieldObstacle(string name, Rect footprint, float height, Color color, bool blocksSight = true)
        {
            terrain.Add(footprint, blocksSight);
            Part(companyScenery, name, new Vector3(footprint.center.x, height / 2, footprint.center.y),
                new Vector3(footprint.width, height, footprint.height), Material(color));
        }
        private void BuildCompanyField()
        {
            // The river blocks movement but not vision. The bridge is
            // a clear gap in both the world geometry and the shared navigation model.
            FieldObstacle("River west bank", new Rect(-34, -5, 30, 3), .25f, new Color(.12f, .28f, .34f), false);
            FieldObstacle("River east bank", new Rect(4, -5, 30, 3), .25f, new Color(.12f, .28f, .34f), false);
            Part(companyScenery, "Bridge deck", new Vector3(0, .025f, -3.5f), new Vector3(8, .05f, 5), Material(new Color(.44f, .34f, .22f)));
            FieldObstacle("Abandoned house", new Rect(-18, -22, 6, 7), 4, new Color(.38f, .35f, .3f));
            FieldObstacle("Ruined storehouse", new Rect(12, 1, 5, 6), 3.8f, new Color(.4f, .37f, .31f));
            FieldObstacle("Blight workshop", new Rect(14, 31, 5, 5), 4, new Color(.32f, .2f, .28f));
            FieldObstacle("Boulder west", new Rect(-15, 2, 3, 3), 2.5f, new Color(.32f, .35f, .34f));
            FieldObstacle("Boulder east", new Rect(22, -18, 3, 4), 2.5f, new Color(.32f, .35f, .34f));
            foreach (Vector2 spot in new[] { new Vector2(-25,-30), new Vector2(-22,-25), new Vector2(21,-29), new Vector2(25,-25), new Vector2(-28,3), new Vector2(-25,0), new Vector2(26,7), new Vector2(29,4) })
            {
                FieldObstacle("Tree trunk", new Rect(spot.x - .6f, spot.y - .6f, 1.2f, 1.2f), 4, new Color(.29f, .2f, .13f));
                Part(companyScenery, "Tree canopy (decorative)", new Vector3(spot.x, 4.5f, spot.y), new Vector3(3.6f, 3, 3.6f), Material(new Color(.16f, .27f, .18f)));
            }
            AddFieldRoute("West roaming thrall", new[] { new Vector3(-22,0,24), new Vector3(-25,0,12), new Vector3(-9,0,10) }, false);
            AddFieldRoute("East roaming thrall", new[] { new Vector3(22,0,24), new Vector3(25,0,12), new Vector3(10,0,10) }, false);
            CreateActor("Blight gatherer west", new Vector3(-12,0,31), true);
            CreateActor("Blight gatherer east", new Vector3(12,0,28), true);
            AddFieldRoute("Blight gatherer west", new[] { new Vector3(-21,0,36), new Vector3(-5,0,31) }, true);
            AddFieldRoute("Blight gatherer east", new[] { new Vector3(23,0,28), new Vector3(5,0,31) }, true);
            foreach (Vector3 point in new[] {new Vector3(-21,0,36),new Vector3(23,0,28),new Vector3(0,0,31)})
                Part(companyScenery, "Blight supplies", point + Vector3.up * .5f, new Vector3(1.5f,1,1.5f), Material(new Color(.48f,.2f,.44f)));
            CompanyLog("Expanded region: bridge, ruins and hostile supply routes. Scout before committing.");
        }
        private void AddFieldRoute(string name, Vector3[] points, bool worker)
        {
            Actor actor = actors.Find(a => a.root.name == name);
            fieldRoutes.Add(new FieldRoute { actor = actor, points = points, worker = worker });
        }
        private bool ControlCompanyFieldIdle(Actor actor, float dt)
        {
            if (!ExpandedCompany || VillageDefense) return false;
            FieldRoute route = fieldRoutes.Find(r => r.actor == actor);
            if (route == null) return false;
            Vector3 destination = route.points[route.next];
            actor.intent = route.worker ? "Gathering blight supplies" : "Hostile patrol";
            if (Vector3.Distance(actor.root.position, destination) > .8f)
            { MoveToward(actor, destination, route.worker ? 1.6f : 1.8f, dt, .5f); return true; }
            route.work += dt;
            if (route.work < (route.worker ? 12 : 3)) return true;
            route.work = 0;
            if (route.worker && route.next == 1) fieldSupplies = Mathf.Min(3, fieldSupplies + 1);
            route.next = (route.next + 1) % route.points.Length;
            return true;
        }
        private void UpdateCompanyField(float dt)
        {
            if (!ExpandedCompany || VillageDefense) return;
            // Spawn outside actor iteration, one reinforcement per three deliveries;
            // six total per operation. Removing both gatherers permanently stops growth.
            if (fieldSupplies >= 3 && fieldReinforcements < 6)
            {
                Vector3 spawn = new Vector3(8,0,38);
                bool occupied = actors.Exists(a => a.fighter.Alive && Vector3.Distance(a.root.position,spawn) < 2);
                if (!occupied)
                {
                    fieldSupplies -= 3; fieldReinforcements++;
                    CreateActor("Mustered thrall " + fieldReinforcements, spawn, true);
                    AddFieldRoute("Mustered thrall " + fieldReinforcements, new[] {spawn, new Vector3(-5 + fieldReinforcements,0,27)}, false);
                }
            }
            bool observed = actors.Exists(a => !a.enemy && a.fighter.Alive && Vector3.Distance(a.root.position,new Vector3(0,0,31)) <= 8 && TerrainSight(a.root.position,new Vector3(0,0,31)));
            if (!observed) return;
            int workers = fieldRoutes.FindAll(r => r.worker && r.actor.fighter.Alive).Count;
            string report = "Supply camp: " + fieldSupplies + "/3 deliveries | " + fieldReinforcements + "/6 reinforcements mustered";
            // Do not reveal workers' life states at distant resource nodes.
            if (workers == 0 && fieldRoutes.TrueForAll(r => !r.worker || companyContacts.Exists(c => c.Name == r.actor.root.name && !c.KnownAlive))) report += " | gatherers confirmed defeated";
            if (fieldIntelAt < 0) CompanyLog("Supply camp observed: gatherers fund reinforcements. Interdict their routes.");
            fieldIntel = report; fieldIntelAt = simulationTime;
        }
    }
}
