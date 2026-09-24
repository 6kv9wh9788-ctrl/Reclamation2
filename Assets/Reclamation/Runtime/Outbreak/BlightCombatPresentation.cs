using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        private readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        private GUIStyle label, title, small, button, centered;
        private Rect StatusRect => new Rect(16, 16, 335, Scenario == BlightScenario.Company ? 116 : HasSquad ? 200 : 116);
        private Rect ScenarioRect => new Rect(UiWidth - 310, 16, 294, scenarioMenu ? 476 : 70);
        private Rect CommandRect => new Rect(16, UiHeight - (help ? 168 : 92), 780, help ? 152 : 76);
        private Rect ObjectiveRect => new Rect(365, 16, Mathf.Max(180, UiWidth - 690), 88);
        private bool ContainsGuiPoint(Vector2 point) =>
            CompanyUiContains(point) || StatusRect.Contains(point) || ScenarioRect.Contains(point) ||
            CommandRect.Contains(point) || ObjectiveRect.Contains(point) || (equipmentMenu && EquipmentRect.Contains(point));

        private Material Material(Color color)
        {
            if (materials.TryGetValue(color, out Material existing)) return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            materials.Add(color, material); return material;
        }

        private Transform Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position;
            go.transform.localScale = scale; go.GetComponent<Collider>().enabled = false;
            go.GetComponent<Renderer>().sharedMaterial = material; return go.transform;
        }

        private Transform Pivot(Transform parent, string name, Vector3 position)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false); pivot.localPosition = position; return pivot;
        }

        private Transform Limb(Transform root, string name, Vector3 position, Material material)
        {
            Transform pivot = Pivot(root, name, position);
            Part(pivot, name + " mesh", Vector3.down * 0.3f, new Vector3(0.19f, 0.6f, 0.22f), material);
            return pivot;
        }

        private Actor CreateActor(string name, Vector3 position, bool enemy, bool hulk = false)
        {
            var actor = new Actor
            {
                root = Pivot(transform, name, position), enemy = enemy, hulk = hulk,
                defenseSeed = actors.Count, fighter = new DuelFighter(hulk ? 260 : enemy && Scenario == BlightScenario.LimbDamage ? 160 : 100, hulk),
                spawn = position, anchor = position, radius = hulk ? 0.8f : 0.43f,
                size = hulk ? 1.65f : 1, delay = 0.7f
            };
            Color color = enemy ? hulk ? new Color(0.4f, 0.2f, 0.45f) : new Color(0.4f, 0.2f, 0.25f)
                : actors.Count == 0 ? new Color(0.13f, 0.4f, 0.65f) : new Color(0.18f, 0.48f, 0.32f);
            Material cloth = Material(color), skin = Material(enemy ? new Color(0.4f, 0.48f, 0.35f) : new Color(0.7f, 0.5f, 0.36f));
            Part(actor.root, "Torso", new Vector3(0, 1.2f, 0), new Vector3(0.55f, 0.65f, 0.32f), cloth);
            Part(actor.root, "Head", new Vector3(0, 1.8f, 0), new Vector3(0.33f, 0.4f, 0.32f), skin);
            Part(actor.root, "Face", new Vector3(0, 1.79f, 0.18f), new Vector3(0.2f, 0.08f, 0.05f), cloth);
            actor.arm = Limb(actor.root, "Weapon arm", new Vector3(0.39f, 1.48f, 0), skin);
            actor.offArm = Limb(actor.root, "Off arm", new Vector3(-0.39f, 1.48f, 0), cloth);
            actor.leftLeg = Limb(actor.root, "Left leg", new Vector3(-0.16f, 0.7f, 0), cloth);
            actor.rightLeg = Limb(actor.root, "Right leg", new Vector3(0.16f, 0.7f, 0), cloth);
            if (!enemy)
            {
                Material steel = Material(new Color(0.7f, 0.78f, 0.85f));
                actor.sword = Pivot(actor.arm, "Sword", Vector3.zero);
                Part(actor.sword, "Blade", new Vector3(0, -0.55f, 0.6f), new Vector3(0.09f, 0.05f, 1.3f), steel);
                Part(actor.sword, "Guard", new Vector3(0, -0.55f, 0.05f), new Vector3(0.3f, 0.08f, 0.06f), steel);
                actor.spear = Pivot(actor.arm, "Spear", Vector3.zero);
                Part(actor.spear, "Shaft", new Vector3(0, -0.5f, 0.7f), new Vector3(0.07f, 0.07f, 2.5f),
                    Material(new Color(0.38f, 0.25f, 0.13f)));
                Part(actor.spear, "Spearhead", new Vector3(0, -0.5f, 2), new Vector3(0.15f, 0.05f, 0.35f), steel);
                actor.axe = Pivot(actor.arm, "Axe", Vector3.zero);
                Part(actor.axe, "Handle", new Vector3(0, -0.55f, 0.5f), new Vector3(0.08f, 0.08f, 1.2f),
                    Material(new Color(0.38f, 0.25f, 0.13f)));
                Part(actor.axe, "Axe head", new Vector3(0, -0.55f, 1.1f), new Vector3(0.48f, 0.1f, 0.3f), steel);
                Equip(actor, BlightWeapon.Sword);
            }
            if (hulk) Part(actor.root, "Blight armour", new Vector3(0, 1.38f, -0.05f),
                new Vector3(0.88f, 0.35f, 0.45f), Material(new Color(0.22f, 0.12f, 0.29f)));
            Face(actor, enemy ? Vector3.back : Vector3.forward);
            if (Scenario == BlightScenario.LimbDamage) BuildLimbRig(actor);
            else if (hulk)
            {
                foreach (Transform child in actor.root) child.gameObject.SetActive(false);
                var visual = new GameObject("Blightbound hulk"); visual.transform.SetParent(actor.root, false);
                actor.hulkVisual = visual.AddComponent<BlightHulkVisual>(); actor.hulkVisual.Build();
            }
            else if (enemy && !hulk)
            {
                foreach (Transform child in actor.root) child.gameObject.SetActive(false);
                var visual = new GameObject("Blighted thrall"); visual.transform.SetParent(actor.root, false);
                actor.thrall = visual.AddComponent<BlightThrallVisual>(); actor.thrall.Build();
            }
            actors.Add(actor); return actor;
        }

        private Transform Marker(string name, Color color, float radius)
        {
            Transform marker = Pivot(transform, name, Vector3.zero);
            Material material = Material(color);
            for (int i = 0; i < 24; i++)
            {
                float angle = i * Mathf.PI * 2 / 24;
                Part(marker, "Marker", new Vector3(Mathf.Sin(angle) * radius, 0.015f, Mathf.Cos(angle) * radius),
                    new Vector3(0.22f, 0.03f, 0.22f), material);
            }
            return marker;
        }

        private void BuildArena()
        {
            Part(transform, "Training ground", new Vector3(0, -0.2f, -2), new Vector3(42, 0.4f, 58),
                Material(new Color(0.19f, 0.23f, 0.2f)));
            Part(transform, "Patrol road", new Vector3(0, 0.002f, -2), new Vector3(7, 0.005f, 42),
                Material(new Color(0.3f, 0.3f, 0.27f)));
            Material stone = Material(new Color(0.28f, 0.27f, 0.32f));
            for (int i = -4; i <= 4; i++)
            {
                Part(transform, "Boundary ruin", new Vector3(-17, 1.5f, i * 5), new Vector3(1.2f, 3, 1.2f), stone);
                Part(transform, "Boundary ruin", new Vector3(17, 1.5f, i * 5), new Vector3(1.2f, 3, 1.2f), stone);
            }
            campMarker = Marker("Camp", new Color(0.15f, 0.65f, 0.95f), 4);
            cacheMarker = Marker("Equipment cache", new Color(0.9f, 0.7f, 0.2f), 2);
            Part(cacheMarker, "Cache", new Vector3(0, 0.3f, 0), new Vector3(0.8f, 0.6f, 0.6f),
                Material(new Color(0.6f, 0.4f, 0.13f)));
            holdMarker = Marker("Hold position", new Color(0.3f, 0.8f, 0.4f), 3);
            var sun = new GameObject("Blight lab sun").AddComponent<Light>(); sun.transform.SetParent(transform);
            sun.type = LightType.Directional; sun.intensity = 1.4f; sun.transform.rotation = Quaternion.Euler(45, -35, 0);
            view = new GameObject("Blight combat camera").AddComponent<Camera>();
            view.transform.SetParent(transform); view.tag = "MainCamera"; view.fieldOfView = 60;
            view.nearClipPlane = 0.1f; view.backgroundColor = new Color(0.14f, 0.17f, 0.22f);
            view.clearFlags = CameraClearFlags.SolidColor;
            if (FindFirstObjectByType<AudioListener>() == null) view.gameObject.AddComponent<AudioListener>();
        }

        private void Pose(Actor actor)
        {
            if (actor.hulkVisual != null)
            {
                actor.root.localScale = Vector3.one * actor.size;
                actor.hulkVisual.Pose(actor.fighter, actor.moving, simulationTime); return;
            }
            if (actor.thrall != null)
            { actor.thrall.Pose(actor.fighter, actor.moving, simulationTime); return; }
            if (actor.hero != null)
            { actor.hero.Pose(actor.fighter, actor.moving, simulationTime); return; }
            if (actor.rig != null) { PoseLimbRig(actor); return; }
            DuelFighter f = actor.fighter;
            bool winding = f.Action == DuelAction.Windup, recovering = f.Action == DuelAction.Recovery;
            float wind = winding ? Mathf.SmoothStep(0, 1, f.Progress) : 0;
            float swing = recovering ? Mathf.Clamp01(f.Progress / 0.25f) : 0;
            float angle = winding ? Mathf.Lerp(-20, f.Heavy ? -170 : -110, wind) :
                recovering ? Mathf.Lerp(f.Heavy ? -170 : -110, 25, swing) : f.Blocking ? -90 : 0;
            float side = 0;
            if (actor.hulk && f.Strike.Kind == BlightAttack.Sweep)
            {
                angle = winding ? -65 : recovering ? -65 + 65 * f.Progress : 0;
                side = winding ? Mathf.Lerp(0, -100, wind) : recovering ? Mathf.Lerp(-100, 100, swing) : 0;
            }
            actor.arm.localPosition = new Vector3(0.39f, 1.48f,
                !actor.enemy && actor.weapon == BlightWeapon.Spear && recovering ? Mathf.Sin(swing * Mathf.PI) * 0.65f : 0);
            actor.arm.localRotation = Quaternion.Euler(angle, side, 0);
            actor.offArm.localRotation = Quaternion.Euler(actor.hulk && f.Strike.Kind == BlightAttack.Smash ? angle :
                f.Blocking ? -90 : 0, 0, 0);
            float stride = actor.moving && f.CanAct ? Mathf.Sin(gait) * 25 : 0;
            actor.leftLeg.localRotation = Quaternion.Euler(stride, 0, 0);
            actor.rightLeg.localRotation = Quaternion.Euler(-stride, 0, 0);
            actor.root.localScale = f.Alive ? Vector3.one * actor.size : new Vector3(actor.size, 0.2f, actor.size);
        }

        private void EnsureStyles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            title = new GUIStyle(label) { fontSize = 21, fontStyle = FontStyle.Bold };
            small = new GUIStyle(label) { fontSize = 13 };
            centered = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 14 };
        }

        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
        }

        private void Meter(Rect rect, float value, float maximum, Color color, string text)
        {
            Fill(rect, new Color(0.04f, 0.05f, 0.07f, 0.9f));
            Fill(new Rect(rect.x + 1, rect.y + 1, (rect.width - 2) * Mathf.Clamp01(value / maximum), rect.height - 2), color);
            GUI.Label(rect, text, centered);
        }

        private void OnGUI()
        {
            if (player == null) return;
            EnsureStyles();
            Matrix4x4 old = GUI.matrix; Color oldColor = GUI.color;
            GUI.matrix = Matrix4x4.Scale(new Vector3(UiScale, UiScale, 1));
            bool oldEnabled = GUI.enabled;
            if (CompanyMapOpen) GUI.enabled = false;
            DrawWorldLabels();
            DrawLootMarkers();
            DrawOutpostMarkers();
            DrawGatewayMarkers();
            DrawCompanyMarkers();
            DrawCombatFeedback();
            GUI.Box(StatusRect, GUIContent.none);
            GUI.Label(new Rect(28, 23, 310, 28), "RECLAMATION | " + (VillageDefense ? "Village" : Scenario.ToString()), title);
            Meter(new Rect(28, 56, 310, 23), player.fighter.Health, player.fighter.MaximumHealth,
                new Color(0.65f, 0.18f, 0.2f), "Health  " + player.fighter.Health.ToString("0"));
            Meter(new Rect(28, 84, 310, 23), player.fighter.Stamina, 100,
                new Color(0.18f, 0.5f, 0.3f), (IsSprinting ? "SPRINT  " : sprintExhausted ? "Release Shift to recover  " : "Stamina  ") + player.fighter.Stamina.ToString("0"));
            if (HasSquad && Scenario != BlightScenario.Company)
            {
                int row = 0;
                foreach (Actor actor in actors)
                    if (actor != player && !actor.enemy)
                    {
                        GUI.Label(new Rect(28, 114 + row * 32, 184, 24), actor.root.name, small);
                        Meter(new Rect(215, 116 + row * 32, 122, 20), actor.fighter.Health, actor.fighter.MaximumHealth,
                            new Color(0.15f, 0.45f, 0.3f), actor.fighter.Alive ? actor.fighter.Health.ToString("0") : "Down");
                        row++;
                    }
                GUI.Label(new Rect(28, Scenario == BlightScenario.Company ? 309 : 181, 310, 22), Scenario == BlightScenario.Company ? "Company | Known contacts: " + KnownHostileCount : "Order: " + (TacticalScenario && Order == SquadOrder.Hold ? (HoldAtAllCosts ? "Stand fast" : "Defend") : Order.ToString()) + "  |  Hostiles: " + LivingEnemies, small);
            }
            GUI.Box(ObjectiveRect, GUIContent.none);
            GUI.Label(new Rect(ObjectiveRect.x + 8, 20, ObjectiveRect.width - 16, 76), ObjectiveText(), label);
            DrawScenarioMenu(); DrawCommands();
            DrawEquipment();
            if ((paused || Ended) && !companyReportOpen && !CompanyMapOpen)
            {
                string text = VillageLost ? "VILLAGE LOST — R to retry" : paused ? "PAUSED — Esc to resume" : !player.fighter.Alive
                    ? "DEFEATED — R to retry" : Scenario == BlightScenario.Outpost ? "OUTPOST CLEARED — R to replay" : "ENCOUNTER CLEAR — R to retry";
                GUI.Box(new Rect(UiWidth / 2 - 210, UiHeight / 2 - 24, 420, 48), text, title);
            }
            else if (simulationTime < feedbackUntil)
                GUI.Box(new Rect(365, 112, Mathf.Max(250, UiWidth - 690), 62), feedback, small);
            DrawCompanyPanels();
            GUI.enabled = oldEnabled;
            DrawCommandMap();
            GUI.color = oldColor; GUI.matrix = old;
        }

        private string ObjectiveText()
        {
            if (VillageDefense) return VillageObjective();
            if (Scenario == BlightScenario.Company) return "Command prototype | 3 small platoons\nScout, coordinate, then return to camp\nF at camp: operation report";
            if (TacticalScenario) return "Hostiles: " + LivingEnemies + " | " + (Scenario == BlightScenario.Gateway ? "Ruined gateway" : "Open ground") +
                "\n2: Defend | 4: Fallback | 5: Stand fast\n" + (HoldAtAllCosts ? "HOLD AT ALL COSTS" : "Adaptive defense");
            if (Scenario == BlightScenario.Outpost) return OutpostObjective();
            if (Scenario == BlightScenario.Weapons)
                return (LivingEnemies == 0 ? "CLEAR — F loot / N next" : "Defeat the thrall; collect its gear") +
                    "\nRecovered: " + OwnedLootCount + "/3 | B compare";
            if (Scenario == BlightScenario.LimbDamage)
                return "Aim: " + LimbAim + " [Z]\n" + (LimbPractice ? "Stationary practice" : "Live enemy") + " [T]\n" +
                    (LowGore ? "Reduced gore" : "Visible debris") + " [G]";
            if (Scenario != BlightScenario.Patrol)
                return "Hostiles: " + LivingEnemies + "\n" + PlayerWeapon + " | Lock " + (locked ? "ON" : "OFF") + " (Tab)";
            switch (patrol.Stage)
            {
                case PatrolStage.Outbound: return "North: clear the road\nHostiles remaining: " + LivingEnemies;
                case PatrolStage.CacheAvailable: return "Cache secured — approach + F\n" + Vector3.Distance(player.root.position, cache).ToString("0") + " m to cache";
                case PatrolStage.Returning: return "Spear recovered — return south\n" + Vector3.Distance(player.root.position, camp).ToString("0") + " m to camp";
                default: return "PATROL COMPLETE\nSpear recovered | Survivors: " + (LivingCompanions + 1);
            }
        }

        private void DrawScenarioMenu()
        {
            GUI.Box(ScenarioRect, GUIContent.none);
            float x = ScenarioRect.x + 8;
            if (GUI.Button(new Rect(x, 23, 278, 27), scenarioMenu ? "Close scenarios" : "Choose scenario", button))
            { scenarioMenu = !scenarioMenu; if (scenarioMenu) equipmentMenu = false; }
            GUI.Label(new Rect(x, 52, 278, 23), "Active: " + Scenario + " | Each choice resets", small);
            if (!scenarioMenu) return;
            for (int i = 0; i < 11; i++)
            {
                BlightScenario scenario = (BlightScenario)i;
                string name = scenario == BlightScenario.Company ? "Company — orders and intelligence" :
                    scenario == BlightScenario.Gateway ? "Gateway — same twelve, terrain" :
                    scenario == BlightScenario.Skirmish ? "Skirmish — squad vs six" :
                    scenario == BlightScenario.Horde ? "Horde — squad vs twelve" :
                    scenario == BlightScenario.Outpost ? "Outpost — connected mission" :
                    scenario == BlightScenario.Squad ? "Squad orders — three thralls" :
                    scenario == BlightScenario.Hulk ? "Hulk — squad vs sweep / smash" :
                    scenario == BlightScenario.Weapons ? "Weapons — sword / spear / axe" :
                    scenario == BlightScenario.Patrol ? "Patrol — cache and recovery" :
                    scenario == BlightScenario.LimbDamage ? "Limb damage — articulated models" : "Duel — one thrall";
                if (GUI.Button(new Rect(x, 82 + i * 35, 278, 30), name, button)) SelectScenario(scenario);
            }
        }

        private void DrawCommands()
        {
            if (Scenario == BlightScenario.Company) { DrawCompanyCommands(); return; }
            GUI.Box(CommandRect, GUIContent.none);
            float y = CommandRect.y + 8;
            if (HasSquad)
            {
                for (int i = 0; i < 4; i++)
                    if (GUI.Button(new Rect(24 + i * 118, y, 112, 27),
                        (Order == (SquadOrder)i ? "> " : "") + (i + 1) + " " + (TacticalScenario && i == 3 ? "Fallback" : TacticalScenario && i == 1 ? "Defend" : ((SquadOrder)i).ToString()), button))
                        GiveOrder((SquadOrder)i);
            }
            else GUI.Label(new Rect(24, y, 450, 27), Scenario == BlightScenario.LimbDamage ?
                "Z aim height | T stationary practice | G reduced gore" :
                "Solo test — try blocking, dodging, and heavy interruptions.", small);
            if (GUI.Button(new Rect(502, y, 142, 27), PlayerWeapon + " [X]", button))
                CycleWeapon();
            if (GUI.Button(new Rect(652, y, 132, 27), help ? "Hide help [H]" : "Help [H]", button)) help = !help;
            AttackSpec light = PlayerAttack(false);
            if (TacticalScenario && GUI.Button(new Rect(24, y + 33, 220, 25), HoldAtAllCosts ? "5: Hold at all costs [ON]" : "5: Hold at all costs [OFF]", button))
                SetHoldAtAllCosts(!HoldAtAllCosts);
            GUI.Label(new Rect(TacticalScenario ? 252 : 24, y + 33, TacticalScenario ? 532 : 755, 25),
                Scenario == BlightScenario.LimbDamage ? PlayerWeapon + " contact test | B equipment | Arm loss: no heavy | Leg wound: limp / crawl" :
                "Light: " + light.Reach.ToString("0.0") + " m | " + light.Windup.ToString("0.00") +
                " s windup | " + light.Cost + " stamina  •  Swap only with 5 m of space", small);
            if (help)
                GUI.Label(new Rect(24, y + 61, 755, 78),
                    "WASD move | LMB light | E heavy | Ctrl block | Space dodge\n" +
                    "RMB camera | Tab lock | Q target | Shift sprint | F interact | B gear\n" +
                    "1–4 squad orders | X weapon | Esc pause | R reset | M sound " + (CombatAudioEnabled ? "ON" : "OFF") + " | I camera motion " + (CameraMotionEnabled ? "ON" : "OFF"), label);
        }

        private void DrawWorldLabels()
        {
            foreach (Actor actor in actors)
            {
                if (!actor.fighter.Alive) continue;
                Vector3 screen = view.WorldToScreenPoint(actor.root.position + Vector3.up * (actor.size * 2.2f));
                if (screen.z <= view.nearClipPlane || screen.x < 0 || screen.x > Screen.width ||
                    screen.y < 0 || screen.y > Screen.height) continue;
                float x = screen.x / UiScale, y = (Screen.height - screen.y) / UiScale;
                bool target = actor == selectedEnemy;
                Rect box = new Rect(x - 125, y - 25, 250, 24);
                GUI.Box(box, Scenario == BlightScenario.Company && !actor.enemy && actor != player ?
                    "P" + (actor.platoon + 1) + " " + actor.root.name.Split(' ')[0] :
                    (target ? "[TARGET] " : "") + actor.root.name + (!actor.enemy && actor != player ? " | " + actor.intent : ""), centered);
                Meter(new Rect(x - 65, y + 1, 130, 8), actor.fighter.Health, actor.fighter.MaximumHealth,
                    actor.enemy ? new Color(0.7f, 0.25f, 0.25f) : new Color(0.25f, 0.7f, 0.4f), "");
                if (actor.enemy && actor.fighter.Blocking && actor.fighter.CanAct)
                    GUI.Box(new Rect(x - 105, y + 12, 210, 24), "BRACED — flank or wait", centered);
                else if (actor.enemy && actor.fighter.Action == DuelAction.Dodge)
                    GUI.Box(new Rect(x - 80, y + 12, 160, 24), "EVADING", centered);
                else if (actor.enemy && actor.fighter.Action == DuelAction.Windup)
                {
                    string tell = actor.fighter.Strike.Kind == BlightAttack.Sweep ? "SWEEP — step back / dodge" :
                        actor.fighter.Strike.Kind == BlightAttack.Smash ? "SMASH — sidestep / dodge" :
                        actor.fighter.Heavy ? "HEAVY — dodge / interrupt" : "STRIKE — block / dodge";
                    Meter(new Rect(x - 128, y + 12, 256, 24), actor.fighter.Progress, 1,
                        new Color(0.65f, 0.35f, 0.12f), tell);
                }
                else if (actor.enemy && actor.fighter.Action == DuelAction.Recovery)
                    GUI.Box(new Rect(x - 95, y + 12, 190, 24), "RECOVERY — opening", centered);
                if (actor.limbs != null)
                    GUI.Box(new Rect(x - 128, y + 39, 256, 22), actor.limbs.Crawling ? "CRAWLING — short-range attack" :
                        actor.limbs.BiteOnly ? "BITE ONLY" : actor.limbs.Limping ? "LIMPING" :
                        !actor.limbs.CanHeavy ? "ARM LOST — heavy disabled" : "LIMBS INTACT", centered);
            }
            if (Scenario == BlightScenario.Patrol)
            {
                WorldMarker(camp + Vector3.up * 0.7f, "CAMP — recovery");
                if (!patrol.SpearRecovered) WorldMarker(cache + Vector3.up, LivingEnemies == 0 ? "CACHE — F to recover spear" : "CACHE — guarded");
            }
        }

        private void WorldMarker(Vector3 position, string text)
        {
            Vector3 screen = view.WorldToScreenPoint(position);
            if (screen.z > view.nearClipPlane)
                GUI.Box(new Rect(screen.x / UiScale - 125, (Screen.height - screen.y) / UiScale, 250, 25), text, centered);
        }

        private void OnDestroy()
        {
            ClearLimbDebris();
            DestroyCombatAudio();
            foreach (Material material in materials.Values) if (material != null) Destroy(material);
        }
    }
}
