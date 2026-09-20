using System;

namespace Reclamation.Outbreak
{
    public enum InfectionState { Healthy, Exposed, Symptomatic, Turned, Neutralized }

    public sealed class InfectionTimeline
    {
        public InfectionState State { get; private set; } = InfectionState.Healthy;
        public double SymptomMinute { get; private set; } = double.PositiveInfinity;
        public double TurnMinute { get; private set; } = double.PositiveInfinity;

        public bool Expose(double now, double incubationMinutes, double symptomaticMinutes)
        {
            if (State != InfectionState.Healthy || !Valid(now) ||
                !Positive(incubationMinutes) || !Positive(symptomaticMinutes)) return false;
            State = InfectionState.Exposed;
            SymptomMinute = now + incubationMinutes;
            TurnMinute = SymptomMinute + symptomaticMinutes;
            return true;
        }

        public bool Advance(double now)
        {
            if (!Valid(now)) return false;
            InfectionState before = State;
            if (State == InfectionState.Exposed && now >= SymptomMinute)
                State = now >= TurnMinute ? InfectionState.Turned : InfectionState.Symptomatic;
            else if (State == InfectionState.Symptomatic && now >= TurnMinute)
                State = InfectionState.Turned;
            return State != before;
        }

        public bool Neutralize()
        {
            if (State != InfectionState.Turned) return false;
            State = InfectionState.Neutralized;
            return true;
        }

        private static bool Valid(double value) => value >= 0 && !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool Positive(double value) => Valid(value) && value > 0;
    }
}
