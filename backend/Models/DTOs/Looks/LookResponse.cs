using System;
using System.Collections.Generic;

namespace StyleScan.Backend.Models.DTOs.Looks
{
    public class LookResponse
    {
        public Guid Id { get; set; }
        public Guid AvatarId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Occasion { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public string Season { get; set; } = string.Empty;
        public string? Note { get; set; }
        public List<string> OccasionTags { get; set; } = new();
        public string? HeroImageUrl { get; set; }
        public string? HeroPreviewMode { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string? ShareSlug { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ClothingItemResponse> Items { get; set; } = new();
    }

    public class ClothingItemResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public Guid StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string ProductUrl { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }
}
