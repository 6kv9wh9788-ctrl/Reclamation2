using Reclamation.Blight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reclamation.Editor
{
    public static class StylizedCharacterPreviewBuilder
    {
        [MenuItem("Reclamation/Art/Preview Stylized Characters")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Stylized character preview").AddComponent<StylizedCharacterPreview>();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Press Play, then choose animation clips. The combat lab's Limb damage scenario uses the same meshes on its live combat rig.");
        }
    }
}
