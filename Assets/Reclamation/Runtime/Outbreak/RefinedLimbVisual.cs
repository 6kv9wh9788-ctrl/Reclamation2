using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    // Retarget the contact rig onto the refined body. Severed pieces are baked
    // snapshots; live skinning bones are never detached or destroyed.
    public sealed class RefinedLimbVisual : MonoBehaviour
    {
        public ModularHumanRig Rig { get; private set; }
        private bool enemy;
        private readonly HashSet<BodyRegion> detached = new HashSet<BodyRegion>();
        private static readonly string[] Upper = { "Chest", "UpperArm_R", "UpperArm_L", "Thigh_R", "Thigh_L" };
        private static readonly string[] Lower = { "", "Forearm_R", "Forearm_L", "Shin_R", "Shin_L" };

        public void Build(bool blighted)
        {
            enemy = blighted;
            Rig = gameObject.AddComponent<ModularHumanRig>();
            Rig.Build(false, false, false, enemy); Rig.BindPose();
            Rig.SetAppearance(!enemy, false, 0);
            Rig.SetBuiltInWeaponsVisible(!enemy);
            Rig.SetExpression(enemy ? "Angry" : "Determined");
        }

        public void Equip(BlightWeapon weapon)
        {
            Rig.SetAppearance(!enemy, weapon == BlightWeapon.Axe, 0);
            Rig.SetBuiltInWeaponsVisible(!enemy);
            // Contact-lab weapons keep its existing long-blade proportions.
            Rig.Bone("WeaponSocket_R").localScale = new Vector3(1, 1,
                weapon == BlightWeapon.Axe ? 1.4f : 1.85f / 1.46f);
        }

        public void Pose(Transform body, Transform head, Transform[] joints, Transform[] bends,
            Transform grip, DuelFighter fighter, BlightLimbs limbs)
        {
            Rig.BindPose();
            Transform pelvis = Rig.Bone("Pelvis");
            pelvis.SetPositionAndRotation(body.position, body.rotation);
            if (enemy) BlightThrallVisual.ShapePose(Rig, fighter, limbs);
            if (!enemy && grip != null && fighter.Alive)
            {
                float elevation = transform.InverseTransformPoint(grip.position).y;
                Rig.Bone("Spine").localRotation = Quaternion.Euler(elevation < .7f ? 45 : elevation < 1.1f ? 20 : 0, 0, 0);
            }
            Rig.Bone("Head").rotation = head.rotation;
            for (int i = 1; i <= 4; i++)
            {
                Transform upper = Rig.Bone(Upper[i]), lower = Rig.Bone(Lower[i]);
                if (enemy || i >= 3) upper.position = joints[i].position;
                upper.rotation = joints[i].rotation; lower.rotation = bends[i].rotation;
            }
            if (!enemy && grip != null && fighter.Alive) Rig.ExternalGrip(grip.position, grip.rotation);
            Rig.SetBuiltInWeaponsVisible(!enemy && fighter.Alive);
            string expression = !fighter.Alive || fighter.Action == DuelAction.Stagger ? "Hurt" :
                enemy && limbs != null && limbs.BiteOnly && fighter.Action == DuelAction.Recovery ? "Shout" :
                enemy ? "Angry" : "Determined";
            if (!enemy && Rig.Expression != expression) Rig.SetExpression(expression);
        }

        public Transform Detach(BodyRegion region, Transform parent, Vector3 pivot, List<Mesh> ownedMeshes)
        {
            if (region == BodyRegion.Torso || !detached.Add(region)) return null;
            var debris = new GameObject("Severed " + region).transform;
            debris.SetParent(parent, false); debris.position = pivot;
            foreach (SkinnedMeshRenderer renderer in Rig.RegionRenderers(region.ToString()))
            {
                if (renderer.enabled && renderer.gameObject.activeInHierarchy)
                {
                    var mesh = new Mesh { name = renderer.name + " severed snapshot" };
                    renderer.BakeMesh(mesh); ownedMeshes.Add(mesh);
                    var piece = new GameObject(renderer.name);
                    piece.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                    piece.transform.localScale = renderer.transform.lossyScale;
                    piece.transform.SetParent(debris, true);
                    piece.AddComponent<MeshFilter>().sharedMesh = mesh;
                    piece.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
                }
                renderer.enabled = false;
            }
            return debris;
        }
    }
}
