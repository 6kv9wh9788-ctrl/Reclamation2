using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        private sealed class LootDrop
        {
            public Transform marker;
            public BlightLoot item;
        }
        private readonly List<LootDrop> lootDrops = new List<LootDrop>();
        private BlightInventory inventory = new BlightInventory();
        private BlightLoot? equippedLoot;
        private int lootRound;
        private bool lootPage;
        public int GroundLootCount => lootDrops.Count;
        public int OwnedLootCount => inventory.Count;
        public BlightLoot? EquippedLoot => equippedLoot;
        public bool OwnsLoot(BlightLoot item) => inventory.Owns(item);
        public AttackSpec PlayerAttack(bool heavy) => equippedLoot.HasValue ? BlightLootCatalog.Attack(equippedLoot.Value, heavy) :
            BlightEquipment.Weapon(PlayerWeapon, heavy);

        private void UpdateLootDrops()
        {
            if (Scenario != BlightScenario.Weapons) return;
            foreach (Actor actor in actors)
            {
                if (!actor.enemy || actor.fighter.Alive || actor.lootDropped) continue;
                actor.lootDropped = true;
                var item = (BlightLoot)(lootRound % 3);
                Transform marker = Marker("Loot: " + BlightLootCatalog.Name(item), new Color(0.95f, 0.74f, 0.25f), 0.65f);
                marker.position = actor.root.position;
                Part(marker, "Recovered weapon bundle", Vector3.up * 0.3f, new Vector3(0.5f, 0.35f, 0.3f),
                    Material(new Color(0.65f, 0.48f, 0.17f)));
                lootDrops.Add(new LootDrop { marker = marker, item = item });
                Say("Loot dropped. Approach the gold marker and press F. B compares recovered gear.");
            }
        }

        private bool CollectNearbyLoot()
        {
            if (Scenario != BlightScenario.Weapons || paused || player == null || !player.fighter.CanAct) return false;
            LootDrop closest = null; float distance = 2.6f;
            foreach (LootDrop drop in lootDrops)
            {
                float next = Vector3.Distance(player.root.position, drop.marker.position);
                if (next <= distance) { closest = drop; distance = next; }
            }
            if (closest == null) { Say("Approach a gold loot marker and press F."); return false; }
            bool added = inventory.Collect(closest.item);
            closest.marker.gameObject.SetActive(false); Destroy(closest.marker.gameObject); lootDrops.Remove(closest);
            equipmentMenu = true; lootPage = true; scenarioMenu = false;
            Say(BlightLootCatalog.Name(closest.item) + (added ? " recovered. Compare in B; equipping is your choice." : " already owned; duplicate cleared."));
            return true;
        }

        public bool TryEquipLoot(BlightLoot item)
        {
            if ((Scenario != BlightScenario.Weapons && Scenario != BlightScenario.Outpost) || !BlightLootCatalog.Valid(item) || !inventory.Owns(item)) return false;
            if (!TryEquip(BlightLootCatalog.Weapon(item))) return false;
            equippedLoot = item; Say("Equipped " + BlightLootCatalog.Name(item) + "."); return true;
        }

        public bool StartNextLootEncounter()
        {
            if (Scenario != BlightScenario.Weapons || paused || player == null || !player.fighter.CanAct || LivingEnemies != 0) return false;
            if (lootDrops.Count > 0) { Say("Collect the dropped gear before starting the next encounter."); return false; }
            for (int i = actors.Count - 1; i >= 0; i--)
                if (actors[i].enemy)
                {
                    actors[i].root.gameObject.SetActive(false); Destroy(actors[i].root.gameObject); actors.RemoveAt(i);
                }
            ClearCombatFeedback();
            player.fighter.Recover(100); player.root.position = new Vector3(0, 0, -4); Face(player, Vector3.forward);
            playerMove = Vector3.zero; pendingAttack = pendingDodge = playerBlock = false;
            lootRound++;
            selectedEnemy = CreateActor("Blighted Thrall", new Vector3(0, 0, 3), true);
            foreach (Actor actor in actors) Pose(actor);
            Say("Encounter " + (lootRound + 1) + ": recovered gear retained; health and stamina restored."); return true;
        }

        private void ResetLoot()
        {
            foreach (LootDrop drop in lootDrops)
                if (drop.marker != null) { drop.marker.gameObject.SetActive(false); Destroy(drop.marker.gameObject); }
            lootDrops.Clear(); inventory = new BlightInventory(); equippedLoot = null; lootRound = 0; lootPage = false;
        }

        private void DrawLootMarkers()
        {
            foreach (LootDrop drop in lootDrops)
                WorldMarker(drop.marker.position + Vector3.up, BlightLootCatalog.Name(drop.item) + " — F collect");
        }

        private void DrawLootEquipment(Rect panel)
        {
            GUI.Label(new Rect(panel.x + 10, panel.y + 66, 342, 24),
                "Current: " + (equippedLoot.HasValue ? BlightLootCatalog.Name(equippedLoot.Value) : PlayerWeapon.ToString()), small);
            AttackSpec currentLight = PlayerAttack(false), currentHeavy = PlayerAttack(true);
            for (int i = 0; i < 3; i++)
            {
                var item = (BlightLoot)i; float y = panel.y + 94 + i * 120;
                AttackSpec light = BlightLootCatalog.Attack(item, false), heavy = BlightLootCatalog.Attack(item, true);
                GUI.Label(new Rect(panel.x + 10, y, 250, 22), BlightLootCatalog.Name(item), label);
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && inventory.Owns(item) && !paused && player.fighter.CanAct;
                if (GUI.Button(new Rect(panel.x + 266, y, 86, 24), equippedLoot == item ? "Equipped" : inventory.Owns(item) ? "Equip" : "Not found", button)) TryEquipLoot(item);
                GUI.enabled = enabled;
                GUI.Label(new Rect(panel.x + 10, y + 23, 342, 20), BlightLootCatalog.Tradeoff(item), small);
                GUI.Label(new Rect(panel.x + 10, y + 44, 342, 76),
                    "L/H damage " + Pair(light.Damage, heavy.Damage) + " (now " + Pair(currentLight.Damage, currentHeavy.Damage) + ")" +
                    "\nStamina " + Pair(light.Cost, heavy.Cost) + " (now " + Pair(currentLight.Cost, currentHeavy.Cost) + ")" +
                    "\nAttack time " + Pair(light.Windup + light.Recovery, heavy.Windup + heavy.Recovery) +
                    "s (now " + Pair(currentLight.Windup + currentLight.Recovery, currentHeavy.Windup + currentHeavy.Recovery) + "s)" +
                    "\nReach " + Pair(light.Reach, heavy.Reach) + "m (now " + Pair(currentLight.Reach, currentHeavy.Reach) + "m)", small);
            }
            GUI.Label(new Rect(panel.x + 10, panel.y + 462, 342, 42), Scenario == BlightScenario.Outpost ? "F at cache: depart when ready\nR: fresh mission (clears recovered gear)" : "N: next encounter after collecting loot\nR: fresh run (clears recovered gear)", small);
        }

        private static string Pair(float light, float heavy) => light.ToString("0.##") + "/" + heavy.ToString("0.##");
    }
}
