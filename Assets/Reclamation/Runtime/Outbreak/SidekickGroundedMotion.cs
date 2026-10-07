using UnityEngine;

namespace Reclamation.Blight
{
    // Presentation only. Targets are measured from this avatar's rest pose;
    // actor movement, attack timing, damage and stamina remain authoritative.
    public sealed partial class SidekickDuelBridge
    {
        public float ReviewDelta { get; set; }
        public bool ReviewWalking { get; set; }
        private sealed class GroundLeg
        {
            public Transform upper, lower, foot;
            public Vector3 origin;
            public Quaternion rotation;
        }
        private readonly GroundLeg[] groundLegs = new GroundLeg[2];
        private Vector3 previousActorPosition, travelDirection = Vector3.forward;
        private float stridePhase, walkBlend, stature = 1, floor;
        private void CacheGroundedBody()
        {
            for (int i = 0; i < 2; i++)
            {
                var foot = animator.GetBoneTransform(i == 0 ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
                groundLegs[i] = new GroundLeg {
                    upper = animator.GetBoneTransform(i == 0 ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg),
                    lower = animator.GetBoneTransform(i == 0 ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg),
                    foot = foot, origin = model.transform.InverseTransformPoint(foot.position),
                    rotation = Quaternion.Inverse(model.transform.rotation) * foot.rotation };
            }
            stature = Mathf.Max(.3f, model.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Hips).position).y);
            floor = Mathf.Min(groundLegs[0].origin.y, groundLegs[1].origin.y) - .07f * stature;
            previousActorPosition = source.transform.parent.position;
            stridePhase = walkBlend = 0;
        }
        // Heel-to-toe travel: stance moves backward at constant speed; swing
        // returns forward with zero vertical velocity at lift-off and landing.
        public static Vector3 GroundedStep(float phase, float stride, float lift)
        {
            phase = Mathf.Repeat(phase, 1);
            if (phase < .6f) return new Vector3(0, 0, Mathf.Lerp(stride * .5f, -stride * .5f, phase / .6f));
            float t = (phase - .6f) / .4f;
            return new Vector3(0, lift * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 2),
                Mathf.Lerp(-stride * .5f, stride * .5f, t * t * (3 - 2 * t)));
        }
        private void ApplyGroundedBody()
        {
            Vector3 actorPosition = source.transform.parent.position;
            Vector3 delta = actorPosition - previousActorPosition; delta.y = 0;
            previousActorPosition = actorPosition;
            float dt = ReviewFighter != null ? ReviewDelta : Mathf.Min(Time.unscaledDeltaTime, .1f);
            bool moving = ReviewFighter != null ? ReviewWalking : delta.sqrMagnitude > .000001f;
            bool neutral = Fighter.Alive && (Fighter.Action == DuelAction.Ready || Fighter.Blocking);
            walkBlend = Mathf.MoveTowards(walkBlend, moving && neutral ? 1 : 0, dt * 7);
            if (ReviewFighter != null) { if (moving) stridePhase += dt / 1.05f; travelDirection = Vector3.forward; }
            else if (moving && dt > 0)
            {
                // Keep cadence tied to distance, including strafing and retreat.
                stridePhase += Mathf.Min(delta.magnitude / (.72f * stature), dt * 2.5f);
                travelDirection = source.transform.parent.InverseTransformDirection(delta.normalized);
            }
            bool sword = ActiveWeapon == BlightWeapon.Sword;
            bool attack = Fighter.Action == DuelAction.Windup || Fighter.Action == DuelAction.Recovery;
            if (!Fighter.Alive || (!neutral && !(sword && attack))) return;
            // Reset torso copied from the prototype, then author weight shift/twist.
            foreach (var id in new[] { HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.Neck })
            { var bone = animator.GetBoneTransform(id); if (bone) bone.localRotation = rest[bone]; }
            float cycle = stridePhase * Mathf.PI * 2;
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            hips.position += model.transform.TransformVector(new Vector3(
                Mathf.Sin(cycle) * .018f * walkBlend, -.025f + .012f * Mathf.Cos(2 * cycle) * walkBlend, 0) * stature);
            float load = 0, strike = 0;
            if (sword && attack) AttackWeights(Fighter.Action, Fighter.Progress, out load, out strike);
            RotateBody(HumanBodyBones.Hips, new Vector3(0, -5 * Mathf.Sin(cycle) * walkBlend - 12 * load + 15 * strike, 0));
            RotateBody(HumanBodyBones.Chest, new Vector3(3 * walkBlend + 7 * strike,
                5 * Mathf.Sin(cycle) * walkBlend - 18 * load + 18 * strike, 0));
            hips.position += model.transform.forward * (.07f * strike - .035f * load) * stature;
            for (int i = 0; i < 2; i++)
            {
                var leg = groundLegs[i];
                Vector3 step = GroundedStep(stridePhase + i * .5f, .43f * stature, .065f * stature) * walkBlend;
                Vector3 target = leg.origin + travelDirection * step.z + Vector3.up * step.y;
                // Stagger the guard, then plant a short lead-foot step into contact.
                target.z += (i == 0 ? .075f + .10f * strike : -.075f) * stature * (1 - walkBlend);
                ModularHumanRig.SolveGrip(leg.upper, leg.lower, leg.foot, model.transform.TransformPoint(target), model.transform.forward);
                leg.foot.rotation = model.transform.rotation * leg.rotation;
            }
        }
        private void ApplyDodgeBody()
        {
            if (!Fighter.Alive || Fighter.Action != DuelAction.Dodge) return;
            float weight = Mathf.Sin(Mathf.Clamp01(Fighter.Progress) * Mathf.PI);
            // A bounded duck/lean follows the already committed dodge phase.
            // Only the avatar's bones move; actor displacement and immunity are untouched.
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            hips.position -= model.transform.up * (.24f * stature * model.transform.lossyScale.y * weight);
            RotateBody(HumanBodyBones.Hips, new Vector3(16 * weight, 0, -8 * weight));
            RotateBody(HumanBodyBones.Chest, new Vector3(8 * weight, 0, 0));
            for (int i = 0; i < 2; i++)
            {
                GroundLeg leg = groundLegs[i];
                Vector3 target = leg.origin + Vector3.forward * ((i == 0 ? .12f : -.12f) * stature * weight);
                ModularHumanRig.SolveGrip(leg.upper, leg.lower, leg.foot, model.transform.TransformPoint(target), model.transform.forward);
                leg.foot.rotation = model.transform.rotation * leg.rotation;
            }
        }
        private void RotateBody(HumanBodyBones id, Vector3 angles)
        {
            var bone = animator.GetBoneTransform(id);
            if (bone) bone.rotation = model.transform.rotation * Quaternion.Euler(angles) * Quaternion.Inverse(model.transform.rotation) * bone.rotation;
        }
        // Contact is exactly the Windup -> Recovery boundary, matching damage.
        public static void AttackWeights(DuelAction action, float progress, out float load, out float strike)
        {
            progress = Mathf.Clamp01(progress);
            if (action == DuelAction.Windup)
            {
                float anticipation = Mathf.SmoothStep(0, 1, progress / .65f);
                strike = Mathf.SmoothStep(0, 1, (progress - .65f) / .35f);
                load = anticipation * (1 - strike);
            }
            else
            {
                load = 0;
                strike = 1 - Mathf.SmoothStep(0, 1, progress);
            }
        }
        private void OverrideSwordPath(ref Vector3 contact, ref Quaternion rotation)
        {
            if (ActiveWeapon != BlightWeapon.Sword || !Fighter.Alive || Fighter.Action == DuelAction.Dodge || Fighter.Action == DuelAction.Stagger) return;
            float load = 0, strike = 0;
            if (Fighter.Action == DuelAction.Windup || Fighter.Action == DuelAction.Recovery)
                AttackWeights(Fighter.Action, Fighter.Progress, out load, out strike);
            Vector3 guard = new Vector3(.04f, 1.08f, .43f);
            Vector3 raised = Fighter.Heavy ? new Vector3(.10f, 1.61f, .20f) : new Vector3(.28f, 1.40f, .24f);
            Vector3 hit = new Vector3(-.04f, 1.30f, .65f);
            Vector3 position = guard + (raised - guard) * load + (hit - guard) * strike;
            Quaternion ready = Quaternion.Euler(-55, 0, 90);
            Quaternion windup = Fighter.Heavy ? Quaternion.Euler(-115, 0, 90) : Quaternion.Euler(-95, 25, 65);
            Quaternion impact = Quaternion.Euler(Fighter.Heavy ? 12 : -2, Fighter.Heavy ? 0 : -15, Fighter.Heavy ? 90 : 65);
            Quaternion local = Quaternion.Slerp(Quaternion.Slerp(ready, windup, load), impact, strike);
            if (Fighter.Blocking) { position = new Vector3(.04f, 1.40f, .40f); local = Quaternion.Euler(-35, -35, 65); }
            // Local blade X is its cutting edge; Z is the blade length.
            contact = model.transform.TransformPoint(position * stature + weaponGripOffset);
            rotation = model.transform.rotation * local * Quaternion.Euler(weaponRotationOffset);
        }
        private void ApplyGroundedDefeat()
        {
            if (Fighter.Alive) return;
            // One deterministic resting pose, rotated about the pelvis rather
            // than the feet. No accumulated rotations or collider movement.
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Vector3 pivot = hips.position;
            model.transform.RotateAround(pivot, model.transform.forward, 88);
            float lowest = float.PositiveInfinity;
            foreach (var id in new[] { HumanBodyBones.Hips, HumanBodyBones.Head, HumanBodyBones.LeftUpperArm,
                HumanBodyBones.RightUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm,
                HumanBodyBones.LeftHand, HumanBodyBones.RightHand, HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.RightLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
            {
                var bone = animator.GetBoneTransform(id);
                if (bone) lowest = Mathf.Min(lowest, bone.position.y - .075f * stature * model.transform.lossyScale.y);
            }
            float ground = source.transform.parent.TransformPoint(new Vector3(0, floor, 0)).y;
            model.transform.position += Vector3.up * (ground - lowest);
        }
    }
}
