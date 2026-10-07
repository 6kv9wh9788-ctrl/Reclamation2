using Reclamation.Blight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reclamation.Editor
{
    public sealed class SidekickCharacterTestBuilder : EditorWindow
    {
        [SerializeField] private GameObject character;
        [SerializeField] private AnimationClip clip;
        [SerializeField] private bool pendingRun;
        private string status = "Choose a completed/baked Sidekick character prefab from the Project window.";
        [MenuItem("Reclamation/Testing/Open Sidekick Character Test")]
        public static void Open() { GetWindow<SidekickCharacterTestBuilder>("Sidekick Test Launcher"); }
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
            clip = (AnimationClip)EditorGUILayout.ObjectField("Optional animation clip", clip, typeof(AnimationClip), false);
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
            var animators = character.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1) return "FAIL: Expected one Animator, found " + animators.Length + ". Use a single baked character.";
            if (clip && clip.legacy) return "FAIL: Optional clip is Legacy. Choose a Mecanim clip or clear the field.";
            return SidekickCharacterTest.Check(animators[0]);
        }
        private bool Create()
        {
            status = Validate(); if (!status.StartsWith("PASS")) return false;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { status = "Cancelled at save prompt. No test started."; return false; }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(character);
            instance.name = "Sidekick Test Subject"; instance.SetActive(true);
            instance.transform.position = Vector3.zero; instance.transform.rotation = Quaternion.identity;
            // This art lab must not run vendor movement/demo behaviours alongside our probes.
            foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (var body in instance.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
            var subject = instance.GetComponentInChildren<Animator>(true);
            subject.gameObject.SetActive(true);
            var test = new GameObject("Sidekick Acceptance Test").AddComponent<SidekickCharacterTest>();
            test.subject = subject; test.optionalClip = clip; test.characterRoot = instance.transform;
            var camera = new GameObject("Test Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.nearClipPlane = .03f;
            camera.backgroundColor = new Color(.15f, .18f, .22f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0, 1.7f, 3.5f); camera.transform.LookAt(Vector3.up);
            test.testCamera = camera;
            var light = new GameObject("Key Light").AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(40, -35, 0);
            RenderSettings.ambientLight = new Color(.5f, .5f, .5f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Test floor";
            floor.transform.position = new Vector3(0, -.06f, 0); floor.transform.localScale = new Vector3(8, .1f, 8);
            Selection.activeGameObject = test.gameObject;
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            if (!test.subject || !test.testCamera || !test.characterRoot)
                throw new System.InvalidOperationException("Test references were not assigned.");
            status = "Scene ready. Test subject, controls, camera and floor created.";
            Debug.Log(status);
            return true;
        }
    }
}
