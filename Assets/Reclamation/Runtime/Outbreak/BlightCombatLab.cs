using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reclamation.Blight
{
    // One scene, resettable scenarios. No dependencies on legacy outbreak/selection code.
    public sealed partial class BlightCombatLab : MonoBehaviour
    {
        private sealed class Actor
        {
            public Transform root, arm, offArm, leftLeg, rightLeg, sword, spear, axe;
            public DuelFighter fighter;
            public bool enemy, hulk, moving, lootDropped;
            public float navigationStall;
            public int slot, attacks, defenseSeed, observedAttack;
            public int platoon = -1;
            public DefenseMemory defense = new DefenseMemory();
            public Actor observedThreat;
            public Vector3 routeGoal, routeWaypoint;
            public float routeUntil;
            public float defenseCooldown, reactionTime;
            public float delay, radius = 0.43f, size = 1;
            public Vector3 spawn, anchor, dodgeDirection;
            public Actor target;
            public Vector3 engagementAxis;
            public string intent = "Follow";
            public BlightWeapon weapon;
            public LimbRig rig;
            public BlightHeroVisual hero;
            public BlightThrallVisual thrall;
            public BlightHulkVisual hulkVisual;
            public BlightLimbs limbs;
            public ImpactFeedback feedback = new ImpactFeedback();
        }

        [Header("Provisional playtest tuning")]
        [SerializeField, Range(0.5f, 2)] private float enemyWindupScale = 1;
        [SerializeField, Range(0.25f, 2)] private float enemyDamageScale = 1;
        [SerializeField, Range(0.5f, 2)] private float playerDamageScale = 1;
        [SerializeField, Range(1, 3)] private int maximumEnemyAttackers = 2;
        [SerializeField] private BlightScenario initialScenario = BlightScenario.Duel;
        private readonly List<Actor> actors = new List<Actor>();
        private readonly List<Actor> impacts = new List<Actor>();
        private readonly List<Actor> victims = new List<Actor>();
        private Actor player, selectedEnemy, assaultTarget;
        private Camera view;
        private Transform campMarker, cacheMarker, holdMarker;
        private BlightPatrol patrol = new BlightPatrol();
        private Vector3 camp, cache;
        private float yaw, pitch = 24, gait;
        private bool paused, locked = true, help = true, scenarioMenu;
        private string feedback = "";
        private float feedbackUntil;
        private float simulationTime;
        private Vector3 playerMove;
        private bool playerBlock;
        private bool pendingAttack, pendingHeavy, pendingDodge;
        public bool InputEnabled { get; set; } = true;
        public bool AutomaticSimulation { get; set; } = true;
        public BlightScenario Scenario { get; private set; }
        public SquadOrder Order { get; private set; }
        public bool Paused => paused;
        public DuelFighter PlayerFighter => player == null ? null : player.fighter;
        public BlightWeapon PlayerWeapon => player == null ? BlightWeapon.Sword : player.weapon;
        public BlightPatrol Patrol => patrol;
        public int LivingEnemies => Count(true);
        public int LivingCompanions => Count(false) - (player != null && player.fighter.Alive ? 1 : 0);
        public Vector3 CampPosition => camp;
        public Vector3 CachePosition => cache;
        public DuelFighter GetFighter(Transform actorRoot)
        {
            foreach (Actor actor in actors) if (actor.root == actorRoot) return actor.fighter;
            return null;
        }
        private bool HasSquad => Scenario == BlightScenario.Squad || Scenario == BlightScenario.Hulk || Scenario == BlightScenario.Patrol || Scenario == BlightScenario.Outpost || Scenario == BlightScenario.Skirmish || Scenario == BlightScenario.Horde || Scenario == BlightScenario.Gateway || Scenario == BlightScenario.Company;
        private bool Ended => player != null && (!player.fighter.Alive || VillageLost ||
            (Scenario == BlightScenario.Outpost ? MissionStage == OutpostStage.Complete :
                Scenario != BlightScenario.Patrol && Scenario != BlightScenario.Weapons && Scenario != BlightScenario.Company && LivingEnemies == 0));
        private float UiScale => Mathf.Max(0.15f, Mathf.Min(Screen.width / 1200f, Screen.height / 800f));
        private float UiWidth => Screen.width / UiScale;
        private float UiHeight => Screen.height / UiScale;

        private void Start()
        {
            BuildArena();
            SelectScenario(initialScenario);
        }

        public void ResetFight() => SelectScenario(Scenario);

        public void SelectScenario(BlightScenario scenario)
        {
            if (view == null || (int)scenario < 0 || (int)scenario > 10) return;
            ClearLimbDebris();
            ClearCombatFeedback();
            ResetLoot();
            ClearOutpost();
            ClearGateway();
            ClearCompany();
            foreach (Actor actor in actors)
            {
                actor.root.gameObject.SetActive(false);
                Destroy(actor.root.gameObject);
            }
            actors.Clear(); impacts.Clear(); victims.Clear();
            Scenario = scenario; scenarioMenu = false; Order = SquadOrder.Follow; patrol = new BlightPatrol();
            equipmentMenu = scenario == BlightScenario.Weapons;
            paused = false; locked = true; selectedEnemy = assaultTarget = null;
            simulationTime = gait = yaw = 0; pitch = 24;
            pendingAttack = pendingDodge = playerBlock = false; playerMove = Vector3.zero;
            ConfigureCompanyGround();
            camp = new Vector3(0, 0, Scenario == BlightScenario.Patrol || Scenario == BlightScenario.Outpost || Scenario == BlightScenario.Company ? -19 : -8) * CompanyScale;
            if (VillageDefense) camp = VillageSite(CompanySite.Fortress);
            cache = new Vector3(0, 0, 13);
            campMarker.position = camp; campMarker.gameObject.SetActive(Scenario == BlightScenario.Patrol || Scenario == BlightScenario.Outpost || Scenario == BlightScenario.Company);
            cacheMarker.position = cache; cacheMarker.gameObject.SetActive(Scenario == BlightScenario.Patrol);
            holdMarker.gameObject.SetActive(false);
            Vector3 origin = Scenario == BlightScenario.Patrol || Scenario == BlightScenario.Outpost || Scenario == BlightScenario.Company ? camp : new Vector3(0, 0, -4);
            player = CreateActor("Company fighter", origin, false);
            if (Scenario != BlightScenario.LimbDamage)
                InstallHumanVisual(player, BlightHumanLook.Hero);
            if (HasSquad)
            {
                Actor left = CreateActor("Mara - swordswoman", origin + new Vector3(-1.8f, 0, -1.8f), false);
                left.slot = 0;
                InstallHumanVisual(left, BlightHumanLook.Mara);
                Actor right = CreateActor("Bren - spearman", origin + new Vector3(1.8f, 0, -1.8f), false);
                right.slot = 1; Equip(right, BlightWeapon.Spear);
                InstallHumanVisual(right, BlightHumanLook.Bren);
            }
            if (Scenario == BlightScenario.Company) BuildCompany();
            else if (Scenario == BlightScenario.Outpost) BuildOutpost();
            else if (Scenario == BlightScenario.Skirmish || Scenario == BlightScenario.Horde || Scenario == BlightScenario.Gateway) BuildLargerEncounter();
            else if (Scenario == BlightScenario.Hulk)
                CreateActor("Blightbound Hulk", new Vector3(0, 0, 4), true, true);
            else if (Scenario == BlightScenario.Squad)
            {
                CreateActor("Thrall 1", new Vector3(-3, 0, 4), true);
                CreateActor("Thrall 2", new Vector3(3, 0, 5), true);
                CreateActor("Thrall 3", new Vector3(0, 0, 7), true);
            }
            else if (Scenario == BlightScenario.Patrol)
            {
                CreateActor("Road thrall 1", new Vector3(-3, 0, 3), true);
                CreateActor("Road thrall 2", new Vector3(3, 0, 5), true);
                CreateActor("Cache guardian", new Vector3(0, 0, 11), true, true);
            }
            else CreateActor("Blighted Thrall", new Vector3(0, 0, 3), true);
            if (TacticalScenario) BuildGateway();
            selectedEnemy = NearestEnemy(player.root.position, float.MaxValue);
            foreach (Actor actor in actors) Pose(actor);
            ResetCombatCamera();
            Say(Scenario == BlightScenario.Outpost ? "Clear the Blighted Outpost: choose a marked approach and press F." : Scenario == BlightScenario.Patrol ? "Patrol: travel north, clear the road, recover the cache, return to camp."
                : "Ready. Every scenario is a fresh fight; R resets this scenario.");
        }

        public void SetPaused(bool value)
        {
            paused = value;
            sprintHeld = cameraManual = false; IsSprinting = false;
            if (impactAudio != null) { if (value) impactAudio.Pause(); else impactAudio.UnPause(); }
            pendingAttack = pendingDodge = playerBlock = false; playerMove = Vector3.zero;
            if (player != null) player.fighter.Blocking = false;
        }

        private void InstallHumanVisual(Actor actor, BlightHumanLook look)
        {
            foreach (Transform child in actor.root) child.gameObject.SetActive(false);
            var visual = new GameObject(look == BlightHumanLook.Hero ? "Refined hero" : "Refined " + look);
            visual.transform.SetParent(actor.root, false);
            actor.hero = visual.AddComponent<BlightHeroVisual>(); actor.hero.Build(look);
            actor.hero.Equip(actor.weapon);
        }

        public void GiveOrder(SquadOrder order)
        {
            if (player == null || !HasSquad || !player.fighter.Alive || (int)order < 0 || (int)order > 3) return;
            if (Scenario == BlightScenario.Company)
            { GiveCompanyOrder(selectedPlatoon, order == SquadOrder.Withdraw ? CompanyOrder.Withdraw : order == SquadOrder.Assault ? CompanyOrder.Assault : order == SquadOrder.Follow ? CompanyOrder.Escort : CompanyOrder.Defend, selectedCompanySite); return; }
            if (TacticalScenario && SetTacticalOrder(order)) return;
            Order = order; assaultTarget = order == SquadOrder.Assault ? selectedEnemy : null;
            foreach (Actor actor in actors)
                if (!actor.enemy && actor != player)
                {
                    actor.anchor = actor.root.position; actor.target = null;
                    // The new order changes the next decision, never cancels a committed attack.
                }
            holdMarker.position = player.root.position;
            holdMarker.gameObject.SetActive(order == SquadOrder.Hold);
            Say(order == SquadOrder.Withdraw ? "Withdraw: survivors return to the camp marker; committed actions finish first."
                : order == SquadOrder.Assault ? "Assault: focus the marked enemy, then nearby threats."
                : order == SquadOrder.Hold ? "Hold: each companion defends their current position."
                : "Follow: companions stay near you and engage nearby threats.");
        }

        public bool TryEquip(BlightWeapon weapon)
        {
            if (player == null || paused || !player.fighter.CanAct || (int)weapon < 0 || (int)weapon > 2) return false;
            if (Scenario == BlightScenario.LimbDamage && weapon == BlightWeapon.Spear)
            { Say("Use sword or axe for limb contact testing; test the spear in Weapons."); return false; }
            if (Scenario == BlightScenario.Patrol && weapon == BlightWeapon.Axe)
            { Say("The axe is available in the combat labs; this patrol retains its spear reward."); return false; }
            if (Scenario == BlightScenario.Patrol && weapon == BlightWeapon.Spear && !patrol.SpearRecovered)
            { Say("Recover the patrol cache to unlock your spear."); return false; }
            if (NearestEnemy(player.root.position, 5) != null)
            { Say("Make five metres of space before changing weapons."); return false; }
            Equip(player, weapon); Say("Equipped " + weapon + "."); return true;
        }

        public bool Interact()
        {
            if (Scenario == BlightScenario.Company) return ReviewCompanyOperation();
            if (Scenario == BlightScenario.Outpost) return InteractOutpost();
            if (Scenario == BlightScenario.Weapons) return CollectNearbyLoot();
            if (Scenario != BlightScenario.Patrol || paused || player == null || !player.fighter.CanAct) return false;
            patrol.ObserveEnemies(LivingEnemies);
            if (patrol.TryRecoverCache(Vector3.Distance(player.root.position, cache), player.fighter.Alive))
            {
                Equip(player, BlightWeapon.Spear); cacheMarker.gameObject.SetActive(false);
                Say("Recovered the company spear and equipped it. Return south to camp (order 4: Withdraw).");
                return true;
            }
            Say(patrol.Stage == PatrolStage.CacheAvailable ? "Approach the gold cache and press F."
                : patrol.Stage == PatrolStage.Outbound ? "Clear all three enemies before recovering the cache."
                : "Return to the blue camp marker; surviving fighters recover there automatically.");
            return false;
        }

        private void Update()
        {
            if (player == null) return;
            ReadInput();
            if (AutomaticSimulation) Simulate(Time.unscaledDeltaTime);
        }

        private void ReadInput()
        {
            playerMove = Vector3.zero; playerBlock = sprintHeld = cameraManual = false;
            if (!InputEnabled || !Application.isFocused) return;
            Keyboard keys = Keyboard.current; Mouse mouse = Mouse.current;
            bool busy = GUIUtility.hotControl != 0;
            if (keys != null && !busy)
            {
                if (keys.escapeKey.wasPressedThisFrame)
                { if (CompanyMapOpen) CloseCompanyMap(false); else if (companyReportOpen) { companyReportOpen = false; SetPaused(false); } else SetPaused(!paused); }
                if (keys.rKey.wasPressedThisFrame) { ResetFight(); return; }
                if (companyReportOpen) return;
                if (Scenario == BlightScenario.Company && keys.gKey.wasPressedThisFrame)
                { if (CompanyMapOpen) CloseCompanyMap(false); else OpenCompanyMap(); return; }
                if (CompanyMapOpen) return;
                if (keys.hKey.wasPressedThisFrame) help = !help;
                if (keys.tabKey.wasPressedThisFrame) locked = !locked;
                if (keys.iKey.wasPressedThisFrame) CameraMotionEnabled = !CameraMotionEnabled;
                if (keys.qKey.wasPressedThisFrame) CycleTarget();
                if (keys.mKey.wasPressedThisFrame) CombatAudioEnabled = !CombatAudioEnabled;
                if (keys.bKey.wasPressedThisFrame) { equipmentMenu = !equipmentMenu; if (equipmentMenu) scenarioMenu = false; }
                if (keys.nKey.wasPressedThisFrame) StartNextLootEncounter();
                if (Scenario == BlightScenario.LimbDamage)
                {
                    if (keys.zKey.wasPressedThisFrame) SetLimbAim((SwingHeight)(((int)LimbAim + 1) % 3));
                    if (keys.tKey.wasPressedThisFrame) LimbPractice = !LimbPractice;
                    if (keys.gKey.wasPressedThisFrame) LowGore = !LowGore;
                }
            }
            Vector2 pixel = mouse == null ? Vector2.zero : mouse.position.ReadValue();
            bool overUi = mouse != null && ContainsGuiPoint(new Vector2(pixel.x, Screen.height - pixel.y) / UiScale);
            bool inside = pixel.x >= 0 && pixel.y >= 0 && pixel.x < Screen.width && pixel.y < Screen.height;
            if (mouse != null && !busy && !overUi && inside && !paused && !Ended && mouse.rightButton.isPressed)
            {
                SetCameraOrbit(mouse.delta.ReadValue(), true);
            }
            if (paused || Ended || busy) return;
            if (keys != null)
            {
                if (Scenario == BlightScenario.Company)
                {
                    if (keys.digit1Key.wasPressedThisFrame) selectedPlatoon = 0;
                    if (keys.digit2Key.wasPressedThisFrame) selectedPlatoon = 1;
                    if (keys.digit3Key.wasPressedThisFrame) selectedPlatoon = 2;
                    if (keys.digit4Key.wasPressedThisFrame) RecallCompany();
                }
                else
                {
                if (keys.digit1Key.wasPressedThisFrame) GiveOrder(SquadOrder.Follow);
                if (keys.digit2Key.wasPressedThisFrame) GiveOrder(SquadOrder.Hold);
                if (keys.digit3Key.wasPressedThisFrame) GiveOrder(SquadOrder.Assault);
                if (keys.digit4Key.wasPressedThisFrame) GiveOrder(SquadOrder.Withdraw);
                if (keys.digit5Key.wasPressedThisFrame) SetHoldAtAllCosts(!HoldAtAllCosts);
                }
                if (keys.xKey.wasPressedThisFrame) CycleWeapon();
                if (keys.fKey.wasPressedThisFrame) Interact();
                Vector3 raw = new Vector3((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
                    0, (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
                playerMove = Quaternion.Euler(0, yaw, 0) * raw.normalized;
                sprintHeld = keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed;
                playerBlock = keys.leftCtrlKey.isPressed || keys.rightCtrlKey.isPressed;
                pendingHeavy = keys.eKey.wasPressedThisFrame;
                pendingAttack |= pendingHeavy;
                pendingDodge |= keys.spaceKey.wasPressedThisFrame;
            }
            if (mouse != null && !overUi && inside)
            {
                if (mouse.leftButton.wasPressedThisFrame && !pendingHeavy) { pendingAttack = true; pendingHeavy = false; }
            }
        }

        // Bounded substeps make the same public simulation path usable by regression tests.
        public void Simulate(float seconds)
        {
            if (player == null || paused || Ended || !(seconds > 0) || float.IsInfinity(seconds)) return;
            float remaining = Mathf.Min(seconds, 0.25f);
            while (remaining > 0.00001f && !Ended)
            {
                float dt = Mathf.Min(remaining, 1f / 60f); remaining -= dt;
                Tick(dt);
            }
        }

        private void Tick(float dt)
        {
            Vector3 previousPlayerPosition = player.root.position;
            CaptureLimbPose();
            AdvanceLimbDebris(dt);
            simulationTime += dt; gait += dt * 9;
            if (selectedEnemy == null || !selectedEnemy.fighter.Alive)
                selectedEnemy = NearestEnemy(player.root.position, float.MaxValue);
            foreach (Actor actor in actors) { actor.moving = false; actor.delay -= dt; actor.defenseCooldown -= dt; }
            if (Scenario == BlightScenario.Company) UpdateCompany(dt);
            ControlPlayer(dt);
            foreach (Actor actor in actors)
                if (actor != player && actor.fighter.Alive)
                { if (actor.enemy) ControlEnemy(actor, dt); else ControlCompanion(actor, dt); }
            foreach (Actor actor in actors)
                if (actor.fighter.Action == DuelAction.Dodge)
                    Move(actor, actor.dodgeDirection * 8 * dt);
            impacts.Clear();
            foreach (Actor actor in actors) if (actor.fighter.Advance(dt)) impacts.Add(actor);
            foreach (Actor actor in impacts)
                if (actor.fighter.Alive && actor.fighter.Action == DuelAction.Recovery &&
                    !(actor == player && actor.rig != null)) ResolveStrike(actor);
            if (Scenario == BlightScenario.Patrol)
            {
                patrol.ObserveEnemies(LivingEnemies);
                foreach (Actor actor in actors)
                    if (!actor.enemy && BlightPatrol.CanRecover(Vector3.Distance(actor.root.position, camp), CampThreatened()))
                        actor.fighter.Recover(dt);
                if (patrol.TryComplete(Vector3.Distance(player.root.position, camp), CampThreatened(), player.fighter.Alive))
                    Say("PATROL COMPLETE — spear recovered. Survivors recover inside camp. R starts a fresh patrol.");
            }
            if (Scenario == BlightScenario.Outpost) UpdateOutpost(dt);
            UpdateFallback();
            foreach (Actor actor in actors) Pose(actor);
            ResolveLimbSweep();
            UpdateLootDrops();
            UpdateCombatFeedback();
            UpdateCombatCamera(dt, previousPlayerPosition);
        }

        private void ControlPlayer(float dt)
        {
            IsSprinting = false;
            if (!sprintHeld && player.fighter.Stamina >= 20) sprintExhausted = false;
            if (player.fighter.CanAct)
            {
                Face(player, CameraLockActive ? selectedEnemy.root.position - player.root.position : playerMove);
                player.fighter.Blocking = playerBlock;
                if (pendingDodge && player.fighter.Dodge())
                    player.dodgeDirection = playerMove.sqrMagnitude > 0 ? playerMove : -player.root.forward;
                else if (pendingAttack) StartAttack(player, pendingHeavy);
                if (player.fighter.CanAct)
                {
                    bool wantsSprint = sprintHeld && !playerBlock && playerMove.sqrMagnitude > .01f && !sprintExhausted;
                    IsSprinting = wantsSprint && player.fighter.TrySprint(dt);
                    if (wantsSprint && !IsSprinting) sprintExhausted = true;
                    Move(player, playerMove * (IsSprinting ? 6.2f : playerBlock ? 1.8f : 3.8f) * dt);
                }
            }
            pendingAttack = pendingDodge = false;
        }

        private void ControlEnemy(Actor actor, float dt)
        {
            if (!actor.fighter.CanAct) return;
            if (actor.limbs != null && LimbPractice) return;
            if (ControlEnemyDefense(actor, dt)) return;
            Actor target = null; float best = Scenario == BlightScenario.Company ? 7 : Scenario == BlightScenario.Outpost ? 9 : Scenario == BlightScenario.Patrol ? 11 : 100;
            foreach (Actor other in actors)
            {
                if (other.enemy || !other.fighter.Alive) continue;
                // A patrol enemy guards its sector; it cannot chase into the camp indefinitely.
                if (!VillageDefense && (Scenario == BlightScenario.Patrol || Scenario == BlightScenario.Outpost || Scenario == BlightScenario.Company) && Vector3.Distance(other.root.position, actor.spawn) > 15 * CompanyScale) continue;
                float distance = Vector3.Distance(other.root.position, actor.root.position);
                if (distance < best && (Scenario != BlightScenario.Company || TerrainSight(actor.root.position, other.root.position))) { best = distance; target = other; }
            }
            actor.target = target;
            if (target == null) { if (!ControlVillageEnemyIdle(actor, dt) && !ControlCompanyFieldIdle(actor, dt)) MoveToward(actor, actor.spawn, 2, dt, 0.2f); return; }
            Face(actor, target.root.position - actor.root.position);
            AttackSpec spec = BlightEquipment.Enemy(actor.hulk ? BlightEnemy.Hulk : BlightEnemy.Thrall, actor.attacks);
            if (actor.limbs != null) spec = actor.limbs.Attack(actor.attacks);
            if (RepositionWaitingEnemy(actor, target, spec, dt)) return;
            float stop = actor.limbs != null && (actor.limbs.Crawling || actor.limbs.BiteOnly) ? 1 : spec.Reach - 0.55f;
            if (best > stop || !TerrainSight(actor.root.position, target.root.position))
                MoveToward(actor, target.root.position, (actor.hulk ? 1.8f : 2.2f) *
                    (actor.limbs == null ? 1 : actor.limbs.SpeedMultiplier), dt, stop);
            else if (actor.delay <= 0 && EnemyAttackSlots() < ActiveEnemyAttackLimit)
            {
                spec.Windup *= enemyWindupScale;
                if (actor.fighter.Attack(spec)) { actor.attacks++; actor.delay = spec.Windup + spec.Recovery + 0.45f; }
            }
        }

        private int EnemyAttackSlots()
        {
            int count = 0;
            foreach (Actor actor in actors)
                if (actor.enemy && (actor.fighter.Action == DuelAction.Windup || actor.fighter.Action == DuelAction.Recovery)) count++;
            return count;
        }

        private bool StartAttack(Actor actor, bool heavy)
        {
            if (!actor.fighter.Attack(actor == player ? PlayerAttack(heavy) : BlightEquipment.Weapon(actor.weapon, heavy))) return false;
            if (actor.rig != null) { actor.rig.height = LimbAim; actor.rig.hit = false; }
            return true;
        }

        public bool RequestPlayerAttack(bool heavy)
            => player != null && !paused && !Ended && StartAttack(player, heavy);

        private void ResolveStrike(Actor source)
        {
            AttackSpec spec = source.fighter.Strike; victims.Clear();
            Actor closest = null; float best = float.MaxValue;
            foreach (Actor target in actors)
            {
                if (source.enemy == target.enemy || !target.fighter.Alive ||
                    !DuelFighter.InReach(source.root.position, source.root.forward, target.root.position, spec.Reach, spec.HalfAngle) ||
                    !TerrainSight(source.root.position, target.root.position)) continue;
                if (spec.MultipleTargets) victims.Add(target);
                else
                {
                    float distance = Vector3.SqrMagnitude(target.root.position - source.root.position);
                    if (distance < best) { best = distance; closest = target; }
                }
            }
            if (!spec.MultipleTargets && closest != null) victims.Add(closest);
            if (victims.Count == 0 && source == player) Say("Missed — check weapon reach and facing.");
            foreach (Actor target in victims)
            {
                bool frontal = Vector3.Angle(target.root.forward, source.root.position - target.root.position) < 70;
                DuelAction previousAction = target.fighter.Action;
                string result = target.fighter.ReceiveAttack(spec, frontal, source.enemy ? enemyDamageScale :
                    source == player ? playerDamageScale : 1);
                ShowCombatImpact(target, result, previousAction);
                if (target == player || source == player || !target.fighter.Alive) Say(target.root.name + ": " + result);
            }
        }

        private void MoveToward(Actor actor, Vector3 goal, float speed, float dt, float stop)
        {
            goal = RouteGoal(actor, goal, ref stop);
            Vector3 delta = goal - actor.root.position; delta.y = 0;
            if (delta.magnitude <= stop) return;
            if (!actor.fighter.Blocking) Face(actor, delta);
            Move(actor, delta.normalized * Mathf.Min(speed * dt, delta.magnitude - stop));
        }

        private void Move(Actor actor, Vector3 offset)
        {
            if (offset.sqrMagnitude < 0.000001f) return;
            Vector3 next = actor.root.position + offset;
            // Separate bodies, then sweep against the static terrain to prevent wall tunnelling.
            for (int pass = 0; pass < 2; pass++)
                foreach (Actor other in actors)
                {
                    if (other == actor || !other.fighter.Alive) continue;
                    Vector3 delta = next - other.root.position; delta.y = 0;
                    float radius = actor.radius + other.radius;
                    if (delta.sqrMagnitude < radius * radius)
                        next = other.root.position + (delta.sqrMagnitude > 0.0001f ? delta.normalized : -actor.root.forward) * radius;
                }
            next = ClampCompanionGoal(next);
            actor.root.position = terrain.Move(actor.root.position, next, actor.radius);
            actor.moving = true;
        }

        private static void Face(Actor actor, Vector3 direction)
        { direction.y = 0; if (direction.sqrMagnitude > 0.001f) actor.root.rotation = Quaternion.LookRotation(direction); }

        private Actor NearestEnemy(Vector3 position, float range)
        {
            Actor best = null;
            foreach (Actor actor in actors)
            {
                if (!actor.enemy || !actor.fighter.Alive) continue;
                float distance = Vector3.Distance(position, actor.root.position);
                if (distance < range) { range = distance; best = actor; }
            }
            return best;
        }

        private void CycleTarget()
        {
            int start = selectedEnemy == null ? -1 : actors.IndexOf(selectedEnemy);
            for (int n = 1; n <= actors.Count; n++)
            {
                Actor actor = actors[(start + n) % actors.Count];
                if (actor.enemy && actor.fighter.Alive) { selectedEnemy = actor; locked = true; return; }
            }
        }

        private int Count(bool enemy)
        { int count = 0; foreach (Actor a in actors) if (a.enemy == enemy && a.fighter.Alive) count++; return count; }
        private bool CampThreatened() => NearestEnemy(camp, 10) != null;
        private void Say(string message) { feedback = message; feedbackUntil = simulationTime + 5; }
        private void Equip(Actor actor, BlightWeapon weapon)
        {
            if (actor == player) equippedLoot = null;
            actor.weapon = weapon;
            if (actor.hero != null)
            { actor.hero.Equip(weapon); Pose(actor); return; }
            if (actor.sword != null) actor.sword.gameObject.SetActive(weapon == BlightWeapon.Sword);
            if (actor.spear != null) actor.spear.gameObject.SetActive(weapon == BlightWeapon.Spear);
            if (actor.axe != null) actor.axe.gameObject.SetActive(weapon == BlightWeapon.Axe);
            if (actor.rig != null) EquipLimbWeapon(actor);
        }

        private void CycleWeapon()
        {
            BlightWeapon next = Scenario == BlightScenario.LimbDamage ?
                (PlayerWeapon == BlightWeapon.Sword ? BlightWeapon.Axe : BlightWeapon.Sword) :
                Scenario == BlightScenario.Patrol ? (PlayerWeapon == BlightWeapon.Sword ? BlightWeapon.Spear : BlightWeapon.Sword) :
                (BlightWeapon)(((int)PlayerWeapon + 1) % 3);
            TryEquip(next);
        }

        private void LateUpdate() => RenderCombatCamera();

        private void OnApplicationFocus(bool focused)
        { if (!focused && player != null) SetPaused(true); }
    }
}
