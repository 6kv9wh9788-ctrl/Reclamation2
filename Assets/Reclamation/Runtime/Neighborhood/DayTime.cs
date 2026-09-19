using System;

namespace Reclamation.Neighborhood
{
    // Pure simulation time: independent of Unity's global time scale.
    public sealed class DayTime
    {
        public double TotalMinutes { get; private set; }
        public int Day => (int)Math.Floor(TotalMinutes / 1440) + 1;
        public double MinuteOfDay => TotalMinutes % 1440;

        public DayTime(double initialMinutes)
        {
            if (!Valid(initialMinutes) || initialMinutes >= int.MaxValue * 1440d)
                throw new ArgumentOutOfRangeException(nameof(initialMinutes));
            TotalMinutes = initialMinutes;
        }

        public void Advance(double seconds, double minutesPerSecond, double speed)
        {
            if (!Valid(seconds) || !Valid(minutesPerSecond) || !Valid(speed))
                throw new ArgumentOutOfRangeException("Time inputs must be finite and non-negative.");
            double result = TotalMinutes + seconds * minutesPerSecond * speed;
            if (!Valid(result) || result >= int.MaxValue * 1440d)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            TotalMinutes = result;
        }

        private static bool Valid(double value) => value >= 0 && !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public enum RoutineDestination { Home, Cafe, Park }

    public static class DailyRoutine
    {
        public static RoutineDestination Resolve(double minuteOfDay, int staggerMinutes)
        {
            double time = ((minuteOfDay - staggerMinutes) % 1440 + 1440) % 1440;
            if (time >= 8 * 60 && time < 10 * 60) return RoutineDestination.Cafe;
            if (time >= 12 * 60 && time < 16 * 60) return RoutineDestination.Park;
            if (time >= 16 * 60 && time < 18 * 60) return RoutineDestination.Cafe;
            return RoutineDestination.Home;
        }
    }
}
