using System;
using System.Collections.Generic;

namespace StyleScan.Backend.Models.DTOs.Looks
{
    public class TryOnPreviewHistoryResponse
    {
        public Guid Id { get; set; }
        public Guid AvatarId { get; set; }
        public string Style { get; set; } = string.Empty;
        public string Occasion { get; set; } = string.Empty;
        public string? BoardId { get; set; }
        public string Mode { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool UsedAi { get; set; }
        public List<string> ProductNames { get; set; } = new();
        public List<string> ProductCategories { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
