using Reclamation.Blight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reclamation.Editor
{
    public static class BlightCombatLabBuilder
    {
        [MenuItem("Reclamation/Play/Create Village Defense Demo")]
        public static void CreateVillageDefense()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lab=new GameObject("Village Defense Demo").AddComponent<BlightCombatLab>();
            var serialized=new SerializedObject(lab);
            serialized.FindProperty("initialScenario").enumValueIndex=(int)BlightScenario.Company;
            serialized.FindProperty("expandedCompany").boolValue=true;
            serialized.FindProperty("villageDefense").boolValue=true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Village defense ready: press Play, plan orders, then Start waves. G map; 4 fallback.");
        }

        [MenuItem("Reclamation/Play/Create Company Command Demo")]
        public static void CreateCompany()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lab = new GameObject("Company Command Lab").AddComponent<BlightCombatLab>();
            var serialized = new SerializedObject(lab);
            serialized.FindProperty("initialScenario").enumValueIndex = (int)BlightScenario.Company;
            serialized.FindProperty("expandedCompany").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Company Command Lab ready. Press Play; select a platoon, location and mission. F at camp reviews the operation.");
        }

        [MenuItem("Reclamation/Play/Create Outpost Mission")]
        public static void CreateOutpost()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lab = new GameObject("Blighted Outpost Mission").AddComponent<BlightCombatLab>();
            var serialized = new SerializedObject(lab);
            serialized.FindProperty("initialScenario").enumValueIndex = (int)BlightScenario.Outpost;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Blighted Outpost ready. Press Play. Choose an approach at a marked route and press F. R restarts the mission.");
        }

        [MenuItem("Reclamation/Testing/Create Combat Sandbox")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Blight Combat Lab").AddComponent<BlightCombatLab>();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Blight Combat Lab ready. Press Play and use Choose scenario: Duel, Squad, Hulk, Weapons, Patrol. The arena is generated at runtime. Save this new scene if desired. Existing labs are unchanged.");
        }
    }
}
