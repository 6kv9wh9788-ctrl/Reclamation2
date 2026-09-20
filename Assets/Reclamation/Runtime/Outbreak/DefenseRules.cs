namespace Reclamation.Outbreak
{
    public enum DefenseMaterial { Wood, ReinforcedWood, Masonry, Steel }
    public enum ZombieClass { Ordinary, Brute, Siege }

    public static class DefenseRules
    {
        // Damage per simulation second. Zero is true immunity, independent of crowd size.
        public static float Damage(ZombieClass attacker, DefenseMaterial material)
        {
            switch (material)
            {
                case DefenseMaterial.Wood: return attacker == ZombieClass.Ordinary ? 4 : attacker == ZombieClass.Brute ? 12 : 30;
                case DefenseMaterial.ReinforcedWood: return attacker == ZombieClass.Ordinary ? 1 : attacker == ZombieClass.Brute ? 6 : 20;
                case DefenseMaterial.Masonry: return attacker == ZombieClass.Ordinary ? 0 : attacker == ZombieClass.Brute ? 2 : 8;
                case DefenseMaterial.Steel: return attacker == ZombieClass.Siege ? 2 : 0;
                default: return 0;
            }
        }
    }
}
