using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StyleScan.Backend.Models.DTOs.Looks;

namespace StyleScan.Backend.Services.Interfaces
{
    public interface ILooksService
    {
        Task<List<LookResponse>> GenerateLooksAsync(Guid userId, GenerateLooksRequest request);
        Task<LookResponse> SaveCustomLookAsync(Guid userId, SaveCustomLookRequest request);
        Task<LookResponse> UpdateLookDetailsAsync(Guid userId, Guid lookId, UpdateLookDetailsRequest request);
        Task<LookResponse> UpdateLookCoverAsync(Guid userId, Guid lookId, UpdateLookCoverRequest request);
        Task<LookResponse> UpdateLookPublicationAsync(Guid userId, Guid lookId, UpdateLookPublicationRequest request);
        Task<TryOnPreviewResponse> GenerateTryOnPreviewAsync(Guid userId, GenerateTryOnPreviewRequest request);
        Task<List<TryOnPreviewHistoryResponse>> GetPreviewHistoryAsync(Guid userId, Guid? avatarId = null, int take = 12);
        Task<List<LookResponse>> GetUserLooksAsync(Guid userId, Guid? avatarId = null, string? occasion = null);
        Task<List<LookResponse>> GetFavoriteLooksAsync(Guid userId);
        Task<List<LookCollectionResponse>> GetLookCollectionsAsync(Guid userId);
        Task AddLookToCollectionAsync(Guid userId, Guid lookId, AddLookToCollectionRequest request);
        Task RemoveLookFromCollectionAsync(Guid userId, Guid lookId, string collectionId);
        Task RenameLookCollectionAsync(Guid userId, string collectionId, RenameLookCollectionRequest request);
        Task DeleteLookCollectionAsync(Guid userId, string collectionId);
        Task<LookResponse?> GetLookByIdAsync(Guid lookId);
        Task AddLookToFavoritesAsync(Guid userId, Guid lookId);
        Task RemoveLookFromFavoritesAsync(Guid userId, Guid lookId);
    }
}
