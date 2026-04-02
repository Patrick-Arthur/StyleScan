using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using StyleScan.Backend.Models.DTOs.Avatar;

namespace StyleScan.Backend.Services.Interfaces
{
    public interface IAvatarService
    { 
        Task<AvatarResponse> CreateAvatarAsync(Guid userId, CreateAvatarRequest request);
        Task<List<AvatarResponse>> GetUserAvatarsAsync(Guid userId);
        Task<AvatarResponse?> GetAvatarByIdAsync(Guid avatarId);
        Task<AvatarResponse?> UpdateAvatarAsync(Guid avatarId, UpdateAvatarRequest request);
        Task<AvatarResponse?> UpdateAvatarPhotosAsync(Guid avatarId, IReadOnlyCollection<IFormFile> photos);
        Task<AvatarResponse?> GenerateTwoDimensionalAvatarAsync(Guid avatarId);
        Task DeleteAvatarAsync(Guid avatarId);
    }
}
