using System;
using System.Collections.Generic;

namespace StyleScan.Backend.Models.DTOs.Shop
{
    public class ProductResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Sizes { get; set; } = new();
        public string ImageUrl { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new();
        public Guid StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string StoreUrl { get; set; } = string.Empty;
        public string ProductUrl { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int Reviews { get; set; }
        public bool InStock { get; set; }
        public string Sku { get; set; } = string.Empty;
    }
}
