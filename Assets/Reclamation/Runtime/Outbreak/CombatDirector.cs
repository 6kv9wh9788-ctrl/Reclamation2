using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Outbreak
{
    // Opt-in component on the Outbreak Director. No independent Update or simulation clock.
    public sealed class CombatDirector : MonoBehaviour
    {
        [SerializeField] private bool requiresWitness = true;
        public bool RequiresWitness => requiresWitness;
        public void ConfigureAwareness(bool value) => requiresWitness = value;
        public bool RecognizesThreat(OutbreakAgent observer, OutbreakAgent other) => !requiresWitness ||
            observer.State == InfectionState.Turned ||
            (observer.GetComponent<Combatant>() != null && observer.GetComponent<Combatant>().Awareness == ThreatAwareness.Alerted);
        private readonly List<Combatant> fighters = new();
        private readonly List<int> revisions = new();
        public string LastEvent { get; private set; } = "Combat enabled: zombies must land a bite to infect.";
        private PerimeterDefense perimeter;
        private float speed;
        private double minute;

        public void Tick(IReadOnlyList<OutbreakAgent> people, float seconds, float multiplier, double now,
            PerimeterDefense walls = null)
        {
            if (!isActiveAndEnabled || !(seconds > 0) || float.IsInfinity(seconds) ||
                !(multiplier > 0) || float.IsInfinity(multiplier)) return;
            speed = multiplier; minute = now; perimeter = walls;
            fighters.Clear(); revisions.Clear();
            foreach (var person in people)
            {
                if (person == null) continue;
                var fighter = person.GetComponent<Combatant>();
                if (fighter == null) continue;
                fighter.Handled = false; fighter.SyncState(); fighters.Add(fighter); revisions.Add(fighter.Revision);
            }
            // Advance committed actions before making new decisions. Newly created actions
            // never resolve on their starting frame, regardless of population ordering.
            foreach (var fighter in fighters) Observe(fighter);
            for (int i = 0; i < fighters.Count; i++)
                if (fighters[i].Revision == revisions[i]) Advance(fighters[i], seconds);
            foreach (var fighter in fighters)
                if (fighter.Available && fighter.Action == CombatAction.Ready) Decide(fighter);
        }

        private bool Clear(Combatant a, Combatant b, float range)
        {
            return a != null && b != null && a.Available && b.Available &&
                Vector3.Distance(a.Person.FeetPosition, b.Person.FeetPosition) <= range &&
                a.Person.ClearCombatLine(b.Person.FeetPosition) &&
                (perimeter == null || !perimeter.BlocksContact(a.Person.FeetPosition + Vector3.up,
                    b.Person.FeetPosition + Vector3.up));
        }

        private void Observe(Combatant observer)
        {
            if (!observer.Available || observer.Zombie) return;
            if (!requiresWitness) { observer.Awareness = ThreatAwareness.Alerted; return; }
            if (observer.Awareness == ThreatAwareness.Alerted) return;
            foreach (var other in fighters)
            {
                if (other == observer || !Clear(observer, other, 8)) continue;
                if (other.Zombie && (other.Action == CombatAction.Lunge || other.Action == CombatAction.Bite))
                { observer.Awareness = ThreatAwareness.Alerted; return; }
                if (other.Person.VisibleSymptoms || other.Person.IsWithdrawing)
                    observer.Awareness = ThreatAwareness.Suspicious;
            }
        }

        private void WitnessAttack(Combatant attacker, Combatant victim)
        {
            victim.Awareness = ThreatAwareness.Alerted;
            foreach (var observer in fighters)
                if (!observer.Zombie && Clear(observer, attacker, 8)) observer.Awareness = ThreatAwareness.Alerted;
            LastEvent = $"{attacker.Person.DisplayName} lunged! Nearby witnesses recognize the threat.";
        }

        private void Advance(Combatant f, float dt)
        {
            if (!f.Available)
            {
                f.ReleaseGrab();
                if (f.Grabber != null) f.Grabber.ReleaseGrab();
                f.Begin(CombatAction.Ready, 0); return;
            }
            f.ShoveCooldown = Mathf.Max(0, f.ShoveCooldown - dt);
            if (!f.Zombie && f.Action != CombatAction.Grabbed)
                f.Energy = Mathf.Min(f.Attributes.MaximumStamina, f.Energy + dt * (f.Action == CombatAction.Ready ? 8 : 3));
            if (f.Action == CombatAction.Ready) return;
            f.Handled = true;
            if (f.Action != CombatAction.Dodge) f.Person.CombatStop(f.Status, f.Target == null ? null : f.Target.transform);
            if (f.Action == CombatAction.Bite && (f.Target == null || f.Target.Grabber != f || !Clear(f, f.Target, 1.9f)))
            { f.ReleaseGrab(); f.Begin(CombatAction.Recover, 0.6f); return; }
            if (f.Action == CombatAction.Grabbed)
            {
                if (f.Grabber == null || !f.Grabber.Available)
                { f.Grabber = null; f.Begin(CombatAction.Recover, 0.25f); return; }
                f.Remaining -= dt;
                if (f.Remaining <= 0 && f.Health > 0 && f.Person.CanFlee && f.TrySpend(35))
                {
                    var source = f.Grabber; source.Interrupt(1);
                    f.Begin(CombatAction.Recover, 0.3f);
                    LastEvent = $"{f.Person.DisplayName} broke free!";
                }
                f.Status = "Breaking grab"; return;
            }
            f.Remaining -= dt;
            if (f.Remaining > 0) return;
            var target = f.Target;
            switch (f.Action)
            {
                case CombatAction.Strike:
                    if (Clear(f, target, 1.8f)) HitZombie(f, target, f.Attributes.Damage, 0.65f);
                    f.Begin(CombatAction.Recover, 0.5f); break;
                case CombatAction.Shove:
                    foreach (var enemy in fighters)
                        if (enemy.Zombie && Clear(f, enemy, 1.9f)) HitZombie(f, enemy, 4, 1.2f);
                    f.Begin(CombatAction.Recover, 0.45f); break;
                case CombatAction.Lunge:
                    if (Clear(f, target, 1.65f) && target.Grabber == null)
                    {
                        // Dodges are movement, not invulnerability; a cornered dodger can be caught.
                        f.Begin(CombatAction.Bite, 1.2f, target);
                        target.Begin(CombatAction.Grabbed, target.Attributes.EscapeTime, f);
                        target.Grabber = f; target.Handled = true;
                        target.Person.CombatStop("Grabbed!", f.transform);
                        LastEvent = $"{target.Person.DisplayName} grabbed — interrupt the bite!";
                    }
                    else f.Begin(CombatAction.Recover, 0.8f);
                    break;
                case CombatAction.Bite:
                    if (target != null && target.Grabber == f && Clear(f, target, 1.9f))
                    {
                        target.Health = Mathf.Max(0, target.Health - 25);
                        if (target.Person.Infectable) target.Person.Expose(minute, 12, 15);
                        LastEvent = $"BITE! {f.Person.DisplayName} bit {target.Person.DisplayName}.";
                    }
                    f.ReleaseGrab(); f.Begin(CombatAction.Recover, 1); break;
                default: f.Begin(CombatAction.Ready, 0); break;
            }
        }

        private void HitZombie(Combatant source, Combatant target, float damage, float stagger)
        {
            if (!target.Zombie) return;
            bool rescue = target.Action == CombatAction.Bite;
            target.Health = Mathf.Max(0, target.Health - damage);
            target.Interrupt(stagger); target.Handled = true;
            target.Person.CombatStop("Staggered", source.transform);
            if (target.Health <= 0)
            { target.Person.Neutralize(); LastEvent = $"{source.Person.DisplayName} defeated {target.Person.DisplayName}."; }
            else if (rescue) LastEvent = $"{source.Person.DisplayName} interrupted a bite!";
        }

        private Combatant Nearest(Combatant f, float radius)
        {
            Combatant best = null; float score = float.MaxValue;
            foreach (var other in fighters)
            {
                if (!other.Available || other.Zombie == f.Zombie || other == f) continue;
                if (!f.Zombie && !RecognizesThreat(f.Person, other.Person)) continue;
                float distance = Vector3.Distance(f.Person.FeetPosition, other.Person.FeetPosition);
                if (distance > radius || !f.Person.CanReachPoint(other.Person.FeetPosition)) continue;
                // Rescue an ally before chasing a slightly closer unoccupied zombie.
                float rank = distance - (!f.Zombie && other.Action == CombatAction.Bite ? 3 : 0);
                if (rank < score) { score = rank; best = other; }
            }
            return best;
        }

        private void Decide(Combatant f)
        {
            var enemy = Nearest(f, f.Zombie ? 2 : 5 + f.Attributes.intelligence * 0.3f);
            if (f.Zombie)
            {
                if (Clear(f, enemy, 1.65f)) Start(f, CombatAction.Lunge, 0.7f, enemy);
                return; // Long-range pursuit and perimeter siege stay with the outbreak AI.
            }
            if (f.Health <= 0)
            { f.Handled = true; f.Person.CombatStop("Downed; awaiting infection progression", null); return; }
            if (!f.Person.CanFlee) return;
            if (requiresWitness && f.Awareness != ThreatAwareness.Alerted)
            {
                foreach (var stranger in fighters)
                    if (stranger != f && (stranger.Person.VisibleSymptoms || stranger.Person.IsWithdrawing) && Clear(f, stranger, 2.5f))
                    {
                        f.Handled = true; f.Status = "Keeping distance; uncertain";
                        f.Person.FleeFrom(stranger.transform.position, speed); return;
                    }
            }
            if (requiresWitness && enemy == null && f.Order == CombatOrder.SelfDefense && f.Person.ShouldWithdraw(minute)) return;
            // Evacuation remains a movement order; defend only when enemies are close.
            if (f.Person.ZoneAssignment == RefugeAssignment.Evacuating && enemy == null) return;
            if (f.Order == CombatOrder.Disengage)
            {
                f.Handled = true;
                if (enemy != null) f.Person.FleeFrom(enemy.transform.position, speed);
                else f.Person.CombatStop("Disengaged; awaiting orders", null);
                f.Status = "Disengaging"; return;
            }
            if (enemy == null)
            {
                if (f.Order == CombatOrder.Hold)
                {
                    f.Handled = true; f.Status = "Holding area";
                    f.Person.CombatMove(f.Anchor, speed, 2.5f, 0.3f, f.Status);
                }
                return;
            }
            f.Handled = true;
            if (f.Order == CombatOrder.Hold && Vector3.Distance(f.Anchor, enemy.Person.FeetPosition) > 4)
            { f.Status = "Returning to hold area"; f.Person.CombatMove(f.Anchor, speed, 2.5f, 0.3f, f.Status); return; }
            if (f.Energy < 14)
            { f.Status = "Retreating — exhausted"; f.Person.FleeFrom(enemy.transform.position, speed); return; }
            if (enemy.Action == CombatAction.Lunge && enemy.Target == f && f.Energy >= f.Attributes.DodgeCost)
            {
                Vector3 away = f.Person.FeetPosition - enemy.Person.FeetPosition; away.y = 0;
                if (away.sqrMagnitude < 0.01f) away = Vector3.back;
                Vector3 side = Vector3.Cross(Vector3.up, away.normalized);
                foreach (float sign in new[] { 1f, -1f })
                {
                    Vector3 point = f.Person.FeetPosition + away.normalized * 0.9f + side * sign * 1.1f;
                    if (!f.Person.ClearCombatLine(point)) continue;
                    if (!f.Person.CombatMove(point, speed, 4 + f.Attributes.agility * 0.15f, 0.1f, "Dodging")) continue;
                    f.TrySpend(f.Attributes.DodgeCost); f.Begin(CombatAction.Dodge, 0.4f); return;
                }
            }
            int close = 0;
            foreach (var other in fighters) if (other.Zombie && Clear(f, other, 1.9f)) close++;
            if (close >= 2 && f.ShoveCooldown <= 0 && f.TrySpend(22))
            { f.ShoveCooldown = 4; Start(f, CombatAction.Shove, 0.25f, enemy); return; }
            if (Clear(f, enemy, 1.6f) && f.TrySpend(14))
            { Start(f, CombatAction.Strike, f.Attributes.Windup, enemy); return; }
            f.Status = enemy.Action == CombatAction.Bite ? "Moving to rescue ally" : "Closing to melee";
            f.Person.CombatMove(enemy.Person.FeetPosition, speed, 2.4f + f.Attributes.speed * 0.1f, 1.2f, f.Status);
        }

        private void Start(Combatant f, CombatAction action, float duration, Combatant target)
        {
            f.Begin(action, duration, target); f.Handled = true;
            if (action == CombatAction.Lunge) WitnessAttack(f, target);
            f.Person.CombatStop(action.ToString(), target.transform);
        }

        private void OnDisable()
        {
            foreach (var fighter in fighters)
                if (fighter != null) { fighter.ReleaseGrab(); fighter.Begin(CombatAction.Ready, 0); fighter.Handled = false; }
        }
    }
}
