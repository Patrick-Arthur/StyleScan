using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StyleScan.Backend.Models.DTOs.Shop;

namespace StyleScan.Backend.Services.Interfaces
{
    public interface IShopService
    {
        Task<List<ProductResponse>> GetProductsAsync(string? category, decimal? minPrice, decimal? maxPrice, int page, int pageSize);
        Task<ProductResponse?> GetProductByIdAsync(Guid productId);
        Task<Guid> CreateOrderAsync(Guid userId, OrderRequest request);
    }
}
