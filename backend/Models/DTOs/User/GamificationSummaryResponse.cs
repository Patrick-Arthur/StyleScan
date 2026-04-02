using System.Collections.Generic;

namespace StyleScan.Backend.Models.DTOs.User
{
    public class GamificationSummaryResponse
    {
        public int CurrentLevel { get; set; }
        public int ExperiencePoints { get; set; }
        public int NextLevelPoints { get; set; }
        public double ProgressPercent { get; set; }
        public string MomentumLabel { get; set; } = string.Empty;
        public List<GamificationBadgeResponse> Badges { get; set; } = new();
        public List<GamificationMissionResponse> Missions { get; set; } = new();
    }
}
