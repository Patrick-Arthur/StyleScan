using System;
using System.Collections.Generic;

namespace StyleScan.Backend.Models.DTOs.User
{
    public class PublicLookSummaryResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Occasion { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public string? Note { get; set; }
        public List<string> OccasionTags { get; set; } = new();
        public string? HeroImageUrl { get; set; }
        public string? HeroPreviewMode { get; set; }
        public string ShareSlug { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
