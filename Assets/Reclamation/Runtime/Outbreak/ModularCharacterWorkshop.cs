using UnityEngine;
using UnityEngine.InputSystem;

namespace Reclamation.Blight
{
    public sealed class ModularCharacterWorkshop : MonoBehaviour
    {
        private ModularHumanRig character;
        private Camera view;
        private bool athletic, heavy = true, axe, spin;
        private float face, yaw = 20, distance = 4.5f;
        private string framing = "Body";
        private int expression;
        private readonly string[] expressions = { "Neutral", "Determined", "Angry", "Hurt", "Shout", "Smile" };
        private readonly string[] clips = { "Idle", "Walk", "Run", "LightAttack", "HeavyAttack", "Dodge", "Block", "Stagger", "Death", "Crawl" };
        private Material groundMaterial;
        private Material targetMaterial;
        private LineRenderer targetGuide;
        private float Scale => Mathf.Max(.2f, Mathf.Min(Screen.width / 1200f, Screen.height / 800f));
        public ModularHumanRig Character => character;

        private void Start()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Workshop floor"; floor.transform.SetParent(transform);
            floor.transform.position = Vector3.down * .15f; floor.transform.localScale = new Vector3(12, .3f, 12);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) shader = Shader.Find("Standard");
            groundMaterial = new Material(shader) { color = new Color(.17f, .20f, .22f) }; floor.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            var light = new GameObject("Workshop key light").AddComponent<Light>(); light.transform.SetParent(transform);
            light.type = LightType.Directional; light.intensity = 1.5f; light.transform.rotation = Quaternion.Euler(35, -30, 0);
            view = new GameObject("Workshop camera").AddComponent<Camera>(); view.transform.SetParent(transform); view.fieldOfView = 36;
            view.nearClipPlane = .05f; view.backgroundColor = new Color(.12f, .15f, .18f); view.clearFlags = CameraClearFlags.SolidColor;
            Rebuild(false);
            targetGuide = new GameObject("Forward target guide").AddComponent<LineRenderer>();
            targetGuide.transform.SetParent(transform, false); targetGuide.useWorldSpace = false;
            targetGuide.positionCount = 4; targetGuide.loop = true; targetGuide.widthMultiplier = .015f;
            targetGuide.SetPositions(new[] { new Vector3(-.3f, .85f, 1.25f), new Vector3(-.3f, 1.65f, 1.25f),
                new Vector3(.3f, 1.65f, 1.25f), new Vector3(.3f, .85f, 1.25f) });
            targetMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            targetMaterial.color = new Color(.3f, .85f, .55f); targetGuide.sharedMaterial = targetMaterial;
        }
        public void Rebuild(bool useAthletic)
        {
            athletic = useAthletic;
            if (character != null) { character.gameObject.SetActive(false); Destroy(character.gameObject); }
            var root = new GameObject("Modular human"); root.transform.SetParent(transform, false);
            character = root.AddComponent<ModularHumanRig>(); character.Build(athletic); character.SetAppearance(heavy, axe, face);
            character.TwoHandGrip = true; character.SetExpression(expressions[expression]);
        }
        private void Update()
        {
            if (character == null) return;
            Mouse mouse = Mouse.current;
            if (mouse != null && GUIUtility.hotControl == 0)
            {
                Vector2 point = mouse.position.ReadValue();
                bool overPanel = point.x / Scale < 316;
                if (!overPanel)
                {
                    if (mouse.rightButton.isPressed) yaw += mouse.delta.ReadValue().x * .25f;
                    distance = Mathf.Clamp(distance - mouse.scroll.ReadValue().y * .003f, .7f, 6);
                }
            }
            if (spin && !character.IsRagdoll) yaw += Time.deltaTime * 18;
        }
        private void LateUpdate()
        {
            if (character == null || view == null) return;
            Vector3 focus = framing == "Face" ? character.Bone("Head").position + Vector3.up * .12f :
                framing == "Hands" ? character.Bone("Hand_R").position : character.Bone("Pelvis").position + Vector3.up * .2f;
            Quaternion rotation = Quaternion.Euler(10, yaw, 0);
            view.transform.position = focus + rotation * Vector3.forward * distance; view.transform.LookAt(focus);
            // Offset the composition so the character clears the controls.
            view.rect = new Rect(.24f, 0, .76f, 1);
        }
        private void OnGUI()
        {
            if (character == null) return;
            Matrix4x4 old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1));
            GUI.Box(new Rect(10, 10, 295, 735), "CHARACTER WORKSHOP");
            GUI.Label(new Rect(24, 42, 260, 45), "Body, face and gear are independent.\nPrototype art / shared 57-bone rig.");
            if (GUI.Button(new Rect(24, 93, 258, 30), athletic ? "Body: Athletic — change" : "Body: Broad — change")) Rebuild(!athletic);
            GUI.Label(new Rect(24, 131, 258, 22), "Face: angular ← → soft");
            float nextFace = GUI.HorizontalSlider(new Rect(24, 159, 258, 20), face, 0, 1);
            if (!Mathf.Approximately(nextFace, face)) { face = nextFace; character.SetAppearance(heavy, axe, face); }
            if (GUI.Button(new Rect(24, 189, 124, 30), heavy ? "Heavy armour" : "Light armour"))
            { heavy = !heavy; character.SetAppearance(heavy, axe, face); }
            if (GUI.Button(new Rect(154, 189, 128, 30), character.LongHair ? "Long hair" : "Short hair")) character.SetHair(!character.LongHair);
            if (GUI.Button(new Rect(24, 227, 258, 30), axe ? "Weapon: Axe — change" : "Weapon: Sword — change"))
            { axe = !axe; character.SetAppearance(heavy, axe, face); }
            character.TwoHandGrip = GUI.Toggle(new Rect(24, 265, 260, 24), character.TwoHandGrip, "Two-hand grip test");
            if (GUI.Button(new Rect(24, 293, 260, 24), "Expression: " + expressions[expression]))
            { expression = (expression + 1) % expressions.Length; character.SetExpression(expressions[expression]); }
            spin = GUI.Toggle(new Rect(24, 321, 260, 24), spin, "Turntable");
            for (int i = 0; i < 3; i++)
                if (GUI.Button(new Rect(24 + i * 87, 354, 82, 28), i == 0 ? "Body" : i == 1 ? "Face" : "Hands"))
                { framing = i == 0 ? "Body" : i == 1 ? "Face" : "Hands"; distance = i == 0 ? 4.5f : i == 1 ? 1.0f : 1.5f; }
            GUI.Label(new Rect(24, 395, 258, 24), "Motion: " + character.CurrentClip);
            bool previous = GUI.enabled; GUI.enabled = previous && !character.IsRagdoll;
            for (int i = 0; i < clips.Length; i++)
                if (GUI.Button(new Rect(24 + i % 2 * 130, 423 + i / 2 * 32, 124, 28), clips[i])) character.Play(clips[i]);
            GUI.enabled = previous;
            if (GUI.Button(new Rect(24, 594, 258, 34), character.IsRagdoll ? "Recover from ragdoll" : "Test ragdoll")) character.SetRagdoll(!character.IsRagdoll);
            GUI.Label(new Rect(24, 639, 258, 75), "Right-drag: orbit | Scroll: zoom\nRecovery resets the pose; it is not\na finished get-up animation.\nCombat lab is a separate test.");
            GUI.Box(new Rect(330, 10, 280, 162), "WEAPON REVIEW");
            character.ShowBladeEdge = GUI.Toggle(new Rect(344, 37, 252, 24), character.ShowBladeEdge, "Highlight cutting edge");
            bool slow = GUI.Toggle(new Rect(344, 63, 252, 24), character.PlaybackSpeed < 1, "Quarter-speed animation");
            character.PlaybackSpeed = slow ? .25f : 1;
            GUI.Label(new Rect(344, 89, 252, 24), "Off-hand contact error: " + (character.GripError * 1000).ToString("F1") + " mm");
            if (targetGuide != null) targetGuide.enabled = GUI.Toggle(new Rect(344, 115, 252, 24), targetGuide.enabled, "Show target in front");
            GUI.Label(new Rect(344, 141, 252, 24), "Guide only; no hit detection.");
            GUI.matrix = old;
        }
        private void OnDestroy()
        { if (groundMaterial != null) Destroy(groundMaterial); if (targetMaterial != null) Destroy(targetMaterial); }
    }
}
