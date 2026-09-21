using UnityEngine;

namespace Reclamation.Prototype
{
    public sealed class SurvivorMorale : MonoBehaviour
    {
        public const float PanicThreshold = 20f;
        public const float RecoveryThreshold = 35f;
        public const float RecoveryTarget = 65f;
        public const float ConfidenceThreshold = 80f;

        [SerializeField, Range(0, 100)] private float morale = 70f;

        public float Morale => morale;
        public bool Panicked => morale <= PanicThreshold;
        public bool NeedsRecovery => morale <= RecoveryThreshold;
        public bool Confident => morale >= ConfidenceThreshold;
        public float WorkSpeedMultiplier => Panicked ? 0.65f : NeedsRecovery ? 0.8f : Confident ? 1.1f : 1f;
        public string Status => Panicked ? "Panicked" : NeedsRecovery ? "Low morale" :
            Confident ? "Confident" : "Steady";

        public void Configure(float startingMorale) => morale = Mathf.Clamp(startingMorale, 0, 100);

        public void Advance(float seconds, SurvivorNeeds needs, MedicalCondition medical)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds)) return;
            float changePerMinute = 0;
            if (needs != null)
            {
                changePerMinute += needs.NeedsMeal ? -10 : needs.Hunger <= 40 ? 1 : 0;
                changePerMinute += needs.NeedsSleep ? -12 : needs.Energy >= 60 ? 1 : 0;
            }
            if (medical != null && medical.NeedsTreatment) changePerMinute -= medical.Critical ? 14 : 8;
            morale = Mathf.Clamp(morale + seconds * changePerMinute / 60f, 0, 100);
        }

        public void ApplyEvent(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount)) return;
            morale = Mathf.Clamp(morale + amount, 0, 100);
        }

        public void Recover(float amount)
        {
            if (!(amount > 0) || float.IsInfinity(amount)) return;
            morale = Mathf.Clamp(morale + amount, 0, 100);
        }
    }
}
