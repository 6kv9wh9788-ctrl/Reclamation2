using UnityEngine;

namespace Reclamation.Neighborhood
{
    public sealed class NeighborhoodPanel : MonoBehaviour
    {
        [SerializeField] private NeighborhoodClock clock;
        [SerializeField] private CivilianRoutine[] civilians;
        private GUIStyle label, button;
        public void Configure(NeighborhoodClock timer, CivilianRoutine[] residents)
        {
            clock = timer; civilians = residents;
        }
        private void OnGUI()
        {
            if (clock == null) return;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true };
                button = new GUIStyle(GUI.skin.button) { fontSize = 19 };
            }
            float scale = Mathf.Max(0.6f, Screen.height / 900f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUILayout.BeginArea(new Rect(16, 16, 440, 465), GUI.skin.box);
            GUILayout.Label("RECLAMATION — BEFORE THE OUTBREAK", label);
            GUILayout.Label(clock.DisplayTime + (clock.Paused ? " (paused)" : $" ({clock.Speed:0}×)"), label);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(clock.Paused ? "Resume" : "Pause", button)) clock.SetPaused(!clock.Paused);
            foreach (float speed in new[] { 1f, 4f, 12f })
                if (GUILayout.Button($"{speed:0}×", button)) clock.SetSpeed(speed);
            GUILayout.EndHorizontal();
            GUILayout.Label("08–10 Café | 12–16 Park | 16–18 Café\nOtherwise home; departures staggered.", label);
            GUILayout.Space(8);
            if (civilians != null) foreach (var person in civilians)
                if (person != null) GUILayout.Label($"{person.DisplayName}: {person.Status}", label);
            GUILayout.Space(8);
            GUILayout.Label("Normal life prototype: six civilians, no infection. One day takes six minutes at 1×.", label);
            GUILayout.EndArea();
            GUI.matrix = previous;
        }
    }
}
