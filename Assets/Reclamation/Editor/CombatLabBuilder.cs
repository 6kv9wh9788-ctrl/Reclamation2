using System.Collections.Generic;
using Reclamation.Neighborhood;
using Reclamation.Outbreak;
using Reclamation.Prototype;
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
        private const string ScenePath = "Assets/Scenes/SystemsValidationLab.unity";
        private static readonly string[] LegacyPrefixes =
            { "CivilianVsOne", "VeteranVsThree", "ProtectCivilian", "ThreeCiviliansVsBrute" };

        [MenuItem("Reclamation/Validation/Create or Replace Systems Validation Lab")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Replace systems lab?", "The generated SystemsValidationLab scene will be replaced.", "Replace", "Cancel")) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 10;
            camera.transform.position = new Vector3(-5, 16, -18); camera.transform.LookAt(Vector3.zero);
            camera.gameObject.AddComponent<LabCameraController>(); camera.backgroundColor = new Color(0.2f, 0.25f, 0.3f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0); sun.intensity = 1.2f; RenderSettings.ambientLight = Color.gray;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "Arena ground";
            ground.transform.position = new Vector3(0, -0.1f, 0); ground.transform.localScale = new Vector3(32, 0.2f, 28);
            ground.GetComponent<Renderer>().sharedMaterial = MaterialFor("Ground", new Color(0.32f, 0.38f, 0.32f));
            var surface = ground.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.All; surface.layerMask = 1;
            var clock = new GameObject("Clock").AddComponent<NeighborhoodClock>();
            var allPeople = new List<OutbreakAgent>(); var allZombies = new List<OutbreakAgent>();
            string[] names = { "[Combat] Civilian vs ordinary", "[Combat] Veteran vs three", "[Combat] Bite-rescue squad", "[Combat] Three civilians vs brute", "[Combat] Rank comparison", "[Settlement] Hunger and meals", "[Settlement] Energy and sleep", "[Settlement] Farming and food supply", "[Settlement] Injury and medicine", "[Settlement] Morale and recreation", "[Settlement] Player work priorities" };
            var roots = new GameObject[names.Length];
            for (int i = 0; i < roots.Length; i++) { roots[i] = new GameObject($"Scenario {i + 1} — {names[i]}"); roots[i].SetActive(false); }
            AddScenario(roots[0], clock, allPeople, allZombies, new[] { 0 }, 1, false);
            AddScenario(roots[1], clock, allPeople, allZombies, new[] { 300 }, 3, false);
            AddScenario(roots[2], clock, allPeople, allZombies, new[] { 300, 300, 0 }, 5, false);
            AddScenario(roots[3], clock, allPeople, allZombies, new[] { 0, 0, 0 }, 1, true);
            AddScenario(roots[4], clock, allPeople, allZombies, new[] { 0, 100, 300 }, 3, false);
            AddSettlementScenario(roots[5], false);
            AddSettlementScenario(roots[6], true);
            AddSettlementScenario(roots[7], false, true);
            AddSettlementScenario(roots[8], false, false, true);
            AddSettlementScenario(roots[9], false, false, false, true);
            AddSettlementScenario(roots[10], false, false, false, false, true);
            var root = new GameObject("Combat and Outbreak Director"); var combat = root.AddComponent<CombatDirector>();
            combat.ConfigureAwareness(false);
            var outbreak = root.AddComponent<OutbreakDirector>(); outbreak.Configure(clock, allPeople.ToArray(), null, 614);
            root.AddComponent<CombatEncounter>().Configure(allZombies.ToArray());
            root.AddComponent<SystemsValidationLab>().Configure(roots, names, outbreak);
            roots[0].SetActive(true); // Gives camera discovery a deterministic active population before runtime Awake ordering.
            surface.BuildNavMesh();
            const string navigationPath = "Assets/Scenes/SystemsValidationNavigation.asset";
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(navigationPath) != null) AssetDatabase.DeleteAsset(navigationPath);
            if (surface.navMeshData != null) AssetDatabase.CreateAsset(surface.navMeshData, navigationPath);
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, ScenePath); Selection.activeGameObject = root;
            Debug.Log("Systems Validation Lab created. Press Play and choose scenarios from the upper-right panel.");
        }

        private static void AddSettlementScenario(GameObject root, bool fatigueScenario, bool farmingScenario = false,
            bool medicalScenario = false, bool moraleScenario = false, bool priorityScenario = false)
        {
            var stockObject = GameObject.CreatePrimitive(PrimitiveType.Cube); stockObject.name = "Wood stockpile";
            stockObject.layer = 2; stockObject.transform.SetParent(root.transform, false);
            stockObject.transform.position = new Vector3(-4, 0.25f, 3); stockObject.transform.localScale = new Vector3(2, 0.5f, 2);
            stockObject.GetComponent<Renderer>().sharedMaterial = MaterialFor("WoodStock", new Color(0.45f, 0.28f, 0.12f));
            var stock = stockObject.AddComponent<Stockpile>();

            var foodObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foodObject.name = farmingScenario ? "Food store — initially empty" : "Food store — 6 meals";
            foodObject.layer = 2; foodObject.transform.SetParent(root.transform, false);
            foodObject.transform.position = new Vector3(4, 0.25f, 3); foodObject.transform.localScale = new Vector3(2, 0.5f, 2);
            foodObject.GetComponent<Renderer>().sharedMaterial = MaterialFor("FoodStore", new Color(0.25f, 0.55f, 0.2f));
            var food = foodObject.AddComponent<FoodStore>(); food.Configure(farmingScenario ? 0 : 6);

            MedicineStore medicine = null;
            if (medicalScenario || priorityScenario)
            {
                var medicineObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                medicineObject.name = "Medicine store — 2 supplies"; medicineObject.layer = 2;
                medicineObject.transform.SetParent(root.transform, false);
                medicineObject.transform.position = new Vector3(7, 0.35f, 1);
                medicineObject.transform.localScale = new Vector3(1.5f, 0.7f, 1.5f);
                medicineObject.GetComponent<Renderer>().sharedMaterial = MaterialFor("Medicine", new Color(0.2f, 0.75f, 0.75f));
                medicine = medicineObject.AddComponent<MedicineStore>(); medicine.Configure(2);
            }

            RecreationSpot recreation = null;
            if (moraleScenario || priorityScenario)
            {
                var recreationObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                recreationObject.name = "Community gathering spot — 2 spaces"; recreationObject.layer = 2;
                recreationObject.transform.SetParent(root.transform, false);
                recreationObject.transform.position = new Vector3(0, 0.15f, 0);
                recreationObject.transform.localScale = new Vector3(2.2f, 0.15f, 2.2f);
                recreationObject.GetComponent<Renderer>().sharedMaterial = MaterialFor("Recreation", new Color(0.9f, 0.5f, 0.15f));
                recreation = recreationObject.AddComponent<RecreationSpot>(); recreation.Configure(2);
            }

            var beds = new Bed[2];
            for (int i = 0; i < beds.Length; i++)
            {
                var bedObject = GameObject.CreatePrimitive(PrimitiveType.Cube); bedObject.name = $"Bed {i + 1}";
                bedObject.layer = 2; bedObject.transform.SetParent(root.transform, false);
                bedObject.transform.position = new Vector3(-5 + i * 2.5f, 0.2f, -6);
                bedObject.transform.localScale = new Vector3(1.1f, 0.35f, 2.2f);
                bedObject.GetComponent<Renderer>().sharedMaterial = MaterialFor("Bed", new Color(0.18f, 0.4f, 0.65f));
                beds[i] = bedObject.AddComponent<Bed>();
            }

            var pileObject = GameObject.CreatePrimitive(PrimitiveType.Cube); pileObject.name = "Loose wood";
            pileObject.layer = 2; pileObject.transform.SetParent(root.transform, false);
            pileObject.transform.position = new Vector3(0, 0.5f, 6); pileObject.transform.localScale = new Vector3(1.5f, 1, 1.5f);
            pileObject.GetComponent<Renderer>().sharedMaterial = MaterialFor("LooseWood", new Color(0.35f, 0.17f, 0.06f));
            var pile = pileObject.AddComponent<ResourcePile>(); pile.Configure(12);

            var boardObject = new GameObject("Settlement job board"); boardObject.transform.SetParent(root.transform, false);
            var board = boardObject.AddComponent<HaulJobBoard>(); board.Configure(stock, new[] { pile });
            var policy = boardObject.AddComponent<SettlementPolicy>(); board.SetPolicy(policy);
            if (farmingScenario || priorityScenario)
            {
                var farms = new FarmPlot[2];
                for (int i = 0; i < farms.Length; i++)
                {
                    var farmObject = GameObject.CreatePrimitive(PrimitiveType.Cube); farmObject.name = $"Farm plot {i + 1}";
                    farmObject.layer = 2; farmObject.transform.SetParent(root.transform, false);
                    farmObject.transform.position = new Vector3(-2.5f + i * 5f, 0.1f, 6);
                    farmObject.transform.localScale = new Vector3(3.5f, 0.2f, 3f);
                    farmObject.GetComponent<Renderer>().sharedMaterial = MaterialFor("Farm" + i,
                        i == 0 ? new Color(0.32f, 0.65f, 0.2f) : new Color(0.36f, 0.28f, 0.12f));
                    farms[i] = farmObject.AddComponent<FarmPlot>();
                    farms[i].Configure(i == 0 ? 1f : 0.5f, i == 0 ? 4f : 0.2f, 3);
                }
                board.SetFarms(farms);
            }
            if (priorityScenario)
            {
                for (int i = 0; i < ShelterBlueprint.WoodCost; i++) stock.DepositOne();
                var shelterObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelterObject.name = "Priority test shelter / defense blueprint"; shelterObject.layer = 2;
                shelterObject.transform.SetParent(root.transform, false);
                shelterObject.transform.position = new Vector3(0, 0.2f, 10);
                shelterObject.transform.localScale = new Vector3(4, 0.4f, 3);
                shelterObject.GetComponent<Renderer>().sharedMaterial = MaterialFor("PriorityBlueprint", new Color(0.25f, 0.65f, 0.8f));
                board.SetShelter(shelterObject.AddComponent<ShelterBlueprint>());
            }
            float[] hunger = priorityScenario ? new[] { 60f, 20f, 20f } :
                moraleScenario ? new[] { 20f, 20f, 20f } :
                farmingScenario ? new[] { 75f, 20f, 20f } :
                fatigueScenario ? new[] { 20f, 20f, 20f } : new[] { 72f, 55f, 20f };
            float[] energy = priorityScenario ? new[] { 50f, 100f, 100f } :
                fatigueScenario ? new[] { 20f, 25f, 100f } : new[] { 100f, 100f, 100f };
            float[] injury = priorityScenario ? new[] { 15f, 0f, 0f } :
                medicalScenario ? new[] { 75f, 35f, 0f } : new[] { 0f, 0f, 0f };
            float[] morale = priorityScenario ? new[] { 50f, 70f, 85f } :
                moraleScenario ? new[] { 15f, 30f, 85f } : new[] { 70f, 70f, 70f };
            for (int i = 0; i < hunger.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.name = $"Settlement survivor {i + 1}";
                go.layer = 2; go.transform.SetParent(root.transform, false); go.transform.position = new Vector3((i - 1) * 2, 0, -3);
                go.GetComponent<Collider>().enabled = false;
                go.GetComponent<Renderer>().sharedMaterial = MaterialFor("Settlement" + i, new Color(0.2f + i * 0.25f, 0.5f, 0.8f - i * 0.2f));
                var nav = go.AddComponent<NavMeshAgent>(); nav.baseOffset = 1; nav.speed = 4; nav.acceleration = 14; nav.angularSpeed = 720;
                var worker = go.AddComponent<HaulWorker>(); worker.Configure(go.name, board); worker.ConfigureNeeds(food, hunger[i]);
                worker.ConfigureRest(beds, energy[i]);
                worker.ConfigureMedicine(medicine, injury[i]);
                worker.ConfigureMorale(recreation, morale[i]);
                HumanVisualBuilder.Add(go);
            }
            var overlay = new GameObject("Settlement decision overlay"); overlay.transform.SetParent(root.transform, false);
            overlay.AddComponent<SettlementDebugOverlay>().Configure(board, stock, food, beds, medicine, recreation);
        }

        [MenuItem("Reclamation/Validation/Remove Legacy Generated Combat Labs")]
        public static void CleanupLegacyLabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !EditorUtility.DisplayDialog("Remove old combat labs?", "Only generated combat scenes/navigation assets with known legacy prefixes will be removed. Patient Zero is preserved.", "Remove", "Cancel")) return;
            int removed = 0;
            foreach (string guid in AssetDatabase.FindAssets("", new[] { "Assets/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); string filename = System.IO.Path.GetFileNameWithoutExtension(path);
                foreach (string prefix in LegacyPrefixes)
                    if (filename.StartsWith(prefix, System.StringComparison.Ordinal))
                    { if (AssetDatabase.DeleteAsset(path)) removed++; break; }
            }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); Debug.Log($"Removed {removed} legacy generated combat-lab assets. Patient Zero was not touched.");
        }

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
            Debug.Log("Autonomous combat and persistent survivor progression enabled. Save the scene.");
        }

        private static void AddScenario(GameObject root, NeighborhoodClock clock, List<OutbreakAgent> people,
            List<OutbreakAgent> zombies, int[] humanExperience, int zombieCount, bool brute)
        {
            for (int i = 0; i < humanExperience.Length; i++) people.Add(Person(root.transform, $"Survivor {i + 1}",
                new Vector3((i - (humanExperience.Length - 1) * 0.5f) * 1.7f, 0, -2), humanExperience[i], false, false, clock));
            for (int i = 0; i < zombieCount; i++)
            {
                var zombie = Person(root.transform, brute ? "Brute" : $"Zombie {i + 1}",
                    new Vector3((i - (zombieCount - 1) * 0.5f) * 1.5f, 0, 2.8f + (i % 2) * 0.6f), 0, true, brute, clock);
                people.Add(zombie); zombies.Add(zombie);
            }
        }

        private static OutbreakAgent Person(Transform parent, string name, Vector3 feet, int experience, bool zombie, bool brute, NeighborhoodClock clock)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.name = name; go.layer = 2; go.transform.SetParent(parent, false);
            go.transform.position = feet; go.GetComponent<Collider>().enabled = false;
            string tier = zombie ? "Zombie" : experience >= 300 ? "Veteran" : experience >= 100 ? "Trained" : "Civilian";
            Color color = zombie ? new Color(0.4f, 0.55f, 0.3f) : experience >= 300 ? new Color(0.2f, 0.5f, 0.9f) :
                experience >= 100 ? new Color(0.65f, 0.35f, 0.85f) : new Color(0.95f, 0.65f, 0.2f);
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(tier, color);
            var nav = go.AddComponent<NavMeshAgent>(); nav.baseOffset = 1; nav.radius = 0.45f;
            var home = new GameObject(name + " hold point").transform; home.SetParent(parent, false); home.position = feet;
            go.AddComponent<CivilianRoutine>().Configure(name, clock, home, home, home, 0);
            var person = go.AddComponent<OutbreakAgent>(); person.Configure(name); if (brute) person.SetZombieClass(ZombieClass.Brute);
            var fighter = go.AddComponent<Combatant>(); fighter.Configure(false, CombatOrder.Hold); fighter.ConfigureProgression(false, experience);
            HumanVisualBuilder.Add(go); return person;
        }

        private static Material MaterialFor(string name, Color color)
        {
            const string folder = "Assets/Reclamation/GeneratedMaterials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Reclamation", "GeneratedMaterials");
            string path = $"{folder}/Combat{name}.mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            AssetDatabase.CreateAsset(material, path); return material;
        }
    }
}
