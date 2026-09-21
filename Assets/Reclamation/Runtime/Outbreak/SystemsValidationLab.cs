using UnityEngine;

namespace Reclamation.Outbreak
{
    public sealed class SystemsValidationLab : MonoBehaviour
    {
        [SerializeField] private GameObject[] phases;
        [SerializeField] private string[] phaseNames;
        [SerializeField] private OutbreakDirector director;
        private int selected;
        private OutbreakAgent[] activePopulation;
        private GUIStyle label, button;
        private bool collapsed = true;
        public int SelectedPhase => selected;
        public int PhaseCount => phases == null ? 0 : phases.Length;
        public bool Collapsed => collapsed;
        private Rect PanelRect => new Rect(Screen.width / Reclamation.Neighborhood.LabCameraController.UiScale - 356,
            16, 340, collapsed ? 106 : 525);
        public bool ContainsGuiPoint(Vector2 point) => isActiveAndEnabled && PanelRect.Contains(point);

        public void Configure(GameObject[] roots, string[] names, OutbreakDirector outbreak)
        { phases = roots; phaseNames = names; director = outbreak; }

        private void Awake() => Select(0);

        public bool Select(int index)
        {
            if (phases == null || index < 0 || index >= phases.Length) return false;
            selected = index;
            for (int i = 0; i < phases.Length; i++) if (phases[i] != null) phases[i].SetActive(i == selected);
            if (director != null && phases[selected] != null)
            {
                activePopulation = phases[selected].GetComponentsInChildren<OutbreakAgent>(true);
                director.SetPopulation(activePopulation);
                bool combatScenario = activePopulation.Length > 0;
                director.SetPanelPresentation(combatScenario, "RECLAMATION — COMBAT VALIDATION");
            }
            var camera = Camera.main == null ? null : Camera.main.GetComponent<Reclamation.Neighborhood.LabCameraController>();
            if (camera != null) camera.RefreshPeople(phases[selected]);
            return true;
        }

        private string ActiveState()
        {
            if (activePopulation == null || activePopulation.Length == 0)
                return "Settlement scenario — no zombies are expected.";
            int humans = 0, zombies = 0, neutralized = 0;
            foreach (OutbreakAgent person in activePopulation)
            {
                if (person == null) continue;
                if (person.State == InfectionState.Neutralized) neutralized++;
                else if (person.State == InfectionState.Turned) zombies++;
                else humans++;
            }
            if (zombies == 0 && neutralized > 0)
                return $"Combat complete — {neutralized} threat(s) neutralized. Restart Play Mode to reset.";
            return $"Combat scenario — {humans} survivor(s), {zombies} active zombie(s).";
        }

        private void OnGUI()
        {
            if (phases == null || phases.Length == 0) return;
            label ??= new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            button ??= new GUIStyle(GUI.skin.button) { fontSize = 16 };
            float scale = Reclamation.Neighborhood.LabCameraController.UiScale;
            Matrix4x4 previous = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUILayout.BeginArea(PanelRect, GUI.skin.box);
            if (GUILayout.Button(collapsed ? "Scenarios  ▼" : "Collapse scenarios  ▲", button)) collapsed = !collapsed;
            GUILayout.Label("Active: " + phaseNames[selected], label);
            if (!collapsed)
            {
                GUILayout.Label(ActiveState(), label);
                for (int i = 0; i < phases.Length; i++)
                    if (GUILayout.Button((i == selected ? "✓ " : "") + phaseNames[i], button)) Select(i);
                GUILayout.Label("Each scenario is fresh once per Play session. Restart Play to reset every scenario.", label);
            }
            GUILayout.EndArea(); GUI.matrix = previous;
        }
    }
}
