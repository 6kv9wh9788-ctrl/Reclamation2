using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        [Header("Camera comfort")]
        [SerializeField, Range(0, 1)] private float cameraShakeStrength = .65f;
        [SerializeField, Range(0, 1)] private float cameraBobStrength = .5f;
        private bool cameraManual, sprintHeld, sprintExhausted;
        private float cameraImpact, cameraImpactStarted, bobPhase, bobBlend;
        private float cameraDistance = 7.5f;
        private Vector3 cameraFocus;
        public bool IsSprinting { get; private set; }
        public bool CameraMotionEnabled { get; set; } = true;
        public float CameraShakeStrength { get => cameraShakeStrength; set => cameraShakeStrength = Mathf.Clamp01(value); }
        public float CameraBobStrength { get => cameraBobStrength; set => cameraBobStrength = Mathf.Clamp01(value); }
        public bool CameraLockActive => locked && player != null && selectedEnemy != null && selectedEnemy.fighter.Alive &&
            Vector3.Distance(player.root.position, selectedEnemy.root.position) <= 18;
        public float CameraYaw => yaw;
        public float CameraImpactStrength => CameraMotionEnabled ? cameraImpact * cameraShakeStrength : 0;
        public Vector3 CameraMotionOffset { get; private set; }

        // Shared input path for keyboard controls and deterministic play tests.
        public void SetPlayerLocomotion(Vector3 direction, bool sprint, bool block)
        {
            if (paused || Ended) return;
            direction.y = 0; playerMove = Vector3.ClampMagnitude(direction, 1);
            sprintHeld = sprint; playerBlock = block;
        }

        public void SetCameraOrbit(Vector2 delta, bool manual)
        {
            if (paused || Ended) return;
            cameraManual = manual;
            if (!manual) return;
            yaw += delta.x * .18f; pitch = Mathf.Clamp(pitch - delta.y * .15f, 12, 60);
        }

        private void ResetCombatCamera()
        {
            sprintHeld = sprintExhausted = cameraManual = IsSprinting = false;
            cameraImpact = cameraImpactStarted = bobPhase = bobBlend = 0;
            cameraDistance = 7.5f; cameraFocus = player.root.position + Vector3.up * 1.3f;
            CameraMotionOffset = Vector3.zero;
        }

        private void AddCameraImpact(CombatImpact kind)
        {
            if (!CameraMotionEnabled) return;
            float strength = kind == CombatImpact.Block ? .8f : kind == CombatImpact.GuardBreak ? 1 :
                kind == CombatImpact.Hit || kind == CombatImpact.Interrupt ? .55f : 0;
            if (strength <= 0) return;
            cameraImpact = Mathf.Min(1, cameraImpact + strength);
            cameraImpactStarted = simulationTime;
        }

        private void UpdateCombatCamera(float dt, Vector3 previousPlayerPosition)
        {
            float follow = 1 - Mathf.Exp(-8 * dt);
            Vector3 focus = player.root.position + Vector3.up * 1.3f;
            float distance = 7.5f;
            if (CameraLockActive && !cameraManual)
            {
                Vector3 direction = selectedEnemy.root.position - player.root.position; direction.y = 0;
                if (direction.sqrMagnitude > .01f)
                    yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, follow);
                // A modest shared focus and pullback keep nearby target/hero in frame.
                focus += Vector3.ClampMagnitude(direction * .4f, 4);
                distance = Mathf.Clamp(7.5f + direction.magnitude * .25f, 7.5f, 12);
            }
            cameraFocus = Vector3.Lerp(cameraFocus, focus, follow);
            cameraDistance = Mathf.Lerp(cameraDistance, distance, follow);
            float moved = Vector3.Distance(previousPlayerPosition, player.root.position);
            bool walking = player.fighter.CanAct && moved > .0001f;
            bobBlend = Mathf.MoveTowards(bobBlend, walking ? 1 : 0, dt * 6);
            if (walking) bobPhase += Mathf.Min(moved, .2f) * 4;
            cameraImpact = Mathf.MoveTowards(cameraImpact, 0, dt * 3.8f);
        }

        private void RenderCombatCamera()
        {
            if (view == null || player == null) return;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            float shake = CameraImpactStrength;
            float clock = simulationTime - cameraImpactStarted;
            float bob = CameraMotionEnabled ? cameraBobStrength * bobBlend : 0;
            Vector3 localOffset = new Vector3(Mathf.Sin(clock * 83) * .065f * shake,
                Mathf.Sin(clock * 107) * .045f * shake + Mathf.Sin(bobPhase * 2) * .036f * bob, 0);
            CameraMotionOffset = rotation * localOffset;
            Quaternion recoil = Quaternion.Euler(Mathf.Cos(clock * 91) * .7f * shake, 0, Mathf.Sin(clock * 73) * .45f * shake);
            view.transform.SetPositionAndRotation(cameraFocus - rotation * Vector3.forward * cameraDistance + CameraMotionOffset, rotation * recoil);
        }
    }
}
