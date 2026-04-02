using Microsoft.AspNetCore.Mvc;
using StyleScan.Backend.Models.DTOs.Shop;
using StyleScan.Backend.Services.Interfaces;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace StyleScan.Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class ShopController : ControllerBase
    {
        private readonly IShopService _shopService;

        public ShopController(IShopService shopService)
        {
            _shopService = shopService;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found or invalid.");
            }
            return userId;
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? category,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var products = await _shopService.GetProductsAsync(category, minPrice, maxPrice, page, pageSize);
            return Ok(new { data = products, total = products.Count });
        }

        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var product = await _shopService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [HttpPost("orders")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequest request)
        {
            var userId = GetUserId();
            var orderId = await _shopService.CreateOrderAsync(userId, request);
            return CreatedAtAction(nameof(CreateOrder), new { id = orderId }, new { id = orderId, message = "Pedido criado com sucesso" });
        }
    }
}
