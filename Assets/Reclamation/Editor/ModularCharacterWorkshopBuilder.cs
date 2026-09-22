using Reclamation.Blight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reclamation.Editor
{
    public static class ModularCharacterWorkshopBuilder
    {
        [MenuItem("Reclamation/Art/Open Modular Character Workshop")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Modular Character Workshop").AddComponent<ModularCharacterWorkshop>();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Press Play. Use the workshop buttons for body, face, gear, animation, hand grip and ragdoll tests.");
        }
    }
}
