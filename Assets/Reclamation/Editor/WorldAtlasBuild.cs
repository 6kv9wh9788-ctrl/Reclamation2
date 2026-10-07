using System;
using System.IO;
using Reclamation.Atlas;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reclamation.Editor
{
    public static class WorldAtlasBuild
    {
        public static void BuildWindows()
        {
            string output=Environment.GetEnvironmentVariable("RECLAMATION_ATLAS_BUILD");
            if(string.IsNullOrEmpty(output)||File.Exists(output))throw new InvalidOperationException("Use a new RECLAMATION_ATLAS_BUILD executable path.");
            const string scenePath="Assets/Scenes/WorldAtlas.unity";
            if(!File.Exists(scenePath))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var demo=new GameObject("World atlas exploration").AddComponent<WorldAtlasDemo>();
                demo.requiredShaders=new[]{Shader.Find("Universal Render Pipeline/Lit"),Shader.Find("Universal Render Pipeline/Unlit"),Shader.Find("Reclamation/IllustratedCharacter")};
                if(!EditorSceneManager.SaveScene(scene,scenePath))throw new InvalidOperationException("Could not save new atlas scene.");
            }
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Atlas build failed.");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"BUILD-RESULT.txt"),"Succeeded\n"+report.summary.totalWarnings+" warnings\n"+report.summary.totalSize+" bytes\nNon-development Windows x64");
        }
    }
}
