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
            public int slot, attacks;
            public float delay, radius = 0.43f, size = 1;
            public Vector3 spawn, anchor, dodgeDirection;
            public Actor target;
            public BlightWeapon weapon;
            public LimbRig rig;
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
        private bool HasSquad => Scenario == BlightScenario.Squad || Scenario == BlightScenario.Hulk || Scenario == BlightScenario.Patrol;
        private bool Ended => player != null && (!player.fighter.Alive ||
            (Scenario != BlightScenario.Patrol && Scenario != BlightScenario.Weapons && LivingEnemies == 0));
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
            if (view == null || (int)scenario < 0 || (int)scenario > 5) return;
            ClearLimbDebris();
            ClearCombatFeedback();
            ResetLoot();
            foreach (Actor actor in actors)
            {
                actor.root.gameObject.SetActive(false);
                Destroy(actor.root.gameObject);
            }
            actors.Clear(); impacts.Clear(); victims.Clear();
            Scenario = scenario; Order = SquadOrder.Follow; patrol = new BlightPatrol();
            equipmentMenu = scenario == BlightScenario.Weapons;
            paused = false; locked = true; selectedEnemy = assaultTarget = null;
            simulationTime = gait = yaw = 0; pitch = 24;
            pendingAttack = pendingDodge = playerBlock = false; playerMove = Vector3.zero;
            camp = new Vector3(0, 0, Scenario == BlightScenario.Patrol ? -19 : -8);
            cache = new Vector3(0, 0, 13);
            campMarker.position = camp; campMarker.gameObject.SetActive(Scenario == BlightScenario.Patrol);
            cacheMarker.position = cache; cacheMarker.gameObject.SetActive(Scenario == BlightScenario.Patrol);
            holdMarker.gameObject.SetActive(false);
            Vector3 origin = Scenario == BlightScenario.Patrol ? camp : new Vector3(0, 0, -4);
            player = CreateActor("Company fighter", origin, false);
            if (HasSquad)
            {
                Actor left = CreateActor("Mara - swordswoman", origin + new Vector3(-1.8f, 0, -1.8f), false);
                left.slot = 0;
                Actor right = CreateActor("Bren - spearman", origin + new Vector3(1.8f, 0, -1.8f), false);
                right.slot = 1; Equip(right, BlightWeapon.Spear);
            }
            if (Scenario == BlightScenario.Hulk)
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
            selectedEnemy = NearestEnemy(player.root.position, float.MaxValue);
            foreach (Actor actor in actors) Pose(actor);
            Say(Scenario == BlightScenario.Patrol ? "Patrol: travel north, clear the road, recover the cache, return to camp."
                : "Ready. Every scenario is a fresh fight; R resets this scenario.");
        }

        public void SetPaused(bool value)
        {
            paused = value;
            if (impactAudio != null) { if (value) impactAudio.Pause(); else impactAudio.UnPause(); }
            pendingAttack = pendingDodge = playerBlock = false; playerMove = Vector3.zero;
            if (player != null) player.fighter.Blocking = false;
        }

        public void GiveOrder(SquadOrder order)
        {
            if (player == null || !HasSquad || !player.fighter.Alive) return;
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
            playerMove = Vector3.zero; playerBlock = false;
            if (!InputEnabled || !Application.isFocused) return;
            Keyboard keys = Keyboard.current; Mouse mouse = Mouse.current;
            bool busy = GUIUtility.hotControl != 0;
            if (keys != null && !busy)
            {
                if (keys.escapeKey.wasPressedThisFrame) SetPaused(!paused);
                if (keys.rKey.wasPressedThisFrame) { ResetFight(); return; }
                if (keys.hKey.wasPressedThisFrame) help = !help;
                if (keys.tabKey.wasPressedThisFrame) locked = !locked;
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
            if (mouse != null && !busy && !overUi && inside && mouse.middleButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * 0.18f; pitch = Mathf.Clamp(pitch - delta.y * 0.15f, 12, 60);
            }
            if (paused || Ended || busy) return;
            if (keys != null)
            {
                if (keys.digit1Key.wasPressedThisFrame) GiveOrder(SquadOrder.Follow);
                if (keys.digit2Key.wasPressedThisFrame) GiveOrder(SquadOrder.Hold);
                if (keys.digit3Key.wasPressedThisFrame) GiveOrder(SquadOrder.Assault);
                if (keys.digit4Key.wasPressedThisFrame) GiveOrder(SquadOrder.Withdraw);
                if (keys.xKey.wasPressedThisFrame) CycleWeapon();
                if (keys.fKey.wasPressedThisFrame) Interact();
                Vector3 raw = new Vector3((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
                    0, (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
                playerMove = Quaternion.Euler(0, yaw, 0) * raw.normalized;
                pendingHeavy = keys.eKey.wasPressedThisFrame;
                pendingAttack |= pendingHeavy;
                pendingDodge |= keys.spaceKey.wasPressedThisFrame;
            }
            if (mouse != null && !overUi && inside)
            {
                playerBlock = mouse.rightButton.isPressed;
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
            CaptureLimbPose();
            AdvanceLimbDebris(dt);
            simulationTime += dt; gait += dt * 9;
            if (selectedEnemy == null || !selectedEnemy.fighter.Alive)
                selectedEnemy = NearestEnemy(player.root.position, float.MaxValue);
            foreach (Actor actor in actors) { actor.moving = false; actor.delay -= dt; }
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
            foreach (Actor actor in actors) Pose(actor);
            ResolveLimbSweep();
            UpdateLootDrops();
            UpdateCombatFeedback();
        }

        private void ControlPlayer(float dt)
        {
            if (player.fighter.CanAct)
            {
                Face(player, locked && selectedEnemy != null ? selectedEnemy.root.position - player.root.position : playerMove);
                player.fighter.Blocking = playerBlock;
                if (pendingDodge && player.fighter.Dodge())
                    player.dodgeDirection = playerMove.sqrMagnitude > 0 ? playerMove : -player.root.forward;
                else if (pendingAttack) StartAttack(player, pendingHeavy);
                if (player.fighter.CanAct) Move(player, playerMove * (playerBlock ? 1.8f : 3.8f) * dt);
            }
            pendingAttack = pendingDodge = false;
        }

        private Actor Incoming(Actor actor)
        {
            foreach (Actor other in actors)
                if (other.enemy != actor.enemy && other.fighter.Action == DuelAction.Windup &&
                    DuelFighter.InReach(other.root.position, other.root.forward, actor.root.position,
                        other.fighter.Strike.Reach + 0.3f, other.fighter.Strike.HalfAngle + 8))
                    return other;
            return null;
        }

        private bool Defend(Actor actor)
        {
            Actor threat = Incoming(actor);
            actor.fighter.Blocking = false;
            if (threat == null || threat.fighter.Progress < 0.38f) return false;
            Face(actor, threat.root.position - actor.root.position);
            if (threat.fighter.Heavy)
            {
                // React late enough that the short dodge can cover impact; otherwise keep distance.
                if (threat.fighter.Remaining <= 0.25f && actor.fighter.Dodge())
                {
                    Vector3 away = actor.root.position - threat.root.position;
                    actor.dodgeDirection = threat.fighter.Strike.Kind == BlightAttack.Smash
                        ? Vector3.Cross(Vector3.up, away).normalized : away.normalized;
                }
            }
            else actor.fighter.Blocking = true;
            return true;
        }

        private void ControlCompanion(Actor actor, float dt)
        {
            if (!actor.fighter.CanAct) return;
            bool defending = Defend(actor);
            if (!actor.fighter.CanAct) return;
            Vector3 anchor = Order == SquadOrder.Hold ? actor.anchor : player.root.position;
            if (Order == SquadOrder.Withdraw)
            {
                Vector3 rally = camp + new Vector3(actor.slot == 0 ? -1.8f : 1.8f, 0, 0);
                MoveToward(actor, rally, 3.8f, dt, 0.4f); return;
            }
            if (defending) return;
            Actor target = Order == SquadOrder.Assault && assaultTarget != null && assaultTarget.fighter.Alive &&
                Vector3.Distance(assaultTarget.root.position, anchor) <= 14 ? assaultTarget : NearestEnemy(actor.root.position, 10);
            if (target != null && (!BlightSquadRules.CanEngage(Order, Vector3.Distance(target.root.position, anchor)) ||
                !BlightSquadRules.CanEngage(Order, Vector3.Distance(actor.root.position, anchor)))) target = null;
            actor.target = target;
            if (target == null)
            {
                Vector3 goal = Order == SquadOrder.Hold ? actor.anchor :
                    BlightSquadRules.Formation(player.root.position, player.root.forward, actor.slot);
                MoveToward(actor, goal, 3.5f, dt, 0.5f); return;
            }
            Face(actor, target.root.position - actor.root.position);
            float reach = BlightEquipment.Weapon(actor.weapon, false).Reach;
            float distance = Vector3.Distance(actor.root.position, target.root.position);
            if (distance > reach - 0.35f)
            {
                // Opposite approach offsets reduce both allies piling into the same lane.
                Vector3 side = Vector3.Cross(Vector3.up, (target.root.position - actor.root.position).normalized);
                MoveToward(actor, target.root.position + side * (actor.slot == 0 ? -0.6f : 0.6f), 3, dt, reach - 0.5f);
            }
            else if (actor.delay <= 0)
            {
                bool heavy = actor.attacks % 3 == 2;
                if (StartAttack(actor, heavy)) { actor.attacks++; actor.delay = 1.1f; }
            }
        }

        private void ControlEnemy(Actor actor, float dt)
        {
            if (!actor.fighter.CanAct) return;
            if (actor.limbs != null && LimbPractice) return;
            Actor target = null; float best = Scenario == BlightScenario.Patrol ? 11 : 100;
            foreach (Actor other in actors)
            {
                if (other.enemy || !other.fighter.Alive) continue;
                // A patrol enemy guards its sector; it cannot chase into the camp indefinitely.
                if (Scenario == BlightScenario.Patrol && Vector3.Distance(other.root.position, actor.spawn) > 15) continue;
                float distance = Vector3.Distance(other.root.position, actor.root.position);
                if (distance < best) { best = distance; target = other; }
            }
            actor.target = target;
            if (target == null) { MoveToward(actor, actor.spawn, 2, dt, 0.2f); return; }
            Face(actor, target.root.position - actor.root.position);
            AttackSpec spec = BlightEquipment.Enemy(actor.hulk ? BlightEnemy.Hulk : BlightEnemy.Thrall, actor.attacks);
            if (actor.limbs != null) spec = actor.limbs.Attack(actor.attacks);
            float stop = actor.limbs != null && (actor.limbs.Crawling || actor.limbs.BiteOnly) ? 1 : spec.Reach - 0.55f;
            if (best > stop)
                MoveToward(actor, target.root.position, (actor.hulk ? 1.8f : 2.2f) *
                    (actor.limbs == null ? 1 : actor.limbs.SpeedMultiplier), dt, stop);
            else if (actor.delay <= 0 && EnemyAttackSlots() < maximumEnemyAttackers)
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
                    !DuelFighter.InReach(source.root.position, source.root.forward, target.root.position, spec.Reach, spec.HalfAngle)) continue;
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
            Vector3 delta = goal - actor.root.position; delta.y = 0;
            if (delta.magnitude <= stop) return;
            if (!actor.fighter.Blocking) Face(actor, delta);
            Move(actor, delta.normalized * Mathf.Min(speed * dt, delta.magnitude - stop));
        }

        private void Move(Actor actor, Vector3 offset)
        {
            if (offset.sqrMagnitude < 0.000001f) return;
            Vector3 next = actor.root.position + offset;
            // Two passes resolve crowd overlap in this obstacle-free greybox.
            for (int pass = 0; pass < 2; pass++)
                foreach (Actor other in actors)
                {
                    if (other == actor || !other.fighter.Alive) continue;
                    Vector3 delta = next - other.root.position; delta.y = 0;
                    float radius = actor.radius + other.radius;
                    if (delta.sqrMagnitude < radius * radius)
                        next = other.root.position + (delta.sqrMagnitude > 0.0001f ? delta.normalized : -actor.root.forward) * radius;
                }
            actor.root.position = new Vector3(Mathf.Clamp(next.x, -15, 15), 0, Mathf.Clamp(next.z, -23, 20));
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

        private void LateUpdate()
        {
            if (view == null || player == null) return;
            Vector3 focus = player.root.position + Vector3.up * 1.3f;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            view.transform.SetPositionAndRotation(focus - rotation * Vector3.forward * 7.5f, rotation);
        }

        private void OnApplicationFocus(bool focused)
        { if (!focused && player != null) SetPaused(true); }
    }
}
