namespace StyleScan.Backend.Models.DTOs.Looks
{
    public class UpdateLookCoverRequest
    {
        public string HeroImageUrl { get; set; } = string.Empty;
        public string? HeroPreviewMode { get; set; }
    }
}
