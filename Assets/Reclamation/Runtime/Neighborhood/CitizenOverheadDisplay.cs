using System.Collections.Generic;
using Reclamation.Outbreak;
using Reclamation.Prototype;
using UnityEngine;

namespace Reclamation.Neighborhood
{
    // One presentation-only overlay for both existing scenes and newly generated labs.
    public sealed class CitizenOverheadDisplay : MonoBehaviour
    {
        private sealed class Citizen
        {
            public Transform root;
            public Renderer[] renderers;
            public Combatant combat;
            public OutbreakAgent person;
            public HaulWorker worker;
            public SurvivorNeeds needs;
            public MedicalCondition medical;
            public SurvivorMorale morale;
            public CivilianRoutine routine;
            public float alpha;
            public Vector3 anchor;
            public bool visible;
        }

        private readonly List<Citizen> citizens = new();
        private readonly HashSet<Transform> discovered = new();
        private float nextScan;
        private GUIStyle label;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<CitizenOverheadDisplay>() != null) return;
            var overlay = new GameObject("Citizen overhead indicators");
            overlay.AddComponent<CitizenOverheadDisplay>();
            DontDestroyOnLoad(overlay);
        }

        public static float FocusOpacity(Vector2 viewport)
        {
            float distance = Vector2.Distance(viewport, new Vector2(0.5f, 0.5f));
            return Mathf.Lerp(0.32f, 0.92f, 1 - Mathf.SmoothStep(0, 1, distance / 0.3f));
        }

        public static string PublicCondition(InfectionState state)
        {
            // Never disclose latent infection through the HUD.
            return state == InfectionState.Symptomatic ? "! Sick" : "";
        }

        private void Scan()
        {
            citizens.RemoveAll(c => c.root == null);
            discovered.Clear();
            foreach (var citizen in citizens) discovered.Add(citizen.root);
            foreach (var routine in FindObjectsByType<CivilianRoutine>(FindObjectsSortMode.None)) Add(routine.transform);
            foreach (var worker in FindObjectsByType<HaulWorker>(FindObjectsSortMode.None)) Add(worker.transform);
        }

        private void Add(Transform actor)
        {
            if (!discovered.Add(actor)) return;
            citizens.Add(new Citizen { root = actor, renderers = actor.GetComponentsInChildren<Renderer>(),
                combat = actor.GetComponent<Combatant>(), person = actor.GetComponent<OutbreakAgent>(),
                worker = actor.GetComponent<HaulWorker>(), needs = actor.GetComponent<SurvivorNeeds>(),
                medical = actor.GetComponent<MedicalCondition>(),
                morale = actor.GetComponent<SurvivorMorale>(),
                routine = actor.GetComponent<CivilianRoutine>() });
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= nextScan) { Scan(); nextScan = Time.unscaledTime + 0.5f; }
            Camera camera = Camera.main;
            LabCameraController controller = camera == null ? null : camera.GetComponent<LabCameraController>();
            foreach (var c in citizens)
            {
                c.visible = false;
                if (camera == null || c.root == null || !c.root.gameObject.activeInHierarchy) continue;
                if (c.person != null && (c.person.State == InfectionState.Turned || c.person.State == InfectionState.Neutralized)) continue;
                // Bounds follow the current character mesh, including model replacements.
                Bounds bounds = new Bounds(c.root.position, Vector3.zero);
                bool hasBounds = false;
                foreach (var renderer in c.renderers)
                    if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                    { if (!hasBounds) bounds = renderer.bounds; else bounds.Encapsulate(renderer.bounds); hasBounds = true; }
                if (!hasBounds) continue;
                Vector3 head = new Vector3(bounds.center.x, bounds.max.y + 0.25f, bounds.center.z);
                Vector3 viewport = camera.WorldToViewportPoint(head);
                if (viewport.z <= camera.nearClipPlane || viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1) continue;
                // Environment geometry occludes indicators; ignore the actor's own colliders.
                Vector3 origin = camera.orthographic
                    ? camera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, camera.nearClipPlane))
                    : camera.transform.position;
                Vector3 ray = head - origin;
                bool blocked = false;
                foreach (var hit in Physics.RaycastAll(origin, ray.normalized, ray.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    if (!hit.transform.IsChildOf(c.root)) { blocked = true; break; }
                if (blocked) continue;
                c.anchor = camera.WorldToScreenPoint(head);
                float targetAlpha = controller != null && controller.SelectedTarget == c.root
                    ? 1f : FocusOpacity(viewport);
                c.alpha = Mathf.Lerp(c.alpha, targetAlpha, 1 - Mathf.Exp(-8 * Time.unscaledDeltaTime));
                c.visible = true;
            }
        }

        private static string Status(Citizen c)
        {
            if (c.combat != null && c.combat.Health <= 0) return "+ Downed";
            if (c.combat != null && c.combat.Action == CombatAction.Grabbed) return "! Grabbed";
            if (c.person != null && c.person.VisibleSymptoms) return PublicCondition(c.person.State);
            if (c.person != null && (c.person.IsFleeing || c.person.IsSprinting)) return "> Escaping";
            if (c.worker != null && c.worker.State == HaulWorker.WorkerState.Treating) return "+ Treating injury";
            if (c.worker != null && c.worker.State == HaulWorker.WorkerState.MovingToTreatment) return "+ Seeking medicine";
            if (c.medical != null && c.medical.Critical) return "! Critical injury";
            if (c.worker != null && c.worker.State == HaulWorker.WorkerState.Socializing) return "* Recovering morale";
            if (c.worker != null && c.worker.State == HaulWorker.WorkerState.MovingToRecreation) return "* Seeking company";
            if (c.morale != null && c.morale.Panicked) return "! Panicked";
            if (c.worker != null && c.worker.State == HaulWorker.WorkerState.Sleeping) return "z Sleeping";
            if (c.worker != null && c.worker.State == HaulWorker.WorkerState.MovingToBed) return "z Going to bed";
            if (c.worker != null && c.worker.State == HaulWorker.WorkerState.Eating) return "o Eating";
            if (c.needs != null && c.needs.NeedsSleep && c.needs.SleepUrgency > c.needs.MealUrgency) return "z Exhausted";
            if (c.needs != null && c.needs.NeedsMeal) return "o Hungry";
            if (c.medical != null && c.medical.NeedsTreatment) return "+ Injured";
            if (c.morale != null && c.morale.NeedsRecovery) return "! Low morale";
            if (c.person != null && c.person.ZoneAssignment == RefugeAssignment.Quarantined) return "+ Quarantine";
            if (c.person != null && c.person.ZoneAssignment == RefugeAssignment.Sheltered) return "[] Sheltered";
            if (c.combat != null && c.combat.Handled) return "! Combat";
            if (c.worker != null && c.worker.State == HaulWorker.WorkerState.Harvesting) return "# Harvesting";
            if (c.worker != null && (c.worker.State == HaulWorker.WorkerState.MovingFoodToStore || c.worker.CarriedFood > 0)) return "[] Food delivery";
            if (c.worker != null && c.worker.State == HaulWorker.WorkerState.Building) return "# Building";
            if (c.worker != null && c.worker.Carrying) return "[] Hauling";
            if (c.morale != null && c.morale.Confident) return "^ Confident";
            return "";
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            label ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12,
                normal = { textColor = Color.white }, clipping = TextClipping.Clip };
            Color previousColor = GUI.color;
            Matrix4x4 previousMatrix = GUI.matrix;
            float scale = LabCameraController.UiScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            foreach (var c in citizens)
            {
                if (!c.visible || c.root == null) continue;
                float x = c.anchor.x / scale, y = (Screen.height - c.anchor.y) / scale;
                GUI.color = new Color(1, 1, 1, c.alpha);
                string name = c.worker != null ? c.worker.WorkerName : c.person != null ? c.person.DisplayName : c.routine.DisplayName;
                GUI.Label(new Rect(x - 64, y - 40, 128, 18), name, label);
                // No invented health for citizens which do not yet have a combat health model.
                if (c.combat != null)
                {
                    float health = Mathf.Clamp01(c.combat.Health / 100f);
                    Draw(new Rect(x - 36, y - 20, 72, 7), new Color(0.06f, 0.08f, 0.1f, c.alpha));
                    Draw(new Rect(x - 35, y - 19, 70 * health, 5), new Color(1 - health * 0.7f, 0.25f + health * 0.55f, 0.3f, c.alpha));
                }
                string status = Status(c);
                if (status.Length > 0)
                {
                    Draw(new Rect(x - 50, y - 10, 100, 19), new Color(0.06f, 0.08f, 0.1f, c.alpha * 0.85f));
                    GUI.color = new Color(1, 1, 1, c.alpha);
                    GUI.Label(new Rect(x - 50, y - 10, 100, 19), status, label);
                }
            }
            GUI.color = previousColor; GUI.matrix = previousMatrix;
        }

        private static void Draw(Rect rect, Color color)
        { GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); }
    }
}
