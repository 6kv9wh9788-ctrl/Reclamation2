using UnityEngine;

namespace Reclamation.Blight
{
    public enum BodyRegion { Torso, RightArm, LeftArm, RightLeg, LeftLeg }
    public enum SwingHeight { Arms, Legs, Torso }

    // Prototype integrity is separate from health: disabling a limb need not kill the creature.
    public sealed class BlightLimbs
    {
        private readonly float[] integrity = { 100, 50, 50, 60, 60 };
        public float Integrity(BodyRegion region) => integrity[(int)region];
        public bool Missing(BodyRegion region) => region != BodyRegion.Torso && Integrity(region) <= 0;
        public bool Crawling => Missing(BodyRegion.RightLeg) || Missing(BodyRegion.LeftLeg);
        public bool Limping => !Crawling && (Integrity(BodyRegion.RightLeg) <= 30 || Integrity(BodyRegion.LeftLeg) <= 30);
        public bool CanHeavy => !Crawling && !Missing(BodyRegion.RightArm) && !Missing(BodyRegion.LeftArm);
        public bool BiteOnly => Missing(BodyRegion.RightArm) && Missing(BodyRegion.LeftArm);
        public float SpeedMultiplier => Crawling ? 0.24f : Limping ? 0.55f : 1;

        public bool Damage(BodyRegion region, float amount, bool cutting)
        {
            if (region == BodyRegion.Torso || Missing(region) || !(amount > 0) || float.IsInfinity(amount)) return false;
            integrity[(int)region] = Mathf.Max(cutting ? 0 : 1, Integrity(region) - amount);
            return Missing(region);
        }

        public AttackSpec Attack(int sequence)
        {
            if (Crawling || BiteOnly)
                return new AttackSpec(BlightAttack.Light, 16, 1.15f, 0.9f, 1.15f, 45, 12, false);
            return BlightEquipment.Enemy(BlightEnemy.Thrall, CanHeavy ? sequence : 0);
        }
    }

    public static class BlightBladeSweep
    {
        public static float SegmentDistanceSquared(Vector3 a, Vector3 b, Vector3 point)
        {
            Vector3 delta = b - a;
            float t = delta.sqrMagnitude > 0.000001f ? Mathf.Clamp01(Vector3.Dot(point - a, delta) / delta.sqrMagnitude) : 0;
            return (point - (a + delta * t)).sqrMagnitude;
        }

        // Sweep samples along the blade relative to the moving target. The lab substeps at 60 Hz.
        public static bool Hits(Vector3 oldBase, Vector3 oldTip, Vector3 newBase, Vector3 newTip,
            Vector3 oldCenter, Vector3 newCenter, float radius)
        {
            float squared = radius * radius;
            if (SegmentDistanceSquared(oldBase, oldTip, oldCenter) <= squared ||
                SegmentDistanceSquared(newBase, newTip, newCenter) <= squared) return true;
            for (int i = 0; i <= 16; i++)
                if (SegmentDistanceSquared(Vector3.Lerp(oldBase, oldTip, i / 16f) - oldCenter,
                    Vector3.Lerp(newBase, newTip, i / 16f) - newCenter, Vector3.zero) <= squared) return true;
            return false;
        }
    }
}
