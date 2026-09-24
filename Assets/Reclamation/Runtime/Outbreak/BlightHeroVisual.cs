using UnityEngine;

namespace Reclamation.Blight
{
    public enum BlightHumanLook { Hero, Mara, Bren }
    // Presentation only: this component never moves the actor or applies damage.
    public sealed class BlightHeroVisual : MonoBehaviour
    {
        public ModularHumanRig Rig { get; private set; }
        public BlightWeapon Weapon { get; private set; }
        public BlightHumanLook Look { get; private set; }
        private Transform spear;
        private Mesh spearMesh;
        private Material spearMaterial;

        public void Build(BlightHumanLook look = BlightHumanLook.Hero)
        {
            Look = look;
            Rig = gameObject.AddComponent<ModularHumanRig>();
            Rig.Build(false, false, false);
            Rig.SetHair(look == BlightHumanLook.Mara);
            if (look == BlightHumanLook.Mara)
                Rig.SetUniformColors(new Color(.16f, .36f, .25f), new Color(.55f, .40f, .18f));
            else if (look == BlightHumanLook.Bren)
                Rig.SetUniformColors(new Color(.39f, .30f, .15f), new Color(.18f, .29f, .43f));
            BuildSpear(); Equip(look == BlightHumanLook.Bren ? BlightWeapon.Spear : BlightWeapon.Sword);
            Pose(new DuelFighter(), false, 0);
        }

        public void Equip(BlightWeapon weapon)
        {
            Weapon = weapon;
            Rig.SetAppearance(Look != BlightHumanLook.Mara, weapon == BlightWeapon.Axe,
                Look == BlightHumanLook.Mara ? .75f : 0);
            Rig.SetBuiltInWeaponsVisible(weapon != BlightWeapon.Spear);
            Rig.SpearGrip = weapon == BlightWeapon.Spear;
            spear.gameObject.SetActive(Rig.SpearGrip);
        }

        public void Pose(DuelFighter fighter, bool moving, float simulationTime)
        {
            string clip = "Idle", expression = "Determined";
            float phase = Mathf.Repeat(simulationTime / 2, 1);
            bool attack = fighter.Action == DuelAction.Windup || fighter.Action == DuelAction.Recovery;
            Rig.TwoHandGrip = fighter.Alive && fighter.Action != DuelAction.Dodge && fighter.Action != DuelAction.Stagger;
            if (!fighter.Alive)
            {
                // The lab stops simulation at defeat. Use the completed death
                // pose rather than leaving a standing body or a crushed root.
                clip = "Death"; phase = 1; expression = "Hurt";
            }
            else if (attack)
            {
                clip = fighter.Heavy ? "HeavyAttack" : "LightAttack";
                // Damage resolves at the transition to Recovery. The visible
                // cutting stroke reaches its forward contact pose there.
                phase = fighter.Action == DuelAction.Windup ?
                    (fighter.Progress < .7f ? fighter.Progress / .7f * .3f :
                        Mathf.Lerp(.3f, .5f, (fighter.Progress - .7f) / .3f)) :
                    Mathf.Lerp(.5f, 1, fighter.Progress);
                expression = fighter.Heavy ? "Angry" : "Determined";
            }
            else if (fighter.Action == DuelAction.Dodge)
            { clip = "Dodge"; phase = fighter.Progress; }
            else if (fighter.Action == DuelAction.Stagger)
            { clip = "Stagger"; phase = fighter.Progress; expression = "Hurt"; }
            else if (fighter.Blocking)
            { clip = "Block"; phase = .5f; }
            else if (moving)
            { clip = "Run"; phase = Mathf.Repeat(simulationTime / .65f, 1); }
            if (Rig.Expression != expression) Rig.SetExpression(expression);
            Rig.SamplePose(clip, phase);
        }

        private void BuildSpear()
        {
            spear = new GameObject("Hero spear").transform;
            spear.SetParent(Rig.Bone("WeaponSocket_R"), false);
            // An eight-sided shaft and a pointed steel head. Vertex colors are
            // not required by the lab's standard/URP material.
            var vertices = new Vector3[20];
            var triangles = new int[8 * 6 + 8 * 3];
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4;
                vertices[i] = new Vector3(Mathf.Cos(a) * .025f, Mathf.Sin(a) * .025f, -.55f);
                vertices[i + 8] = new Vector3(vertices[i].x, vertices[i].y, 2.35f);
                int n = (i + 1) % 8, t = i * 6;
                triangles[t] = i; triangles[t + 1] = n; triangles[t + 2] = i + 8;
                triangles[t + 3] = n; triangles[t + 4] = n + 8; triangles[t + 5] = i + 8;
            }
            vertices[16] = new Vector3(-.11f, 0, 2.37f);
            vertices[17] = new Vector3(.11f, 0, 2.37f);
            vertices[18] = new Vector3(0, .045f, 2.37f);
            vertices[19] = new Vector3(0, 0, 2.78f);
            // Double-sided triangular spearhead, sharing the shaft material.
            int[] head = {16,18,19,18,17,19,17,16,19,16,17,18,
                          19,18,16,19,17,18,19,16,17,18,17,16};
            System.Array.Copy(head, 0, triangles, 48, head.Length);
            spearMesh = new Mesh { name = "Combat spear" };
            spearMesh.vertices = vertices; spearMesh.triangles = triangles;
            spearMesh.RecalculateNormals(); spearMesh.RecalculateBounds();
            spear.gameObject.AddComponent<MeshFilter>().sharedMesh = spearMesh;
            spearMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            spearMaterial.color = new Color(.48f, .51f, .54f);
            spear.gameObject.AddComponent<MeshRenderer>().sharedMaterial = spearMaterial;
        }

        private void OnDestroy()
        {
            if (spearMesh != null) Destroy(spearMesh);
            if (spearMaterial != null) Destroy(spearMaterial);
        }
    }
}
