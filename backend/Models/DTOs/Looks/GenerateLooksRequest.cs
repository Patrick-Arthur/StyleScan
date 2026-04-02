using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StyleScan.Backend.Models.DTOs.Looks
{
    public class GenerateLooksRequest
    {
        [Required]
        public Guid AvatarId { get; set; }

        [Required]
        public string Occasion { get; set; } = string.Empty;

        [Required]
        public string Style { get; set; } = string.Empty;

        public string? Season { get; set; }

        public List<string>? ColorPreferences { get; set; }

        public decimal? Budget { get; set; }
    }
}
