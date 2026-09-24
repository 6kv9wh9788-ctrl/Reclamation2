using UnityEngine;

namespace Reclamation.Blight
{
    public static class BlightDefenseRules
    {
        // Hysteresis prevents a soldier from alternating attack/rest at one threshold.
        public static bool Recovering(bool alreadyRecovering, float healthFraction, float stamina)
            => healthFraction <= .35f || stamina < (alreadyRecovering ? 65 : 30);
        public static bool Overwhelmed(float visibleThreat, float readySupport)
            => visibleThreat >= 3 && visibleThreat > Mathf.Max(1, readySupport) * 1.75f;
    }

    public sealed partial class BlightCombatLab
    {
        private sealed class DefenseMemory
        {
            public bool recovering;
            public float nextThink;
            public Vector3 goal;
            public Actor covering;
        }
        private float defensePressureTime, nextPressureCheck;
        public bool HoldAtAllCosts { get; private set; }
        public string GetCompanionIntent(Transform root)
        {
            foreach (Actor actor in actors) if (actor.root == root) return actor.intent;
            return "";
        }
        public bool CompanionRecovering(Transform root)
        {
            foreach (Actor actor in actors) if (actor.root == root) return actor.defense.recovering;
            return false;
        }
        public void SetHoldAtAllCosts(bool enabled)
        {
            if (!TacticalScenario || player == null || !player.fighter.Alive || paused) return;
            if (!tacticalLine || fallingBack) SetTacticalOrder(SquadOrder.Hold);
            if (!tacticalLine || fallingBack) return;
            HoldAtAllCosts = enabled; defensePressureTime = 0; nextPressureCheck = simulationTime;
            foreach (Actor actor in actors) if (!actor.enemy && actor != player) actor.defense = new DefenseMemory();
            Say(enabled ? "HOLD AT ALL COSTS: defend the slots; no automatic fallback. Dodges and blocks remain available." :
                "DEFEND: reposition, cover teammates and fall back if sustained pressure overwhelms the area.");
        }

        private bool VisibleTo(Actor observer, Actor other, float range)
            => other != observer && other.fighter.Alive && Vector3.Distance(observer.root.position, other.root.position) <= range &&
                TerrainSight(observer.root.position, other.root.position);

        private Actor RecoveringBuddy(Actor actor)
        {
            foreach (Actor other in actors)
                if (!other.enemy && other != player && VisibleTo(actor, other, 6) &&
                    BlightDefenseRules.Recovering(other.defense.recovering, other.fighter.Health / other.fighter.MaximumHealth, other.fighter.Stamina)) return other;
            return null;
        }

        private Vector3 DefensivePosition(Actor actor, bool recovering)
        {
            Vector3 preferred = actor.anchor - lineForward * (recovering ? 2.1f : 0);
            Vector3 best = actor.root.position; float bestScore = float.NegativeInfinity;
            // Fixed candidate positions stay within the defended area; this is not unlimited kiting.
            for (int i = 0; i < 7; i++)
            {
                Vector3 candidate = i == 0 ? preferred : i == 1 ? actor.anchor :
                    actor.anchor + Quaternion.Euler(0, (i - 2) * 45 - 90, 0) * -lineForward * (recovering ? 2.5f : 1.4f);
                candidate = ClampCompanionGoal(candidate);
                if (Vector3.Dot(candidate - actor.anchor, lineForward) > .6f || !terrain.Clear(candidate, candidate, actor.radius)) continue;
                float score = -Vector3.Distance(candidate, preferred) * .7f - Vector3.Distance(candidate, actor.root.position) * .2f;
                foreach (Actor other in actors)
                {
                    if (!VisibleTo(actor, other, 6)) continue;
                    float distance = Vector3.Distance(candidate, other.root.position);
                    if (other.enemy && TerrainSight(candidate, other.root.position))
                        score -= Mathf.Max(0, (recovering ? 3.2f : 1.7f) - distance) * (other.hulk ? 3 : 2);
                    else if (!other.enemy) score -= Mathf.Max(0, 1.15f - distance) * 5;
                }
                if (score > bestScore) { bestScore = score; best = candidate; }
            }
            return best;
        }

        private bool ControlAdaptiveDefense(Actor actor, float dt)
        {
            DefenseMemory memory = actor.defense;
            // Perception/position choices run at a modest cadence, not every rendering frame.
            if (simulationTime >= memory.nextThink)
            {
                memory.nextThink = simulationTime + .35f;
                memory.recovering = BlightDefenseRules.Recovering(memory.recovering,
                    actor.fighter.Health / actor.fighter.MaximumHealth, actor.fighter.Stamina);
                memory.covering = memory.recovering ? null : RecoveringBuddy(actor);
                memory.goal = DefensivePosition(actor, memory.recovering);
            }
            if (!actor.fighter.CanAct)
            {
                actor.intent = actor.fighter.Action == DuelAction.Dodge ? "Evading" :
                    actor.fighter.Action == DuelAction.Windup ? "Committed strike" : "Recovering action";
                return true;
            }
            actor.fighter.Blocking = false;
            Actor threat = Incoming(actor);
            if (DefendCompanion(actor, threat, dt, true) || !actor.fighter.CanAct) return true;
            string intent = memory.recovering ? (actor.fighter.Health / actor.fighter.MaximumHealth <= .35f ? "Wounded: yielding ground" : "Recovering stamina") :
                memory.covering != null ? "Covering " + (memory.covering.slot == 0 ? "Mara" : "Bren") : "Defending area";
            Actor target = null; float best = float.PositiveInfinity;
            float reach = BlightEquipment.Weapon(actor.weapon, false).Reach;
            foreach (Actor enemy in actors)
            {
                if (!enemy.enemy || !VisibleTo(actor, enemy, reach - .1f) || Vector3.Distance(enemy.root.position, actor.anchor) > 4) continue;
                float score = Vector3.Distance(enemy.root.position, memory.covering == null ? actor.root.position : memory.covering.root.position);
                if (score < best) { best = score; target = enemy; }
            }
            actor.target = target; actor.intent = intent;
            bool displaced = Vector3.Distance(actor.root.position, actor.anchor) > 2.8f;
            bool movingToSafety = memory.recovering || displaced || Vector3.Distance(actor.root.position, memory.goal) > .55f;
            if (movingToSafety)
            {
                MoveCompanion(actor, memory.goal, actor.fighter.Blocking ? 2.2f : 3.2f, dt, .3f, threat == null && target == null);
                if (target != null) Face(actor, target.root.position - actor.root.position);
                // Injured soldiers can fight a nearby opening from their rear position;
                // exhausted soldiers reserve their stamina instead of attacking immediately.
                if (Vector3.Distance(actor.root.position, memory.goal) > .55f || actor.fighter.Stamina < 65 || displaced) return true;
            }
            Face(actor, target == null ? lineForward : target.root.position - actor.root.position);
            if (target == null || threat != null || actor.delay > 0 || actor.fighter.Stamina < BlightEquipment.Weapon(actor.weapon, false).Cost + 25) return true;
            if (memory.recovering && target.fighter.Action != DuelAction.Recovery) return true;
            bool heavy = !memory.recovering && actor.attacks % 3 == 2 && actor.fighter.Stamina >= BlightEquipment.Weapon(actor.weapon, true).Cost + 30;
            if (StartAttack(actor, heavy)) { actor.attacks++; actor.delay = 1.1f; actor.intent = memory.covering != null ? intent : "Defensive strike"; }
            return true;
        }

        private void UpdateDefensePressure()
        {
            if (!TacticalScenario || !tacticalLine || fallingBack || HoldAtAllCosts || Order != SquadOrder.Hold) return;
            if (simulationTime < nextPressureCheck) return;
            nextPressureCheck = simulationTime + .25f;
            bool overwhelmed = false, canFallBack = false;
            foreach (Actor actor in actors)
            {
                if (actor.enemy || actor == player || !actor.fighter.Alive) continue;
                canFallBack |= actor.anchor.z > fallbackPoint.z + 1;
                float pressure = 0, support = actor.fighter.Health / actor.fighter.MaximumHealth > .35f && actor.fighter.Stamina >= 30 ? 1 : .5f;
                foreach (Actor other in actors)
                {
                    if (!VisibleTo(actor, other, 4.5f)) continue;
                    if (other.enemy) pressure += other.hulk ? 2 : 1;
                    else support += other.fighter.Health / other.fighter.MaximumHealth > .35f && other.fighter.Stamina >= 30 ? 1 : .5f;
                }
                overwhelmed |= BlightDefenseRules.Overwhelmed(pressure, support);
            }
            defensePressureTime = overwhelmed ? defensePressureTime + .25f : Mathf.Max(0, defensePressureTime - .5f);
            if (!canFallBack || defensePressureTime < 1.5f) return;
            SetTacticalOrder(SquadOrder.Withdraw);
            foreach (Actor actor in actors) if (!actor.enemy && actor != player && actor.fighter.Alive) actor.intent = "Overwhelmed: falling back";
            Say("Squad overwhelmed: yielding to the fallback line. Move with them, or press 5 to demand a stand.");
        }
    }
}
