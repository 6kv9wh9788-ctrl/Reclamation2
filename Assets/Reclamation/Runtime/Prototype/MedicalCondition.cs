using UnityEngine;

namespace Reclamation.Prototype
{
    public sealed class MedicalCondition : MonoBehaviour
    {
        public const float TreatmentThreshold = 25f;
        public const float CriticalThreshold = 70f;
        public const float TreatmentRelief = 65f;

        [SerializeField, Range(0, 100)] private float injury;

        public float Injury => injury;
        public bool NeedsTreatment => injury >= TreatmentThreshold;
        public bool Critical => injury >= CriticalThreshold;
        public string Status => Critical ? "Critical injury" : NeedsTreatment ? "Injured" :
            injury > 0 ? "Recovering" : "Healthy";

        public void Configure(float startingInjury) => injury = Mathf.Clamp(startingInjury, 0, 100);

        public void ApplyTrauma(float amount)
        {
            if (!(amount > 0) || float.IsInfinity(amount)) return;
            injury = Mathf.Clamp(injury + amount, 0, 100);
        }

        public bool Treat(float threshold = TreatmentThreshold)
        {
            if (injury < Mathf.Max(0.01f, threshold)) return false;
            injury = Mathf.Max(0, injury - TreatmentRelief);
            return true;
        }
    }
}
