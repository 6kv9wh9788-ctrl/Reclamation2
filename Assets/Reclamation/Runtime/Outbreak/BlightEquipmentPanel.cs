using UnityEngine;

namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        private bool equipmentMenu;
        private Rect EquipmentRect => new Rect(UiWidth - 378, ScenarioRect.yMax + 8, 362,
            Scenario == BlightScenario.Weapons && lootPage ? 510 : 294);

        private void DrawEquipment()
        {
            if (!equipmentMenu) return;
            Rect panel = EquipmentRect;
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 10, panel.y + 6, 340, 24), "EQUIPMENT [B]  |  Equipped: " + PlayerWeapon, label);
            if (Scenario == BlightScenario.Weapons)
            {
                if (GUI.Button(new Rect(panel.x + 10, panel.y + 34, 164, 25), "Standard weapons", button)) lootPage = false;
                if (GUI.Button(new Rect(panel.x + 184, panel.y + 34, 168, 25), "Recovered gear (" + OwnedLootCount + ")", button)) lootPage = true;
                if (lootPage) { DrawLootEquipment(panel); return; }
            }
            for (int i = 0; i < 3; i++)
            {
                BlightWeapon weapon = (BlightWeapon)i;
                AttackSpec light = BlightEquipment.Weapon(weapon, false), heavy = BlightEquipment.Weapon(weapon, true);
                float y = panel.y + 66 + i * 64;
                bool available = Scenario == BlightScenario.LimbDamage ? weapon != BlightWeapon.Spear :
                    Scenario != BlightScenario.Patrol || weapon == BlightWeapon.Sword ||
                    (weapon == BlightWeapon.Spear && patrol.SpearRecovered);
                GUI.Label(new Rect(panel.x + 10, y, 250, 21), BlightEquipment.Role(weapon), small);
                bool wasEnabled = GUI.enabled;
                GUI.enabled = wasEnabled && available && !paused && player.fighter.CanAct;
                if (GUI.Button(new Rect(panel.x + 266, y, 86, 23),
                    PlayerWeapon == weapon && !EquippedLoot.HasValue ? "Equipped" : available ? "Equip" : "Locked", button)) TryEquip(weapon);
                GUI.enabled = wasEnabled;
                GUI.Label(new Rect(panel.x + 10, y + 22, 342, 39),
                    "L/H  Damage " + light.Damage + "/" + heavy.Damage + "   Stamina " + light.Cost + "/" + heavy.Cost +
                    "\nWindup " + light.Windup.ToString("0.00") + "/" + heavy.Windup.ToString("0.00") +
                    "s   Reach " + light.Reach.ToString("0.0") + "/" + heavy.Reach.ToString("0.0") + "m", small);
            }
            GUI.Label(new Rect(panel.x + 10, panel.y + 262, 342, 28), Scenario == BlightScenario.LimbDamage ?
                "Swap at 5m. Limb hits use visible blade/head contact." : "Swap at 5m. X cycles weapons. Combat stays live.", small);
        }
    }
}
