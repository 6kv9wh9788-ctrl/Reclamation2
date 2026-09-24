using UnityEngine;

namespace Reclamation.Blight
{
    public sealed class BlightThrallVisual : MonoBehaviour
    {
        public ModularHumanRig Rig { get; private set; }
        public void Build()
        {
            Rig = gameObject.AddComponent<ModularHumanRig>();
            Rig.Build(false, false, false, true);
            Rig.SetAppearance(false, false, 0); Rig.SetBuiltInWeaponsVisible(false);
            Pose(new DuelFighter(), false, 0);
        }

        public void Pose(DuelFighter fighter, bool moving, float time)
        {
            string clip = !fighter.Alive ? "Death" : fighter.Action == DuelAction.Stagger ? "Stagger" : fighter.Action == DuelAction.Dodge ? "Dodge" : moving ? "Walk" : "Idle";
            float phase = !fighter.Alive ? 1 : (fighter.Action == DuelAction.Stagger || fighter.Action == DuelAction.Dodge) ? fighter.Progress : Mathf.Repeat(time / (moving ? 1 : 2), 1);
            Rig.SamplePose(clip, phase);
            ShapePose(Rig, fighter, null);
            if (!fighter.Alive || fighter.Action == DuelAction.Stagger || fighter.Action == DuelAction.Dodge) return;
            bool wind = fighter.Action == DuelAction.Windup, strike = fighter.Action == DuelAction.Recovery;
            float angle = wind ? Mathf.Lerp(-35, -140, Mathf.SmoothStep(0, 1, fighter.Progress)) :
                strike ? Mathf.Lerp(-75, -20, Mathf.SmoothStep(0, 1, fighter.Progress)) : -20;
            Rig.Bone("UpperArm_R").localRotation = Quaternion.Euler(angle, 0, -12);
            Rig.Bone("Forearm_R").localRotation = Quaternion.Euler(-30, 0, 0);
            Rig.Bone("UpperArm_L").localRotation = Quaternion.Euler(fighter.Heavy && (wind || strike) ? angle : -25, 0, 12);
            Rig.Bone("Forearm_L").localRotation = Quaternion.Euler(-30, 0, 0);
        }

        // Called after each fresh sampled/bind pose; no wall-clock animation or
        // accumulated rotations. Limb-lab joint positions are restored afterward
        // so the visual posture cannot move its existing contact targets.
        public static void ShapePose(ModularHumanRig rig, DuelFighter fighter, BlightLimbs limbs)
        {
            bool crawl = limbs != null && limbs.Crawling;
            bool bite = limbs != null && (limbs.BiteOnly || crawl);
            bool wind = fighter.Action == DuelAction.Windup, strike = fighter.Action == DuelAction.Recovery;
            if (fighter.Alive)
            {
                rig.Bone("Spine").localRotation *= Quaternion.Euler(crawl ? 0 : 18, 0, -3);
                rig.Bone("Neck").localRotation *= Quaternion.Euler(crawl ? 0 : -12, 0, 0);
                if (bite && strike) rig.Bone("Neck").localPosition += Vector3.forward * (.07f * (1 - fighter.Progress));
            }
            float jaw = bite && wind ? Mathf.Lerp(5, 30, fighter.Progress) :
                bite && strike ? Mathf.Lerp(30, 2, Mathf.Clamp01(fighter.Progress / .3f)) : 5;
            rig.Bone("Jaw").localRotation = Quaternion.Euler(jaw, 0, 0);
            foreach (string side in new[] {"R", "L"})
                foreach (string finger in new[] {"Thumb", "Index", "Middle", "Ring", "Little"})
                    for (int segment = 1; segment <= 3; segment++)
                        rig.Bone(finger + segment + "_" + side).localRotation = Quaternion.Euler(-8 * segment, 0, 0);
            string expression = !fighter.Alive || fighter.Action == DuelAction.Stagger ? "Hurt" :
                bite && (wind || strike) ? "Shout" : "Angry";
            if (rig.Expression != expression) rig.SetExpression(expression);
        }
    }
}
