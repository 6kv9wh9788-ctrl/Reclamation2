using Reclamation.Blight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reclamation.Editor
{
    public static class CharacterCrowdBenchmarkBuilder
    {
        [MenuItem("Reclamation/Testing/Open Character Crowd Benchmark")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Character Crowd Benchmark").AddComponent<CharacterCrowdBenchmark>();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Press Play. Start with 16 humans and increase only while the editor remains responsive.");
        }
    }
}
