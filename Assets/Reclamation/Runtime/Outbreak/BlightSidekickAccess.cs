using UnityEngine;
namespace Reclamation.Blight
{
    public sealed partial class BlightCombatLab
    {
        public bool SidekickReviewActive { get; set; }
        public void SidekickRehearse(DuelFighter fighter, BlightWeapon weapon, bool moving, float clock)
        {
            if (player == null || player.hero == null) return;
            if (player.hero.Weapon != weapon) player.hero.Equip(weapon);
            player.hero.Pose(fighter, moving, clock);
        }
        public Transform SidekickPlayerRoot => player == null ? null : player.root;
        public Camera SidekickCamera => view;
        public BlightHeroVisual SidekickAnimationSource => player == null ? null : player.hero;
    }
}
