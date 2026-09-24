using UnityEngine;

namespace Reclamation.Blight
{
    // Presentation only. Combat still resolves its existing strike at the
    // Windup -> Recovery transition, with its original reach and damage.
    public sealed class BlightHulkVisual : MonoBehaviour
    {
        public ModularHumanRig Rig { get; private set; }
        public float HandTargetError { get; private set; }
        public void Build()
        {
            Rig = gameObject.AddComponent<ModularHumanRig>();
            Rig.Build(false, false, false, false, true);
            Rig.SetAppearance(false, false, 0); Rig.SetBuiltInWeaponsVisible(false);
            Pose(new DuelFighter(260, true), false, 0);
        }

        public void Pose(DuelFighter fighter, bool moving, float time)
        {
            HandTargetError = 0;
            bool wind = fighter.Action == DuelAction.Windup, recover = fighter.Action == DuelAction.Recovery;
            bool attack = wind || recover;
            string clip = !fighter.Alive ? "Death" : fighter.Action == DuelAction.Stagger ? "Stagger" : moving && !attack ? "Walk" : "Idle";
            float phase = !fighter.Alive ? 1 : fighter.Action == DuelAction.Stagger ? fighter.Progress : Mathf.Repeat(time / (moving ? 1.4f : 2.8f), 1);
            if (attack) Rig.BindPose(); else Rig.SamplePose(clip, phase);
            string expression = !fighter.Alive || fighter.Action == DuelAction.Stagger ? "Hurt" :
                attack && fighter.Strike.Kind == BlightAttack.Smash ? "Shout" : "Angry";
            if (Rig.Expression != expression) Rig.SetExpression(expression);
            if (!fighter.Alive || fighter.Action == DuelAction.Stagger) return;

            // Capture feet before the attack crouch. Leg IK keeps them planted.
            Vector3 rightFoot = Rig.Bone("Foot_R").position, leftFoot = Rig.Bone("Foot_L").position;
            Vector3 right = new Vector3(.59f, .64f, .14f), left = new Vector3(-.57f, .69f, .13f);
            float lean = 10, twist = 0, dip = 0;
            if (fighter.Blocking && fighter.CanAct)
            {
                right = new Vector3(.32f, 1.50f, .55f); left = new Vector3(-.32f, 1.45f, .5f);
                lean = 16;
            }
            if (attack)
            {
                float charge = wind ? Mathf.SmoothStep(0, 1, fighter.Progress / .8f) : 1;
                float strike = wind ? Mathf.SmoothStep(0, 1, (fighter.Progress - .8f) / .2f) : 1;
                float settle = recover ? Mathf.SmoothStep(0, 1, fighter.Progress) : 0;
                if (fighter.Strike.Kind == BlightAttack.Smash)
                {
                    right = Vector3.Lerp(right, new Vector3(.26f, 2.02f, .08f), charge);
                    left = Vector3.Lerp(left, new Vector3(-.26f, 2.02f, .08f), charge);
                    right = Vector3.Lerp(right, new Vector3(.22f, .32f, .58f), strike);
                    left = Vector3.Lerp(left, new Vector3(-.22f, .32f, .58f), strike);
                    // Carry the fists in front of the face/chest on the downstroke.
                    right.z += Mathf.Sin(strike * Mathf.PI) * .42f;
                    left.z += Mathf.Sin(strike * Mathf.PI) * .42f;
                    lean = Mathf.Lerp(10, 55, strike); dip = -.10f * strike;
                    right = Vector3.Lerp(right, new Vector3(.59f, .64f, .14f), settle);
                    left = Vector3.Lerp(left, new Vector3(-.57f, .69f, .13f), settle);
                    lean = Mathf.Lerp(lean, 10, settle); dip *= 1 - settle;
                }
                else
                {
                    right = Vector3.Lerp(right, new Vector3(.93f, 1.30f, -.12f), charge);
                    right = Vector3.Lerp(right, new Vector3(.18f, 1.14f, .85f), strike);
                    twist = Mathf.Lerp(30 * charge, 0, strike);
                    if (recover)
                    {
                        float follow = Mathf.SmoothStep(0, 1, fighter.Progress / .25f);
                        float returnToRest = Mathf.SmoothStep(0, 1, (fighter.Progress - .25f) / .75f);
                        right = Vector3.Lerp(right, new Vector3(-.34f, 1.16f, .46f), follow);
                        right = Vector3.Lerp(right, new Vector3(.59f, .64f, .14f), returnToRest);
                        twist = -35 * follow * (1 - returnToRest);
                    }
                    left = Vector3.Lerp(left, new Vector3(-.52f, .98f, .28f), charge);
                    left = Vector3.Lerp(left, new Vector3(-.57f, .69f, .13f), settle);
                }
            }
            Rig.Bone("Pelvis").localPosition += Vector3.up * dip;
            Rig.Bone("Spine").localRotation *= Quaternion.Euler(lean, twist, -3);
            Rig.Bone("Neck").localRotation *= Quaternion.Euler(-lean * .5f, 0, 0);
            Hand("R", right); Hand("L", left);
            if (attack)
            {
                Foot("R", rightFoot); Foot("L", leftFoot);
            }
            foreach (string side in new[] {"R", "L"})
                foreach (string finger in new[] {"Thumb", "Index", "Middle", "Ring", "Little"})
                    for (int segment = 1; segment <= 3; segment++)
                        Rig.Bone(finger + segment + "_" + side).localRotation = Quaternion.Euler(-65, 0, 0);
        }

        private void Hand(string side, Vector3 localTarget)
        {
            Transform hand = Rig.Bone("Hand_" + side);
            ModularHumanRig.SolveGrip(Rig.Bone("UpperArm_" + side), Rig.Bone("Forearm_" + side), hand,
                transform.TransformPoint(localTarget), transform.right * (side == "R" ? 1 : -1) - transform.forward * .2f);
            hand.rotation = transform.rotation * Quaternion.Euler(-20, 0, side == "R" ? -10 : 10);
            HandTargetError = Mathf.Max(HandTargetError,
                Vector3.Distance(transform.InverseTransformPoint(hand.position), localTarget));
        }
        private void Foot(string side, Vector3 target)
        {
            Transform foot = Rig.Bone("Foot_" + side); Quaternion rotation = foot.rotation;
            ModularHumanRig.SolveGrip(Rig.Bone("Thigh_" + side), Rig.Bone("Shin_" + side), foot, target, transform.forward);
            foot.rotation = rotation;
        }
    }
}
