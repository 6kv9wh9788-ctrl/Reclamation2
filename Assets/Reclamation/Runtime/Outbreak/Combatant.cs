using System;
using UnityEngine;

namespace Reclamation.Outbreak
{
    public enum CombatOrder { SelfDefense, Hold, Disengage }
    public enum ThreatAwareness { Unaware, Suspicious, Alerted }
    public enum CombatAction { Ready, Strike, Shove, Dodge, Lunge, Bite, Grabbed, Stagger, Recover, Sweep, KnockedBack, Reposition }

    [Serializable]
    public sealed class CombatAttributes
    {
        [Range(1, 15)] public int strength = 4, dexterity = 4, agility = 4, speed = 4, intelligence = 4, endurance = 4;
        public static CombatAttributes Civilian() => new CombatAttributes();
        public static CombatAttributes Veteran() => new CombatAttributes
            { strength = 12, dexterity = 9, agility = 9, speed = 8, intelligence = 9, endurance = 8 };
        public float Damage => 10 + strength * 3;
        public float Windup => Mathf.Max(0.2f, 0.65f - dexterity * 0.035f);
        public float MaximumStamina => 60 + endurance * 10;
        public float DodgeCost => Mathf.Max(12, 26 - agility);
        public float EscapeTime => Mathf.Max(0.4f, 1.8f - strength * 0.1f);
    }

    // State only. The director owns decisions and time, so pause/speed have one authority.
    [RequireComponent(typeof(OutbreakAgent))]
    public sealed class Combatant : MonoBehaviour
    {
        [SerializeField] private CombatAttributes attributes = new CombatAttributes();
        [SerializeField] private CombatOrder initialOrder;
        public CombatAttributes Attributes => attributes;
        public OutbreakAgent Person { get; private set; }
        public CombatOrder Order { get; private set; }
        public CombatAction Action { get; internal set; }
        public Combatant Target { get; internal set; }
        public Combatant Grabber { get; internal set; }
        public float Remaining { get; internal set; }
        public float Duration { get; private set; }
        public float Health { get; internal set; } = 100;
        public float Energy { get; internal set; }
        public float ShoveCooldown { get; internal set; }
        public float SweepCooldown { get; internal set; }
        public float StaggerResistanceRemaining { get; internal set; }
        public bool SweepRecovery { get; internal set; }
        public int LastSweepHits { get; internal set; }
        internal Combatant NoticedSweep { get; set; }
        internal int NoticedSweepRevision { get; set; }
        public float SweepReactionRemaining { get; internal set; }
        public float SweepReactionTime => Mathf.Clamp(0.55f - attributes.dexterity * 0.05f, 0.12f, 0.4f);
        public Vector3 SweepForward { get; internal set; }
        public Vector3 PushDirection { get; internal set; }
        public Vector3 MovementGoal { get; internal set; }
        public bool IsBrute => Zombie && Person.Class == ZombieClass.Brute;
        public Vector3 Anchor { get; private set; }
        public bool Handled { get; internal set; }
        internal int Revision { get; private set; }
        public bool Zombie => Person != null && Person.State == InfectionState.Turned;
        public bool Available => isActiveAndEnabled && Person != null && !Person.Isolated &&
            !Person.IsProtected && Person.State != InfectionState.Neutralized;
        public float Progress => Duration > 0 ? Mathf.Clamp01(1 - Remaining / Duration) : 0;
        public string Status { get; internal set; } = "Ready";
        public ThreatAwareness Awareness { get; internal set; }
        private bool wasZombie;
        private bool anchored;

        private void Awake()
        {
            Person = GetComponent<OutbreakAgent>(); Order = initialOrder;
            Anchor = Person.FeetPosition; Energy = attributes.MaximumStamina;
        }

        public void Configure(bool veteran, CombatOrder order)
        {
            attributes = veteran ? CombatAttributes.Veteran() : CombatAttributes.Civilian();
            initialOrder = order; Order = order; Energy = attributes.MaximumStamina;
            anchored = false;
        }

        public void GiveOrder(CombatOrder order)
        {
            Order = order; Anchor = Person.FeetPosition; anchored = true;
            // Orders do not cancel a grab, committed attack, or recovery for free.
            Status = order == CombatOrder.Hold ? "Holding this area" : order == CombatOrder.Disengage ? "Disengaging" : "Self defense";
        }

        internal void SyncState()
        {
            // Defer the initial anchor until all actor Awakes/navigation placement finish.
            if (!anchored) { Anchor = Person.FeetPosition; anchored = true; }
            if (wasZombie == Zombie) return;
            ReleaseGrab();
            if (Grabber != null) Grabber.ReleaseGrab();
            wasZombie = Zombie;
            SweepCooldown = StaggerResistanceRemaining = 0;
            Health = Zombie ? Person.Class == ZombieClass.Brute ? 180 : 80 : 100;
            Begin(CombatAction.Ready, 0); Target = null;
        }

        public bool TrySpend(float amount)
        {
            if (!(amount >= 0) || float.IsInfinity(amount) || Energy < amount) return false;
            Energy -= amount; return true;
        }

        internal void Begin(CombatAction action, float seconds, Combatant target = null)
        {
            Revision++;
            SweepRecovery = false;
            Action = action; Remaining = Duration = seconds; Target = target;
            Status = action.ToString();
        }

        internal void ReleaseGrab()
        {
            if (Target != null && Target.Grabber == this)
            {
                Target.Grabber = null;
                if (Target.Action == CombatAction.Grabbed) Target.Begin(CombatAction.Recover, 0.25f);
            }
        }

        internal void Interrupt(float seconds)
        {
            ReleaseGrab();
            if (Grabber != null) { var source = Grabber; Grabber = null; source.ReleaseGrab(); source.Begin(CombatAction.Recover, 0.8f); }
            Begin(CombatAction.Stagger, seconds);
        }

        internal bool TryStaggerFromHit(float seconds)
        {
            // Damage always lands. Only repeated control is resisted. A teammate must
            // still be able to rescue a bite victim, including during this guard window.
            if (IsBrute && Action != CombatAction.Bite && (StaggerResistanceRemaining > 0 || SweepRecovery)) return false;
            if (Action == CombatAction.Sweep) SweepCooldown = 0; // An interrupted windup can be retried after stagger.
            Interrupt(seconds);
            if (IsBrute) StaggerResistanceRemaining = 2.5f;
            return true;
        }

        private void OnDisable()
        {
            ReleaseGrab();
            if (Grabber != null) { Grabber.ReleaseGrab(); Grabber = null; }
            Handled = false;
        }
    }
}
