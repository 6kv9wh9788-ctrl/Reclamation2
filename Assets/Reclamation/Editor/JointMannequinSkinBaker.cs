using System;
using Reclamation.Blight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reclamation.Editor
{
    public static class JointMannequinSkinBaker
    {
        public const string AssetPath = "Assets/Reclamation/Resources/Player/ContinuousMannequinBody.asset";
        [MenuItem("Reclamation/Art/Rebuild Continuous Mannequin Skin")]
        public static void Bake()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before rebuilding the skin.");
            Scene preview = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("Temporary skin bake");
            SceneManager.MoveGameObjectToScene(root, preview);
            try
            {
                var rig = root.AddComponent<JointMannequin>(); rig.Build();
                var skin = root.AddComponent<JointMannequinSkin>(); skin.Build(rig);
                Mesh copy = UnityEngine.Object.Instantiate(skin.BodyMesh);
                copy.name = "Continuous mannequin body v1";
                Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(AssetPath);
                if (existing) { EditorUtility.CopySerialized(copy, existing); UnityEngine.Object.DestroyImmediate(copy); EditorUtility.SetDirty(existing); }
                else AssetDatabase.CreateAsset(copy, AssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log("CONTINUOUS_BODY_BAKED: " + skin.BodyMesh.vertexCount + " vertices; " + skin.GenerationSeconds.ToString("F2") + " seconds");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(preview); }
        }
    }
}
