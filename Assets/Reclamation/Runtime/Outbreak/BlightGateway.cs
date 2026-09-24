using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        private readonly BlightTerrain terrain = new BlightTerrain();
        private Transform gatewayScenery;
        private bool tacticalLine, fallingBack;
        private Vector3 lineForward = Vector3.forward;
        private readonly Vector3 fallbackPoint = new Vector3(0, 0, -11.5f);
        public bool TacticalScenario => Scenario == BlightScenario.Horde || Scenario == BlightScenario.Gateway;
        public bool FallingBack => fallingBack;
        public Vector3 FallbackPosition => fallbackPoint;
        public bool TerrainSight(Vector3 from, Vector3 to) => terrain.Clear(from, to, .03f, true);
        public bool TerrainPassable(Vector3 from, Vector3 to, float radius) => terrain.Clear(from, to, radius);

        private void ClearGateway()
        {
            terrain.Clear(); tacticalLine = fallingBack = false; HoldAtAllCosts = false; defensePressureTime = nextPressureCheck = 0;
            if (gatewayScenery != null) { gatewayScenery.gameObject.SetActive(false); Destroy(gatewayScenery.gameObject); }
            gatewayScenery = null;
        }
        private void BuildGateway()
        {
            gatewayScenery = Pivot(transform, "Gateway tactics", Vector3.zero);
            if (Scenario == BlightScenario.Gateway)
            {
                AddWall(-12, -1.8f, 0); AddWall(1.8f, 12, 0);
                AddWall(-12, -2.2f, -9); AddWall(2.2f, 12, -9);
            }
            Transform marker = Marker("Fallback line", new Color(.25f, .55f, .9f), 2);
            marker.SetParent(gatewayScenery, true); marker.position = fallbackPoint;
            // Identical starts/orders in both comparisons: only the wall layout differs.
            lineForward = Vector3.forward;
            foreach (Actor actor in actors)
                if (!actor.enemy && actor != player)
                { actor.root.position = new Vector3(actor.slot == 0 ? -.7f : .7f, 0, -2.5f); actor.anchor = actor.root.position; }
            tacticalLine = true; Order = SquadOrder.Hold;
            holdMarker.position = new Vector3(0, 0, -2.5f); holdMarker.gameObject.SetActive(true);
        }
        private void AddWall(float left, float right, float z)
        {
            Rect bounds = new Rect(left, z - .6f, right - left, 1.2f); terrain.Add(bounds);
            Part(gatewayScenery, "Solid ruined wall", new Vector3((left + right) * .5f, 1.2f, z),
                new Vector3(right - left, 2.4f, 1.2f), Material(new Color(.32f, .31f, .34f)));
        }
        private Vector3 RouteGoal(Actor actor, Vector3 goal, ref float stop)
        {
            if (terrain.Count == 0) return goal;
            goal = terrain.NearbyGoal(goal, actor.radius);
            if (terrain.Clear(actor.root.position, goal, actor.radius)) return goal;
            if (simulationTime >= actor.routeUntil || Vector3.Distance(goal, actor.routeGoal) > .8f ||
                Vector3.Distance(actor.root.position, actor.routeWaypoint) < .2f ||
                !terrain.Clear(actor.root.position, actor.routeWaypoint, actor.radius))
            {
                actor.routeGoal = goal; actor.routeWaypoint = terrain.Next(actor.root.position, goal, actor.radius);
                actor.routeUntil = simulationTime + .5f;
            }
            // A clearance corner must be reached, not stopped short of. Otherwise
            // replanning can repeatedly select the same corner inside the stop radius.
            stop = 0; return actor.routeWaypoint;
        }
        private bool SetTacticalOrder(SquadOrder order)
        {
            tacticalLine = order == SquadOrder.Hold || order == SquadOrder.Withdraw;
            fallingBack = order == SquadOrder.Withdraw;
            if (!tacticalLine) return false;
            Vector3 center = fallingBack ? fallbackPoint : player.root.position;
            Vector3 forward = fallingBack ? Vector3.forward : player.root.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            // Validate both slots before changing the order; never anchor a soldier inside a wall.
            foreach (Actor actor in actors)
                if (!actor.enemy && actor != player && actor.fighter.Alive)
                {
                    Vector3 slot = center + right * (actor.slot == 0 ? -.7f : .7f);
                    if (!terrain.Clear(slot, slot, actor.radius) || Mathf.Abs(slot.x) > 14.5f || slot.z < -22.5f || slot.z > 19.5f)
                    { tacticalLine = Order == SquadOrder.Hold || Order == SquadOrder.Withdraw; fallingBack = Order == SquadOrder.Withdraw; Say("Move into open space before placing the line."); return true; }
                }
            Order = order; assaultTarget = null; lineForward = forward;
            HoldAtAllCosts = false; defensePressureTime = nextPressureCheck = 0;
            foreach (Actor actor in actors)
                if (!actor.enemy && actor != player)
                { actor.anchor = center + right * (actor.slot == 0 ? -.7f : .7f); actor.target = null; actor.routeUntil = 0; actor.defense = new DefenseMemory(); }
            holdMarker.position = center; holdMarker.gameObject.SetActive(true);
            Say(fallingBack ? "Fallback: companions retire to the blue line, then hold. Move your hero there too." :
                "Defend this area: reposition, cover and yield ground if overwhelmed. 5 switches to Hold at all costs.");
            return true;
        }
        private bool ControlTacticalCompanion(Actor actor, float dt)
        {
            if (!TacticalScenario || !tacticalLine) return false;
            if (!fallingBack && !HoldAtAllCosts) return ControlAdaptiveDefense(actor, dt);
            if (!actor.fighter.CanAct) { actor.intent = "Committed"; return true; }
            actor.fighter.Blocking = false;
            Actor threat = Incoming(actor);
            if (DefendCompanion(actor, threat, dt, true) || !actor.fighter.CanAct) return true;
            if (Vector3.Distance(actor.root.position, actor.anchor) > .35f)
            {
                actor.target = null; actor.intent = fallingBack ? "Fallback" : "Reform";
                MoveCompanion(actor, actor.anchor, actor.fighter.Blocking ? 2.2f : 3.4f, dt, .15f, threat == null); return true;
            }
            Actor target = null; float nearest = float.MaxValue;
            float reach = BlightEquipment.Weapon(actor.weapon, false).Reach;
            foreach (Actor enemy in actors)
            {
                if (!enemy.enemy || !enemy.fighter.Alive || !TerrainSight(actor.root.position, enemy.root.position)) continue;
                Vector3 delta = enemy.root.position - actor.root.position;
                if (delta.magnitude > reach - .1f || delta.sqrMagnitude >= nearest) continue;
                target = enemy; nearest = delta.sqrMagnitude;
            }
            actor.target = target; actor.intent = "Hold line";
            Face(actor, target == null ? lineForward : target.root.position - actor.root.position);
            if (target == null || threat != null || actor.delay > 0 || actor.fighter.Stamina < 41) return true;
            bool heavy = actor.attacks % 3 == 2 && actor.fighter.Stamina >= BlightEquipment.Weapon(actor.weapon, true).Cost + 25;
            if (StartAttack(actor, heavy)) { actor.attacks++; actor.delay = 1.1f; actor.intent = "Strike"; }
            return true;
        }
        private void UpdateFallback()
        {
            UpdateDefensePressure();
            if (!fallingBack || !TacticalScenario) return;
            foreach (Actor actor in actors)
                if (!actor.enemy && actor != player && actor.fighter.Alive && Vector3.Distance(actor.root.position, actor.anchor) > .4f) return;
            fallingBack = false; Order = SquadOrder.Hold; Say("Surviving companions have reached the fallback line and are holding.");
        }
        private void DrawGatewayMarkers()
        {
            if (!TacticalScenario) return;
            WorldMarker(fallbackPoint + Vector3.up, "FALLBACK LINE — order 4");
            if (Scenario == BlightScenario.Gateway)
            {
                WorldMarker(new Vector3(-13.5f, 1, 0), "WEST SIDE PASSAGE");
                WorldMarker(new Vector3(13.5f, 1, 0), "EAST SIDE PASSAGE");
            }
        }
    }
}
