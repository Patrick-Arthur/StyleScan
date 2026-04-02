using System.ComponentModel.DataAnnotations;

namespace StyleScan.Backend.Models.DTOs.Looks
{
    public class AddLookToCollectionRequest
    {
        [Required]
        [MaxLength(80)]
        public string Name { get; set; } = string.Empty;
    }
}
