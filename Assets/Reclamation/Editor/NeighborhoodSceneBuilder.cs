using System.Collections.Generic;
using Reclamation.Neighborhood;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Reclamation.Editor
{
    public static class NeighborhoodSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/LivingNeighborhoodLab.unity";
        private const string OutbreakPath = "Assets/Scenes/PatientZeroLab.unity";
        private static readonly Color Concrete = new Color(0.72f, 0.71f, 0.65f);
        private static readonly Color Grass = new Color(0.29f, 0.43f, 0.31f);

        [MenuItem("Reclamation/Create Living Neighborhood Lab")]
        public static void Create()
        {
            CreateInternal(false);
        }

        [MenuItem("Reclamation/Create Patient Zero Outbreak Lab")]
        public static void CreateOutbreak()
        {
            CreateInternal(true);
        }

        private static void CreateInternal(bool outbreak)
        {
            string scenePath = outbreak ? OutbreakPath : ScenePath;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null &&
                !EditorUtility.DisplayDialog("Replace neighborhood lab?",
                    "The existing generated neighborhood scene will be replaced.", "Replace", "Cancel")) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 48, -48);
            camera.transform.LookAt(new Vector3(0, 0, 3));
            camera.orthographic = true;
            camera.orthographicSize = 32;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.68f, 0.76f);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(55, -35, 0);
            RenderSettings.ambientLight = new Color(0.6f, 0.65f, 0.7f);

            var ground = Box("Neighborhood ground", new Vector3(0, -0.1f, 0),
                new Vector3(64, 0.2f, 64), Grass, true);
            var surface = ground.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.layerMask = 1;

            Box("East-west road", new Vector3(0, 0.01f, 0), new Vector3(60, 0.02f, 4), Color.gray);
            Box("North-south road", new Vector3(0, 0.015f, 2), new Vector3(4, 0.02f, 58), Color.gray);
            Box("Home sidewalk", new Vector3(0, 0.025f, -6.5f), new Vector3(46, 0.03f, 3), Concrete);
            Box("Cafe forecourt", new Vector3(-12, 0.025f, 4.5f), new Vector3(12, 0.03f, 5), Concrete);
            Box("Park path", new Vector3(12, 0.025f, 13), new Vector3(12, 0.03f, 9), Concrete);

            float[] houseX = { -16, 0, 16 };
            for (int i = 0; i < 3; i++)
            {
                Box($"House {i + 1}", new Vector3(houseX[i], 1.7f, -13),
                    new Vector3(9, 3.4f, 8), new Color(0.68f, 0.55f + i * 0.06f, 0.4f), true);
                Box($"House {i + 1} roof", new Vector3(houseX[i], 3.6f, -13),
                    new Vector3(10, 0.4f, 9), new Color(0.28f, 0.32f, 0.4f));
                Sign($"HOME {i + 1}", new Vector3(houseX[i], 5, -12), camera);
            }
            Box("Corner Cafe", new Vector3(-12, 1.8f, 13), new Vector3(11, 3.6f, 8),
                new Color(0.65f, 0.3f, 0.24f), true);
            Box("Cafe awning", new Vector3(-12, 2.7f, 8.5f), new Vector3(12, 0.2f, 2),
                new Color(0.95f, 0.76f, 0.3f));
            Sign("CORNER CAFE", new Vector3(-12, 5, 13), camera);
            Sign("NEIGHBORHOOD PARK", new Vector3(12, 2, 19), camera);
            for (int i = 0; i < 6; i++)
            {
                float x = 6 + i % 3 * 6;
                float z = i < 3 ? 20 : 7;
                Box("Tree trunk", new Vector3(x, 1, z), new Vector3(0.5f, 2, 0.5f), new Color(0.35f, 0.2f, 0.1f));
                Box("Tree canopy", new Vector3(x, 3, z), new Vector3(3, 3, 3), new Color(0.15f, 0.36f, 0.2f));
            }

            var clock = new GameObject("Neighborhood Clock").AddComponent<NeighborhoodClock>();
            var residents = new List<CivilianRoutine>();
            var outbreakPopulation = new List<Reclamation.Outbreak.OutbreakAgent>();
            string[] names = { "Avery", "Morgan", "Riley", "Casey", "Jordan", "Sam" };
            Color[] colors = { Color.cyan, new Color(1, 0.6f, 0.1f), new Color(0.7f, 0.3f, 0.85f),
                new Color(0.3f, 0.6f, 1), new Color(1, 0.35f, 0.4f), new Color(0.65f, 0.85f, 0.2f) };
            for (int i = 0; i < 6; i++)
            {
                Transform home = Point($"{names[i]} home", new Vector3(houseX[i / 2] + (i % 2 == 0 ? -1 : 1), 0, -7));
                Transform cafe = Point($"{names[i]} cafe place", new Vector3(-15 + i % 3 * 3, 0, 3.5f + i / 3 * 2));
                Transform park = Point($"{names[i]} park place", new Vector3(9 + i % 3 * 3, 0, 11 + i / 3 * 3));
                var person = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                person.name = names[i]; person.layer = 2;
                person.transform.position = home.position;
                person.GetComponent<Collider>().enabled = false;
                person.GetComponent<Renderer>().sharedMaterial = MaterialFor(colors[i]);
                var agent = person.AddComponent<NavMeshAgent>();
                agent.baseOffset = 1; agent.radius = 0.3f; agent.stoppingDistance = 0.25f;
                var routine = person.AddComponent<CivilianRoutine>();
                routine.Configure(names[i], clock, home, cafe, park, i * 12);
                residents.Add(routine);
                HumanVisualBuilder.Add(person);
                if (outbreak)
                {
                    var outbreakAgent = person.AddComponent<Reclamation.Outbreak.OutbreakAgent>();
                    outbreakAgent.Configure(names[i]);
                    outbreakPopulation.Add(outbreakAgent);
                }
            }
            if (outbreak)
            {
                Transform arrival = Point("Visitor arrival", new Vector3(-27, 0, -2));
                Transform visitorCafe = Point("Visitor cafe place", new Vector3(-9, 0, 5.5f));
                Transform visitorPark = Point("Visitor park place", new Vector3(15, 0, 14));
                var visitorObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visitorObject.name = "Visitor"; visitorObject.layer = 2;
                visitorObject.transform.position = arrival.position;
                visitorObject.GetComponent<Collider>().enabled = false;
                visitorObject.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(0.9f, 0.9f, 0.92f));
                var visitorNav = visitorObject.AddComponent<NavMeshAgent>();
                visitorNav.baseOffset = 1; visitorNav.radius = 0.3f; visitorNav.stoppingDistance = 0.25f;
                var visitorRoutine = visitorObject.AddComponent<CivilianRoutine>();
                visitorRoutine.Configure("Visitor", clock, arrival, visitorCafe, visitorPark, 0);
                var visitor = visitorObject.AddComponent<Reclamation.Outbreak.OutbreakAgent>();
                visitor.Configure("Visitor");
                HumanVisualBuilder.Add(visitorObject);
                outbreakPopulation.Add(visitor);
                var director = new GameObject("Outbreak Director").AddComponent<Reclamation.Outbreak.OutbreakDirector>();
                director.Configure(clock, outbreakPopulation.ToArray(), visitor, 614);
            }
            else
                new GameObject("Neighborhood Status").AddComponent<NeighborhoodPanel>().Configure(clock, residents.ToArray());
            surface.BuildNavMesh();
            if (surface.navMeshData != null)
                AssetDatabase.CreateAsset(surface.navMeshData,
                    AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/NeighborhoodNavigation.asset"));
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, scenePath);
            Selection.activeObject = clock.gameObject;
            Debug.Log(outbreak
                ? "Patient Zero scenario ready. Press Play; the visitor heads to the café after 08:00."
                : "Living neighborhood ready. Press Play; morning café departures begin after 08:00.");
        }

        private static Transform Point(string name, Vector3 position)
        {
            var go = new GameObject(name); go.transform.position = position; return go.transform;
        }
        private static GameObject Box(string name, Vector3 position, Vector3 size, Color color, bool blocks = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.layer = blocks ? 0 : 2;
            go.transform.position = position; go.transform.localScale = size;
            go.GetComponent<Collider>().enabled = blocks;
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(color);
            return go;
        }
        private static void Sign(string text, Vector3 position, Camera camera)
        {
            var go = new GameObject(text);
            go.transform.position = position;
            go.transform.rotation = camera.transform.rotation;
            var label = go.AddComponent<TextMesh>();
            label.text = text; label.anchor = TextAnchor.MiddleCenter;
            label.fontSize = 64; label.characterSize = 0.09f; label.color = Color.white;
        }
        private static Material MaterialFor(Color color)
        {
            const string folder = "Assets/Reclamation/GeneratedMaterials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Reclamation", "GeneratedMaterials");
            string path = $"{folder}/Neighborhood-{ColorUtility.ToHtmlStringRGBA(color)}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
