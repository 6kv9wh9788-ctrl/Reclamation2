using System.Collections.Generic;
using Reclamation.Prototype;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Reclamation.Editor
{
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/AutonomousHaulingLab.unity";

        [MenuItem("Reclamation/Create Autonomous Hauling Lab")]
        public static void CreateScene()
        {
            CreateSceneAt(ScenePath, false);
        }

        [MenuItem("Reclamation/Create Shelter Construction Lab")]
        public static void CreateShelterScene()
        {
            CreateSceneAt("Assets/Scenes/ShelterConstructionLab.unity", true);
        }

        private static void CreateSceneAt(string scenePath, bool shelterMode)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null &&
                !EditorUtility.DisplayDialog("Replace generated lab?",
                    "This replaces the generated lab scene. Save a separate copy if you have customized it.",
                    "Replace lab", "Cancel")) return;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLighting();
            CreateCamera();

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Navigation Ground";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            SetColor(ground, new Color(0.24f, 0.32f, 0.22f));

            var surface = ground.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.layerMask = 1 << 0;

            Stockpile stockpile = CreateStockpile(new Vector3(0f, 0.5f, 0f));
            FoodStore food = CreateFoodStore(new Vector3(4f, 0.5f, 0f));
            MedicineStore medicine = CreateMedicineStore(new Vector3(7f, 0.35f, 0f));
            RecreationSpot recreation = CreateRecreationSpot(new Vector3(0f, 0.15f, 3f));
            Bed[] beds = { CreateBed("Bed 1", new Vector3(-5f, 0.2f, -1f)),
                CreateBed("Bed 2", new Vector3(-7.5f, 0.2f, -1f)) };
            List<ResourcePile> resources = CreateResourcePiles();
            var farms = new[]
            {
                CreateFarm("Farm Plot 1", new Vector3(5f, 0.1f, 6f), 1f),
                CreateFarm("Farm Plot 2", new Vector3(9f, 0.1f, 6f), 0.65f)
            };

            var boardObject = new GameObject("Settlement Job Board");
            HaulJobBoard board = boardObject.AddComponent<HaulJobBoard>();
            board.Configure(stockpile, resources);
            var policy = boardObject.AddComponent<SettlementPolicy>(); board.SetPolicy(policy);
            board.SetFarms(farms);

            CreateWorker("Avery", new Vector3(-2f, 0.5f, -4f), new Color(0.25f, 0.65f, 0.95f), board, food, medicine, recreation, beds, 72f, 100f);
            CreateWorker("Dylan", new Vector3(0f, 0.5f, -4f), new Color(0.95f, 0.65f, 0.2f), board, food, medicine, recreation, beds, 55f, 25f);
            CreateWorker("Morgan", new Vector3(2f, 0.5f, -4f), new Color(0.65f, 0.35f, 0.85f), board, food, medicine, recreation, beds, 20f, 100f);

            var debugObject = new GameObject("Decision Debug Overlay");
            debugObject.AddComponent<SettlementDebugOverlay>().Configure(board, stockpile, food, beds, medicine, recreation);
            if (shelterMode)
                debugObject.AddComponent<ShelterPlanner>().Configure(board);

            surface.BuildNavMesh();
            if (surface.navMeshData != null)
            {
                string navPath = AssetDatabase.GenerateUniqueAssetPath(
                    "Assets/Scenes/HaulingLabNavigation.asset");
                AssetDatabase.CreateAsset(surface.navMeshData, navPath);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, scenePath);
            Selection.activeObject = boardObject;
            Debug.Log($"Created {scenePath}. Press Play to run the simulation.");
        }

        private static void CreateLighting()
        {
            var lightObject = new GameObject("Sun");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 17f, -18f);
            camera.transform.rotation = Quaternion.Euler(38f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.Skybox;
        }

        private static Stockpile CreateStockpile(Vector3 position)
        {
            GameObject stockpileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stockpileObject.name = "Stockpile";
            stockpileObject.layer = 2;
            stockpileObject.transform.position = position;
            stockpileObject.transform.localScale = new Vector3(3f, 0.25f, 3f);
            SetColor(stockpileObject, new Color(0.45f, 0.28f, 0.12f));
            return stockpileObject.AddComponent<Stockpile>();
        }

        private static FoodStore CreateFoodStore(Vector3 position)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Food Store"; go.layer = 2; go.transform.position = position;
            go.transform.localScale = new Vector3(2f, 0.5f, 2f);
            SetColor(go, new Color(0.25f, 0.55f, 0.2f));
            FoodStore store = go.AddComponent<FoodStore>(); store.Configure(9); return store;
        }

        private static MedicineStore CreateMedicineStore(Vector3 position)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Medicine Store"; go.layer = 2; go.transform.position = position;
            go.transform.localScale = new Vector3(1.5f, 0.7f, 1.5f);
            SetColor(go, new Color(0.2f, 0.75f, 0.75f));
            MedicineStore store = go.AddComponent<MedicineStore>(); store.Configure(4); return store;
        }

        private static RecreationSpot CreateRecreationSpot(Vector3 position)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Community Gathering Spot"; go.layer = 2; go.transform.position = position;
            go.transform.localScale = new Vector3(2.2f, 0.15f, 2.2f);
            SetColor(go, new Color(0.9f, 0.5f, 0.15f));
            RecreationSpot spot = go.AddComponent<RecreationSpot>(); spot.Configure(2); return spot;
        }

        private static Bed CreateBed(string name, Vector3 position)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.layer = 2; go.transform.position = position;
            go.transform.localScale = new Vector3(1.1f, 0.35f, 2.2f);
            SetColor(go, new Color(0.18f, 0.4f, 0.65f));
            return go.AddComponent<Bed>();
        }

        private static FarmPlot CreateFarm(string name, Vector3 position, float growth)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.layer = 2; go.transform.position = position;
            go.transform.localScale = new Vector3(3f, 0.2f, 3f);
            SetColor(go, growth >= 1f ? new Color(0.32f, 0.65f, 0.2f) : new Color(0.36f, 0.28f, 0.12f));
            FarmPlot farm = go.AddComponent<FarmPlot>(); farm.Configure(growth, 4f, 3); return farm;
        }

        private static List<ResourcePile> CreateResourcePiles()
        {
            Vector3[] positions =
            {
                new(-10f, 0.5f, 6f),
                new(-5f, 0.5f, 10f),
                new(6f, 0.5f, 9f),
                new(11f, 0.5f, 4f)
            };

            var resources = new List<ResourcePile>();
            for (int index = 0; index < positions.Length; index++)
            {
                GameObject resourceObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                resourceObject.name = $"Wood Pile {index + 1}";
                resourceObject.layer = 2;
                resourceObject.transform.position = positions[index];
                resourceObject.transform.localScale = new Vector3(1.5f, 1f, 1.5f);
                SetColor(resourceObject, new Color(0.35f, 0.17f, 0.06f));
                ResourcePile resource = resourceObject.AddComponent<ResourcePile>();
                resource.Configure(4, index == 0 ? 2 : 1);
                resources.Add(resource);
            }

            return resources;
        }

        private static void CreateWorker(string name, Vector3 position, Color color, HaulJobBoard board,
            FoodStore food, MedicineStore medicine, RecreationSpot recreation, IEnumerable<Bed> beds,
            float startingHunger, float startingEnergy)
        {
            GameObject workerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            workerObject.name = name;
            workerObject.layer = 2;
            workerObject.transform.position = position;
            SetColor(workerObject, color);

            NavMeshAgent agent = workerObject.AddComponent<NavMeshAgent>();
            agent.speed = 4f;
            agent.angularSpeed = 720f;
            agent.acceleration = 14f;
            agent.stoppingDistance = 0.25f;

            HaulWorker worker = workerObject.AddComponent<HaulWorker>();
            worker.Configure(name, board); worker.ConfigureNeeds(food, startingHunger);
            worker.ConfigureRest(beds, startingEnergy);
            worker.ConfigureMedicine(medicine);
            worker.ConfigureMorale(recreation);
            HumanVisualBuilder.Add(workerObject);
        }

        private static void SetColor(GameObject target, Color color)
        {
            const string folder = "Assets/Reclamation/GeneratedMaterials";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Reclamation", "GeneratedMaterials");
            string path = $"{folder}/Lab-{ColorUtility.ToHtmlStringRGBA(color)}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = color;
                AssetDatabase.CreateAsset(material, path);
            }
            target.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
