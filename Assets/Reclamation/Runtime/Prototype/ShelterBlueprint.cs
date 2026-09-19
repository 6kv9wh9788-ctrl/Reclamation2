using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Prototype
{
    /// <summary>Owns delivered materials and exclusive delivery/build claims.</summary>
    public sealed class ShelterBlueprint : MonoBehaviour
    {
        public const int WoodCost = 8;
        public const float WorkRequired = 10f;
        [SerializeField] private int deliveredWood;
        [SerializeField] private float work;
        [SerializeField] private bool cancelled;
        private readonly HashSet<string> deliveries = new();
        private string builder;
        public int DeliveredWood => deliveredWood;
        public float Progress => work / WorkRequired;
        public bool Complete => work >= WorkRequired;
        public bool Cancelled => cancelled;
        public bool Available => isActiveAndEnabled && !cancelled && !Complete;
        public int DeliveryClaims => deliveries.Count;
        public Vector3 WorkPoint => transform.position + new Vector3(0, 0, -2.6f);

        public bool TryReserveDelivery(string owner)
        {
            if (!Available || string.IsNullOrEmpty(owner)) return false;
            if (deliveries.Contains(owner)) return true;
            if (deliveredWood + deliveries.Count >= WoodCost) return false;
            return deliveries.Add(owner);
        }

        public bool TryDeliver(string owner)
        {
            if (!Available || deliveredWood >= WoodCost || !deliveries.Remove(owner)) return false;
            deliveredWood++;
            return true;
        }

        public bool TryReserveBuild(string owner)
        {
            if (!Available || deliveredWood != WoodCost || string.IsNullOrEmpty(owner)) return false;
            if (builder != null && builder != owner) return false;
            builder = owner;
            return true;
        }

        public bool TryWork(string owner, float seconds)
        {
            if (!Available || string.IsNullOrEmpty(owner) || builder != owner || deliveredWood != WoodCost ||
                seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
            work = Mathf.Min(WorkRequired, work + seconds);
            if (Complete) builder = null;
            return true;
        }

        public void Release(string owner)
        {
            deliveries.Remove(owner);
            if (builder == owner) builder = null;
        }

        public bool TryCancel(Stockpile refundTo)
        {
            if (cancelled || Complete || refundTo == null) return false;
            for (int i = 0; i < deliveredWood; i++) refundTo.DepositOne();
            deliveredWood = 0;
            work = 0;
            cancelled = true;
            deliveries.Clear();
            builder = null;
            return true;
        }

        private void OnDisable()
        {
            deliveries.Clear();
            builder = null;
        }
    }
}
