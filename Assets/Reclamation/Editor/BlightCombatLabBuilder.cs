using Reclamation.Blight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reclamation.Editor
{
    public static class BlightCombatLabBuilder
    {
        [MenuItem("Reclamation/Combat/Create Blight Combat Lab")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Blight Combat Lab").AddComponent<BlightCombatLab>();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Blight Combat Lab ready. Press Play and use Choose scenario: Duel, Squad, Hulk, Weapons, Patrol. The arena is generated at runtime. Save this new scene if desired. Existing labs are unchanged.");
        }
    }
}
