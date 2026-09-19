using UnityEngine;

namespace Reclamation.Neighborhood
{
    public sealed class NeighborhoodClock : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float minutesPerSecond = 4;
        [SerializeField, Range(0, 1439)] private int startingMinute = 465;
        private DayTime time;
        public float Speed { get; private set; } = 1;
        public bool Paused { get; private set; }
        public double MinuteOfDay => time == null ? startingMinute : time.MinuteOfDay;
        public int Day => time == null ? 1 : time.Day;
        public string DisplayTime => $"Day {Day} — {(int)MinuteOfDay / 60:00}:{(int)MinuteOfDay % 60:00}";
        private void Awake() => time = new DayTime(startingMinute);
        private void Update()
        {
            if (!Paused) time.Advance(Time.deltaTime, minutesPerSecond, Speed);
        }
        public void SetPaused(bool paused) => Paused = paused;
        public void SetSpeed(float speed)
        {
            if (speed == 1 || speed == 4 || speed == 12) Speed = speed;
        }
    }
}
