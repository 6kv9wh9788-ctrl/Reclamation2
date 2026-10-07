using System;
using Reclamation.Blight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reclamation.Editor
{
    public static class SidekickPlayerSetup
    {
        public const string PrefabPath = "Assets/Reclamation/Resources/Player/SidekickPlayer.prefab";
        private const string SourcePath = "Assets/Synty/SidekickCharacters/_Demos/Prefabs/PF_SampleFace.prefab";

        public static GameObject EnsurePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab) return prefab;
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            if (!source) throw new InvalidOperationException("Missing approved PF_SampleFace character.");
            if (!AssetDatabase.IsValidFolder("Assets/Reclamation/Resources"))
                AssetDatabase.CreateFolder("Assets/Reclamation", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Reclamation/Resources/Player"))
                AssetDatabase.CreateFolder("Assets/Reclamation/Resources", "Player");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                string check = SidekickDuelBridge.Validate(instance);
                if (check != "PASS") throw new InvalidOperationException(check);
                instance.name = "Sidekick Player";
                prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
                if (!prefab) throw new InvalidOperationException("Could not save player prefab variant.");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
            return prefab;
        }

        public static void Configure(BlightCombatLab lab)
        {
            var bridge = lab.GetComponent<SidekickDuelBridge>();
            if (!bridge) bridge = lab.gameObject.AddComponent<SidekickDuelBridge>();
            bridge.lab = lab;
            if (!bridge.characterPrefab) bridge.characterPrefab = EnsurePrefab();
            bridge.hideUnrelatedPreviews = false;
            bridge.showStatus = false;
        }

        [MenuItem("Reclamation/Art/Prepare Sidekick Player Demo")]
        public static void PrepareDemo()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before preparing the demo.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsurePrefab();
            Scene scene = EditorSceneManager.OpenScene(CombatDemoBuild.ScenePath, OpenSceneMode.Single);
            BlightCombatLab lab = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.TryGetComponent(out BlightCombatLab found)) { lab = found; break; }
            if (!lab) throw new InvalidOperationException("Demo scene has no combat lab.");
            Configure(lab);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save demo scene.");
            Debug.Log("SIDEKICK_PLAYER_PREPARED: PF_SampleFace; player-only presentation.");
        }
    }
}
