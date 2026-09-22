using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    [Serializable] public sealed class CharacterArtBone { public string name; public int parent; public float[] position; }
    [Serializable] public sealed class CharacterArtPart { public string name; public int bone; public bool prop; public float[] color, positions, normals; public int[] triangles; }
    [Serializable] public sealed class CharacterArtFrame { public float time; public float[] rotations, offsets; }
    [Serializable] public sealed class CharacterArtClip { public string name; public float duration; public bool loop; public CharacterArtFrame[] frames; }
    [Serializable] public sealed class CharacterArtData { public string name; public CharacterArtBone[] bones; public CharacterArtPart[] parts; public CharacterArtClip[] clips; }

    // Same generated mesh data as the supplied animated GLBs. No third-party importer required.
    public sealed class StylizedCharacterArt : MonoBehaviour
    {
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<AnimationClip> clips = new List<AnimationClip>();
        public static CharacterArtData Load(bool thrall)
        {
            TextAsset json = Resources.Load<TextAsset>("ReclamationArt/" + (thrall ? "BlightedThrall" : "CompanyFighter"));
            return json == null ? null : JsonUtility.FromJson<CharacterArtData>(json.text);
        }
        public static Vector3 Vector(float[] values, int start = 0) => new Vector3(values[start], values[start + 1], values[start + 2]);

        public void Build(CharacterArtData data, Transform[] bones, bool includeProps = false)
        {
            var palette = new Dictionary<Color, Material>();
            foreach (CharacterArtPart part in data.parts)
            {
                if (part.prop && !includeProps) continue;
                var mesh = new Mesh { name = part.name };
                int count = part.positions.Length / 3;
                var vertices = new Vector3[count]; var normals = new Vector3[count];
                for (int i = 0; i < count; i++) { vertices[i] = Vector(part.positions, i * 3); normals[i] = Vector(part.normals, i * 3); }
                mesh.vertices = vertices; mesh.normals = normals; mesh.triangles = part.triangles; mesh.RecalculateBounds(); meshes.Add(mesh);
                Color color = new Color(part.color[0], part.color[1], part.color[2], part.color[3]);
                if (!palette.TryGetValue(color, out Material material))
                {
                    Shader shader = Shader.Find("Reclamation/IllustratedCharacter");
                    if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null) shader = Shader.Find("Standard");
                    material = new Material(shader) { name = "Character palette", color = color };
                    if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0);
                    palette.Add(color, material); materials.Add(material);
                }
                var go = new GameObject(part.name); go.transform.SetParent(bones[part.bone], false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
        }

        public Animation BuildPreviewAnimations(CharacterArtData data, Transform[] bones)
        {
            Animation animation = gameObject.AddComponent<Animation>(); animation.playAutomatically = false;
            foreach (CharacterArtClip source in data.clips)
            {
                var clip = new AnimationClip { name = source.name, legacy = true, frameRate = 24,
                    wrapMode = source.loop ? WrapMode.Loop : WrapMode.ClampForever };
                for (int bone = 0; bone < bones.Length; bone++)
                {
                    string path = bones[bone].name; Transform ancestor = bones[bone].parent;
                    while (ancestor != transform && ancestor != null) { path = ancestor.name + "/" + path; ancestor = ancestor.parent; }
                    for (int axis = 0; axis < 4; axis++)
                    {
                        var keys = new Keyframe[source.frames.Length];
                        for (int i = 0; i < keys.Length; i++) keys[i] = new Keyframe(source.frames[i].time, source.frames[i].rotations[bone * 4 + axis]);
                        clip.SetCurve(path, typeof(Transform), "localRotation." + "xyzw"[axis], new AnimationCurve(keys));
                    }
                    for (int axis = 0; axis < 3; axis++)
                    {
                        var keys = new Keyframe[source.frames.Length];
                        for (int i = 0; i < keys.Length; i++) keys[i] = new Keyframe(source.frames[i].time,
                            data.bones[bone].position[axis] + source.frames[i].offsets[bone * 3 + axis]);
                        clip.SetCurve(path, typeof(Transform), "localPosition." + "xyz"[axis], new AnimationCurve(keys));
                    }
                }
                clip.EnsureQuaternionContinuity(); clips.Add(clip); animation.AddClip(clip, clip.name);
            }
            animation.Play("Idle"); return animation;
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in meshes) if (mesh != null) Destroy(mesh);
            foreach (Material material in materials) if (material != null) Destroy(material);
            foreach (AnimationClip clip in clips) if (clip != null) Destroy(clip);
        }
    }
}
