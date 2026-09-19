using System.Collections.Generic;
using Reclamation.AI;
using UnityEngine;

namespace Reclamation.Prototype
{
    public sealed class HaulJobBoard : MonoBehaviour
    {
        [SerializeField] private Stockpile destination;
        [SerializeField] private List<ResourcePile> sources = new();

        private readonly ReservationRegistry _reservations = new();

        public Stockpile Destination => destination;
        public int OpenReservationCount => _reservations.Count;

        public void Configure(Stockpile stockpile, IEnumerable<ResourcePile> resourceSources)
        {
            destination = stockpile;
            sources.Clear();
            sources.AddRange(resourceSources);
        }

        public bool TryClaimBest(
            string workerId,
            Vector3 workerPosition,
            out ResourcePile claimedSource,
            out float winningScore,
            out string explanation,
            System.Func<ResourcePile, bool> canUse = null)
        {
            claimedSource = null;
            winningScore = float.NegativeInfinity;
            explanation = "No available haul jobs";

            foreach (ResourcePile source in sources)
            {
                if (source == null || !source.isActiveAndEnabled || source.Amount <= 0)
                {
                    continue;
                }

                if (_reservations.IsReservedByOther(source.ReservationId, workerId))
                {
                    continue;
                }

                if (canUse != null && !canUse(source)) continue;

                float distance = Vector3.Distance(workerPosition, source.transform.position);
                float score = HaulJobScorer.Score(source.PolicyPriority, distance);
                if (score > winningScore)
                {
                    winningScore = score;
                    claimedSource = source;
                }
            }

            if (claimedSource == null
                || !_reservations.TryReserve(claimedSource.ReservationId, workerId))
            {
                claimedSource = null;
                winningScore = 0f;
                return false;
            }

            float winningDistance = Vector3.Distance(workerPosition, claimedSource.transform.position);
            explanation = $"priority {claimedSource.PolicyPriority} × {HaulJobScorer.PriorityWeight:0} "
                + $"− distance {winningDistance:0.0} = {winningScore:0.0}";
            return true;
        }

        public void Release(ResourcePile source, string workerId)
        {
            if (source != null)
            {
                _reservations.Release(source.ReservationId, workerId);
            }
        }

        public void ReleaseAll(string workerId)
        {
            _reservations.ReleaseAll(workerId);
        }
    }
}
