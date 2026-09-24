using UnityEngine;

namespace Reclamation.Blight
{
    public enum CombatImpact { None, Hit, Block, Dodge, Interrupt, Sever, Defeat, Armoured, GuardBreak }

    public sealed partial class BlightCombatLab
    {
        private sealed class ImpactFeedback
        {
            public CombatImpact kind;
            public float until;
            public Renderer[] renderers;
            public MaterialPropertyBlock tint;
        }
        private AudioSource impactAudio;
        private AudioClip hitSound, blockSound, breakSound;
        private bool combatAudioEnabled = true, combatFeedbackEnabled = true;
        private float nextImpactSound;
        public int FeedbackEventCount { get; private set; }
        public CombatImpact LastCombatImpact { get; private set; }
        public bool CombatAudioEnabled
        {
            get => combatAudioEnabled;
            set { combatAudioEnabled = value; if (!value && impactAudio != null) impactAudio.Stop(); }
        }
        public bool CombatFeedbackEnabled
        {
            get => combatFeedbackEnabled;
            set { combatFeedbackEnabled = value; if (!value) ClearCombatFeedback(); }
        }

        public static CombatImpact ClassifyImpact(string result, DuelAction previousAction, DuelAction currentAction)
        {
            switch (result)
            {
                case "Blocked": return CombatImpact.Block;
                case "Dodged": return CombatImpact.Dodge;
                case "severed": return CombatImpact.Sever;
                case "Defeated": return CombatImpact.Defeat;
                case "Armoured windup": return CombatImpact.Armoured;
                case "Guard broken": return CombatImpact.GuardBreak;
                case "Hit": return previousAction == DuelAction.Windup && currentAction == DuelAction.Stagger
                    ? CombatImpact.Interrupt : CombatImpact.Hit;
                default: return CombatImpact.None;
            }
        }

        private void ShowCombatImpact(Actor target, string result, DuelAction previousAction)
        {
            if (!combatFeedbackEnabled) return;
            CombatImpact kind = ClassifyImpact(result, previousAction, target.fighter.Action);
            if (kind == CombatImpact.None) return;
            if (target == player) AddCameraImpact(kind);
            LastCombatImpact = kind; FeedbackEventCount++;
            target.feedback.kind = kind; target.feedback.until = simulationTime + 0.55f;
            if (combatAudioEnabled && kind != CombatImpact.Dodge && simulationTime >= nextImpactSound)
            {
                EnsureCombatAudio();
                impactAudio.PlayOneShot(kind == CombatImpact.Block || kind == CombatImpact.Armoured ? blockSound :
                    kind == CombatImpact.Sever || kind == CombatImpact.Interrupt || kind == CombatImpact.GuardBreak ? breakSound : hitSound, 0.22f);
                nextImpactSound = simulationTime + 0.065f;
            }
        }

        private static Color ImpactColor(CombatImpact kind)
        {
            if (kind == CombatImpact.Block || kind == CombatImpact.Armoured) return new Color(0.95f, 0.8f, 0.3f);
            if (kind == CombatImpact.Dodge) return new Color(0.35f, 0.85f, 1);
            if (kind == CombatImpact.Sever || kind == CombatImpact.Interrupt || kind == CombatImpact.GuardBreak)
                return new Color(1, 0.5f, 0.2f);
            return new Color(1, 0.85f, 0.75f);
        }

        private void UpdateCombatFeedback()
        {
            foreach (Actor actor in actors)
            {
                ImpactFeedback f = actor.feedback;
                bool active = combatFeedbackEnabled && simulationTime < f.until;
                if (active && f.renderers == null)
                {
                    f.renderers = actor.root.GetComponentsInChildren<Renderer>(true);
                    f.tint = new MaterialPropertyBlock();
                }
                if (f.renderers == null) continue;
                float strength = active && f.kind != CombatImpact.Dodge ?
                    Mathf.Clamp01((f.until - simulationTime - 0.31f) / 0.24f) * 0.65f : 0;
                foreach (Renderer renderer in f.renderers)
                {
                    if (renderer == null) continue;
                    f.tint.Clear();
                    if (strength > 0 && renderer.sharedMaterial != null)
                    {
                        Color tint = Color.Lerp(renderer.sharedMaterial.color, ImpactColor(f.kind), strength);
                        f.tint.SetColor("_BaseColor", tint); f.tint.SetColor("_Color", tint);
                    }
                    renderer.SetPropertyBlock(f.tint);
                }
                if (active && actor.enemy && actor.fighter.Alive && strength > 0 &&
                    (f.kind == CombatImpact.Hit || f.kind == CombatImpact.Armoured || f.kind == CombatImpact.Block))
                {
                    ModularHumanRig visualRig = actor.thrall != null ? actor.thrall.Rig :
                        actor.hulkVisual != null ? actor.hulkVisual.Rig : null;
                    // Head-only flinch preserves attack/weapon/contact geometry.
                    if (visualRig != null)
                        visualRig.Bone("Neck").localRotation *= Quaternion.Euler(-24 * strength, 0, 10 * strength);
                }
                // The head is cosmetic: it is not used by limb contact queries.
                // Never move the actor root, sword or region joints for feedback.
                if (actor.rig != null && actor.fighter.Alive && strength > 0)
                    actor.rig.head.localRotation *= Quaternion.Euler(-18 * strength, 0, 8 * strength);
            }
        }

        private void ClearCombatFeedback()
        {
            foreach (Actor actor in actors)
            {
                actor.feedback.until = 0;
                if (actor.feedback.renderers != null)
                    foreach (Renderer renderer in actor.feedback.renderers) if (renderer != null) renderer.SetPropertyBlock(null);
            }
            if (impactAudio != null) impactAudio.Stop();
            cameraImpact = 0;
            FeedbackEventCount = 0; LastCombatImpact = CombatImpact.None; nextImpactSound = 0;
        }

        private void DrawCombatFeedback()
        {
            if (!combatFeedbackEnabled) return;
            Color original = GUI.color;
            foreach (Actor actor in actors)
            {
                float height = actor.limbs != null && actor.limbs.Crawling ? 1.3f : actor.size * 2.7f;
                Vector3 screen = view.WorldToScreenPoint(actor.root.position + Vector3.up * height);
                if (screen.z <= view.nearClipPlane || screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height) continue;
                float x = screen.x / UiScale, y = (Screen.height - screen.y) / UiScale;
                ImpactFeedback impact = actor.feedback;
                if (simulationTime < impact.until)
                {
                    float remaining = impact.until - simulationTime;
                    Color color = ImpactColor(impact.kind); color.a = Mathf.Clamp01(remaining / 0.15f);
                    GUI.color = color;
                    string text = impact.kind == CombatImpact.Interrupt ? "INTERRUPTED" :
                        impact.kind == CombatImpact.Sever ? "LIMB LOST" :
                        impact.kind == CombatImpact.GuardBreak ? "GUARD BROKEN" :
                        impact.kind == CombatImpact.Armoured ? "ARMOURED" : impact.kind.ToString().ToUpperInvariant();
                    GUI.Box(new Rect(x - 85, y - (0.55f - remaining) * 28, 170, 26), text, centered);
                }
                else if (actor.enemy && actor.fighter.Alive)
                {
                    DuelFighter fighter = actor.fighter;
                    if (fighter.Action == DuelAction.Windup)
                    {
                        GUI.color = fighter.Heavy ? new Color(1, 0.55f, 0.25f) : new Color(1, 0.86f, 0.3f);
                        string tell = fighter.Strike.Kind == BlightAttack.Sweep ? "< SWEEP >" :
                            fighter.Strike.Kind == BlightAttack.Smash ? "! SMASH !" : fighter.Heavy ? "! HEAVY !" : "! STRIKE !";
                        GUI.Box(new Rect(x - 70, y, 140, 25), tell, centered);
                    }
                    else if (fighter.Action == DuelAction.Recovery || fighter.Action == DuelAction.Stagger)
                    {
                        GUI.color = new Color(0.35f, 0.9f, 0.8f);
                        GUI.Box(new Rect(x - 65, y, 130, 25), fighter.Action == DuelAction.Stagger ? "STAGGERED" : "OPENING", centered);
                    }
                }
                GUI.color = original;
            }
            GUI.color = original;
        }

        private void EnsureCombatAudio()
        {
            if (impactAudio != null) return;
            impactAudio = gameObject.AddComponent<AudioSource>(); impactAudio.playOnAwake = false;
            impactAudio.spatialBlend = 0;
            hitSound = MakeImpactSound("Prototype hit", 110, 0.12f, 0.55f);
            blockSound = MakeImpactSound("Prototype block", 760, 0.15f, 0.15f);
            breakSound = MakeImpactSound("Prototype break", 65, 0.2f, 0.65f);
        }

        private static AudioClip MakeImpactSound(string name, float frequency, float duration, float noiseMix)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(duration * rate)];
            var noise = new System.Random(715); // Does not consume gameplay randomness.
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate, progress = i / (float)samples.Length;
                float envelope = Mathf.Min(1, t / 0.004f) * Mathf.Pow(1 - progress, 3);
                samples[i] = envelope * ((1 - noiseMix) * Mathf.Sin(2 * Mathf.PI * frequency * t) +
                    noiseMix * ((float)noise.NextDouble() * 2 - 1)) * 0.6f;
            }
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }

        private void DestroyCombatAudio()
        {
            if (impactAudio != null) { impactAudio.Stop(); Destroy(impactAudio); }
            if (hitSound != null) Destroy(hitSound);
            if (blockSound != null) Destroy(blockSound);
            if (breakSound != null) Destroy(breakSound);
        }
    }
}
