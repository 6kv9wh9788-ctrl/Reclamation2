using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        private sealed class LimbRig
        {
            public Transform body, head, grip, bladeBase, bladeTip, swordModel, axeModel;
            public readonly Transform[] joints = new Transform[5], bends = new Transform[5], caps = new Transform[5];
            public readonly Vector3[] previous = new Vector3[5];
            public Vector3 previousBase, previousTip;
            public SwingHeight height;
            public bool hit;
        }
        private sealed class LimbDebris
        {
            public Transform root;
            public Vector3 velocity;
            public float age;
        }
        private readonly List<LimbDebris> limbDebris = new List<LimbDebris>();
        private bool lowGore = true;
        public bool LowGore
        {
            get => lowGore;
            set { lowGore = value; RefreshLimbVisibility(); }
        }
        public bool LimbPractice { get; set; }
        public SwingHeight LimbAim { get; private set; }
        public BodyRegion? LastLimbHit { get; private set; }
        public bool SetLimbAim(SwingHeight height)
        {
            if (player == null || paused || !player.fighter.CanAct || (int)height < 0 || (int)height > 2) return false;
            LimbAim = height; return true;
        }
        public BlightLimbs GetLimbs(Transform actorRoot)
        {
            foreach (Actor actor in actors) if (actor.root == actorRoot) return actor.limbs;
            return null;
        }

        private Transform Rounded(Transform parent, string name, Vector3 position, Vector3 scale, Material material,
            PrimitiveType shape = PrimitiveType.Sphere)
        {
            var go = GameObject.CreatePrimitive(shape); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Collider>().enabled = false; go.GetComponent<Renderer>().sharedMaterial = material;
            return go.transform;
        }

        private void BuildLimbRig(Actor actor)
        {
            foreach (Transform child in actor.root) child.gameObject.SetActive(false);
            var rig = new LimbRig(); actor.rig = rig;
            if (actor.enemy) actor.limbs = new BlightLimbs();
            Material skin = Material(actor.enemy ? new Color(0.42f, 0.49f, 0.32f) : new Color(0.72f, 0.54f, 0.39f));
            Material cloth = Material(actor.enemy ? new Color(0.29f, 0.18f, 0.26f) : new Color(0.12f, 0.32f, 0.51f));
            Material steel = Material(new Color(0.57f, 0.65f, 0.72f));
            rig.body = Pivot(actor.root, "Articulated body", Vector3.up * 0.85f);
            Rounded(rig.body, "Pelvis", Vector3.up * 0.02f, new Vector3(0.44f, 0.3f, 0.31f), cloth);
            Rounded(rig.body, "Chest", Vector3.up * 0.4f, new Vector3(0.58f, 0.67f, 0.36f), cloth);
            if (!actor.enemy) Rounded(rig.body, "Breastplate", new Vector3(0, 0.46f, 0.13f), new Vector3(0.49f, 0.48f, 0.14f), steel);
            Part(rig.body, "Belt", Vector3.up * 0.09f, new Vector3(0.45f, 0.09f, 0.32f), Material(new Color(0.23f, 0.17f, 0.12f)));
            rig.head = Pivot(rig.body, "Neck", Vector3.up * 0.78f);
            Rounded(rig.head, "Head", Vector3.up * 0.1f, new Vector3(0.32f, 0.39f, 0.31f), skin);
            for (int side = -1; side <= 1; side += 2)
                Rounded(rig.head, "Eye", new Vector3(side * 0.075f, 0.14f, 0.148f), new Vector3(0.045f, 0.033f, 0.026f),
                    Material(actor.enemy ? new Color(0.95f, 0.66f, 0.14f) : Color.black));
            for (int i = 1; i <= 4; i++)
            {
                bool arm = i <= 2; float sign = i == 1 || i == 3 ? 1 : -1;
                Vector3 joint = new Vector3(sign * (arm ? 0.4f : 0.16f), arm ? 0.59f : -0.04f, 0);
                rig.joints[i] = Pivot(rig.body, ((BodyRegion)i).ToString(), joint);
                Rounded(rig.joints[i], "Joint", Vector3.zero, Vector3.one * 0.2f, arm ? skin : cloth);
                Rounded(rig.joints[i], "Upper segment", Vector3.down * 0.19f, new Vector3(0.18f, 0.19f, 0.2f), arm ? skin : cloth, PrimitiveType.Capsule);
                rig.bends[i] = Pivot(rig.joints[i], arm ? "Elbow" : "Knee", Vector3.down * 0.38f);
                Rounded(rig.bends[i], "Joint", Vector3.zero, Vector3.one * 0.18f, skin);
                Rounded(rig.bends[i], "Lower segment", Vector3.down * 0.19f, new Vector3(0.15f, 0.19f, 0.17f), arm ? skin : cloth, PrimitiveType.Capsule);
                Rounded(rig.bends[i], arm ? "Hand" : "Boot", new Vector3(0, -0.36f, arm ? 0 : 0.05f),
                    new Vector3(0.17f, 0.17f, arm ? 0.16f : 0.3f), arm ? skin : cloth);
                rig.caps[i] = Rounded(rig.body, "Sever cap " + (BodyRegion)i, joint, Vector3.one * 0.17f, CapMaterial());
                rig.caps[i].gameObject.SetActive(false);
            }
            if (!actor.enemy)
            {
                rig.grip = Pivot(actor.root, "Swept weapon", Vector3.zero);
                rig.swordModel = Pivot(rig.grip, "Sword model", Vector3.zero);
                Part(rig.swordModel, "Blade", new Vector3(0, 0, 1), new Vector3(0.07f, 0.035f, 1.7f), steel);
                Part(rig.swordModel, "Guard", new Vector3(0, 0, 0.12f), new Vector3(0.3f, 0.065f, 0.06f), steel);
                Part(rig.swordModel, "Hilt", Vector3.back * 0.02f, new Vector3(0.065f, 0.065f, 0.23f), cloth);
                rig.axeModel = Pivot(rig.grip, "Axe model", Vector3.zero);
                Part(rig.axeModel, "Handle", new Vector3(0, 0, 0.68f), new Vector3(0.08f, 0.08f, 1.45f),
                    Material(new Color(0.38f, 0.25f, 0.13f)));
                Part(rig.axeModel, "Axe head", new Vector3(0, 0, 1.4f), new Vector3(0.48f, 0.1f, 0.3f), steel);
                rig.bladeBase = Pivot(rig.grip, "Blade base", Vector3.forward * 0.15f);
                rig.bladeTip = Pivot(rig.grip, "Blade tip", Vector3.forward * 1.85f);
                EquipLimbWeapon(actor);
            }
            InstallStylizedArt(actor);
        }

        private void InstallStylizedArt(Actor actor)
        {
            CharacterArtData data = StylizedCharacterArt.Load(actor.enemy);
            if (data == null) return; // Original primitives remain a functional fallback.
            LimbRig rig = actor.rig;
            foreach (Renderer renderer in rig.body.GetComponentsInChildren<Renderer>(true))
                if (!renderer.name.StartsWith("Sever cap")) renderer.enabled = false;
            Transform[] bones = { rig.body, rig.head, rig.joints[1], rig.bends[1], rig.joints[2], rig.bends[2],
                rig.joints[3], rig.bends[3], rig.joints[4], rig.bends[4] };
            actor.root.gameObject.AddComponent<StylizedCharacterArt>().Build(data, bones);
        }

        private void EquipLimbWeapon(Actor actor)
        {
            LimbRig rig = actor.rig;
            if (rig == null || rig.grip == null) return;
            bool axe = actor.weapon == BlightWeapon.Axe;
            rig.swordModel.gameObject.SetActive(!axe); rig.axeModel.gameObject.SetActive(axe);
            // Only the axe head deals cutting damage, not its handle.
            rig.bladeBase.localPosition = axe ? new Vector3(-0.24f, 0, 1.4f) : Vector3.forward * 0.15f;
            rig.bladeTip.localPosition = axe ? new Vector3(0.24f, 0, 1.4f) : Vector3.forward * 1.85f;
        }

        private Material CapMaterial() => Material(lowGore ? new Color(0.11f, 0.1f, 0.14f) : new Color(0.36f, 0.07f, 0.09f));
        private void PoseLimbRig(Actor actor)
        {
            LimbRig rig = actor.rig; DuelFighter f = actor.fighter;
            bool crawl = actor.limbs != null && actor.limbs.Crawling;
            bool wind = f.Action == DuelAction.Windup, swing = f.Action == DuelAction.Recovery;
            SwingHeight height = wind || swing ? rig.height : LimbAim;
            bool crouch = actor == player && height == SwingHeight.Legs && f.Alive;
            float bodyHeight = crawl || !f.Alive ? 0.3f : crouch ? 0.43f : 0.85f;
            rig.body.localPosition = Vector3.up * bodyHeight;
            rig.body.localRotation = Quaternion.Euler(!f.Alive ? 85 : crawl ? 75 : 0, 0, 0);
            rig.head.localRotation = Quaternion.Euler(actor.limbs != null && actor.limbs.BiteOnly && swing ? -25 : 0, 0, 0);
            for (int i = 1; i <= 4; i++)
            {
                if (actor.limbs != null && actor.limbs.Missing((BodyRegion)i)) continue;
                float stride = actor.moving ? Mathf.Sin(gait + (i % 2) * Mathf.PI) * 24 : 0;
                bool strikingArm = i == 1 || (i == 2 && actor.limbs != null && actor.limbs.Missing(BodyRegion.RightArm));
                float attack = wind ? Mathf.Lerp(0, -125, f.Progress) : swing ? Mathf.Lerp(-125, 20, Mathf.Clamp01(f.Progress / 0.3f)) : 0;
                rig.joints[i].localRotation = Quaternion.Euler(i >= 3 && crouch ? -60 :
                    i <= 2 && strikingArm && actor.enemy ? attack : stride, 0, 0);
                rig.bends[i].localRotation = Quaternion.Euler(i >= 3 && crouch ? 120 : i <= 2 ? -12 : Mathf.Max(0, -stride), 0, 0);
            }
            if (rig.grip == null) return;
            float elevation = height == SwingHeight.Arms ? 1.28f : height == SwingHeight.Legs ? 0.42f : 1;
            float yawAngle = wind ? Mathf.Lerp(0, -75, Mathf.SmoothStep(0, 1, f.Progress)) :
                swing ? Mathf.Lerp(-75, 75, Mathf.Clamp01((f.Duration - f.Remaining) / 0.22f)) : 10;
            rig.grip.localPosition = new Vector3(0.18f, elevation, 0.4f);
            rig.grip.localRotation = Quaternion.Euler(f.Blocking ? -60 : 0, yawAngle, 0);
            if (f.Alive) AimArm(rig.joints[1], rig.bends[1], rig.grip.position, actor.root.right);
            rig.grip.gameObject.SetActive(f.Alive);
        }

        private static void AimArm(Transform shoulder, Transform elbow, Vector3 hand, Vector3 bendDirection)
        {
            Vector3 delta = hand - shoulder.position;
            float distance = Mathf.Clamp(delta.magnitude, 0.02f, 0.759f);
            Vector3 axis = delta.normalized;
            Vector3 bend = Vector3.ProjectOnPlane(bendDirection, axis).normalized;
            Vector3 middle = shoulder.position + axis * distance * 0.5f + bend * Mathf.Sqrt(0.38f * 0.38f - distance * distance * 0.25f);
            shoulder.rotation = Quaternion.FromToRotation(Vector3.down, middle - shoulder.position);
            elbow.rotation = Quaternion.FromToRotation(Vector3.down, hand - elbow.position);
        }

        private Vector3 RegionCenter(Actor actor, int region)
            => region == 0 ? actor.rig.body.TransformPoint(Vector3.up * 0.38f) :
                actor.rig.joints[region].TransformPoint(Vector3.down * (region <= 2 ? 0.19f : 0.28f));

        private void CaptureLimbPose()
        {
            foreach (Actor actor in actors)
            {
                if (actor.rig == null) continue;
                for (int i = 0; i <= 4; i++)
                    if (actor.limbs == null || !actor.limbs.Missing((BodyRegion)i)) actor.rig.previous[i] = RegionCenter(actor, i);
                if (actor.rig.grip == null) continue;
                actor.rig.previousBase = actor.rig.bladeBase.position; actor.rig.previousTip = actor.rig.bladeTip.position;
                if (actor.fighter.Action == DuelAction.Windup) actor.rig.hit = false;
            }
        }

        private void ResolveLimbSweep()
        {
            if (player.rig == null || !player.fighter.Alive || player.rig.hit || player.fighter.Action != DuelAction.Recovery ||
                player.fighter.Duration - player.fighter.Remaining > 0.22f) return;
            LimbRig blade = player.rig;
            foreach (Actor target in actors)
            {
                if (!target.enemy || !target.fighter.Alive || target.limbs == null) continue;
                // Limbs before torso where contact volumes meet; one victim/region per committed swing.
                for (int n = 1; n <= 5; n++)
                {
                    int i = n % 5; BodyRegion region = (BodyRegion)i;
                    if (target.limbs.Missing(region) || !BlightBladeSweep.Hits(blade.previousBase, blade.previousTip,
                        blade.bladeBase.position, blade.bladeTip.position, target.rig.previous[i], RegionCenter(target, i), i == 0 ? 0.27f : 0.19f)) continue;
                    blade.hit = true; LastLimbHit = region;
                    float before = target.fighter.Health;
                    DuelAction previousAction = target.fighter.Action;
                    bool frontal = Vector3.Angle(target.root.forward, player.root.position - target.root.position) < 70;
                    string result = target.fighter.ReceiveAttack(player.fighter.Strike, frontal,
                        playerDamageScale * (i == 0 ? 1 : 0.3f));
                    if (target.fighter.Health < before && target.limbs.Damage(region,
                        BlightEquipment.LimbDamage(player.weapon, player.fighter.Heavy), true))
                    {
                        DetachLimb(target, i);
                        target.fighter.Interrupt(player.weapon == BlightWeapon.Axe ? Mathf.Max(0.8f, target.fighter.Remaining) : 0.8f);
                        result = "severed";
                    }
                    ShowCombatImpact(target, result, previousAction);
                    Say(region + ": " + result); Pose(target); return;
                }
            }
        }

        private void DetachLimb(Actor actor, int region)
        {
            Transform part = actor.rig.joints[region]; part.SetParent(transform, true);
            limbDebris.Add(new LimbDebris { root = part, velocity = player.root.right * 1.3f + Vector3.up * 2 });
            actor.rig.caps[region].gameObject.SetActive(true); RefreshLimbVisibility();
        }
        private void RefreshLimbVisibility()
        {
            foreach (LimbDebris debris in limbDebris) if (debris.root != null) debris.root.gameObject.SetActive(!lowGore);
            foreach (Actor actor in actors)
                if (actor.rig != null)
                    for (int i = 1; i <= 4; i++) actor.rig.caps[i].GetComponent<Renderer>().sharedMaterial = CapMaterial();
        }
        private void AdvanceLimbDebris(float dt)
        {
            for (int i = limbDebris.Count - 1; i >= 0; i--)
            {
                LimbDebris debris = limbDebris[i]; debris.age += dt;
                if (debris.age > 8) { Destroy(debris.root.gameObject); limbDebris.RemoveAt(i); continue; }
                if (debris.root.position.y > 0.45f || debris.velocity.y > 0)
                {
                    debris.velocity += Vector3.down * 9.8f * dt;
                    Vector3 next = debris.root.position + debris.velocity * dt; next.y = Mathf.Max(0.45f, next.y);
                    debris.root.position = next; debris.root.Rotate(Vector3.right, 100 * dt, Space.World);
                }
            }
        }
        private void ClearLimbDebris()
        {
            foreach (LimbDebris debris in limbDebris) if (debris.root != null) Destroy(debris.root.gameObject);
            limbDebris.Clear(); LimbAim = SwingHeight.Arms; LimbPractice = false; LastLimbHit = null;
        }
    }
}
