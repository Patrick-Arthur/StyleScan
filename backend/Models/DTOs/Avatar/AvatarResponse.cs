using System;
using System.Collections.Generic;

namespace StyleScan.Backend.Models.DTOs.Avatar
{
    public class AvatarResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ModelUrl { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public List<string> PhotoUrls { get; set; } = new();
        public string? GeneratedAvatarImageUrl { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string BodyType { get; set; } = string.Empty;
        public string SkinTone { get; set; } = string.Empty;
        public double Height { get; set; }
        public double Chest { get; set; }
        public double Waist { get; set; }
        public double Hips { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
