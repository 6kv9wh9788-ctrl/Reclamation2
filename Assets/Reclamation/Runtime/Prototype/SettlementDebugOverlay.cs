using UnityEngine;

namespace Reclamation.Prototype
{
    public sealed class SettlementDebugOverlay : MonoBehaviour
    {
        [SerializeField] private HaulJobBoard jobBoard;
        [SerializeField] private Stockpile stockpile;
        [SerializeField] private FoodStore foodStore;
        private HaulWorker[] workers;
        private GUIStyle textStyle;

        private void Start()
        {
            workers = FindObjectsByType<HaulWorker>(FindObjectsSortMode.None);
        }

        public void Configure(HaulJobBoard board, Stockpile destination, FoodStore meals = null)
        {
            jobBoard = board;
            stockpile = destination;
            foodStore = meals;
        }

        private void OnGUI()
        {
            if (textStyle == null)
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true };
            Matrix4x4 previous = GUI.matrix;
            float scale = Mathf.Max(0.6f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUILayout.BeginArea(new Rect(16, 16, 620, 340), GUI.skin.box);
            GUILayout.Label("RECLAMATION — AUTONOMOUS HAULING LAB", textStyle);
            GUILayout.Label($"Stockpile: {(stockpile == null ? 0 : stockpile.StoredUnits)} wood", textStyle);
            GUILayout.Label($"Food: {(foodStore == null ? 0 : foodStore.Servings)} servings " +
                $"({(foodStore == null ? 0 : foodStore.ReservedServings)} reserved)", textStyle);
            GUILayout.Label($"Active reservations: {(jobBoard == null ? 0 : jobBoard.OpenReservationCount)}", textStyle);
            GUILayout.Space(8);

            if (workers != null) foreach (HaulWorker worker in workers)
            {
                if (worker == null) continue;
                GUILayout.Label($"{worker.WorkerName} — {worker.State} — cargo: {(worker.Carrying ? 1 : 0)}", textStyle);
                GUILayout.Label($"    hunger: {(worker.Needs == null ? 0 : worker.Needs.Hunger):0}/100", textStyle);
                GUILayout.Label($"    {worker.DecisionExplanation}", textStyle);
            }

            GUILayout.EndArea();
            GUI.matrix = previous;
        }
    }
}
