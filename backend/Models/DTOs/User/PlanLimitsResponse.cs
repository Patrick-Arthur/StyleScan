namespace StyleScan.Backend.Models.DTOs.User
{
    public class PlanLimitsResponse
    {
        public int Avatars { get; set; }
        public int AvatarTryOnsPerWeek { get; set; }
        public int RealisticRendersPerMonth { get; set; }
        public int SavedLooks { get; set; }
    }
}
