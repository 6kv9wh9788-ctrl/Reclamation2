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
            List<ResourcePile> resources = CreateResourcePiles();

            var boardObject = new GameObject("Settlement Job Board");
            HaulJobBoard board = boardObject.AddComponent<HaulJobBoard>();
            board.Configure(stockpile, resources);

            CreateWorker("Avery", new Vector3(-2f, 0.5f, -4f), new Color(0.25f, 0.65f, 0.95f), board);
            CreateWorker("Dylan", new Vector3(0f, 0.5f, -4f), new Color(0.95f, 0.65f, 0.2f), board);
            CreateWorker("Morgan", new Vector3(2f, 0.5f, -4f), new Color(0.65f, 0.35f, 0.85f), board);

            var debugObject = new GameObject("Decision Debug Overlay");
            debugObject.AddComponent<SettlementDebugOverlay>().Configure(board, stockpile);

            surface.BuildNavMesh();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeObject = boardObject;
            Debug.Log($"Created {ScenePath}. Press Play to run the hauling simulation.");
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

        private static void CreateWorker(string name, Vector3 position, Color color, HaulJobBoard board)
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

            workerObject.AddComponent<HaulWorker>().Configure(name, board);
        }

        private static void SetColor(GameObject target, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            target.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
