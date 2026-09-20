using System;

namespace Reclamation.Outbreak
{
    // Simulation seconds, independent of rendered frame rate and the day-clock minute scale.
    public sealed class BurstStamina
    {
        private readonly float duration;
        private readonly float recovery;
        public float Fraction { get; private set; } = 1;
        public bool Bursting { get; private set; }

        public BurstStamina(float durationSeconds, float recoverySeconds)
        {
            if (!(durationSeconds > 0) || float.IsInfinity(durationSeconds) ||
                !(recoverySeconds > 0) || float.IsInfinity(recoverySeconds))
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            duration = durationSeconds; recovery = recoverySeconds;
        }

        public void Advance(float seconds, bool requestBurst)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds)) return;
            if (!requestBurst) Bursting = false;
            else if (!Bursting && Fraction >= 1) Bursting = true;
            if (Bursting)
            {
                float availableSeconds = Fraction * duration;
                if (seconds < availableSeconds) Fraction -= seconds / duration;
                else
                {
                    Bursting = false;
                    // Recover any time remaining after exhaustion, without starting another burst this tick.
                    Fraction = Math.Min(1, (seconds - availableSeconds) / recovery);
                }
            }
            else Fraction = Math.Min(1, Fraction + seconds / recovery);
        }
    }
}
