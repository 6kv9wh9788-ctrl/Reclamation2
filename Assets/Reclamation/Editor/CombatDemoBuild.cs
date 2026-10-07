using System;
using System.IO;
using Reclamation.Blight;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reclamation.Editor
{
    public static class CombatDemoBuild
    {
        public const string ScenePath = "Assets/Scenes/CombatDemo.unity";

        [MenuItem("Reclamation/Build/Windows Combat Demo")]
        public static void BuildWindows()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new BuildFailedException("Stop Play mode before building the demo.");
            string output = Path.GetFullPath("Builds/WindowsCombatDemo/ReclamationCombatDemo.exe");
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-combatBuildPath") output = Path.GetFullPath(args[i + 1]);
            if (!output.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException("Combat demo output must be an .exe path.");
            EnsureScene();
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Combat demo build failed: " + report.summary.result + "; errors=" + report.summary.totalErrors);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "BUILD-RESULT.txt"),
                "Succeeded\nUnity: " + Application.unityVersion + "\nScene: " + ScenePath +
                "\nTarget: Windows x64 Development\nBytes: " + report.summary.totalSize +
                "\nWarnings: " + report.summary.totalWarnings + "\n");
            Debug.Log("COMBAT_DEMO_BUILD_SUCCEEDED: " + output);
        }

        private static void EnsureScene()
        {
            // Create only once. A later rebuild must preserve deliberate scene edits.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new BuildFailedException("Build cancelled while saving scene work.");
            Scene previous = SceneManager.GetActiveScene();
            bool replaceStartup = Application.isBatchMode || string.IsNullOrEmpty(previous.path);
            Scene demo = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                replaceStartup ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(demo);
                var root = new GameObject("Combat Demo");
                var lab = root.AddComponent<BlightCombatLab>();
                var serialized = new SerializedObject(lab);
                serialized.FindProperty("initialScenario").enumValueIndex = (int)BlightScenario.Squad;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                SidekickPlayerSetup.Configure(lab);
                var smoke = root.AddComponent<CombatDemoSmoke>();
                // Runtime Shader.Find calls alone do not guarantee build inclusion.
                smoke.requiredShaders = new[]
                {
                    Shader.Find("Universal Render Pipeline/Lit"),
                    Shader.Find("Universal Render Pipeline/Unlit"),
                    Shader.Find("Reclamation/IllustratedCharacter")
                };
                foreach (Shader shader in smoke.requiredShaders)
                    if (shader == null) throw new BuildFailedException("A required combat shader is missing.");
                if (!EditorSceneManager.SaveScene(demo, ScenePath))
                    throw new BuildFailedException("Could not save " + ScenePath);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                {
                    SceneManager.SetActiveScene(previous);
                    EditorSceneManager.CloseScene(demo, true);
                }
            }
        }
    }
}
