using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        public Transform GetCompanionTarget(Transform root)
        {
            foreach (Actor actor in actors)
                if (actor.root == root && !actor.enemy && actor != player)
                    return actor.target == null ? null : actor.target.root;
            return null;
        }

        private Actor Incoming(Actor actor)
        {
            Actor soonest = null;
            foreach (Actor other in actors)
                if (other.enemy != actor.enemy && other.fighter.Alive && other.fighter.Action == DuelAction.Windup && TerrainSight(other.root.position, actor.root.position) &&
                    DuelFighter.InReach(other.root.position, other.root.forward, actor.root.position,
                        other.fighter.Strike.Reach + 1, other.fighter.Strike.HalfAngle + 8) &&
                    (soonest == null || other.fighter.Remaining < soonest.fighter.Remaining)) soonest = other;
            return soonest;
        }

        private bool EligibleTarget(Actor actor, Actor target, Vector3 anchor)
            => target != null && target.enemy && target.fighter.Alive && TerrainSight(actor.root.position, target.root.position) &&
                BlightSquadRules.CanEngage(Order, Vector3.Distance(target.root.position, anchor)) &&
                BlightSquadRules.CanEngage(Order, Vector3.Distance(actor.root.position, anchor));

        private Actor CompanionTarget(Actor actor, Vector3 anchor)
        {
            if (Order == SquadOrder.Assault && EligibleTarget(actor, assaultTarget, anchor)) return assaultTarget;
            if (EligibleTarget(actor, actor.target, anchor)) return actor.target;
            Actor best = null; float nearest = float.MaxValue;
            foreach (Actor candidate in actors)
            {
                if (!EligibleTarget(actor, candidate, anchor)) continue;
                float distance = (actor.root.position - candidate.root.position).sqrMagnitude;
                if (distance < nearest) { best = candidate; nearest = distance; }
            }
            return best;
        }

        private bool DefendCompanion(Actor actor, Actor threat, float dt, bool withdrawing)
        {
            if (threat == null || threat.fighter.Progress < .38f) return false;
            Face(actor, threat.root.position - actor.root.position);
            Vector3 away = actor.root.position - threat.root.position; away.y = 0;
            if (away.sqrMagnitude < .001f) away = -threat.root.forward;
            away.Normalize();
            bool smash = threat.fighter.Strike.Kind == BlightAttack.Smash;
            Vector3 preferred = smash ? threat.root.right * (actor.slot == 0 ? -1 : 1) : away;
            if (!DuelFighter.InReach(threat.root.position, threat.root.forward, actor.root.position,
                threat.fighter.Strike.Reach + .3f, threat.fighter.Strike.HalfAngle + 3))
            { actor.intent = "Guard"; return !withdrawing; }
            if (!threat.fighter.Heavy)
            {
                actor.fighter.Blocking = true; actor.intent = withdrawing ? "Withdraw" : "Guard";
                return !withdrawing;
            }
            if (threat.fighter.Remaining <= .24f && TryEscapeDirection(actor, threat, preferred, withdrawing, out Vector3 escape) &&
                actor.fighter.Dodge())
            {
                actor.dodgeDirection = escape; actor.intent = "Evade"; return true;
            }
            // Guard is the fallback if stamina or an obstructed escape prevents
            // dodging. A committed attack is never cancelled to obtain this guard.
            actor.fighter.Blocking = true;
            actor.intent = withdrawing ? "Withdraw" : "Evade";
            if (withdrawing) return false;
            MoveCompanion(actor, actor.root.position + preferred * 2, 2.7f, dt, .05f, false);
            return true;
        }

        private bool TryEscapeDirection(Actor actor, Actor threat, Vector3 preferred, bool withdrawing, out Vector3 result)
        {
            Vector3 away = (actor.root.position - threat.root.position).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, away);
            Vector3[] choices = { preferred, away, side, -side };
            result = Vector3.zero; float best = float.NegativeInfinity;
            foreach (Vector3 candidate in choices)
            {
                if (candidate.sqrMagnitude < .1f) continue;
                Vector3 direction = candidate.normalized; bool clear = true;
                for (int step = 1; step <= 4 && clear; step++)
                {
                    Vector3 point = actor.root.position + direction * (.64f * step);
                    if (!terrain.Bounds.Contains(new Vector2(point.x, point.z))) { clear = false; break; }
                    if (!terrain.Clear(actor.root.position, point, actor.radius)) { clear = false; break; }
                    foreach (Actor other in actors)
                        if (other != actor && other.fighter.Alive &&
                            Vector3.Distance(point, other.root.position) < actor.radius + other.radius + .05f)
                        { clear = false; break; }
                }
                if (!clear) continue;
                Vector3 end = actor.root.position + direction * 2.56f;
                float score = Vector3.Dot(direction, preferred) +
                    (DuelFighter.InReach(threat.root.position, threat.root.forward, end,
                        threat.fighter.Strike.Reach, threat.fighter.Strike.HalfAngle) ? 0 : 4);
                if (withdrawing) score += .25f * (Vector3.Distance(actor.root.position, camp) - Vector3.Distance(end, camp));
                if (score > best) { best = score; result = direction; }
            }
            return result.sqrMagnitude > .1f;
        }

        private void ControlCompanion(Actor actor, float dt)
        {
            if (Founding) { ControlFoundingSoldier(actor, dt); return; }
            if (Scenario == BlightScenario.Company && actor.platoon >= 0) { ControlCompanySoldier(actor, dt); return; }
            if (ControlTacticalCompanion(actor, dt)) return;
            if (!actor.fighter.CanAct)
            {
                actor.intent = actor.fighter.Action == DuelAction.Dodge ? "Evade" :
                    actor.fighter.Action == DuelAction.Windup ? "Strike" : "Recover";
                return;
            }
            actor.fighter.Blocking = false;
            Actor threat = Incoming(actor);
            bool withdraw = Order == SquadOrder.Withdraw;
            if (withdraw) actor.target = null;
            if (Scenario == BlightScenario.Squad || Scenario == BlightScenario.Hulk)
                UpdateSquadRecovery(actor, threat, withdraw);
            if (DefendCompanion(actor, threat, dt, withdraw) || !actor.fighter.CanAct) return;
            if (withdraw)
            {
                actor.intent = "Withdraw";
                MoveCompanion(actor, camp + new Vector3(actor.slot == 0 ? -1.8f : 1.8f, 0, 0), 3.8f, dt, .4f, threat == null);
                return;
            }
            Vector3 anchor = Order == SquadOrder.Hold ? actor.anchor : player.root.position;
            Actor target = CompanionTarget(actor, anchor);
            if (target != actor.target)
            {
                actor.target = target;
                if (target != null)
                {
                    actor.engagementAxis = player.root.position - target.root.position; actor.engagementAxis.y = 0;
                    if (actor.engagementAxis.sqrMagnitude < .01f) actor.engagementAxis = -target.root.forward;
                    actor.engagementAxis.Normalize();
                }
            }
            if ((Scenario == BlightScenario.Squad || Scenario == BlightScenario.Hulk) &&
                ControlSquadRecovery(actor, target, anchor, dt)) return;
            if (target == null)
            {
                actor.intent = Order == SquadOrder.Hold ? "Hold" : "Follow";
                Vector3 goal = Order == SquadOrder.Hold ? actor.anchor :
                    BlightSquadRules.Formation(player.root.position, player.root.forward, actor.slot);
                MoveCompanion(actor, goal, 3.5f, dt, .45f, true); return;
            }
            float reach = BlightEquipment.Weapon(actor.weapon, false).Reach;
            Vector3 lane = Quaternion.Euler(0, actor.slot == 0 ? 40 : -40, 0) * actor.engagementAxis;
            Vector3 station = target.root.position + lane * (reach - .45f);
            float stationLeash = Order == SquadOrder.Hold ? 2.5f : Order == SquadOrder.Follow ? 6.5f : 13.5f;
            station = anchor + Vector3.ClampMagnitude(station - anchor, stationLeash);
            station = ClampCompanionGoal(station);
            Face(actor, target.root.position - actor.root.position);
            bool inReach = DuelFighter.InReach(actor.root.position, actor.root.forward, target.root.position, reach - .1f, 60);
            if (!inReach || Vector3.Distance(actor.root.position, station) > .65f)
            {
                actor.intent = "Flank";
                MoveCompanion(actor, station, 3, dt, .25f, false);
                Face(actor, target.root.position - actor.root.position); return;
            }
            actor.intent = "Ready";
            if (actor.delay > 0) return;
            bool heavy = actor.attacks % 3 == 2;
            if (actor.fighter.Stamina < BlightEquipment.Weapon(actor.weapon, heavy).Cost + 25) heavy = false;
            if (actor.fighter.Stamina < BlightEquipment.Weapon(actor.weapon, heavy).Cost + 25) return;
            if (threat != null)
            {
                AttackSpec planned = BlightEquipment.Weapon(actor.weapon, heavy);
                if (threat.fighter.Remaining < planned.Windup + planned.Recovery + .15f) heavy = false;
                planned = BlightEquipment.Weapon(actor.weapon, heavy);
                if (threat.fighter.Remaining < planned.Windup + planned.Recovery + .15f)
                { actor.intent = "Guard"; return; }
            }
            if (StartAttack(actor, heavy)) { actor.attacks++; actor.delay = 1.1f; actor.intent = "Strike"; }
        }

        // Ordinary Squad/Hulk policy only. Tactical/company defense keeps its
        // existing memory and decisions, and shared fighter/movement rules stay unchanged.
        private void UpdateSquadRecovery(Actor actor, Actor threat, bool withdrawing)
        {
            DefenseMemory memory = actor.defense;
            if (withdrawing)
            { memory.recovering = false; memory.nextThink = 0; return; }
            if (simulationTime < memory.nextThink) return;
            memory.nextThink = simulationTime + .35f;
            bool wasRecovering = memory.recovering;
            memory.recovering = BlightDefenseRules.Recovering(wasRecovering,
                actor.fighter.Health / actor.fighter.MaximumHealth, actor.fighter.Stamina);
            if (!memory.recovering || wasRecovering) return;

            Vector3 anchor = Order == SquadOrder.Hold ? actor.anchor : player.root.position;
            Actor danger = threat ?? CompanionTarget(actor, anchor);
            Vector3 away = danger == null ? Vector3.zero : actor.root.position - danger.root.position;
            away.y = 0;
            if (danger != null && away.sqrMagnitude < .01f) away = -danger.root.forward;
            // Latch one nearby pocket on entry. Repeated decisions must not
            // walk a wounded companion farther and farther away from the fight.
            memory.goal = actor.root.position + away.normalized * 2.1f;
        }

        private bool ControlSquadRecovery(Actor actor, Actor target, Vector3 anchor, float dt)
        {
            DefenseMemory memory = actor.defense;
            if (!memory.recovering) return false;
            float leash = Order == SquadOrder.Hold ? 2.5f : Order == SquadOrder.Follow ? 6.5f : 13.5f;
            Vector3 goal = ClampCompanionGoal(anchor + Vector3.ClampMagnitude(memory.goal - anchor, leash));
            actor.intent = actor.fighter.Health / actor.fighter.MaximumHealth <= .35f
                ? "Wounded: yielding ground" : "Recovering stamina";
            if (Vector3.Distance(actor.root.position, goal) > .3f)
            {
                MoveCompanion(actor, goal, 3.2f, dt, .3f, target == null);
                if (target != null) Face(actor, target.root.position - actor.root.position);
                return true;
            }

            if (target != null) Face(actor, target.root.position - actor.root.position);
            // Wounded soldiers may take a local recovery opening, but never
            // pursue it or spend their last dodge reserve. Exhaustion must recover first.
            AttackSpec light = BlightEquipment.Weapon(actor.weapon, false);
            if (target == null || target.fighter.Action != DuelAction.Recovery || actor.delay > 0 ||
                target.fighter.Remaining < light.Windup + .1f || actor.fighter.Stamina < 65 || Incoming(actor) != null ||
                !DuelFighter.InReach(actor.root.position, actor.root.forward, target.root.position, light.Reach - .1f, light.HalfAngle)) return true;
            if (StartAttack(actor, false))
            { actor.attacks++; actor.delay = 1.1f; actor.intent = "Defensive opening"; }
            return true;
        }

        private void MoveCompanion(Actor actor, Vector3 goal, float speed, float dt, float stop, bool faceMovement, bool stagingTransit = false)
        {
            goal = ClampCompanionGoal(goal);
            Vector3 requestedGoal = goal;
            goal = RouteGoal(actor, goal, ref stop);
            Vector3 delta = goal - actor.root.position; delta.y = 0;
            float distance = delta.magnitude;
            if (distance <= stop)
            {
                actor.navigationStall = Vector3.Distance(actor.root.position, requestedGoal) > 1 ? actor.navigationStall + dt : 0;
                if (actor.navigationStall >= 1.5f) actor.intent = "Route blocked";
                return;
            }
            Vector3 direction = delta / distance, steering = direction;
            foreach (Actor other in actors)
            {
                if (other == actor || !other.fighter.Alive || Scenario == BlightScenario.Company && other == actor.target) continue;
                Vector3 offset = other.root.position - actor.root.position; offset.y = 0;
                float separation = offset.magnitude;
                if (separation < 1.35f && separation > .001f)
                    steering -= offset / separation * ((1.35f - separation) / 1.35f);
                float forward = Vector3.Dot(offset, direction);
                Vector3 sideways = offset - direction * forward;
                float clearance = actor.radius + other.radius + .18f;
                if (forward > 0 && forward < 1.8f && sideways.magnitude < clearance)
                {
                    Vector3 tangent = Vector3.Cross(Vector3.up, direction);
                    float sign = Vector3.Dot(sideways, tangent);
                    // A staging soldier must pass a held friendly line on one side.
                    // Independent left/right tangents can cancel at an impassable gap.
                    float passSide = stagingTransit && !other.enemy ? (actor.slot == 0 ? -1 : 1) :
                        Mathf.Abs(sign) < .05f ? (actor.slot == 0 ? -1 : 1) : -Mathf.Sign(sign);
                    steering += tangent * passSide * 1.2f;
                }
            }
            if (steering.sqrMagnitude < .01f) steering = direction;
            if (faceMovement && !actor.fighter.Blocking) Face(actor, steering);
            float step = Mathf.Min(speed * dt, distance - stop);
            Vector3 moveOffset = steering.normalized * step;
            // Local body avoidance must not steer a valid static route into a bank
            // or wall. Prefer the route segment when the sidestep is obstructed.
            if (!terrain.Clear(actor.root.position, actor.root.position + moveOffset, actor.radius) &&
                terrain.Clear(actor.root.position, actor.root.position + direction * step, actor.radius))
                moveOffset = direction * step;
            Vector3 beforeMove = actor.root.position;
            Move(actor, moveOffset);
            actor.navigationStall = (actor.root.position - beforeMove).sqrMagnitude < .000001f ? actor.navigationStall + dt : 0;
            if (actor.navigationStall >= 1.5f)
            { actor.routeUntil = 0; actor.intent = "Route blocked"; }
        }

        private Vector3 ClampCompanionGoal(Vector3 goal)
            => new Vector3(Mathf.Clamp(goal.x, -15 * CompanyScale, 15 * CompanyScale), 0, Mathf.Clamp(goal.z, -23 * CompanyScale, 20 * CompanyScale));
    }
}
