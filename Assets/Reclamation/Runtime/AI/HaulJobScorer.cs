namespace Reclamation.AI
{
    public static class HaulJobScorer
    {
        public const float PriorityWeight = 100f;

        public static float Score(int policyPriority, float travelDistance)
        {
            return (policyPriority * PriorityWeight) - travelDistance;
        }
    }
}
