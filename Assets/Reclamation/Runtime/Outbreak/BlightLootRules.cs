using System.Collections.Generic;
using UnityEngine;

namespace Reclamation.Blight
{
    public enum BlightLoot { DuelistSword, WardensSpear, ExecutionersAxe }

    public static class BlightLootCatalog
    {
        public static bool Valid(BlightLoot item) => (int)item >= 0 && (int)item < 3;
        public static BlightWeapon Weapon(BlightLoot item) => item == BlightLoot.WardensSpear ? BlightWeapon.Spear :
            item == BlightLoot.ExecutionersAxe ? BlightWeapon.Axe : BlightWeapon.Sword;
        public static string Name(BlightLoot item) => item == BlightLoot.DuelistSword ? "Duelist's Sword" :
            item == BlightLoot.WardensSpear ? "Warden's Spear" : "Executioner's Axe";
        public static string Tradeoff(BlightLoot item) => item == BlightLoot.DuelistSword ? "Faster, cheaper strikes; lower damage" :
            item == BlightLoot.WardensSpear ? "More damage; slower, higher stamina cost" : "Heavy stagger 1.2s; 25% longer recovery";

        public static AttackSpec Attack(BlightLoot item, bool heavy)
        {
            AttackSpec spec = BlightEquipment.Weapon(Weapon(item), heavy);
            if (item == BlightLoot.DuelistSword)
            {
                spec.Damage = Mathf.Round(spec.Damage * 0.85f); spec.Cost = Mathf.Round(spec.Cost * 0.85f);
                spec.Windup *= 0.8f; spec.Recovery *= 0.8f;
            }
            else if (item == BlightLoot.WardensSpear)
            {
                spec.Damage = Mathf.Round(spec.Damage * 1.2f); spec.Cost = Mathf.Round(spec.Cost * 1.2f);
                spec.Windup *= 1.15f; spec.Recovery *= 1.15f;
            }
            else { spec.Recovery *= 1.25f; if (heavy) spec.Stagger = 1.2f; }
            return spec;
        }
    }

    public sealed class BlightInventory
    {
        private readonly HashSet<BlightLoot> items = new HashSet<BlightLoot>();
        public int Count => items.Count;
        public bool Owns(BlightLoot item) => items.Contains(item);
        public bool Collect(BlightLoot item) => BlightLootCatalog.Valid(item) && items.Add(item);
    }
}
