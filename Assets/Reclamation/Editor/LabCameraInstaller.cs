using Reclamation.Neighborhood;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reclamation.Editor
{
    public static class LabCameraInstaller
    {
        [MenuItem("Reclamation/Legacy/Upgrade Open Scene with Camera Controls")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var camera = Camera.main;
            if (camera == null || camera.gameObject.scene != SceneManager.GetActiveScene())
            {
                EditorUtility.DisplayDialog("Camera required", "Open your lab scene with an active camera tagged MainCamera.", "OK");
                return;
            }
            if (camera.GetComponent<LabCameraController>() == null)
            {
                Undo.AddComponent<LabCameraController>(camera.gameObject);
                EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
            }
            Selection.activeGameObject = camera.gameObject;
            Debug.Log("Camera controls ready. Save the scene, press Play, and click the Game view. Open Controls for help.");
        }
    }
}
