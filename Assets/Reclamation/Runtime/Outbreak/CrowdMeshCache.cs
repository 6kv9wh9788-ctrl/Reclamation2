using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    // One renderer per crowd actor; geometry/materials shared by appearance variant.
    // The workshop continues to use separate editable parts and expression targets.
    public static class CrowdMeshCache
    {
        public sealed class Entry
        {
            public Mesh mesh;
            public Material[] materials;
            internal int key, users;
        }
        private static readonly Dictionary<int, Entry> entries = new Dictionary<int, Entry>();
        public static int ActiveVariants => entries.Count;
        private static Vector3 V(float[] a, int i) => new Vector3(a[i], a[i + 1], a[i + 2]);

        public static Entry Acquire(ModularHumanData data, Matrix4x4[] bindPoses, bool athletic, bool heavy, bool axe, bool longHair)
        {
            int key = (athletic ? 1 : 0) | (heavy ? 2 : 0) | (axe ? 4 : 0) | (longHair ? 8 : 0);
            if (entries.TryGetValue(key, out Entry existing)) { existing.users++; return existing; }
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var deltas = new List<Vector3>();
            var weights = new List<BoneWeight>(); var colors = new List<Color>();
            var triangles = new List<List<int>>(); var materials = new List<Material>();
            foreach (ModularHumanPart part in data.parts)
            {
                bool visible = part.slot == "Body" ? !(heavy && part.name == "TorsoCloth") :
                    part.slot == "Heavy" ? heavy : part.slot == "HairLong" ? longHair : part.slot == "HairShort" ? !longHair :
                    part.slot == "Axe" ? axe : part.slot == "Sword" && !axe;
                if (!visible) continue;
                Color color = new Color(part.color[0], part.color[1], part.color[2], part.color[3]);
                int group = colors.IndexOf(color);
                if (group < 0)
                {
                    group = colors.Count; colors.Add(color); triangles.Add(new List<int>());
                    var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    material.color = color;
                    if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0);
                    if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", part.slot == "Heavy" ? .35f : .12f);
                    materials.Add(material);
                }
                // Copy only vertices referenced by visible triangles, not unused part-array vertices.
                var remap = new Dictionary<int, int>();
                foreach (int original in part.triangles)
                {
                    if (!remap.TryGetValue(original, out int index))
                    {
                        index = vertices.Count; remap.Add(original, index);
                        int v = original * 3, w = original * 4;
                        vertices.Add(V(part.positions, v)); normals.Add(V(part.normals, v)); deltas.Add(V(part.faceDelta, v));
                        weights.Add(new BoneWeight { boneIndex0 = part.joints[w], boneIndex1 = part.joints[w + 1],
                            boneIndex2 = part.joints[w + 2], boneIndex3 = part.joints[w + 3],
                            weight0 = part.weights[w], weight1 = part.weights[w + 1], weight2 = part.weights[w + 2], weight3 = part.weights[w + 3] });
                    }
                    triangles[group].Add(index);
                }
            }
            var mesh = new Mesh { name = "Shared crowd variant " + key, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.boneWeights = weights.ToArray(); mesh.bindposes = bindPoses;
            mesh.subMeshCount = triangles.Count;
            for (int i = 0; i < triangles.Count; i++) mesh.SetTriangles(triangles[i], i);
            mesh.AddBlendShapeFrame("SoftFace", 100, deltas.ToArray(), null, null); mesh.RecalculateBounds();
            var entry = new Entry { key = key, users = 1, mesh = mesh, materials = materials.ToArray() };
            entries.Add(key, entry); return entry;
        }

        public static void Release(Entry entry)
        {
            if (entry == null || --entry.users > 0) return;
            entries.Remove(entry.key);
            Object.Destroy(entry.mesh);
            foreach (Material material in entry.materials) Object.Destroy(material);
        }
    }
}
