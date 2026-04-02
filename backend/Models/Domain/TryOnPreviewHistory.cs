using System;

namespace StyleScan.Backend.Models.Domain
{
    public class TryOnPreviewHistory
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid AvatarId { get; set; }
        public string Style { get; set; } = string.Empty;
        public string Occasion { get; set; } = string.Empty;
        public string? BoardId { get; set; }
        public string Mode { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool UsedAi { get; set; }
        public string[] ProductNames { get; set; } = Array.Empty<string>();
        public string[] ProductCategories { get; set; } = Array.Empty<string>();
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
        public Avatar Avatar { get; set; } = null!;
    }
}
