using System;
using System.Collections.Generic;
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
        [SerializeField] private bool persistentProgression = true;
        [SerializeField, Min(0)] private int startingExperience;
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
        public int Experience { get; private set; }
        public SurvivorRank Rank => ProgressionRules.Rank(Experience);
        public string ExperienceStatus => Rank == SurvivorRank.Veteran ? $"Veteran · {Experience} XP" :
            $"{Rank} · {Experience}/{ProgressionRules.NextRankAt(Experience)} XP";
        public bool PersistentProgression => persistentProgression;
        public string SessionExperienceSummary { get; private set; } = "No XP earned this encounter.";
        private readonly Dictionary<string, int> sessionAwards = new();
        private bool wasZombie;
        private bool anchored;
        internal bool EngagedThisEncounter { get; set; }

        private void Awake()
        {
            Person = GetComponent<OutbreakAgent>(); Order = initialOrder;
            Anchor = Person.FeetPosition;
            Experience = persistentProgression ? SurvivorProgressionStore.Load(Person.DisplayName) : startingExperience;
            ApplyProgression();
        }

        public void Configure(bool veteran, CombatOrder order)
        {
            startingExperience = veteran ? ProgressionRules.VeteranExperience : 0;
            Experience = startingExperience; ApplyProgression();
            initialOrder = order; Order = order; Energy = attributes.MaximumStamina;
            anchored = false;
        }

        public void ConfigureProgression(bool persistent, int initialExperience = 0)
        {
            persistentProgression = persistent; startingExperience = Mathf.Max(0, initialExperience);
            Experience = persistent ? SurvivorProgressionStore.Load(Person == null ? GetComponent<OutbreakAgent>().DisplayName : Person.DisplayName) : startingExperience;
            ApplyProgression();
        }

        public int AwardExperience(string reason, int amount)
        {
            if (Zombie || amount <= 0 || string.IsNullOrWhiteSpace(reason)) return 0;
            Experience += amount;
            sessionAwards.TryGetValue(reason, out int current); sessionAwards[reason] = current + amount;
            ApplyProgression();
            if (persistentProgression) SurvivorProgressionStore.Save(Person.DisplayName, Experience);
            SessionExperienceSummary = BuildSummary();
            return amount;
        }

        public int AwardObjective(string objectiveId, string reason, int amount)
        {
            if (!persistentProgression || Zombie || amount <= 0 ||
                !SurvivorProgressionStore.CompleteObjective(Person.DisplayName, objectiveId)) return 0;
            return AwardExperience(reason, amount);
        }

        public void ResetProgression()
        {
            Experience = startingExperience; sessionAwards.Clear(); SessionExperienceSummary = "No XP earned this encounter.";
            ApplyProgression();
        }

        private void ApplyProgression()
        {
            attributes = ProgressionRules.Attributes(Experience);
            Energy = Mathf.Min(attributes.MaximumStamina, Energy <= 0 ? attributes.MaximumStamina : Energy);
        }

        private string BuildSummary()
        {
            var parts = new List<string>();
            foreach (var award in sessionAwards) parts.Add($"{award.Key} +{award.Value}");
            return string.Join(" · ", parts);
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
