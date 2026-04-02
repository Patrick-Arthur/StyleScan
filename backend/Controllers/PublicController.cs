using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StyleScan.Backend.Data;
using StyleScan.Backend.Models.Domain;
using StyleScan.Backend.Models.DTOs.Looks;
using StyleScan.Backend.Models.DTOs.User;

namespace StyleScan.Backend.Controllers
{
    [AllowAnonymous]
    [Route("api/v1/public")]
    [ApiController]
    public class PublicController : ControllerBase
    {
        private readonly StyleScanDbContext _context;

        public PublicController(StyleScanDbContext context)
        {
            _context = context;
        }

        [HttpGet("profiles/{slug}")]
        public async Task<ActionResult<PublicProfileResponse>> GetProfile(string slug)
        {
            var normalizedSlug = slug.Trim().ToLowerInvariant();
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(existingUser => existingUser.PublicProfileSlug == normalizedSlug);

            if (user == null)
            {
                return NotFound(new { message = "Perfil publico nao encontrado." });
            }

            var looks = await _context.Looks
                .AsNoTracking()
                .Where(look => look.UserId == user.Id && look.IsPublished)
                .OrderByDescending(look => look.PublishedAt ?? look.CreatedAt)
                .ToListAsync();

            return Ok(new PublicProfileResponse
            {
                DisplayName = $"{user.FirstName} {user.LastName}".Trim(),
                PublicProfileSlug = user.PublicProfileSlug ?? string.Empty,
                Bio = user.Bio,
                PublishedLooksCount = looks.Count,
                Looks = looks.Select(MapPublicLookSummary).ToList()
            });
        }

        [HttpGet("looks/{slug}")]
        public async Task<ActionResult<PublicLookDetailResponse>> GetLook(string slug)
        {
            var normalizedSlug = slug.Trim().ToLowerInvariant();
            var look = await _context.Looks
                .AsNoTracking()
                .Include(existingLook => existingLook.User)
                .Include(existingLook => existingLook.LookClothings)
                .ThenInclude(item => item.Clothing)
                .ThenInclude(clothing => clothing.Store)
                .FirstOrDefaultAsync(existingLook => existingLook.ShareSlug == normalizedSlug && existingLook.IsPublished);

            if (look == null)
            {
                return NotFound(new { message = "Look publico nao encontrado." });
            }

            return Ok(new PublicLookDetailResponse
            {
                OwnerDisplayName = $"{look.User.FirstName} {look.User.LastName}".Trim(),
                OwnerPublicProfileSlug = look.User.PublicProfileSlug ?? string.Empty,
                Look = MapPublicLookSummary(look),
                Items = look.LookClothings.Select(item => MapClothingItem(item.Clothing)).ToList()
            });
        }

        private static PublicLookSummaryResponse MapPublicLookSummary(Look look)
        {
            return new PublicLookSummaryResponse
            {
                Id = look.Id,
                Name = look.Name,
                Occasion = look.Occasion,
                Style = look.Style,
                Note = look.Note,
                OccasionTags = look.OccasionTags.ToList(),
                HeroImageUrl = look.HeroImageUrl,
                HeroPreviewMode = look.HeroPreviewMode,
                ShareSlug = look.ShareSlug ?? string.Empty,
                TotalPrice = look.TotalPrice,
                CreatedAt = look.CreatedAt,
                PublishedAt = look.PublishedAt
            };
        }

        private static ClothingItemResponse MapClothingItem(Clothing item)
        {
            return new ClothingItemResponse
            {
                Id = item.Id,
                Name = item.Name,
                Category = item.Category,
                Color = item.Color,
                Price = item.Price,
                StoreId = item.StoreId,
                StoreName = item.Store?.Name ?? string.Empty,
                ProductUrl = item.ProductUrl,
                ImageUrl = item.ImageUrl
            };
        }
    }
}
