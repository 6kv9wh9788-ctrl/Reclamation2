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
            // Exposed people deliberately look identical to healthy people.
            Color color = turned ? new Color(0.36f, 0.61f, 0.29f) :
                infection != null && infection.State == InfectionState.Symptomatic
                    ? new Color(0.8f, 0.72f, 0.36f) : naturalSkin;
            tint.SetColor(BaseColor, color);
            skin.SetPropertyBlock(tint);
        }
    }
}
