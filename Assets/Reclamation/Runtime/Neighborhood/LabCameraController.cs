using Reclamation.Outbreak;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

namespace Reclamation.Neighborhood
{
    [RequireComponent(typeof(Camera))]
    public sealed class LabCameraController : MonoBehaviour
    {
        [SerializeField, Range(0.05f, 0.5f)]
        [Tooltip("Zoom strength per wheel step. Default 0.18 changes the view size by about 16% per step.")]
        private float scrollSensitivity = 0.18f;
        private Camera view;
        private Vector3 pivot, initialPivot;
        private float yaw, pitch, distance, initialYaw, initialPitch, initialDistance, initialSize;
        private Transform followed, selected;
        private NavMeshAgent[] people;
        private OutbreakDirector outbreakPanel;
        private NeighborhoodPanel neighborhoodPanel;
        private SystemsValidationLab validationPanel;
        private Reclamation.Prototype.SettlementDebugOverlay settlementPanel;
        private bool showHelp;
        private GUIStyle textStyle, buttonStyle;
        public Transform FollowTarget => followed;
        public Transform SelectedTarget => selected;
        public bool InputEnabled { get; set; } = true;
        public static float UiScale => Mathf.Max(0.2f, Mathf.Min(Screen.height / 900f, Screen.width / 1000f));
        private Rect PanelRect => new Rect(Screen.width / UiScale - 356, Screen.height / UiScale - (showHelp ? 250 : 150), 340, showHelp ? 234 : 134);

        private void Awake()
        {
            view = GetComponent<Camera>();
            pitch = transform.eulerAngles.x;
            yaw = transform.eulerAngles.y;
            var ground = new Plane(Vector3.up, Vector3.zero);
            var ray = new Ray(transform.position, transform.forward);
            pivot = ground.Raycast(ray, out float t) ? ray.GetPoint(t) : transform.position + transform.forward * 25;
            distance = Vector3.Distance(transform.position, pivot);
            initialPivot = pivot; initialYaw = yaw; initialPitch = pitch;
            initialDistance = distance; initialSize = view.orthographicSize;
            people = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.InstanceID);
            outbreakPanel = FindFirstObjectByType<OutbreakDirector>();
            neighborhoodPanel = FindFirstObjectByType<NeighborhoodPanel>();
            validationPanel = FindFirstObjectByType<SystemsValidationLab>();
            settlementPanel = FindFirstObjectByType<Reclamation.Prototype.SettlementDebugOverlay>();
        }

        public void Follow(Transform person)
        {
            if (person == null || !person.gameObject.activeInHierarchy) return;
            selected = followed = person;
            pivot = person.position;
        }

        public void Select(Transform person, bool follow = false)
        {
            if (person == null || !person.gameObject.activeInHierarchy) return;
            selected = person;
            if (follow) Follow(person);
            else followed = null;
        }

        public void ClearSelection()
        {
            selected = null;
            followed = null;
        }

        public void ResetView()
        {
            selected = followed = null; pivot = initialPivot; yaw = initialYaw; pitch = initialPitch;
            distance = initialDistance; view.orthographicSize = initialSize;
        }

        public void RefreshPeople(GameObject scenarioRoot)
        {
            selected = followed = null;
            settlementPanel = scenarioRoot == null ?
                FindFirstObjectByType<Reclamation.Prototype.SettlementDebugOverlay>() :
                scenarioRoot.GetComponentInChildren<Reclamation.Prototype.SettlementDebugOverlay>(false);
            people = scenarioRoot == null
                ? FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.InstanceID)
                : scenarioRoot.GetComponentsInChildren<NavMeshAgent>(false);
        }

        private void Cycle(int direction)
        {
            if (people == null || people.Length == 0) return;
            int current = direction > 0 ? -1 : 0;
            for (int i = 0; i < people.Length; i++)
                if (people[i] != null && people[i].transform == followed) current = i;
            for (int n = 1; n <= people.Length; n++)
            {
                int index = (current + direction * n + people.Length * 2) % people.Length;
                if (people[index] != null && people[index].gameObject.activeInHierarchy)
                { Follow(people[index].transform); return; }
            }
        }

        private void LateUpdate()
        {
            if (followed != null && !followed.gameObject.activeInHierarchy) followed = null;
            if (selected != null && !selected.gameObject.activeInHierarchy) selected = null;
            if (followed != null) pivot = followed.position;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            bool uiBusy = GUIUtility.keyboardControl != 0 || GUIUtility.hotControl != 0;
            float dt = Time.unscaledDeltaTime;
            if (InputEnabled && !uiBusy && keyboard != null)
            {
                if (keyboard.homeKey.wasPressedThisFrame) ResetView();
                if (keyboard.escapeKey.wasPressedThisFrame) ClearSelection();
                if (keyboard.tabKey.wasPressedThisFrame) Cycle(keyboard.shiftKey.isPressed ? -1 : 1);
                float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) -
                    (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                float z = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) -
                    (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
                if (x != 0 || z != 0)
                {
                    followed = null;
                    pivot += Quaternion.Euler(0, yaw, 0) * new Vector3(x, 0, z).normalized *
                        Mathf.Max(5, view.orthographic ? view.orthographicSize : distance * 0.5f) * dt;
                }
                yaw += ((keyboard.eKey.isPressed ? 1 : 0) - (keyboard.qKey.isPressed ? 1 : 0)) * 75 * dt;
            }
            if (InputEnabled && !uiBusy && mouse != null)
            {
                Vector2 pixel = mouse.position.ReadValue();
                Vector2 gui = new Vector2(pixel.x, Screen.height - pixel.y) / UiScale;
                bool overUI = PanelRect.Contains(gui) ||
                    (outbreakPanel != null && outbreakPanel.ContainsGuiPoint(gui)) ||
                    (neighborhoodPanel != null && neighborhoodPanel.ContainsGuiPoint(gui)) ||
                    (settlementPanel != null && settlementPanel.ContainsGuiPoint(gui)) ||
                    (validationPanel != null && validationPanel.ContainsGuiPoint(gui));
                bool insideView = pixel.x >= 0 && pixel.y >= 0 && pixel.x < Screen.width && pixel.y < Screen.height;
                if (!overUI && insideView)
                {
                    if (mouse.leftButton.wasPressedThisFrame) SelectAt(pixel);
                    float scrollSteps = mouse.scroll.ReadValue().y;
                    // Input System 1.20 defaults to normalized steps. Only legacy Windows
                    // platform-specific input uses 120 units per step; do not scale twice.
                    bool nativeWindowsScroll = InputSystem.settings.scrollDeltaBehavior ==
                        InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange &&
                        (Application.platform == RuntimePlatform.WindowsEditor ||
                         Application.platform == RuntimePlatform.WindowsPlayer);
                    if (nativeWindowsScroll) scrollSteps /= 120f;
                    float zoom = Mathf.Exp(-scrollSteps * scrollSensitivity);
                    if (view.orthographic) view.orthographicSize = Mathf.Clamp(view.orthographicSize * zoom, 3, 45);
                    else distance = Mathf.Clamp(distance * zoom, 6, 100);
                    if (mouse.rightButton.isPressed)
                    {
                        Vector2 delta = mouse.delta.ReadValue();
                        yaw += delta.x * 0.2f; pitch = Mathf.Clamp(pitch - delta.y * 0.2f, 20, 80);
                    }
                    if (mouse.middleButton.isPressed)
                    {
                        Vector2 delta = mouse.delta.ReadValue();
                        float extent = view.orthographic ? view.orthographicSize : distance * Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad / 2);
                        followed = null;
                        pivot -= Quaternion.Euler(0, yaw, 0) * new Vector3(delta.x, 0, delta.y) * (2 * extent / Screen.height);
                    }
                }
            }
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            transform.SetPositionAndRotation(pivot - rotation * Vector3.forward * distance, rotation);
        }

        private void SelectAt(Vector2 pixel)
        {
            Ray ray = view.ScreenPointToRay(pixel);
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            RaycastHit nearest = default;
            bool found = false;
            foreach (RaycastHit hit in hits)
                if (!found || hit.distance < nearest.distance) { nearest = hit; found = true; }
            if (!found) { ClearSelection(); return; }
            NavMeshAgent person = nearest.transform.GetComponentInParent<NavMeshAgent>();
            if (person == null) ClearSelection();
            else Select(person.transform);
        }

        private void OnGUI()
        {
            if (textStyle == null)
            {
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
                buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 17 };
            }
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(UiScale, UiScale, 1));
            GUILayout.BeginArea(PanelRect, GUI.skin.box);
            string cameraState = followed != null ? $"Following: {followed.name}" :
                selected != null ? $"Selected: {selected.name}" : "Camera: free view";
            GUILayout.Label(cameraState, textStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Previous", buttonStyle)) Cycle(-1);
            if (GUILayout.Button("Next person", buttonStyle)) Cycle(1);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Overview", buttonStyle)) ResetView();
            if (GUILayout.Button(showHelp ? "Hide help" : "Controls", buttonStyle)) showHelp = !showHelp;
            GUILayout.EndHorizontal();
            if (showHelp) GUILayout.Label("Scroll: zoom · WASD/arrows: pan\nMiddle-drag: pan · Right-drag: orbit\nQ/E: rotate · Tab: next person\nEsc: stop following · Home: overview\nCamera works while simulation is paused.", textStyle);
            GUILayout.EndArea();
            GUI.matrix = previous;
        }
    }
}
