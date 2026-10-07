using System;
using System.IO;
using Reclamation.Blight;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reclamation.Editor
{
    public static class JointMannequinLabBuilder
    {
        public const string ScenePath="Assets/Scenes/JointMannequinLab.unity";
        [MenuItem("Reclamation/Art/Open Joint Mannequin Lab")]
        public static void Open()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if(File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else Prepare();
        }
        public static void Prepare()
        {
            if(File.Exists(ScenePath)) return;
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var lab=new GameObject("Isolated joint mannequin lab").AddComponent<JointMannequinLab>();
            lab.bodyShader=Shader.Find("Universal Render Pipeline/Lit");
            if(!EditorSceneManager.SaveScene(scene,ScenePath)) throw new InvalidOperationException("Could not save mannequin scene.");
        }
        public static void BuildWindows()
        {
            string destination=Environment.GetEnvironmentVariable("RECLAMATION_MANNEQUIN_BUILD");
            if(string.IsNullOrWhiteSpace(destination)) throw new InvalidOperationException("Set RECLAMATION_MANNEQUIN_BUILD to a new executable path.");
            if(File.Exists(destination)) throw new InvalidOperationException("Use a new build path.");
            Prepare();
            BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{ scenes=new[]{ScenePath},locationPathName=destination,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development });
            if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Mannequin build failed.");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(destination),"BUILD-RESULT.txt"),"Succeeded\n"+report.summary.totalSize+" bytes\n"+report.summary.totalWarnings+" warnings");
        }
    }
}
