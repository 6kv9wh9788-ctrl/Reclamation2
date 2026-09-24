using UnityEngine;

namespace Reclamation.Blight
{
    public enum OutpostStage { Approach, Patrol, Regroup, Stronghold, Cache, PrepareReturn, Rearguard, Returning, Complete }
    public enum OutpostRoute { None, West, East }

    public sealed partial class BlightCombatLab
    {
        private Transform outpostScenery, westRouteMarker, eastRouteMarker, musterMarker;
        private readonly Vector3 westApproach = new Vector3(-8, 0, -12);
        private readonly Vector3 eastApproach = new Vector3(8, 0, -12);
        private readonly Vector3 muster = new Vector3(0, 0, -3);
        public OutpostStage MissionStage { get; private set; }
        public OutpostRoute MissionRoute { get; private set; }
        public int MissionKills { get; private set; }
        public float MissionSeconds { get; private set; }
        public int MissionSurvivors { get; private set; }

        private void ClearOutpost()
        {
            if (outpostScenery != null)
            { outpostScenery.gameObject.SetActive(false); Destroy(outpostScenery.gameObject); }
            outpostScenery = westRouteMarker = eastRouteMarker = musterMarker = null;
            MissionStage = OutpostStage.Approach; MissionRoute = OutpostRoute.None;
            MissionKills = MissionSurvivors = 0; MissionSeconds = 0;
        }

        private void BuildOutpost()
        {
            outpostScenery = Pivot(transform, "Outpost mission scenery", Vector3.zero);
            westRouteMarker = MissionMarker("West approach", westApproach, new Color(.4f, .7f, .5f));
            eastRouteMarker = MissionMarker("East approach", eastApproach, new Color(.8f, .55f, .25f));
            musterMarker = MissionMarker("Outpost rally", muster, new Color(.5f, .65f, .85f));
            musterMarker.gameObject.SetActive(false);
            // Open routes: scenery stays outside the traversable arena. No fake cover or impassable walls.
            Material gravel = Material(new Color(.34f, .32f, .26f));
            for (int side = -1; side <= 1; side += 2)
            {
                Part(outpostScenery, "Approach lane", new Vector3(side * 8, .01f, -7), new Vector3(4, .015f, 19), gravel);
                for (int z = -10; z <= 15; z += 5)
                {
                    Part(outpostScenery, "Ruined house", new Vector3(side * 18.5f, 1.4f, z),
                        new Vector3(2.5f, 2.8f, 3), Material(new Color(.3f, .29f, .25f)));
                    Part(outpostScenery, "Blight growth", new Vector3(side * 16.5f, .9f, z + 1),
                        new Vector3(.5f, 1.8f, .6f), Material(new Color(.3f, .16f, .36f)));
                }
            }
            Part(outpostScenery, "Outpost courtyard", new Vector3(0, .012f, 12), new Vector3(24, .02f, 13), gravel);
            Part(outpostScenery, "Outpost rear wall", new Vector3(0, 1.8f, 22), new Vector3(32, 3.6f, 1),
                Material(new Color(.26f, .26f, .3f)));
            cacheMarker.gameObject.SetActive(false);
        }

        private Transform MissionMarker(string name, Vector3 position, Color color)
        {
            Transform marker = Marker(name, color, 2);
            marker.SetParent(outpostScenery, true); marker.position = position; return marker;
        }

        // Every wave begins through an explicit rally interaction. Living companions must be
        // present, so a held/withdrawing companion cannot have a new wave spawned around them.
        private bool SquadRallied(Vector3 point)
        {
            foreach (Actor actor in actors)
                if (!actor.enemy && actor.fighter.Alive && Vector3.Distance(actor.root.position, point) > 5) return false;
            return true;
        }

        private bool InteractOutpost()
        {
            if (paused || player == null || !player.fighter.CanAct || MissionStage == OutpostStage.Complete) return false;
            Vector3 position = player.root.position;
            bool started = false;
            switch (MissionStage)
            {
                case OutpostStage.Approach:
                    bool west = Vector3.Distance(position, westApproach) <= 2.6f;
                    bool east = Vector3.Distance(position, eastApproach) <= 2.6f;
                    if (!west && !east) break;
                    Vector3 rally = west ? westApproach : eastApproach;
                    if (!SquadRallied(rally)) return RallyReminder();
                    MissionRoute = west ? OutpostRoute.West : OutpostRoute.East;
                    MissionStage = OutpostStage.Patrol; started = true;
                    westRouteMarker.gameObject.SetActive(false); eastRouteMarker.gameObject.SetActive(false);
                    float x = west ? -8 : 8;
                    CreateActor("Approach thrall 1", new Vector3(x - 1.5f, 0, 0), true);
                    CreateActor("Approach thrall 2", new Vector3(x + 1.5f, 0, 2), true);
                    if (east) CreateActor("Approach thrall 3", new Vector3(x, 0, 4), true);
                    Say(west ? "West route: clear the pair, then regroup at the blue rally marker." :
                        "East route: clear the larger patrol, then regroup at the blue rally marker.");
                    break;
                case OutpostStage.Regroup:
                    if (Vector3.Distance(position, muster) > 2.6f) break;
                    if (!SquadRallied(muster)) return RallyReminder();
                    MissionStage = OutpostStage.Stronghold; started = true; musterMarker.gameObject.SetActive(false);
                    CreateActor("Outpost Hulk", new Vector3(0, 0, 12), true, true);
                    CreateActor("Outpost defender 1", new Vector3(-4, 0, 9), true);
                    CreateActor("Outpost defender 2", new Vector3(4, 0, 9), true);
                    cacheMarker.gameObject.SetActive(true);
                    Say("Assault the outpost. Clear the Hulk and both defenders to secure the cache.");
                    break;
                case OutpostStage.Cache:
                    if (Vector3.Distance(position, cache) > 2.6f) break;
                    inventory.Collect(BlightLoot.WardensSpear);
                    MissionStage = OutpostStage.PrepareReturn;
                    equipmentMenu = lootPage = true; scenarioMenu = false;
                    Say("Warden's Spear recovered. B compares gear; equip if desired. F at the cache departs when your squad is ready.");
                    return true;
                case OutpostStage.PrepareReturn:
                    if (Vector3.Distance(position, cache) > 2.6f) break;
                    if (!SquadRallied(cache)) return RallyReminder();
                    MissionStage = OutpostStage.Rearguard; started = true; equipmentMenu = false;
                    cacheMarker.gameObject.SetActive(false);
                    CreateActor("Return thrall 1", new Vector3(-3, 0, -4), true);
                    CreateActor("Return thrall 2", new Vector3(3, 0, -4), true);
                    CreateActor("Return thrall 3", new Vector3(0, 0, -7), true);
                    Say("A rearguard blocks the road home. Clear it, then bring the surviving squad to camp.");
                    break;
                default: break;
            }
            if (started)
            {
                selectedEnemy = NearestEnemy(position, float.MaxValue);
                foreach (Actor actor in actors) Pose(actor);
                return selectedEnemy != null;
            }
            Say(OutpostObjective()); return false;
        }

        private bool RallyReminder()
        { Say("Bring surviving companions within 5 m of this marker. Use 1: Follow and let them catch up."); return false; }

        private void UpdateOutpost(float dt)
        {
            MissionSeconds += dt;
            MissionKills = 0;
            foreach (Actor actor in actors) if (actor.enemy && !actor.fighter.Alive) MissionKills++;
            if (!player.fighter.Alive) return;
            if (LivingEnemies == 0)
            {
                if (MissionStage == OutpostStage.Patrol)
                { MissionStage = OutpostStage.Regroup; musterMarker.gameObject.SetActive(true); Say("Approach clear. Regroup at the blue rally marker, then F to assault."); }
                else if (MissionStage == OutpostStage.Stronghold)
                { MissionStage = OutpostStage.Cache; Say("Outpost secured. Approach the gold cache and press F to recover your reward."); }
                else if (MissionStage == OutpostStage.Rearguard)
                { MissionStage = OutpostStage.Returning; Say("Road clear. Return south to camp with your surviving companions. 4: Withdraw."); }
            }
            bool threatened = CampThreatened();
            foreach (Actor actor in actors)
                if (!actor.enemy && BlightPatrol.CanRecover(Vector3.Distance(actor.root.position, camp), threatened))
                    actor.fighter.Recover(dt);
            if (MissionStage == OutpostStage.Returning && !threatened &&
                Vector3.Distance(player.root.position, camp) <= 4 && SquadRallied(camp))
            {
                MissionStage = OutpostStage.Complete; MissionSurvivors = LivingCompanions + 1;
                equipmentMenu = false;
            }
        }

        private string OutpostObjective()
        {
            switch (MissionStage)
            {
                case OutpostStage.Approach: return "Choose approach + F\nWest: 2 thralls | East: 3\nBring your squad to the marker";
                case OutpostStage.Patrol: return MissionRoute + " approach: clear patrol\nHostiles: " + LivingEnemies;
                case OutpostStage.Regroup: return "Regroup at blue rally + F\n" + DistanceTo(muster) + " m | Bring surviving companions";
                case OutpostStage.Stronghold: return "Clear the outpost defenders\nHostiles: " + LivingEnemies;
                case OutpostStage.Cache: return "Recover cache + F\n" + DistanceTo(cache) + " m to reward";
                case OutpostStage.PrepareReturn: return "B: compare / equip reward\nF at cache: begin return\nBring surviving companions";
                case OutpostStage.Rearguard: return "Clear the return road\nHostiles: " + LivingEnemies;
                case OutpostStage.Returning: return "Return to camp | 4: Withdraw\n" + DistanceTo(camp) + " m | Bring surviving companions";
                default: return "OUTPOST CLEARED | " + MissionRoute + "\nSurvivors: " + MissionSurvivors + "/3 | Kills: " + MissionKills +
                    "\n" + Mathf.FloorToInt(MissionSeconds / 60) + "m " + Mathf.FloorToInt(MissionSeconds % 60) + "s | Spear recovered";
            }
        }

        private string DistanceTo(Vector3 point) => Vector3.Distance(player.root.position, point).ToString("0");

        private void DrawOutpostMarkers()
        {
            if (Scenario != BlightScenario.Outpost) return;
            WorldMarker(camp + Vector3.up, "CAMP — recovery / return");
            if (MissionStage == OutpostStage.Approach)
            {
                WorldMarker(westApproach + Vector3.up, "WEST — 2 thralls | F begin");
                WorldMarker(eastApproach + Vector3.up, "EAST — 3 thralls | F begin");
            }
            if (MissionStage == OutpostStage.Regroup) WorldMarker(muster + Vector3.up, "RALLY — F assault");
            if (cacheMarker.gameObject.activeSelf) WorldMarker(cache + Vector3.up,
                MissionStage == OutpostStage.PrepareReturn ? "F depart when ready | B gear" :
                MissionStage == OutpostStage.Cache ? "REWARD — F collect" : "CACHE — guarded");
        }
    }
}
