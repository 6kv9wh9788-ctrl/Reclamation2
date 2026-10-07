using System;
using System.IO;

namespace Reclamation.Blight
{
    public enum DefenseApproach { HoldVillage, InterceptRoad }
    public enum FounderPerk { None, FieldDressing, ForwardRally }
    public enum CaptainPerk { None, WatchCaptain, CarefulRelief }

    [Serializable]
    public sealed class DefenseCampaign
    {
        public int completed, playerXp, commanderXp, playerPerk, commanderPerk, approach;
        public int lastSurvivors = 8, lastFoodLost;
        public bool lastProtected, lastParticipated;
        public string outcome = "Scouts report raiders assembling on the north road. Choose your defense plan.";
        public void Validate()
        {
            if(completed<0||completed>2||playerXp<0||playerXp>350||commanderXp<0||commanderXp>300||
                playerPerk<0||playerPerk>2||commanderPerk<0||commanderPerk>2||approach<0||approach>1||
                lastSurvivors<0||lastSurvivors>8||lastFoodLost<0||lastFoodLost>5||outcome==null||outcome.Length>512||
                playerXp>completed*175||commanderXp>completed*150||
                playerPerk!=0&&playerXp<100||commanderPerk!=0&&commanderXp<100)
                throw new InvalidDataException("Invalid defense progression.");
        }
        public bool ChooseFounder(FounderPerk perk)
        {
            if(playerXp<100||playerPerk!=0||perk<=FounderPerk.None||perk>FounderPerk.ForwardRally)return false;
            playerPerk=(int)perk;return true;
        }
        public bool ChooseCaptain(CaptainPerk perk)
        {
            if(commanderXp<100||commanderPerk!=0||perk<=CaptainPerk.None||perk>CaptainPerk.CarefulRelief)return false;
            commanderPerk=(int)perk;return true;
        }
    }
}
