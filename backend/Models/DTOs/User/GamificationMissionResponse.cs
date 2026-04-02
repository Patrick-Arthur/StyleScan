namespace StyleScan.Backend.Models.DTOs.User
{
    public class GamificationMissionResponse
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Progress { get; set; }
        public int Goal { get; set; }
        public bool Completed { get; set; }
        public int RewardPoints { get; set; }
    }
}
