using System.Collections.Generic;
using StyleScan.Backend.Models.DTOs.Looks;

namespace StyleScan.Backend.Models.DTOs.User
{
    public class PublicLookDetailResponse
    {
        public string OwnerDisplayName { get; set; } = string.Empty;
        public string OwnerPublicProfileSlug { get; set; } = string.Empty;
        public PublicLookSummaryResponse Look { get; set; } = new();
        public List<ClothingItemResponse> Items { get; set; } = new();
    }
}
