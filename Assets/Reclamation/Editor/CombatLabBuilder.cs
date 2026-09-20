using System.Collections.Generic;
using Reclamation.Neighborhood;
using Reclamation.Outbreak;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Reclamation.Editor
{
    public static class CombatLabBuilder
    {
        [MenuItem("Reclamation/Combat/Create Civilian vs One Lab")]
        public static void Civilian() => Create(0);
        [MenuItem("Reclamation/Combat/Create Veteran vs Three Lab")]
        public static void Veteran() => Create(1);
        [MenuItem("Reclamation/Combat/Create Protect Civilian Lab")]
        public static void Squad() => Create(2);

        [MenuItem("Reclamation/Combat/Enable Combat in Open Outbreak Scene")]
        public static void Enable()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var director = Object.FindFirstObjectByType<OutbreakDirector>();
            if (director == null || director.gameObject.scene != SceneManager.GetActiveScene())
            { Debug.LogWarning("Open an outbreak scene first."); return; }
            if (director.GetComponent<CombatDirector>() == null) Undo.AddComponent<CombatDirector>(director.gameObject);
            foreach (var person in director.Population)
                if (person != null && person.GetComponent<Combatant>() == null) Undo.AddComponent<Combatant>(person.gameObject);
            EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
            Debug.Log("Autonomous combat enabled. Existing survivors use civilian attributes. Save the scene.");
        }

        private static void Create(int scenario)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string title = scenario == 0 ? "CivilianVsOne" : scenario == 1 ? "VeteranVsThree" : "ProtectCivilian";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 10;
            camera.transform.position = new Vector3(-5, 16, -18); camera.transform.LookAt(new Vector3(-5, 0, 0));
            camera.gameObject.AddComponent<LabCameraController>();
            camera.backgroundColor = new Color(0.2f, 0.25f, 0.3f); camera.clearFlags = CameraClearFlags.SolidColor;
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0); sun.intensity = 1.2f;
            RenderSettings.ambientLight = Color.gray;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "Arena ground";
            ground.transform.position = new Vector3(0, -0.1f, 0); ground.transform.localScale = new Vector3(32, 0.2f, 28);
            ground.GetComponent<Renderer>().sharedMaterial = MaterialFor("Ground", new Color(0.32f, 0.38f, 0.32f));
            var surface = ground.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.All; surface.layerMask = 1;
            var clock = new GameObject("Clock").AddComponent<NeighborhoodClock>();
            var people = new List<OutbreakAgent>(); var zombies = new List<OutbreakAgent>();
            people.Add(Person(scenario == 0 ? "Civilian" : "Veteran Alpha", new Vector3(-1, 0, -1), scenario != 0, false, clock));
            if (scenario == 2)
            {
                people.Add(Person("Veteran Bravo", new Vector3(1, 0, -1), true, false, clock));
                people.Add(Person("Protected civilian", new Vector3(0, 0, -4), false, false, clock));
            }
            int count = scenario == 0 ? 1 : scenario == 1 ? 3 : 5;
            for (int i = 0; i < count; i++)
            {
                var zombie = Person($"Zombie {i + 1}", new Vector3((i - (count - 1) * 0.5f) * 1.7f, 0, 4 + i % 2), false, true, clock);
                people.Add(zombie); zombies.Add(zombie);
            }
            var root = new GameObject("Combat and Outbreak Director"); root.AddComponent<CombatDirector>();
            root.AddComponent<OutbreakDirector>().Configure(clock, people.ToArray(), null, 614);
            root.AddComponent<CombatEncounter>().Configure(zombies.ToArray());
            surface.BuildNavMesh();
            if (surface.navMeshData != null) AssetDatabase.CreateAsset(surface.navMeshData,
                AssetDatabase.GenerateUniqueAssetPath($"Assets/Scenes/{title}Navigation.asset"));
            AssetDatabase.SaveAssets();
            string path = AssetDatabase.GenerateUniqueAssetPath($"Assets/Scenes/{title}.unity");
            EditorSceneManager.SaveScene(scene, path); Selection.activeGameObject = root;
            Debug.Log($"Combat lab saved to {path}. Play at 1x first. Hold/disengage controls are in the scrolling outbreak panel.");
        }

        private static OutbreakAgent Person(string name, Vector3 feet, bool veteran, bool zombie, NeighborhoodClock clock)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.name = name; go.layer = 2;
            go.transform.position = feet; go.GetComponent<Collider>().enabled = false;
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(zombie ? "Zombie" : veteran ? "Veteran" : "Civilian",
                zombie ? new Color(0.4f, 0.55f, 0.3f) : veteran ? new Color(0.2f, 0.5f, 0.9f) : new Color(0.95f, 0.65f, 0.2f));
            var nav = go.AddComponent<NavMeshAgent>(); nav.baseOffset = 1; nav.radius = 0.45f;
            var home = new GameObject(name + " hold point").transform; home.position = feet;
            go.AddComponent<CivilianRoutine>().Configure(name, clock, home, home, home, 0);
            var person = go.AddComponent<OutbreakAgent>(); person.Configure(name);
            go.AddComponent<Combatant>().Configure(veteran, CombatOrder.Hold);
            HumanVisualBuilder.Add(go); return person;
        }

        private static Material MaterialFor(string name, Color color)
        {
            const string folder = "Assets/Reclamation/GeneratedMaterials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Reclamation", "GeneratedMaterials");
            string path = $"{folder}/Combat{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            AssetDatabase.CreateAsset(material, path); return material;
        }
    }
}
