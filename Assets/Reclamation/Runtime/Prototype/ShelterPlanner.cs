using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Reclamation.Prototype
{
    public sealed class ShelterPlanner : MonoBehaviour
    {
        [SerializeField] private HaulJobBoard board;
        private bool placing;
        private GameObject preview;
        private Material previewMaterial;
        private GUIStyle label;
        private GUIStyle button;
        private string message = "Place one shelter on open ground. Cost: 8 wood.";
        private ResourcePile[] piles;
        private HaulWorker[] workers;
        private float Scale => Mathf.Max(0.6f, Screen.height / 900f);
        private Rect Panel => new Rect(Screen.width / Scale - 446, 16, 430, 320);

        public void Configure(HaulJobBoard jobBoard) => board = jobBoard;

        private void Start()
        {
            piles = FindObjectsByType<ResourcePile>(FindObjectsSortMode.None);
            workers = FindObjectsByType<HaulWorker>(FindObjectsSortMode.None);
            preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            preview.name = "Shelter placement preview";
            preview.layer = 2;
            preview.transform.localScale = new Vector3(5, 0.08f, 4);
            preview.GetComponent<Collider>().enabled = false;
            previewMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            preview.GetComponent<Renderer>().sharedMaterial = previewMaterial;
            preview.SetActive(false);
        }

        private void Update()
        {
            if (!placing || Mouse.current == null || Camera.main == null) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                StopPlacement();
                return;
            }
            Vector2 mouse = Mouse.current.position.ReadValue();
            Vector2 guiPoint = new Vector2(mouse.x, Screen.height - mouse.y) / Scale;
            if (Panel.Contains(guiPoint) || new Rect(16, 16, 620, 340).Contains(guiPoint))
            {
                preview.SetActive(false);
                return;
            }
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(mouse), out RaycastHit hit, 1000, 1 << 0))
            {
                preview.SetActive(false);
                return;
            }
            Vector3 position = new Vector3(Mathf.Round(hit.point.x), hit.point.y, Mathf.Round(hit.point.z));
            bool valid = CanPlace(position);
            preview.SetActive(true);
            preview.transform.position = position + Vector3.up * 0.06f;
            previewMaterial.color = valid ? Color.cyan : new Color(0.9f, 0.15f, 0.1f);
            message = valid ? "Click to place. Escape cancels placement." : "Choose open, reachable ground away from supplies.";
            if (!valid || !Mouse.current.leftButton.wasPressedThisFrame) return;
            var go = new GameObject("Shelter Blueprint");
            go.transform.position = position;
            var site = go.AddComponent<ShelterBlueprint>();
            go.AddComponent<ShelterAppearance>();
            board.SetShelter(site);
            StopPlacement();
            message = "Survivors will supply and build the shelter automatically.";
        }

        private bool CanPlace(Vector3 position)
        {
            if (board == null || board.Destination == null) return false;
            // Require the entire footprint and front work point to lie on navigation ground.
            Vector3[] offsets = { Vector3.zero, new Vector3(-2.8f, 0, -2.2f),
                new Vector3(2.8f, 0, -2.2f), new Vector3(-2.8f, 0, 2.2f),
                new Vector3(2.8f, 0, 2.2f), new Vector3(0, 0, -2.6f) };
            foreach (Vector3 offset in offsets)
                if (!NavMesh.SamplePosition(position + offset, out _, 0.2f, NavMesh.AllAreas)) return false;

            foreach (Collider obstacle in Physics.OverlapBox(position + Vector3.up,
                new Vector3(3, 1, 3), Quaternion.identity, 1 << 2))
            {
                if (obstacle.GetComponent<HaulWorker>() == null) return false;
            }
            if (!NavMesh.SamplePosition(board.Destination.transform.position, out NavMeshHit start, 1, NavMesh.AllAreas))
                return false;
            var path = new NavMeshPath();
            return NavMesh.CalculatePath(start.position, position + new Vector3(0, 0, -2.6f),
                NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        private void StopPlacement()
        {
            placing = false;
            if (preview != null) preview.SetActive(false);
        }

        private void OnGUI()
        {
            if (board == null) return;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true };
                button = new GUIStyle(GUI.skin.button) { fontSize = 20 };
            }
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1));
            GUILayout.BeginArea(Panel, GUI.skin.box);
            GUILayout.Label("SHELTER CONSTRUCTION", label);
            ShelterBlueprint site = board.Shelter;
            if (site == null || site.Cancelled)
            {
                if (GUILayout.Button(placing ? "Cancel placement" : "Place shelter (8 wood)", button, GUILayout.Height(42)))
                {
                    if (placing) StopPlacement(); else placing = true;
                }
            }
            else
            {
                GUILayout.Label($"Delivered: {site.DeliveredWood}/{ShelterBlueprint.WoodCost} wood", label);
                GUILayout.Label(site.Complete ? "Shelter complete!" : $"Construction: {site.Progress:P0}", label);
                if (!site.Complete && GUILayout.Button("Cancel blueprint / refund wood", button, GUILayout.Height(42)))
                {
                    if (site.TryCancel(board.Destination))
                        message = "Delivered wood refunded. Carried wood returns to storage.";
                }
            }
            int loose = 0, carried = 0;
            if (piles != null) foreach (var pile in piles) if (pile != null) loose += pile.Amount;
            if (workers != null) foreach (var worker in workers) if (worker != null && worker.Carrying) carried++;
            int stored = board.Destination == null ? 0 : board.Destination.StoredUnits;
            int built = site == null ? 0 : site.DeliveredWood;
            GUILayout.Label($"Wood: ground {loose} + carried {carried} + stored {stored} + shelter {built} = {loose + carried + stored + built}", label);
            GUILayout.Label(message, label);
            GUILayout.Label("Prototype: one visual shelter; beds and interiors come later.", label);
            GUILayout.EndArea();
            GUI.matrix = previous;
        }

        private void OnDisable() => StopPlacement();
        private void OnDestroy()
        {
            if (preview != null) Destroy(preview);
            if (previewMaterial != null) Destroy(previewMaterial);
        }
    }
}
