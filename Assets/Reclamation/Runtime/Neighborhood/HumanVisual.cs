using Reclamation.Outbreak;
using UnityEngine;

namespace Reclamation.Neighborhood
{
    // Presentation only: movement, collisions, and infection remain on the actor root.
    public sealed class HumanVisual : MonoBehaviour
    {
        [SerializeField] private Transform leftArm, rightArm, leftLeg, rightLeg;
        [SerializeField] private Renderer skin;
        private CivilianRoutine routine;
        private OutbreakAgent infection;
        private Combatant combat;
        private Vector3 restingPosition;
        private Vector3 previousPosition;
        private float phase;
        private Color naturalSkin;
        private MaterialPropertyBlock tint;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public void Configure(Transform armL, Transform armR, Transform legL, Transform legR, Renderer face)
        {
            leftArm = armL; rightArm = armR; leftLeg = legL; rightLeg = legR; skin = face;
        }

        private void Awake()
        {
            routine = GetComponentInParent<CivilianRoutine>();
            infection = GetComponentInParent<OutbreakAgent>();
            combat = GetComponentInParent<Combatant>();
            restingPosition = transform.localPosition;
            naturalSkin = skin.sharedMaterial.color;
            tint = new MaterialPropertyBlock();
        }

        private void OnEnable() => previousPosition = transform.position;

        private void LateUpdate()
        {
            Vector3 delta = transform.position - previousPosition;
            previousPosition = transform.position;
            if (routine != null && routine.Clock != null && routine.Clock.Paused) return;
            delta.y = 0;
            float distance = delta.magnitude;
            phase = (phase + Mathf.Min(distance, 1f) * 5f) % (Mathf.PI * 2);
            bool turned = infection != null && infection.State == InfectionState.Turned;
            float swing = distance > 0.001f ? Mathf.Sin(phase) * 25f : 0;
            leftLeg.localRotation = Quaternion.Euler(swing, 0, 0);
            rightLeg.localRotation = Quaternion.Euler(-swing, 0, 0);
            leftArm.localRotation = Quaternion.Euler(turned ? -65 : -swing, 0, -5);
            rightArm.localRotation = Quaternion.Euler(turned ? -65 : swing, 0, 5);
            transform.localPosition = restingPosition;
            transform.localRotation = Quaternion.identity;
            if (combat != null && combat.Handled)
            {
                float p = combat.Progress;
                float pulse = Mathf.Sin(p * Mathf.PI);
                switch (combat.Action)
                {
                    case CombatAction.Strike:
                        rightArm.localRotation = Quaternion.Euler(-150 + p * 95, -35 + p * 70, 10);
                        transform.localRotation = Quaternion.Euler(0, -20 + p * 40, 0); break;
                    case CombatAction.Shove:
                    case CombatAction.Lunge:
                        leftArm.localRotation = Quaternion.Euler(-55 - p * 45, 0, -12);
                        rightArm.localRotation = Quaternion.Euler(-55 - p * 45, 0, 12);
                        transform.localRotation = Quaternion.Euler(p * 18, 0, 0); break;
                    case CombatAction.Bite:
                        leftArm.localRotation = Quaternion.Euler(-85, -25, -25);
                        rightArm.localRotation = Quaternion.Euler(-85, 25, 25);
                        // Pull back, then snap forward near the actual contact time.
                        transform.localRotation = Quaternion.Euler(p < 0.7f ? -12 * pulse : Mathf.Lerp(-10, 35, (p - 0.7f) / 0.3f), 0, 0);
                        transform.localPosition += Vector3.forward * p * 0.25f; break;
                    case CombatAction.Grabbed:
                        leftArm.localRotation = Quaternion.Euler(-100, 0, -35);
                        rightArm.localRotation = Quaternion.Euler(-100, 0, 35);
                        transform.localRotation = Quaternion.Euler(-15, 0, Mathf.Sin(p * 20) * 8); break;
                    case CombatAction.Dodge:
                        transform.localRotation = Quaternion.Euler(-12 * pulse, 0, -20 * pulse);
                        transform.localPosition += Vector3.down * pulse * 0.22f; break;
                    case CombatAction.Stagger:
                        transform.localRotation = Quaternion.Euler(-25 * (1 - p), 0, 10 * (1 - p)); break;
                    case CombatAction.Sweep:
                        // The actor's facing stays locked; only the visible torso winds up.
                        float wind = Mathf.Clamp01(p / 0.75f);
                        float release = Mathf.Clamp01((p - 0.75f) / 0.25f);
                        leftArm.localRotation = Quaternion.Euler(-65, -35, -65);
                        rightArm.localRotation = Quaternion.Euler(-65, 35, 65);
                        transform.localRotation = Quaternion.Euler(-10 * wind, -50 * wind + 110 * release, 0);
                        break;
                    case CombatAction.KnockedBack:
                        leftArm.localRotation = Quaternion.Euler(-110, 0, -40);
                        rightArm.localRotation = Quaternion.Euler(-110, 0, 40);
                        transform.localRotation = Quaternion.Euler(-35 * (1 - p), 0, 12 * (1 - p)); break;
                    case CombatAction.Recover:
                        if (combat.SweepRecovery)
                        {
                            transform.localRotation = Quaternion.Euler(25 * (1 - p), 35 * (1 - p), 0);
                            leftArm.localRotation = Quaternion.Euler(-30, 0, -30 * (1 - p));
                            rightArm.localRotation = Quaternion.Euler(-30, 0, 30 * (1 - p));
                        }
                        break;
                }
            }
            // Exposed people deliberately look identical to healthy people.
            Color color = turned ? new Color(0.36f, 0.61f, 0.29f) :
                infection != null && infection.State == InfectionState.Symptomatic
                    ? new Color(0.8f, 0.72f, 0.36f) : naturalSkin;
            tint.SetColor(BaseColor, color);
            skin.SetPropertyBlock(tint);
        }
    }
}
