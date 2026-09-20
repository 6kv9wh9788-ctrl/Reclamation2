using UnityEngine;

namespace Reclamation.Outbreak
{
    public sealed class SystemsValidationLab : MonoBehaviour
    {
        [SerializeField] private GameObject[] phases;
        [SerializeField] private string[] phaseNames;
        [SerializeField] private OutbreakDirector director;
        private int selected;
        private GUIStyle label, button;
        public int SelectedPhase => selected;
        public int PhaseCount => phases == null ? 0 : phases.Length;
        private Rect PanelRect => new Rect(Screen.width / Reclamation.Neighborhood.LabCameraController.UiScale - 356, 16, 340, 325);
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
                director.SetPopulation(phases[selected].GetComponentsInChildren<OutbreakAgent>(true));
            return true;
        }

        private void OnGUI()
        {
            if (phases == null || phases.Length == 0) return;
            label ??= new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            button ??= new GUIStyle(GUI.skin.button) { fontSize = 16 };
            float scale = Reclamation.Neighborhood.LabCameraController.UiScale;
            Matrix4x4 previous = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUILayout.BeginArea(PanelRect, GUI.skin.box);
            GUILayout.Label("SYSTEMS VALIDATION LAB", label);
            GUILayout.Label("Active: " + phaseNames[selected], label);
            for (int i = 0; i < phases.Length; i++)
                if (GUILayout.Button((i == selected ? "✓ " : "") + phaseNames[i], button)) Select(i);
            GUILayout.Label("Each scenario is fresh once per Play session. Restart Play to reset every scenario.", label);
            GUILayout.EndArea(); GUI.matrix = previous;
        }
    }
}
