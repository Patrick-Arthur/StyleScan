using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StyleScan.Backend.Models.DTOs.Looks
{
    public class SaveCustomLookRequest
    {
        [Required]
        public Guid AvatarId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Occasion { get; set; } = string.Empty;

        [Required]
        public string Style { get; set; } = string.Empty;

        public string? Season { get; set; }
        [MaxLength(220)]
        public string? Note { get; set; }
        public List<string> OccasionTags { get; set; } = new();
        public string? HeroImageUrl { get; set; }
        public string? HeroPreviewMode { get; set; }

        [MinLength(2)]
        public List<Guid> ProductIds { get; set; } = new();
    }
}
