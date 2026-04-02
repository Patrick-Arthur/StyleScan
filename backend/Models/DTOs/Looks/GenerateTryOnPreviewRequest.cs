using System;
using System.Collections.Generic;

namespace StyleScan.Backend.Models.DTOs.Looks
{
    public class GenerateTryOnPreviewRequest
    {
        public Guid AvatarId { get; set; }
        public string Style { get; set; } = string.Empty;
        public string Occasion { get; set; } = string.Empty;
        public string? BoardId { get; set; }
        public List<string> Palette { get; set; } = new();
        public string Mode { get; set; } = "avatar";
        public List<Guid> ProductIds { get; set; } = new();
    }
}
