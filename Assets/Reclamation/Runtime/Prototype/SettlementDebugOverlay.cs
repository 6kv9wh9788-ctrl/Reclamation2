using UnityEngine;

namespace Reclamation.Prototype
{
    public sealed class SettlementDebugOverlay : MonoBehaviour
    {
        [SerializeField] private HaulJobBoard jobBoard;
        [SerializeField] private Stockpile stockpile;

        public void Configure(HaulJobBoard board, Stockpile destination)
        {
            jobBoard = board;
            stockpile = destination;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 520, 230), GUI.skin.box);
            GUILayout.Label("RECLAMATION — AUTONOMOUS HAULING LAB");
            GUILayout.Label($"Stockpile: {(stockpile == null ? 0 : stockpile.StoredUnits)} wood");
            GUILayout.Label($"Active reservations: {(jobBoard == null ? 0 : jobBoard.OpenReservationCount)}");
            GUILayout.Space(8);

            HaulWorker[] workers = FindObjectsByType<HaulWorker>(FindObjectsSortMode.None);
            foreach (HaulWorker worker in workers)
            {
                GUILayout.Label($"{worker.WorkerName} — {worker.State}");
                GUILayout.Label($"    {worker.DecisionExplanation}");
            }

            GUILayout.EndArea();
        }
    }
}
