using UnityEngine;

namespace Reclamation.Prototype
{
    public enum SettlementFocus { Balanced, Food, BuildDefense, Supplies, Recovery }
    public enum SettlementJob { Food, BuildDefense, Supplies }

    public sealed class SettlementPolicy : MonoBehaviour
    {
        [SerializeField] private SettlementFocus focus;

        public SettlementFocus Focus => focus;
        public int Revision { get; private set; }
        public float MealThreshold => focus == SettlementFocus.Food ? 55f : SurvivorNeeds.UrgentHunger;
        public float RestThreshold => focus == SettlementFocus.Recovery ? 55f : SurvivorNeeds.UrgentEnergy;
        public float TreatmentThreshold => focus == SettlementFocus.Recovery ? 10f : MedicalCondition.TreatmentThreshold;
        public float MoraleThreshold => focus == SettlementFocus.Recovery ? 55f : SurvivorMorale.RecoveryThreshold;
        public string Label => focus == SettlementFocus.BuildDefense ? "BUILD / DEFENSE" : focus.ToString().ToUpperInvariant();

        public void SetFocus(SettlementFocus value)
        {
            if (focus == value) return;
            focus = value;
            Revision++;
        }

        public int Priority(SettlementJob job)
        {
            return focus switch
            {
                SettlementFocus.Food => job == SettlementJob.Food ? 100 :
                    job == SettlementJob.BuildDefense ? 40 : 20,
                SettlementFocus.BuildDefense => job == SettlementJob.BuildDefense ? 100 :
                    job == SettlementJob.Supplies ? 60 : 30,
                SettlementFocus.Supplies => job == SettlementJob.Supplies ? 100 :
                    job == SettlementJob.BuildDefense ? 45 : 25,
                _ => job == SettlementJob.Food ? 60 :
                    job == SettlementJob.BuildDefense ? 50 : 40
            };
        }
    }
}
