using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StyleScan.Backend.Models.DTOs.Looks
{
    public class UpdateLookDetailsRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [MaxLength(220)]
        public string? Note { get; set; }

        public List<string> OccasionTags { get; set; } = new();
    }
}
