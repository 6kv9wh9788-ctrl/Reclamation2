using System;
using System.IO;
using Reclamation.Blight;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reclamation.Editor
{
    public static class FoundingVillageBuild
    {
        private const string Scene = "Assets/Scenes/FoundingVillage.unity";
        public static void BuildWindows()
        {
            string destination = Environment.GetEnvironmentVariable("RECLAMATION_FOUNDING_BUILD");
            if (string.IsNullOrEmpty(destination) || File.Exists(destination)) throw new InvalidOperationException("Use a new RECLAMATION_FOUNDING_BUILD exe path.");
            if (!File.Exists(Scene))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("First village - player and commander");
                var lab = root.AddComponent<BlightCombatLab>();
                var serialized = new SerializedObject(lab);
                serialized.FindProperty("initialScenario").enumValueIndex = (int)BlightScenario.Founding;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                root.AddComponent<FoundingVillageDemo>().requiredShaders = new[] { Shader.Find("Universal Render Pipeline/Lit"), Shader.Find("Universal Render Pipeline/Unlit"), Shader.Find("Reclamation/IllustratedCharacter") };
                if (!EditorSceneManager.SaveScene(scene, Scene)) throw new InvalidOperationException("Cannot save founding scene.");
            }
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Scene }, locationPathName = destination, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Founding build failed.");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(destination), "BUILD-RESULT.txt"), "Succeeded\n" + report.summary.totalWarnings + " warnings\nNon-development Windows x64\n" + report.summary.totalSize + " bytes");
        }
    }
}
