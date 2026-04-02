using System;
using System.Collections.Generic;

namespace StyleScan.Backend.Models.Domain
{
    public class Look
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid AvatarId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Occasion { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public string Season { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string[] OccasionTags { get; set; } = Array.Empty<string>();
        public string? HeroImageUrl { get; set; }
        public string? HeroPreviewMode { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string? ShareSlug { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
        public Avatar Avatar { get; set; } = null!;
        public ICollection<LookClothing> LookClothings { get; set; } = new List<LookClothing>();
    }

    public class LookClothing
    {
        public Guid LookId { get; set; }
        public Guid ClothingId { get; set; }
        public Look Look { get; set; } = null!;
        public Clothing Clothing { get; set; } = null!;
    }
}
