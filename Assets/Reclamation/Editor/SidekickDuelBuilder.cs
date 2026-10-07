using Reclamation.Blight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reclamation.Editor
{
    public sealed class SidekickDuelBuilder : EditorWindow
    {
        [SerializeField] private GameObject character;
        [SerializeField] private bool pendingRun;
        private string status = "Choose a completed/baked Sidekick character prefab from the Project window.";
        [MenuItem("Reclamation/Testing/Open Sidekick Hero Duel")]
        public static void Open() { GetWindow<SidekickDuelBuilder>("Sidekick Morning Review"); }
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (pendingRun && !EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.delayCall += RunPending;
        }
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.delayCall -= RunPending;
        }
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode && pendingRun)
                EditorApplication.delayCall += RunPending;
        }
        private void RequestRun()
        {
            status = Validate();
            if (!status.StartsWith("PASS")) { Repaint(); return; }
            pendingRun = true;
            status = "Preparing test. Stopping Play mode if necessary...";
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
            else EditorApplication.delayCall += RunPending;
        }
        private void RunPending()
        {
            if (!this || !pendingRun || EditorApplication.isPlayingOrWillChangePlaymode) return;
            pendingRun = false;
            try
            {
                if (Create())
                {
                    status = "Test scene built. Starting Play mode; controls appear in the Game view.";
                    EditorApplication.isPlaying = true;
                }
            }
            catch (System.Exception exception)
            {
                status = "BUILD FAILED: " + exception.Message + " — see Console for details.";
                Debug.LogException(exception);
            }
            Repaint();
        }
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Use Sidekick's character creator to save/bake a complete character first. This tool does not require or modify Synty's code. Select a prefab asset, not an individual mesh or scene object.", MessageType.Info);
            character = (GameObject)EditorGUILayout.ObjectField("Baked character prefab", character, typeof(GameObject), false);

            EditorGUILayout.HelpBox(status, MessageType.None);
            if (GUILayout.Button("Validate selection")) status = Validate();
            EditorGUILayout.HelpBox("Build & Run handles stopping Play mode, creating the test scene, and starting it. Selecting or validating a prefab alone does not launch the test.", MessageType.Info);
            using (new EditorGUI.DisabledScope(pendingRun || EditorApplication.isCompiling))
                if (GUILayout.Button("BUILD & RUN TEST", GUILayout.Height(42))) RequestRun();
            if (pendingRun) EditorGUILayout.HelpBox("Waiting for Unity to exit Play mode. Keep this launcher open.", MessageType.Info);
            if (EditorApplication.isCompiling) EditorGUILayout.HelpBox("Waiting for script compilation.", MessageType.Info);
        }
        private string Validate()
        {
            if (!character || !PrefabUtility.IsPartOfPrefabAsset(character)) return "FAIL: Select a saved character prefab asset.";
            return SidekickDuelBridge.Validate(character);
        }
        private bool Create()
        {
            status = Validate(); if (!status.StartsWith("PASS")) return false;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { status = "Cancelled at save prompt. No test started."; return false; }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lab = new GameObject("Sidekick Morning Review").AddComponent<BlightCombatLab>();
            var bridge = lab.gameObject.AddComponent<SidekickDuelBridge>();
            bridge.lab = lab; bridge.characterPrefab = character;
            var review = lab.gameObject.AddComponent<SidekickMorningReview>();
            review.lab = lab; review.bridge = bridge;
            Selection.activeGameObject = lab.gameObject;
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            status = "Sidekick duel ready. Arena and player are created when Play starts.";
            Debug.Log(status);
            return true;
        }
    }
}
