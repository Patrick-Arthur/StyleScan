using System.Collections.Generic;

namespace StyleScan.Backend.Models.DTOs.User
{
    public class PublicProfileResponse
    {
        public string DisplayName { get; set; } = string.Empty;
        public string PublicProfileSlug { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public int PublishedLooksCount { get; set; }
        public List<PublicLookSummaryResponse> Looks { get; set; } = new();
    }
}
