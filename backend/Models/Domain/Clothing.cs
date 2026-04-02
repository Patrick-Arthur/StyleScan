using System;
using System.Collections.Generic;

namespace StyleScan.Backend.Models.Domain
{
    public class Clothing
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public List<string> Sizes { get; set; } = new();
        public string ImageUrl { get; set; } = string.Empty;
        public string ProductUrl { get; set; } = string.Empty;
        public Guid StoreId { get; set; }
        public double Rating { get; set; }
        public int ReviewsCount { get; set; }
        public bool InStock { get; set; }
        public string Sku { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Store Store { get; set; } = null!;
        public ICollection<LookClothing> LookClothings { get; set; } = new List<LookClothing>();
    }
}
