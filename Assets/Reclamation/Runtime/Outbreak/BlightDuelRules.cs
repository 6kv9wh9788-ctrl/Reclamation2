using UnityEngine;

namespace Reclamation.Blight
{
    public enum DuelAction { Ready, Windup, Recovery, Dodge, Stagger, Defeated }
    public enum BlightWeapon { Sword, Spear, Axe }
    public enum BlightEnemy { Thrall, Hulk }
    public enum BlightAttack { Light, Heavy, Sweep, Smash }

    public struct AttackSpec
    {
        public float Cost, Windup, Recovery, Reach, HalfAngle, Damage, Stagger;
        public bool Heavy, MultipleTargets;
        public BlightAttack Kind;

        public AttackSpec(BlightAttack kind, float cost, float windup, float recovery,
            float reach, float halfAngle, float damage, bool heavy, bool multiple = false, float stagger = 0.65f)
        {
            Kind = kind; Cost = cost; Windup = windup; Recovery = recovery;
            Reach = reach; HalfAngle = halfAngle; Damage = damage;
            Heavy = heavy; MultipleTargets = multiple;
            Stagger = stagger;
        }
    }

    public static class BlightEquipment
    {
        public static AttackSpec Weapon(BlightWeapon weapon, bool heavy)
        {
            if (weapon == BlightWeapon.Axe)
                return new AttackSpec(heavy ? BlightAttack.Heavy : BlightAttack.Light,
                    heavy ? 42 : 24, heavy ? 1 : 0.5f, heavy ? 1.3f : 0.85f,
                    heavy ? 2.3f : 2.1f, 55, heavy ? 44 : 24, heavy, false, heavy ? 0.95f : 0.65f);
            if (weapon == BlightWeapon.Spear)
                return new AttackSpec(heavy ? BlightAttack.Heavy : BlightAttack.Light,
                    heavy ? 36 : 21, heavy ? 0.85f : 0.38f, heavy ? 1.05f : 0.65f,
                    heavy ? 3.8f : 3.4f, 28, heavy ? 36 : 20, heavy);
            return new AttackSpec(heavy ? BlightAttack.Heavy : BlightAttack.Light,
                heavy ? 30 : 16, heavy ? 0.65f : 0.24f, heavy ? 0.85f : 0.48f,
                heavy ? 2.65f : 2.3f, 65, heavy ? 32 : 18, heavy);
        }

        public static float LimbDamage(BlightWeapon weapon, bool heavy)
            => weapon == BlightWeapon.Axe ? (heavy ? 55 : 30) : (heavy ? 35 : 18);

        public static string Role(BlightWeapon weapon)
            => weapon == BlightWeapon.Axe ? "Axe: limb damage / longer heavy stagger" :
                weapon == BlightWeapon.Spear ? "Spear: long reach / narrow thrust" : "Sword: quick / low stamina cost";

        public static AttackSpec Enemy(BlightEnemy enemy, int sequence)
        {
            if (enemy == BlightEnemy.Hulk)
                return sequence % 2 == 0
                    ? new AttackSpec(BlightAttack.Sweep, 24, 1.35f, 1.2f, 3.6f, 110, 24, true, true)
                    : new AttackSpec(BlightAttack.Smash, 32, 1.6f, 1.5f, 4, 24, 42, true);
            bool heavy = sequence % 3 == 2;
            return new AttackSpec(heavy ? BlightAttack.Heavy : BlightAttack.Light,
                heavy ? 30 : 16, heavy ? 1.25f : 0.85f, heavy ? 0.85f : 0.48f,
                heavy ? 2.65f : 2.3f, 65, heavy ? 30 : 18, heavy);
        }
    }

    // Shared by player, companions, and enemies. Presentation never owns damage or timing.
    public sealed class DuelFighter
    {
        public float MaximumHealth { get; }
        public float Health { get; private set; }
        public float Stamina { get; private set; } = 100;
        public DuelAction Action { get; private set; }
        public float Remaining { get; private set; }
        public float Duration { get; private set; }
        public AttackSpec Strike { get; private set; }
        public bool Heavy => Strike.Heavy;
        public bool Blocking { get; set; }
        public bool Alive => Health > 0;
        public bool CanAct => Alive && Action == DuelAction.Ready;
        public float Progress => Duration > 0 ? Mathf.Clamp01(1 - Remaining / Duration) : 0;
        private readonly bool armouredWindup;
        private int poiseHits;
        private float sprintRecoveryDelay;
        public int AttackSequence { get; private set; }
        public bool SprintRecoveryBlocked => sprintRecoveryDelay > 0;

        public DuelFighter(float maximumHealth = 100, bool armoured = false)
        {
            MaximumHealth = Mathf.Max(1, maximumHealth);
            Health = MaximumHealth; armouredWindup = armoured;
        }

        // Retains the original duel API for its existing tests.
        public bool Attack(bool heavy, bool enemy = false)
        {
            return Attack(enemy ? BlightEquipment.Enemy(BlightEnemy.Thrall, heavy ? 2 : 0)
                : BlightEquipment.Weapon(BlightWeapon.Sword, heavy));
        }

        public bool Attack(AttackSpec strike)
        {
            if (!CanAct || !(strike.Cost >= 0) || !Finite(strike.Cost) ||
                !(strike.Windup > 0) || !Finite(strike.Windup) ||
                !(strike.Recovery > 0) || !Finite(strike.Recovery) ||
                !(strike.Damage >= 0) || !Finite(strike.Damage) || Stamina < strike.Cost) return false;
            AttackSequence++; Stamina -= strike.Cost; Blocking = false; Strike = strike; poiseHits = 0;
            Begin(DuelAction.Windup, strike.Windup); return true;
        }

        public bool Dodge()
        {
            if (!CanAct || Stamina < 25) return false;
            Stamina -= 25; Blocking = false; Begin(DuelAction.Dodge, 0.32f); return true;
        }

        // Sprint shares stamina with attacks, dodges and blocking. No regeneration
        // during sprint or for a short interval after the last sprint step.
        public bool TrySprint(float seconds)
        {
            if (!CanAct || Blocking || !(seconds > 0) || !Finite(seconds)) return false;
            float cost = 18 * seconds;
            if (Stamina < cost) return false;
            Stamina -= cost; sprintRecoveryDelay = .7f; return true;
        }

        // Returns true once at impact. Callers substep to retain recovery and dodge windows.
        public bool Advance(float seconds)
        {
            if (!Alive || !(seconds > 0) || !Finite(seconds)) return false;
            bool sprintRest = sprintRecoveryDelay > 0;
            sprintRecoveryDelay = Mathf.Max(0, sprintRecoveryDelay - seconds);
            if (Action == DuelAction.Ready)
            {
                if (sprintRest) return false;
                Stamina = Mathf.Min(100, Stamina + seconds * (Blocking ? 7 : 24));
                return false;
            }
            Remaining = Mathf.Max(0, Remaining - seconds);
            if (Remaining > 0) return false;
            if (Action == DuelAction.Windup)
            { Begin(DuelAction.Recovery, Strike.Recovery); return true; }
            Begin(DuelAction.Ready, 0); return false;
        }

        public string Receive(float damage, bool frontal, bool heavy)
        {
            if (!Alive) return "Defeated";
            if (!(damage > 0) || !Finite(damage)) return "No damage";
            if (Action == DuelAction.Dodge) return "Dodged";
            if (Blocking && CanAct && frontal)
            {
                float cost = heavy ? 42 : 24;
                if (Stamina >= cost) { Stamina -= cost; return "Blocked"; }
                Stamina = 0; Blocking = false;
                Health = Mathf.Max(0, Health - damage);
                Begin(Alive ? DuelAction.Stagger : DuelAction.Defeated, 0.9f);
                return "Guard broken";
            }
            Health = Mathf.Max(0, Health - damage);
            if (!Alive) { Blocking = false; Begin(DuelAction.Defeated, 0); return "Defeated"; }
            // Two heavy hits within the same Hulk windup break its poise.
            if (heavy && armouredWindup && Action == DuelAction.Windup && ++poiseHits < 2)
                return "Armoured windup";
            if (heavy) { Blocking = false; Begin(DuelAction.Stagger, 0.65f); }
            return "Hit";
        }

        public string ReceiveAttack(AttackSpec strike, bool frontal, float damageScale = 1)
        {
            string result = Receive(strike.Damage * damageScale, frontal, strike.Heavy);
            // Armour, dodges, blocks and guard breaks retain their existing rules.
            if (result == "Hit" && strike.Heavy && Action == DuelAction.Stagger &&
                Finite(strike.Stagger) && strike.Stagger > Remaining)
                Begin(DuelAction.Stagger, strike.Stagger);
            return result;
        }

        public void Interrupt(float seconds)
        {
            if (!Alive || !(seconds > 0) || !Finite(seconds)) return;
            Blocking = false; Begin(DuelAction.Stagger, seconds);
        }

        public void Recover(float seconds)
        {
            if (!CanAct || !(seconds > 0) || !Finite(seconds)) return;
            Health = Mathf.Min(MaximumHealth, Health + seconds * 18);
            if (!SprintRecoveryBlocked) Stamina = Mathf.Min(100, Stamina + seconds * 35);
        }

        public static bool InReach(Vector3 origin, Vector3 forward, Vector3 target,
            float reach, float halfAngle = 65)
        {
            Vector3 delta = target - origin; delta.y = 0;
            return delta.sqrMagnitude <= reach * reach && Vector3.Angle(forward, delta) <= halfAngle;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private void Begin(DuelAction action, float duration)
        { Action = action; Duration = Remaining = duration; }
    }
}
