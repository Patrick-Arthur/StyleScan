using Microsoft.AspNetCore.Mvc;
using StyleScan.Backend.Models.DTOs.Looks;
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
    public class LooksController : ControllerBase
    {
        private readonly ILooksService _looksService;

        public LooksController(ILooksService looksService)
        {
            _looksService = looksService;
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

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] GenerateLooksRequest request)
        {
            try
            {
                var userId = GetUserId();
                var looks = await _looksService.GenerateLooksAsync(userId, request);
                return CreatedAtAction(nameof(List), new { avatarId = request.AvatarId }, new { data = looks, total = looks.Count });
            }
            catch (InvalidOperationException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }

        [HttpPost("custom")]
        public async Task<IActionResult> SaveCustom([FromBody] SaveCustomLookRequest request)
        {
            try
            {
                var userId = GetUserId();
                var look = await _looksService.SaveCustomLookAsync(userId, request);
                return CreatedAtAction(nameof(GetById), new { id = look.Id }, look);
            }
            catch (InvalidOperationException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }

        [HttpPut("{id}/cover")]
        public async Task<IActionResult> UpdateCover(Guid id, [FromBody] UpdateLookCoverRequest request)
        {
            try
            {
                var userId = GetUserId();
                var look = await _looksService.UpdateLookCoverAsync(userId, id, request);
                return Ok(look);
            }
            catch (InvalidOperationException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDetails(Guid id, [FromBody] UpdateLookDetailsRequest request)
        {
            try
            {
                var userId = GetUserId();
                var look = await _looksService.UpdateLookDetailsAsync(userId, id, request);
                return Ok(look);
            }
            catch (InvalidOperationException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }

        [HttpPut("{id}/publication")]
        public async Task<IActionResult> UpdatePublication(Guid id, [FromBody] UpdateLookPublicationRequest request)
        {
            try
            {
                var userId = GetUserId();
                var look = await _looksService.UpdateLookPublicationAsync(userId, id, request);
                return Ok(look);
            }
            catch (InvalidOperationException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }

        [HttpPost("try-on")]
        public async Task<IActionResult> GenerateTryOn([FromBody] GenerateTryOnPreviewRequest request)
        {
            try
            {
                var userId = GetUserId();
                var preview = await _looksService.GenerateTryOnPreviewAsync(userId, request);
                return Ok(preview);
            }
            catch (InvalidOperationException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> List([FromQuery] Guid? avatarId, [FromQuery] string? occasion)
        {
            var userId = GetUserId();
            var looks = await _looksService.GetUserLooksAsync(userId, avatarId, occasion);
            return Ok(new { data = looks, total = looks.Count });
        }

        [HttpGet("preview-history")]
        public async Task<IActionResult> PreviewHistory([FromQuery] Guid? avatarId, [FromQuery] int take = 12)
        {
            var userId = GetUserId();
            var history = await _looksService.GetPreviewHistoryAsync(userId, avatarId, take);
            return Ok(new { data = history, total = history.Count });
        }

        [HttpGet("favorites")]
        public async Task<IActionResult> Favorites()
        {
            var userId = GetUserId();
            var looks = await _looksService.GetFavoriteLooksAsync(userId);
            return Ok(new { data = looks, total = looks.Count });
        }

        [HttpGet("collections")]
        public async Task<IActionResult> Collections()
        {
            var userId = GetUserId();
            var collections = await _looksService.GetLookCollectionsAsync(userId);
            return Ok(new { data = collections, total = collections.Count });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var look = await _looksService.GetLookByIdAsync(id);
            if (look == null)
            {
                return NotFound();
            }

            return Ok(look);
        }

        [HttpPost("{id}/favorite")]
        public async Task<IActionResult> AddToFavorites(Guid id)
        {
            var userId = GetUserId();
            await _looksService.AddLookToFavoritesAsync(userId, id);
            return Ok(new { message = "Look adicionado aos favoritos" });
        }

        [HttpDelete("{id}/favorite")]
        public async Task<IActionResult> RemoveFromFavorites(Guid id)
        {
            var userId = GetUserId();
            await _looksService.RemoveLookFromFavoritesAsync(userId, id);
            return Ok(new { message = "Look removido dos favoritos" });
        }

        [HttpPost("{id}/collections")]
        public async Task<IActionResult> AddToCollection(Guid id, [FromBody] AddLookToCollectionRequest request)
        {
            var userId = GetUserId();
            await _looksService.AddLookToCollectionAsync(userId, id, request);
            return Ok(new { message = "Look adicionado a colecao" });
        }

        [HttpDelete("{id}/collections/{collectionId}")]
        public async Task<IActionResult> RemoveFromCollection(Guid id, string collectionId)
        {
            var userId = GetUserId();
            await _looksService.RemoveLookFromCollectionAsync(userId, id, collectionId);
            return Ok(new { message = "Look removido da colecao" });
        }

        [HttpPut("collections/{collectionId}")]
        public async Task<IActionResult> RenameCollection(string collectionId, [FromBody] RenameLookCollectionRequest request)
        {
            try
            {
                var userId = GetUserId();
                await _looksService.RenameLookCollectionAsync(userId, collectionId, request);
                return Ok(new { message = "Colecao atualizada" });
            }
            catch (InvalidOperationException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }

        [HttpDelete("collections/{collectionId}")]
        public async Task<IActionResult> DeleteCollection(string collectionId)
        {
            try
            {
                var userId = GetUserId();
                await _looksService.DeleteLookCollectionAsync(userId, collectionId);
                return Ok(new { message = "Colecao excluida" });
            }
            catch (InvalidOperationException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }
    }
}
