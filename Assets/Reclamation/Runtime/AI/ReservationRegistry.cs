using System.Collections.Generic;

namespace Reclamation.AI
{
    /// <summary>
    /// Grants exclusive ownership of simulation targets such as resource piles.
    /// The registry deliberately contains no Unity object references, making its
    /// behavior deterministic and straightforward to test.
    /// </summary>
    public sealed class ReservationRegistry
    {
        private readonly Dictionary<string, string> _ownersByTarget = new();

        public int Count => _ownersByTarget.Count;

        public bool TryReserve(string targetId, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(targetId) || string.IsNullOrWhiteSpace(ownerId))
            {
                return false;
            }

            if (_ownersByTarget.TryGetValue(targetId, out string existingOwner))
            {
                return existingOwner == ownerId;
            }

            _ownersByTarget.Add(targetId, ownerId);
            return true;
        }

        public bool IsReservedByOther(string targetId, string ownerId)
        {
            return _ownersByTarget.TryGetValue(targetId, out string existingOwner)
                && existingOwner != ownerId;
        }

        public bool Release(string targetId, string ownerId)
        {
            if (!_ownersByTarget.TryGetValue(targetId, out string existingOwner)
                || existingOwner != ownerId)
            {
                return false;
            }

            return _ownersByTarget.Remove(targetId);
        }

        public void ReleaseAll(string ownerId)
        {
            var targets = new List<string>();
            foreach (KeyValuePair<string, string> reservation in _ownersByTarget)
            {
                if (reservation.Value == ownerId)
                {
                    targets.Add(reservation.Key);
                }
            }

            foreach (string target in targets)
            {
                _ownersByTarget.Remove(target);
            }
        }
    }
}
