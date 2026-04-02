using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace StyleScan.Backend.Models.DTOs.Avatar
{
    public class CreateAvatarRequest
    {
        public List<IFormFile> Photos { get; set; } = new();

        [Required]
        public string Gender { get; set; } = string.Empty;

        public string BodyType { get; set; } = string.Empty;

        public string SkinTone { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public double Height { get; set; }
        public double Chest { get; set; }
        public double Waist { get; set; }
        public double Hips { get; set; }
    }
}
