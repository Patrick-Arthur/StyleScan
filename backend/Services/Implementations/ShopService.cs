using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StyleScan.Backend.Data;
using StyleScan.Backend.Models.DTOs.Shop;
using StyleScan.Backend.Services.Interfaces;

namespace StyleScan.Backend.Services.Implementations
{
    public class ShopService : IShopService
    {
        private readonly StyleScanDbContext _context;

        public ShopService(StyleScanDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProductResponse>> GetProductsAsync(string? category, decimal? minPrice, decimal? maxPrice, int page, int pageSize)
        {
            var query = _context.Clothings.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(c => c.Category == category);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(c => c.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(c => c.Price <= maxPrice.Value);
            }

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(c => c.Store)
                .Select(c => new ProductResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Category = c.Category,
                    Price = c.Price,
                    Color = c.Color,
                    Description = c.Description,
                    Sizes = c.Sizes,
                    ImageUrl = c.ImageUrl,
                    ImageUrls = new List<string> { c.ImageUrl },
                    StoreId = c.StoreId,
                    StoreName = c.Store.Name,
                    StoreUrl = c.Store.WebsiteUrl,
                    ProductUrl = c.ProductUrl,
                    Rating = c.Rating,
                    Reviews = c.ReviewsCount,
                    InStock = c.InStock,
                    Sku = c.Sku
                })
                .ToListAsync();

            return products;
        }

        public async Task<ProductResponse?> GetProductByIdAsync(Guid productId)
        {
            var product = await _context.Clothings
                .Include(c => c.Store)
                .FirstOrDefaultAsync(c => c.Id == productId);

            if (product == null)
            {
                return null;
            }

            return new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Category = product.Category,
                Price = product.Price,
                Color = product.Color,
                Description = product.Description,
                Sizes = product.Sizes,
                ImageUrl = product.ImageUrl,
                ImageUrls = new List<string> { product.ImageUrl },
                StoreId = product.StoreId,
                StoreName = product.Store.Name,
                StoreUrl = product.Store.WebsiteUrl,
                ProductUrl = product.ProductUrl,
                Rating = product.Rating,
                Reviews = product.ReviewsCount,
                InStock = product.InStock,
                Sku = product.Sku
            };
        }

        public Task<Guid> CreateOrderAsync(Guid userId, OrderRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
