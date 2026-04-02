using Microsoft.AspNetCore.Mvc;
using StyleScan.Backend.Models.DTOs.Avatar;
using StyleScan.Backend.Services.Interfaces;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace StyleScan.Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class AvatarController : ControllerBase
    {
        private readonly IAvatarService _avatarService;

        public AvatarController(IAvatarService avatarService)
        {
            _avatarService = avatarService;
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

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromForm] CreateAvatarRequest request)
        {
            var userId = GetUserId();
            try
            {
                var response = await _avatarService.CreateAvatarAsync(userId, request);
                return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            var userId = GetUserId();
            var avatars = await _avatarService.GetUserAvatarsAsync(userId);
            return Ok(new { data = avatars, total = avatars.Count });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var avatar = await _avatarService.GetAvatarByIdAsync(id);
            if (avatar == null)
            {
                return NotFound();
            }

            if (avatar.UserId != GetUserId())
            {
                return Forbid();
            }

            return Ok(avatar);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAvatarRequest request)
        {
            var existingAvatar = await _avatarService.GetAvatarByIdAsync(id);
            if (existingAvatar == null)
            {
                return NotFound();
            }

            if (existingAvatar.UserId != GetUserId())
            {
                return Forbid();
            }

            var updatedAvatar = await _avatarService.UpdateAvatarAsync(id, request);
            return Ok(updatedAvatar);
        }

        [HttpPut("{id}/photos")]
        public async Task<IActionResult> UpdatePhotos(Guid id, [FromForm] List<IFormFile>? photos)
        {
            var existingAvatar = await _avatarService.GetAvatarByIdAsync(id);
            if (existingAvatar == null)
            {
                return NotFound();
            }

            if (existingAvatar.UserId != GetUserId())
            {
                return Forbid();
            }

            if (photos == null || photos.Count == 0)
            {
                return BadRequest(new { message = "Envie ao menos uma imagem para atualizar o avatar." });
            }

            try
            {
                var updatedAvatar = await _avatarService.UpdateAvatarPhotosAsync(id, photos);
                return Ok(updatedAvatar);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/generate-2d")]
        public async Task<IActionResult> GenerateTwoDimensionalAvatar(Guid id)
        {
            var existingAvatar = await _avatarService.GetAvatarByIdAsync(id);
            if (existingAvatar == null)
            {
                return NotFound();
            }

            if (existingAvatar.UserId != GetUserId())
            {
                return Forbid();
            }

            try
            {
                var updatedAvatar = await _avatarService.GenerateTwoDimensionalAvatarAsync(id);
                return Ok(updatedAvatar);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existingAvatar = await _avatarService.GetAvatarByIdAsync(id);
            if (existingAvatar == null)
            {
                return NotFound();
            }

            if (existingAvatar.UserId != GetUserId())
            {
                return Forbid();
            }

            await _avatarService.DeleteAvatarAsync(id);
            return NoContent();
        }
    }
}
