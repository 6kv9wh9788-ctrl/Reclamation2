using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    public enum FoundingDirective { Accompany, DefendVillage, ScoutRoad, Peacekeep }

    public sealed partial class BlightCombatLab
    {
        public bool Founding => Scenario == BlightScenario.Founding;
        public FoundingVillage Village { get; private set; }
        public FoundingDirective CommanderDirective { get; private set; }
        public Transform FoundingCommander => foundingCommander == null ? null : foundingCommander.root;
        public string CommanderReport { get; private set; }
        public bool ScoutReportReceived { get; private set; }
        private Actor foundingCommander;
        private readonly List<Actor> foundingSoldiers = new List<Actor>();
        private readonly List<Transform> foundingWorkers = new List<Transform>();
        private Transform foundingScenery, foundingStorehouse, foundingWatchpost, foundingRoof, foundingPlatform;
        private bool scoutReturning;
        private float patrolClock;
        private Vector3 VillageCenter => new Vector3(0, 0, -14);
        private Vector3 RoadSurvey => new Vector3(0, 0, 6);
        private void ClearFounding()
        {
            if (foundingScenery) { foundingScenery.gameObject.SetActive(false); Destroy(foundingScenery.gameObject); }
            foundingScenery = foundingStorehouse = foundingWatchpost = null;
            Campaign=null;campaignRaid=false;dressingUsed=rallyPlaced=false;breachSeconds=participationSeconds=0;
            foundingCommander = null; Village = null; foundingSoldiers.Clear(); foundingWorkers.Clear();
        }
        private void BuildFounding()
        {
            ResetFoundingRaid(); Village = new FoundingVillage(); CommanderDirective = FoundingDirective.DefendVillage;
            ScoutReportReceived = scoutReturning = false; patrolClock = 0;
            CommanderReport = "Mara: I am holding the village. Order a road scout, or ask us to accompany you north.";
            camp = VillageCenter; cache = new Vector3(0, 0, 15);
            campMarker.position = camp; campMarker.gameObject.SetActive(true);
            cacheMarker.position = cache; cacheMarker.gameObject.SetActive(true);
            player.root.position = camp + Vector3.back * 2; player.spawn = player.root.position;
            player.root.name = "Player - founder";
            foundingScenery = Pivot(transform, "Founding village", Vector3.zero);
            void House(string name, Vector3 point, Vector3 size)
            {
                terrain.Add(new Rect(point.x - size.x / 2, point.z - size.z / 2, size.x, size.z));
                Part(foundingScenery, name, point + Vector3.up * size.y / 2, size, Material(new Color(.48f, .37f, .25f)));
                Part(foundingScenery, name + " roof", point + Vector3.up * (size.y + .2f), new Vector3(size.x + .4f, .4f, size.z + .4f), Material(new Color(.24f, .28f, .23f)));
            }
            House("West village home", new Vector3(-10, 0, -15), new Vector3(4, 3, 5));
            House("East village home", new Vector3(10, 0, -15), new Vector3(4, 3, 5));
            House("Council workshop", new Vector3(-10, 0, -7), new Vector3(4, 2.7f, 4));
            terrain.Add(new Rect(-.8f, -14.35f, 1.6f, .7f));
            Part(foundingScenery, "Council workbench", new Vector3(0, .5f, -14), new Vector3(1.6f, 1, .7f), Material(new Color(.46f, .29f, .14f)));
            // Construction sites reserve space up front; completing a building never traps actors.
            terrain.Add(new Rect(8, -9, 4, 4));
            foundingStorehouse = Part(foundingScenery, "Storehouse construction", new Vector3(10, .12f, -7), new Vector3(4, .24f, 4), Material(new Color(.62f, .45f, .22f)));
            terrain.Add(new Rect(7, -1, 2, 2));
            foundingWatchpost = Part(foundingScenery, "Watchpost foundation", new Vector3(8, .1f, 0), new Vector3(2, .2f, 2), Material(new Color(.35f, .4f, .5f)));
            foundingRoof = Part(foundingScenery, "Storehouse roof", new Vector3(10, 3.45f, -7), new Vector3(4.5f, .5f, 4.5f), Material(new Color(.25f, .32f, .28f)));
            foundingRoof.gameObject.SetActive(false);
            foundingPlatform = Part(foundingScenery, "Watch platform", new Vector3(8, 4.1f, 0), new Vector3(3, .3f, 3), Material(new Color(.3f, .25f, .19f)));
            foundingPlatform.gameObject.SetActive(false);
            for (int i = 0; i < 8; i++)
            {
                Actor soldier = CreateActor(i == 0 ? "Mara - platoon commander" : "Mara's soldier " + i, camp + new Vector3((i % 4 - 1.5f) * 1.7f, 0, 3 + i / 4 * 1.7f), false);
                soldier.slot = i; InstallHumanVisual(soldier, i == 0 ? BlightHumanLook.Mara : BlightHumanLook.Bren);
                foundingSoldiers.Add(soldier); if (i == 0) foundingCommander = soldier;
            }
            for (int i = 0; i < 2; i++)
            {
                var worker = Pivot(foundingScenery, "Civilian " + (i + 1), new Vector3(-4 - i * 1.2f, 0, -14));
                Part(worker, "Clothes", Vector3.up, new Vector3(.5f, 1.4f, .4f), Material(new Color(.65f, .5f, .23f)));
                Part(worker, "Head", Vector3.up * 1.9f, Vector3.one * .35f, Material(new Color(.65f, .49f, .35f)));
                foundingWorkers.Add(worker);
            }
            CreateActor("Road raider 1", new Vector3(-2, 0, 11), true);
            CreateActor("Road raider 2", new Vector3(2, 0, 12), true);
            CreateActor("Supply raider", new Vector3(0, 0, 15), true);
            BuildVillageLife(); ResetDefense();
            if(DefenseLoopLaunch)PrepareDefenseCampaign();
        }
        public bool IssueFoundingDirective(FoundingDirective directive)
        {
            if (!Founding || paused || !player.fighter.Alive || foundingCommander == null || !foundingCommander.fighter.Alive ||
                Vector3.Distance(player.root.position, foundingCommander.root.position) > 12 || (int)directive < 0 || (int)directive > 3) return false;
            if(campaignRaid && directive != FoundingDirective.DefendVillage)return false;
            if (directive == FoundingDirective.Peacekeep && !Village.PatrolDoctrine) return false;
            CommanderDirective = directive; scoutReturning = false; RefreshMaraAssignments(); ReviewDefense();
            foreach (var soldier in foundingSoldiers) soldier.target = null;
            CommanderReport = directive == FoundingDirective.ScoutRoad ? "Mara: Two soldiers will scout the north road and return. The rest will guard the village." :
                directive == FoundingDirective.Accompany ? "Mara: Follow me, platoon. We will accompany you and engage nearby threats." :
                directive == FoundingDirective.DefendVillage ? "Mara: I will establish a garrison and reserve, then schedule patrols as our doctrine allows." : "Mara: A pair will patrol the village approaches. The rest remain on guard.";
            return true;
        }
        private Vector3 FoundingAssignment(Actor actor)
        {
            bool pair = DutyPair(actor);
            if (actor.fighter.Health <= SoldierReliefHealth) return VillageCenter + new Vector3(-4 + actor.slot % 3 * 1.4f, 0, -4);
            if (CommanderDirective == FoundingDirective.DefendVillage && foundingCommander.fighter.Alive)
                return DelegatedDefenseGoal(actor);
            if (foundingCommander != null && foundingCommander.fighter.Alive)
            {
                if (CommanderDirective == FoundingDirective.Accompany)
                    return actor == foundingCommander ? player.root.position - player.root.forward * 2 + player.root.right * 1.6f :
                        foundingCommander.root.position + new Vector3((actor.slot % 4 - 1.5f) * 1.6f, 0, -2 - actor.slot / 4 * 1.5f);
                if (pair && CommanderDirective == FoundingDirective.ScoutRoad && !scoutReturning)
                    return RoadSurvey + Vector3.right * (actor.slot == dutyFirst ? -1 : 1);
                if (pair && CommanderDirective == FoundingDirective.Peacekeep)
                    return new Vector3(Mathf.Sin(patrolClock * .12f + (actor.slot == dutyFirst ? 0 : Mathf.PI)) * 6, 0, -6 + Mathf.Cos(patrolClock * .12f) * 3);
            }
            return VillageCenter + new Vector3((actor.slot % 4 - 1.5f) * 1.7f, 0, 3 + actor.slot / 4 * 1.7f);
        }
        private void ControlFoundingSoldier(Actor actor, float dt)
        {
            if (!actor.fighter.CanAct) return;
            actor.fighter.Blocking = false;
            Vector3 goal = FoundingAssignment(actor);
            bool scouting = CommanderDirective == FoundingDirective.ScoutRoad && DutyPair(actor);
            Actor threat = Incoming(actor);
            if (DefendCompanion(actor, threat, dt, scouting) || !actor.fighter.CanAct) return;
            Actor target = null; float best = 6;
            foreach (Actor enemy in actors)
            {
                if (!enemy.enemy || !enemy.fighter.Alive || !TerrainSight(actor.root.position, enemy.root.position)) continue;
                float distance = Vector3.Distance(actor.root.position, enemy.root.position);
                if (distance < best && Vector3.Distance(enemy.root.position, goal) < 7) { target = enemy; best = distance; }
            }
            actor.target = target;
            if (target == null || scouting)
            { actor.intent = scouting ? "Scout / return" : "Assigned post"; MoveCompanion(actor, goal, 3.5f, dt, .5f, true); return; }
            Face(actor, target.root.position - actor.root.position);
            if (!DuelFighter.InReach(actor.root.position, actor.root.forward, target.root.position, 2.1f, 65))
            { MoveCompanion(actor, target.root.position, 3, dt, 1.65f, false); return; }
            actor.intent = "Defend assignment";
            if (actor.delay <= 0 && actor.fighter.Stamina >= 41 && StartAttack(actor, false)) { actor.attacks++; actor.delay = .8f; }
        }
        private void UpdateFounding(float dt)
        {
            Village.Tick(dt); patrolClock += dt; UpdateFoundingRaid(dt);
            RefreshFoundingBuildings(); UpdateVillageLife(dt); UpdateDefense();
            if (CommanderDirective == FoundingDirective.ScoutRoad && !scoutReturning)
            {
                foreach (var scout in foundingSoldiers)
                {
                    if (!DutyPair(scout) || !scout.fighter.Alive || Vector3.Distance(scout.root.position, RoadSurvey) > 3) continue;
                    int contacts = 0;
                    foreach (var enemy in actors) if (enemy.enemy && enemy.fighter.Alive && Vector3.Distance(scout.root.position, enemy.root.position) <= 12 && TerrainSight(scout.root.position, enemy.root.position)) contacts++;
                    scoutReturning = ScoutReportReceived = true;
                    CommanderReport = "Mara: Scout reports " + contacts + " hostiles near the north road. Last observed at " + simulationTime.ToString("0") + "s; the pair is returning.";
                    break;
                }
            }
            if (!foundingCommander.fighter.Alive) CommanderReport = "Mara has fallen. Survivors hold village posts; you can still complete the supply operation.";
        }
        private bool InteractFounding()
        {
            if (paused || !player.fighter.CanAct) return false;
            if (Vector3.Distance(player.root.position, cache) <= 2.6f && LivingEnemies == 0 && Village.RecoverSupplies())
            { cacheMarker.gameObject.SetActive(false); CommanderReport = "Supplies recovered. Carry them south to the blue village circle and press F."; return true; }
            if (Vector3.Distance(player.root.position, VillageCenter) <= 3 && Village.DeliverSupplies())
            { CommanderReport = "Supplies delivered: 6 timber and 2 insight. Commission research at the council bench."; return true; }
            return false;
        }
        private bool AtFoundingCouncil => Founding && !paused && player.fighter.CanAct && Vector3.Distance(player.root.position, VillageCenter) <= 4;
        public bool ResearchFounding(FoundingVillage.Technology technology) => AtFoundingCouncil && Village.StartResearch(technology);
        public bool ConstructFounding(bool military) => AtFoundingCouncil && (military ? Village.BuildWatchpost() : Village.StartStorehouse());
    }
}
