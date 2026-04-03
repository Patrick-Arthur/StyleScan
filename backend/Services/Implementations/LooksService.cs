using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StyleScan.Backend.Data;
using StyleScan.Backend.Models.Domain;
using StyleScan.Backend.Models.DTOs.Looks;
using StyleScan.Backend.Services.Interfaces;
using StyleScan.Backend.Services.Support;

namespace StyleScan.Backend.Services.Implementations
{
    public class LooksService : ILooksService
    {
        private const string FavoriteLookPreferenceKey = "favorite-look";
        private const string LookCollectionKeyPrefix = "look-collection:";
        private const string LookCollectionLabelPrefix = "look-collection-label:";
        private readonly StyleScanDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public LooksService(
            StyleScanDbContext context,
            IWebHostEnvironment environment,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<List<LookResponse>> GenerateLooksAsync(Guid userId, GenerateLooksRequest request)
        {
            var user = await GetUserAsync(userId);
            await EnsureSavedLookCapacityAsync(user);
            var avatar = await GetUserAvatarAsync(userId, request.AvatarId);
            var catalog = await GetCatalogAsync();
            var selectedItems = BuildLookItems(catalog, request);
            var totalPrice = selectedItems.Sum(item => item.Price);

            if (request.Budget.HasValue && totalPrice > request.Budget.Value)
            {
                selectedItems = selectedItems
                    .OrderBy(item => item.Price)
                    .Take(Math.Max(2, selectedItems.Count - 1))
                    .ToList();

                totalPrice = selectedItems.Sum(item => item.Price);
            }

            var look = BuildLookEntity(
                userId,
                request.AvatarId,
                BuildLookName(request, avatar),
                request.Occasion.Trim(),
                request.Style.Trim(),
                string.IsNullOrWhiteSpace(request.Season) ? "all season" : request.Season.Trim(),
                selectedItems);

            _context.Looks.Add(look);
            await IncrementUsageAsync(userId, UsageMetricType.SavedLook, DateTime.UtcNow);
            await _context.SaveChangesAsync();

            return new List<LookResponse> { MapLookResponse(look, selectedItems) };
        }

        public async Task<LookResponse> SaveCustomLookAsync(Guid userId, SaveCustomLookRequest request)
        {
            var user = await GetUserAsync(userId);
            await EnsureSavedLookCapacityAsync(user);
            var avatar = await GetUserAvatarAsync(userId, request.AvatarId);

            var normalizedIds = request.ProductIds.Distinct().ToList();
            if (normalizedIds.Count < 2)
            {
                throw new InvalidOperationException("Selecione pelo menos duas pecas para salvar o look.");
            }

            var selectedItems = await _context.Clothings
                .Include(clothing => clothing.Store)
                .Where(clothing => normalizedIds.Contains(clothing.Id))
                .ToListAsync();

            if (selectedItems.Count != normalizedIds.Count)
            {
                throw new InvalidOperationException("Uma ou mais pecas escolhidas nao foram encontradas.");
            }

            var look = BuildLookEntity(
                userId,
                request.AvatarId,
                string.IsNullOrWhiteSpace(request.Name) ? BuildLookName(request, avatar) : request.Name.Trim(),
                request.Occasion.Trim(),
                request.Style.Trim(),
                string.IsNullOrWhiteSpace(request.Season) ? request.Style.Trim() : request.Season.Trim(),
                selectedItems,
                request.Note,
                request.OccasionTags,
                request.HeroImageUrl,
                request.HeroPreviewMode);

            _context.Looks.Add(look);
            await IncrementUsageAsync(userId, UsageMetricType.SavedLook, DateTime.UtcNow);
            await _context.SaveChangesAsync();

            return MapLookResponse(look, selectedItems);
        }

        public async Task<LookResponse> UpdateLookCoverAsync(Guid userId, Guid lookId, UpdateLookCoverRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.HeroImageUrl))
            {
                throw new InvalidOperationException("A imagem de capa do look nao foi informada.");
            }

            var look = await _context.Looks
                .Include(existingLook => existingLook.LookClothings)
                .ThenInclude(item => item.Clothing)
                .ThenInclude(clothing => clothing.Store)
                .FirstOrDefaultAsync(existingLook => existingLook.Id == lookId && existingLook.UserId == userId);

            if (look == null)
            {
                throw new InvalidOperationException("Look nao encontrado para atualizar a capa.");
            }

            look.HeroImageUrl = request.HeroImageUrl.Trim();
            look.HeroPreviewMode = string.IsNullOrWhiteSpace(request.HeroPreviewMode) ? null : request.HeroPreviewMode.Trim();

            await _context.SaveChangesAsync();
            return MapLookResponse(look, look.LookClothings.Select(item => item.Clothing).ToList());
        }

        public async Task<LookResponse> UpdateLookDetailsAsync(Guid userId, Guid lookId, UpdateLookDetailsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new InvalidOperationException("O nome do look precisa ser informado.");
            }

            var look = await _context.Looks
                .Include(existingLook => existingLook.LookClothings)
                .ThenInclude(item => item.Clothing)
                .ThenInclude(clothing => clothing.Store)
                .FirstOrDefaultAsync(existingLook => existingLook.Id == lookId && existingLook.UserId == userId);

            if (look == null)
            {
                throw new InvalidOperationException("Look nao encontrado para atualizar.");
            }

            look.Name = request.Name.Trim();
            look.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
            look.OccasionTags = request.OccasionTags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToArray();

            await _context.SaveChangesAsync();
            return MapLookResponse(look, look.LookClothings.Select(item => item.Clothing).ToList());
        }

        public async Task<LookResponse> UpdateLookPublicationAsync(Guid userId, Guid lookId, UpdateLookPublicationRequest request)
        {
            var user = await GetUserAsync(userId);
            var look = await _context.Looks
                .Include(existingLook => existingLook.LookClothings)
                .ThenInclude(item => item.Clothing)
                .ThenInclude(clothing => clothing.Store)
                .FirstOrDefaultAsync(existingLook => existingLook.Id == lookId && existingLook.UserId == userId);

            if (look == null)
            {
                throw new InvalidOperationException("Look nao encontrado para atualizar a publicacao.");
            }

            look.IsPublished = request.IsPublished;
            if (request.IsPublished)
            {
                user.PublicProfileSlug ??= BuildProfileSlug(user.FirstName, user.LastName, user.Id);
                look.PublishedAt ??= DateTime.UtcNow;
                look.ShareSlug ??= BuildShareSlug(look.Name, look.Id);
            }

            await _context.SaveChangesAsync();
            return MapLookResponse(look, look.LookClothings.Select(item => item.Clothing).ToList());
        }

        public async Task<TryOnPreviewResponse> GenerateTryOnPreviewAsync(Guid userId, GenerateTryOnPreviewRequest request)
        {
            if (request.ProductIds == null || request.ProductIds.Count == 0)
            {
                throw new InvalidOperationException("Selecione ao menos uma peca para gerar o provador.");
            }

            var avatar = await _context.Avatars
                .AsNoTracking()
                .FirstOrDefaultAsync(existingAvatar => existingAvatar.Id == request.AvatarId && existingAvatar.UserId == userId);

            if (avatar == null)
            {
                throw new InvalidOperationException("Avatar nao encontrado para gerar o provador.");
            }

            var selectedProducts = await _context.Clothings
                .Include(clothing => clothing.Store)
                .Where(clothing => request.ProductIds.Contains(clothing.Id))
                .ToListAsync();

            if (selectedProducts.Count == 0)
            {
                throw new InvalidOperationException("Nao encontramos as pecas escolhidas para o provador.");
            }

            var mode = string.IsNullOrWhiteSpace(request.Mode) ? "avatar" : request.Mode.Trim().ToLowerInvariant();
            var user = await GetUserAsync(userId);
            var plan = AccountPlanCatalog.Resolve(user.AccountPlan);
            if (mode == "realistic")
            {
                await EnsureUsageLimitAsync(userId, UsageMetricType.RealisticRender, plan.RealisticRendersPerMonth, "Seu plano atual atingiu o limite mensal de fotos realistas.");
            }
            else
            {
                await EnsureUsageLimitAsync(userId, UsageMetricType.AvatarTryOn, plan.AvatarTryOnsPerWeek, "Seu plano atual atingiu o limite semanal de provas no avatar.");
            }

            var generatedWithAi = await TryGenerateTryOnWithOpenAiAsync(avatar, selectedProducts, request, mode);
            if (!string.IsNullOrWhiteSpace(generatedWithAi))
            {
                await SavePreviewHistoryAsync(userId, avatar.Id, request, mode, generatedWithAi, true, selectedProducts);
                await IncrementUsageAsync(userId, mode == "realistic" ? UsageMetricType.RealisticRender : UsageMetricType.AvatarTryOn, DateTime.UtcNow);
                await _context.SaveChangesAsync();
                return new TryOnPreviewResponse
                {
                    ImageUrl = generatedWithAi,
                    UsedAi = true
                };
            }

            var fallback = await CreateLocalTryOnPreviewAsync(avatar, selectedProducts, request.Style);
            await SavePreviewHistoryAsync(userId, avatar.Id, request, mode, fallback, false, selectedProducts);
            await IncrementUsageAsync(userId, mode == "realistic" ? UsageMetricType.RealisticRender : UsageMetricType.AvatarTryOn, DateTime.UtcNow);
            await _context.SaveChangesAsync();
            return new TryOnPreviewResponse
            {
                ImageUrl = fallback,
                UsedAi = false
            };
        }

        public async Task<List<LookResponse>> GetUserLooksAsync(Guid userId, Guid? avatarId = null, string? occasion = null)
        {
            var query = _context.Looks
                .Include(l => l.LookClothings)
                .ThenInclude(lc => lc.Clothing)
                .ThenInclude(clothing => clothing.Store)
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .AsQueryable();

            if (avatarId.HasValue)
            {
                query = query.Where(l => l.AvatarId == avatarId.Value);
            }

            if (!string.IsNullOrWhiteSpace(occasion))
            {
                query = query.Where(l => l.Occasion == occasion);
            }

            var looks = await query.ToListAsync();
            return looks.Select(look => MapLookResponse(look, look.LookClothings.Select(item => item.Clothing).ToList())).ToList();
        }

        public async Task<List<LookResponse>> GetFavoriteLooksAsync(Guid userId)
        {
            var favoriteIds = await _context.UserPreferences
                .AsNoTracking()
                .Where(preference => preference.UserId == userId && preference.PreferenceKey == FavoriteLookPreferenceKey)
                .Select(preference => preference.PreferenceValue)
                .ToListAsync();

            if (favoriteIds.Count == 0)
            {
                return new List<LookResponse>();
            }

            var normalizedIds = favoriteIds
                .Select(value => Guid.TryParse(value, out var lookId) ? lookId : Guid.Empty)
                .Where(lookId => lookId != Guid.Empty)
                .ToList();

            var looks = await _context.Looks
                .Include(look => look.LookClothings)
                .ThenInclude(item => item.Clothing)
                .ThenInclude(clothing => clothing.Store)
                .Where(look => look.UserId == userId && normalizedIds.Contains(look.Id))
                .ToListAsync();

            var orderedLooks = normalizedIds
                .Select(id => looks.FirstOrDefault(look => look.Id == id))
                .Where(look => look != null)
                .Cast<Look>()
                .ToList();

            return orderedLooks
                .Select(look => MapLookResponse(look, look.LookClothings.Select(item => item.Clothing).ToList()))
                .ToList();
        }

        public async Task<List<LookCollectionResponse>> GetLookCollectionsAsync(Guid userId)
        {
            var allCollectionPreferences = await _context.UserPreferences
                .AsNoTracking()
                .Where(preference => preference.UserId == userId
                    && (preference.PreferenceKey.StartsWith(LookCollectionKeyPrefix) || preference.PreferenceKey.StartsWith(LookCollectionLabelPrefix)))
                .ToListAsync();

            if (allCollectionPreferences.Count == 0)
            {
                return new List<LookCollectionResponse>();
            }

            var labelPreferences = allCollectionPreferences
                .Where(preference => preference.PreferenceKey.StartsWith(LookCollectionLabelPrefix))
                .ToDictionary(
                    preference => preference.PreferenceKey[LookCollectionLabelPrefix.Length..],
                    preference => preference.PreferenceValue);

            var groupedLookIds = allCollectionPreferences
                .Where(preference => preference.PreferenceKey.StartsWith(LookCollectionKeyPrefix) && !preference.PreferenceKey.StartsWith(LookCollectionLabelPrefix))
                .GroupBy(preference => preference.PreferenceKey[LookCollectionKeyPrefix.Length..])
                .ToDictionary(group => group.Key, group => group.Select(preference => preference.PreferenceValue).ToList());

            var allLookIds = groupedLookIds.Values
                .SelectMany(ids => ids)
                .Select(value => Guid.TryParse(value, out var lookId) ? lookId : Guid.Empty)
                .Where(lookId => lookId != Guid.Empty)
                .Distinct()
                .ToList();

            var looks = await _context.Looks
                .Include(look => look.LookClothings)
                .ThenInclude(item => item.Clothing)
                .ThenInclude(clothing => clothing.Store)
                .Where(look => look.UserId == userId && allLookIds.Contains(look.Id))
                .ToListAsync();

            return groupedLookIds
                .Select(group =>
                {
                    var collectionLooks = group.Value
                        .Select(value => Guid.TryParse(value, out var lookId) ? lookId : Guid.Empty)
                        .Where(lookId => lookId != Guid.Empty)
                        .Select(lookId => looks.FirstOrDefault(look => look.Id == lookId))
                        .Where(look => look != null)
                        .Cast<Look>()
                        .Select(look => MapLookResponse(look, look.LookClothings.Select(item => item.Clothing).ToList()))
                        .ToList();

                    return new LookCollectionResponse
                    {
                        Id = group.Key,
                        Label = labelPreferences.TryGetValue(group.Key, out var label) ? label : group.Key,
                        Looks = collectionLooks
                    };
                })
                .Where(collection => collection.Looks.Count > 0)
                .OrderByDescending(collection => collection.Looks.Count)
                .ThenBy(collection => collection.Label)
                .ToList();
        }

        public async Task<List<TryOnPreviewHistoryResponse>> GetPreviewHistoryAsync(Guid userId, Guid? avatarId = null, int take = 12)
        {
            var normalizedTake = Math.Clamp(take, 1, 24);
            var query = _context.TryOnPreviewHistories
                .AsNoTracking()
                .Where(history => history.UserId == userId);

            if (avatarId.HasValue)
            {
                query = query.Where(history => history.AvatarId == avatarId.Value);
            }

            var historyItems = await query
                .OrderByDescending(history => history.CreatedAt)
                .Take(normalizedTake)
                .ToListAsync();

            return historyItems.Select(MapPreviewHistoryResponse).ToList();
        }

        public async Task<LookResponse?> GetLookByIdAsync(Guid lookId)
        {
            var look = await _context.Looks
                .Include(l => l.LookClothings)
                .ThenInclude(lc => lc.Clothing)
                .ThenInclude(clothing => clothing.Store)
                .FirstOrDefaultAsync(l => l.Id == lookId);

            if (look == null)
            {
                return null;
            }

            return MapLookResponse(look, look.LookClothings.Select(item => item.Clothing).ToList());
        }

        public Task AddLookToFavoritesAsync(Guid userId, Guid lookId)
        {
            return AddLookToFavoritesInternalAsync(userId, lookId);
        }

        public Task RemoveLookFromFavoritesAsync(Guid userId, Guid lookId)
        {
            return RemoveLookFromFavoritesInternalAsync(userId, lookId);
        }

        public Task AddLookToCollectionAsync(Guid userId, Guid lookId, AddLookToCollectionRequest request)
        {
            return AddLookToCollectionInternalAsync(userId, lookId, request);
        }

        public Task RemoveLookFromCollectionAsync(Guid userId, Guid lookId, string collectionId)
        {
            return RemoveLookFromCollectionInternalAsync(userId, lookId, collectionId);
        }

        public Task RenameLookCollectionAsync(Guid userId, string collectionId, RenameLookCollectionRequest request)
        {
            return RenameLookCollectionInternalAsync(userId, collectionId, request);
        }

        public Task DeleteLookCollectionAsync(Guid userId, string collectionId)
        {
            return DeleteLookCollectionInternalAsync(userId, collectionId);
        }

        private async Task<Avatar> GetUserAvatarAsync(Guid userId, Guid avatarId)
        {
            var avatar = await _context.Avatars
                .AsNoTracking()
                .FirstOrDefaultAsync(existingAvatar => existingAvatar.Id == avatarId && existingAvatar.UserId == userId);

            if (avatar == null)
            {
                throw new InvalidOperationException("Avatar nao encontrado para esse look.");
            }

            return avatar;
        }

        private async Task<User> GetUserAsync(Guid userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(existingUser => existingUser.Id == userId)
                ?? throw new InvalidOperationException("Usuario nao encontrado.");
        }

        private async Task AddLookToFavoritesInternalAsync(Guid userId, Guid lookId)
        {
            var lookExists = await _context.Looks.AnyAsync(look => look.Id == lookId && look.UserId == userId);
            if (!lookExists)
            {
                throw new InvalidOperationException("Look nao encontrado para favoritar.");
            }

            var alreadyFavorited = await _context.UserPreferences.AnyAsync(preference =>
                preference.UserId == userId
                && preference.PreferenceKey == FavoriteLookPreferenceKey
                && preference.PreferenceValue == lookId.ToString());

            if (alreadyFavorited)
            {
                return;
            }

            _context.UserPreferences.Add(new UserPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PreferenceKey = FavoriteLookPreferenceKey,
                PreferenceValue = lookId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        private async Task RemoveLookFromFavoritesInternalAsync(Guid userId, Guid lookId)
        {
            var preference = await _context.UserPreferences.FirstOrDefaultAsync(existingPreference =>
                existingPreference.UserId == userId
                && existingPreference.PreferenceKey == FavoriteLookPreferenceKey
                && existingPreference.PreferenceValue == lookId.ToString());

            if (preference == null)
            {
                return;
            }

            _context.UserPreferences.Remove(preference);
            await _context.SaveChangesAsync();
        }

        private async Task AddLookToCollectionInternalAsync(Guid userId, Guid lookId, AddLookToCollectionRequest request)
        {
            var lookExists = await _context.Looks.AnyAsync(look => look.Id == lookId && look.UserId == userId);
            if (!lookExists)
            {
                throw new InvalidOperationException("Look nao encontrado para colecao.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new InvalidOperationException("Informe o nome da colecao.");
            }

            var normalizedLabel = request.Name.Trim();
            var collectionId = NormalizeCollectionId(normalizedLabel);
            if (string.IsNullOrWhiteSpace(collectionId))
            {
                throw new InvalidOperationException("Nome da colecao invalido.");
            }

            var collectionPreferenceKey = $"{LookCollectionKeyPrefix}{collectionId}";
            var labelPreferenceKey = $"{LookCollectionLabelPrefix}{collectionId}";
            var lookIdValue = lookId.ToString();

            var existingMembership = await _context.UserPreferences.AnyAsync(preference =>
                preference.UserId == userId
                && preference.PreferenceKey == collectionPreferenceKey
                && preference.PreferenceValue == lookIdValue);

            if (!existingMembership)
            {
                _context.UserPreferences.Add(new UserPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PreferenceKey = collectionPreferenceKey,
                    PreferenceValue = lookIdValue,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            var labelPreference = await _context.UserPreferences.FirstOrDefaultAsync(preference =>
                preference.UserId == userId
                && preference.PreferenceKey == labelPreferenceKey);

            if (labelPreference == null)
            {
                _context.UserPreferences.Add(new UserPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PreferenceKey = labelPreferenceKey,
                    PreferenceValue = normalizedLabel,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                labelPreference.PreferenceValue = normalizedLabel;
                labelPreference.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private async Task RemoveLookFromCollectionInternalAsync(Guid userId, Guid lookId, string collectionId)
        {
            var normalizedCollectionId = NormalizeCollectionId(collectionId);
            if (string.IsNullOrWhiteSpace(normalizedCollectionId))
            {
                return;
            }

            var membershipPreferenceKey = $"{LookCollectionKeyPrefix}{normalizedCollectionId}";
            var labelPreferenceKey = $"{LookCollectionLabelPrefix}{normalizedCollectionId}";
            var lookIdValue = lookId.ToString();

            var membership = await _context.UserPreferences.FirstOrDefaultAsync(preference =>
                preference.UserId == userId
                && preference.PreferenceKey == membershipPreferenceKey
                && preference.PreferenceValue == lookIdValue);

            if (membership != null)
            {
                _context.UserPreferences.Remove(membership);
            }

            var hasRemainingMembership = await _context.UserPreferences.AnyAsync(preference =>
                preference.UserId == userId
                && preference.PreferenceKey == membershipPreferenceKey
                && preference.PreferenceValue != lookIdValue);

            if (!hasRemainingMembership)
            {
                var labelPreference = await _context.UserPreferences.FirstOrDefaultAsync(preference =>
                    preference.UserId == userId
                    && preference.PreferenceKey == labelPreferenceKey);

                if (labelPreference != null)
                {
                    _context.UserPreferences.Remove(labelPreference);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task RenameLookCollectionInternalAsync(Guid userId, string collectionId, RenameLookCollectionRequest request)
        {
            var sourceCollectionId = NormalizeCollectionId(collectionId);
            if (string.IsNullOrWhiteSpace(sourceCollectionId))
            {
                throw new InvalidOperationException("Colecao invalida para renomear.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new InvalidOperationException("Informe o novo nome da colecao.");
            }

            var normalizedLabel = request.Name.Trim();
            var targetCollectionId = NormalizeCollectionId(normalizedLabel);
            if (string.IsNullOrWhiteSpace(targetCollectionId))
            {
                throw new InvalidOperationException("Novo nome da colecao invalido.");
            }

            var sourceMembershipKey = $"{LookCollectionKeyPrefix}{sourceCollectionId}";
            var sourceLabelKey = $"{LookCollectionLabelPrefix}{sourceCollectionId}";
            var sourceMemberships = await _context.UserPreferences
                .Where(preference => preference.UserId == userId && preference.PreferenceKey == sourceMembershipKey)
                .ToListAsync();

            if (sourceMemberships.Count == 0)
            {
                throw new InvalidOperationException("Colecao nao encontrada para renomear.");
            }

            if (sourceCollectionId == targetCollectionId)
            {
                var sameLabelPreference = await _context.UserPreferences.FirstOrDefaultAsync(preference =>
                    preference.UserId == userId && preference.PreferenceKey == sourceLabelKey);

                if (sameLabelPreference == null)
                {
                    _context.UserPreferences.Add(new UserPreference
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        PreferenceKey = sourceLabelKey,
                        PreferenceValue = normalizedLabel,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    sameLabelPreference.PreferenceValue = normalizedLabel;
                    sameLabelPreference.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return;
            }

            var targetMembershipKey = $"{LookCollectionKeyPrefix}{targetCollectionId}";
            var targetLabelKey = $"{LookCollectionLabelPrefix}{targetCollectionId}";
            var existingTargetMembershipValues = await _context.UserPreferences
                .Where(preference => preference.UserId == userId && preference.PreferenceKey == targetMembershipKey)
                .Select(preference => preference.PreferenceValue)
                .ToListAsync();

            var existingTargetMembershipSet = existingTargetMembershipValues.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var membership in sourceMemberships)
            {
                if (existingTargetMembershipSet.Contains(membership.PreferenceValue))
                {
                    _context.UserPreferences.Remove(membership);
                    continue;
                }

                membership.PreferenceKey = targetMembershipKey;
                membership.UpdatedAt = DateTime.UtcNow;
                existingTargetMembershipSet.Add(membership.PreferenceValue);
            }

            var sourceLabelPreference = await _context.UserPreferences.FirstOrDefaultAsync(preference =>
                preference.UserId == userId && preference.PreferenceKey == sourceLabelKey);
            if (sourceLabelPreference != null)
            {
                _context.UserPreferences.Remove(sourceLabelPreference);
            }

            var targetLabelPreference = await _context.UserPreferences.FirstOrDefaultAsync(preference =>
                preference.UserId == userId && preference.PreferenceKey == targetLabelKey);

            if (targetLabelPreference == null)
            {
                _context.UserPreferences.Add(new UserPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PreferenceKey = targetLabelKey,
                    PreferenceValue = normalizedLabel,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                targetLabelPreference.PreferenceValue = normalizedLabel;
                targetLabelPreference.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private async Task DeleteLookCollectionInternalAsync(Guid userId, string collectionId)
        {
            var normalizedCollectionId = NormalizeCollectionId(collectionId);
            if (string.IsNullOrWhiteSpace(normalizedCollectionId))
            {
                throw new InvalidOperationException("Colecao invalida para excluir.");
            }

            var membershipPreferenceKey = $"{LookCollectionKeyPrefix}{normalizedCollectionId}";
            var labelPreferenceKey = $"{LookCollectionLabelPrefix}{normalizedCollectionId}";

            var preferences = await _context.UserPreferences
                .Where(preference => preference.UserId == userId
                    && (preference.PreferenceKey == membershipPreferenceKey || preference.PreferenceKey == labelPreferenceKey))
                .ToListAsync();

            if (preferences.Count == 0)
            {
                throw new InvalidOperationException("Colecao nao encontrada para excluir.");
            }

            _context.UserPreferences.RemoveRange(preferences);
            await _context.SaveChangesAsync();
        }

        private async Task SavePreviewHistoryAsync(
            Guid userId,
            Guid avatarId,
            GenerateTryOnPreviewRequest request,
            string mode,
            string imageUrl,
            bool usedAi,
            List<Clothing> selectedProducts)
        {
            var history = new TryOnPreviewHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AvatarId = avatarId,
                Style = string.IsNullOrWhiteSpace(request.Style) ? "StyleScan" : request.Style.Trim(),
                Occasion = string.IsNullOrWhiteSpace(request.Occasion) ? "casual" : request.Occasion.Trim(),
                BoardId = string.IsNullOrWhiteSpace(request.BoardId) ? null : request.BoardId.Trim(),
                Mode = mode,
                ImageUrl = imageUrl,
                UsedAi = usedAi,
                ProductNames = selectedProducts.Select(product => product.Name).ToArray(),
                ProductCategories = selectedProducts.Select(product => product.Category).ToArray(),
                CreatedAt = DateTime.UtcNow
            };

            _context.TryOnPreviewHistories.Add(history);

            var staleItems = await _context.TryOnPreviewHistories
                .Where(existingHistory => existingHistory.UserId == userId && existingHistory.AvatarId == avatarId)
                .OrderByDescending(existingHistory => existingHistory.CreatedAt)
                .Skip(18)
                .ToListAsync();

            if (staleItems.Count > 0)
            {
                _context.TryOnPreviewHistories.RemoveRange(staleItems);
            }
        }

        private async Task<List<Clothing>> GetCatalogAsync()
        {
            var catalog = await _context.Clothings
                .Include(clothing => clothing.Store)
                .Where(clothing => clothing.InStock)
                .ToListAsync();

            if (catalog.Count == 0)
            {
                throw new InvalidOperationException("Nao ha produtos disponiveis no catalogo para gerar looks.");
            }

            return catalog;
        }

        private static Look BuildLookEntity(
            Guid userId,
            Guid avatarId,
            string name,
            string occasion,
            string style,
            string season,
            List<Clothing> items,
            string? note = null,
            List<string>? occasionTags = null,
            string? heroImageUrl = null,
            string? heroPreviewMode = null)
        {
            var lookId = Guid.NewGuid();
            return new Look
            {
                Id = lookId,
                UserId = userId,
                AvatarId = avatarId,
                Name = name,
                Occasion = occasion,
                Style = style,
                Season = season,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                OccasionTags = occasionTags?
                    .Where(tag => !string.IsNullOrWhiteSpace(tag))
                    .Select(tag => tag.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(6)
                    .ToArray() ?? Array.Empty<string>(),
                HeroImageUrl = string.IsNullOrWhiteSpace(heroImageUrl) ? null : heroImageUrl.Trim(),
                HeroPreviewMode = string.IsNullOrWhiteSpace(heroPreviewMode) ? null : heroPreviewMode.Trim(),
                IsPublished = false,
                TotalPrice = items.Sum(item => item.Price),
                CreatedAt = DateTime.UtcNow,
                LookClothings = items.Select(item => new LookClothing
                {
                    LookId = lookId,
                    ClothingId = item.Id
                }).ToList()
            };
        }

        private static LookResponse MapLookResponse(Look look, List<Clothing> items)
        {
            return new LookResponse
            {
                Id = look.Id,
                AvatarId = look.AvatarId,
                Name = look.Name,
                Occasion = look.Occasion,
                Style = look.Style,
                Season = look.Season,
                Note = look.Note,
                OccasionTags = look.OccasionTags.ToList(),
                HeroImageUrl = look.HeroImageUrl,
                HeroPreviewMode = look.HeroPreviewMode,
                IsPublished = look.IsPublished,
                PublishedAt = look.PublishedAt,
                ShareSlug = look.ShareSlug,
                TotalPrice = look.TotalPrice,
                CreatedAt = look.CreatedAt,
                Items = items.Select(item => new ClothingItemResponse
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
                }).ToList()
            };
        }

        private static List<Clothing> BuildLookItems(List<Clothing> catalog, GenerateLooksRequest request)
        {
            var normalizedOccasion = request.Occasion.Trim().ToLowerInvariant();
            var normalizedStyle = request.Style.Trim().ToLowerInvariant();
            var preferredColors = request.ColorPreferences?
                .Where(color => !string.IsNullOrWhiteSpace(color))
                .Select(color => color.Trim().ToLowerInvariant())
                .ToHashSet() ?? new HashSet<string>();

            var useDress = normalizedOccasion.Contains("party") || normalizedOccasion.Contains("formal");
            var desiredCategories = useDress
                ? new[] { "dress", "shoes", "accessory" }
                : new[] { "top", "bottom", "shoes" };

            var selected = new List<Clothing>();

            foreach (var category in desiredCategories)
            {
                var item = catalog
                    .Where(clothing => clothing.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(clothing => ScoreClothing(clothing, normalizedOccasion, normalizedStyle, preferredColors))
                    .ThenBy(clothing => clothing.Price)
                    .FirstOrDefault();

                if (item != null && selected.All(existing => existing.Id != item.Id))
                {
                    selected.Add(item);
                }
            }

            if (selected.Count < 2)
            {
                selected = catalog
                    .OrderByDescending(clothing => ScoreClothing(clothing, normalizedOccasion, normalizedStyle, preferredColors))
                    .ThenBy(clothing => clothing.Price)
                    .Take(3)
                    .ToList();
            }

            return selected;
        }

        private static int ScoreClothing(Clothing clothing, string occasion, string style, HashSet<string> preferredColors)
        {
            var score = 0;
            var category = clothing.Category.ToLowerInvariant();
            var name = clothing.Name.ToLowerInvariant();
            var color = clothing.Color.ToLowerInvariant();

            if (preferredColors.Contains(color)) score += 4;

            if (occasion.Contains("formal"))
            {
                if (category is "dress" or "shoes") score += 3;
                if (name.Contains("blazer") || name.Contains("alfaiataria") || name.Contains("tailored")) score += 3;
            }
            else if (occasion.Contains("party"))
            {
                if (category is "dress" or "accessory" or "shoes") score += 3;
                if (name.Contains("night") || name.Contains("festa") || name.Contains("statement")) score += 2;
            }
            else
            {
                if (category is "top" or "bottom" or "shoes") score += 2;
                if (name.Contains("casual") || name.Contains("essential")) score += 2;
            }

            if (style.Contains("minimal"))
            {
                if (color is "white" or "black" or "beige") score += 3;
                if (name.Contains("essential")) score += 2;
            }
            else if (style.Contains("classic"))
            {
                if (name.Contains("classic") || name.Contains("tailored")) score += 3;
            }
            else if (style.Contains("trendy"))
            {
                if (name.Contains("trend") || name.Contains("statement")) score += 3;
            }

            if (clothing.Rating >= 4.5) score += 2;
            return score;
        }

        private static string BuildLookName(GenerateLooksRequest request, Avatar avatar)
        {
            var avatarName = string.IsNullOrWhiteSpace(avatar.Name) ? "StyleScan" : avatar.Name.Trim();
            return $"{avatarName} • {request.Occasion.Trim()} {request.Style.Trim()}";
        }

        private static string BuildLookName(SaveCustomLookRequest request, Avatar avatar)
        {
            var avatarName = string.IsNullOrWhiteSpace(avatar.Name) ? "StyleScan" : avatar.Name.Trim();
            return $"{avatarName} • {request.Style.Trim()}";
        }

        private static TryOnPreviewHistoryResponse MapPreviewHistoryResponse(TryOnPreviewHistory history)
        {
            return new TryOnPreviewHistoryResponse
            {
                Id = history.Id,
                AvatarId = history.AvatarId,
                Style = history.Style,
                Occasion = history.Occasion,
                BoardId = history.BoardId,
                Mode = history.Mode,
                ImageUrl = history.ImageUrl,
                UsedAi = history.UsedAi,
                ProductNames = history.ProductNames.ToList(),
                ProductCategories = history.ProductCategories.ToList(),
                CreatedAt = history.CreatedAt
            };
        }

        private async Task<string?> TryGenerateTryOnWithOpenAiAsync(Avatar avatar, List<Clothing> products, GenerateTryOnPreviewRequest request, string mode)
        {
            var apiKey = _configuration["ExternalApis:OpenAI:ApiKey"]?.Trim();
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("your-openai-api-key", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var references = await LoadAvatarAndProductReferencesAsync(avatar, products, mode);
            if (references.Count < 2)
            {
                return null;
            }

            var baseUrl = _configuration["ExternalApis:OpenAI:BaseUrl"]?.Trim().TrimEnd('/') ?? "https://api.openai.com/v1";
            var model = _configuration["ExternalApis:OpenAI:Model"]?.Trim();
            if (string.IsNullOrWhiteSpace(model))
            {
                model = "gpt-image-1-mini";
            }

            var payload = new JsonObject
            {
                ["model"] = model,
                ["prompt"] = BuildTryOnPrompt(avatar, products, request, mode, references.Count),
                ["size"] = "1024x1536",
                ["quality"] = "medium",
                ["output_format"] = "png",
                ["background"] = "transparent",
                ["images"] = new JsonArray(references.Select(reference =>
                    (JsonNode)new JsonObject
                    {
                        ["image_url"] = reference
                    }).ToArray())
            };

            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/images/edits")
                {
                    Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json")
                };

                httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
                var client = _httpClientFactory.CreateClient();
                using var response = await client.SendAsync(httpRequest);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var root = JsonNode.Parse(responseContent);
                var imageBase64 = root?["data"]?
                    .AsArray()
                    .FirstOrDefault()?["b64_json"]?
                    .GetValue<string>();

                if (string.IsNullOrWhiteSpace(imageBase64))
                {
                    return null;
                }

                return await SaveTryOnPngAsync(avatar.Id, imageBase64);
            }
            catch
            {
                return null;
            }
        }

        private async Task<List<string>> LoadAvatarAndProductReferencesAsync(Avatar avatar, List<Clothing> products, string mode)
        {
            var references = new List<string>();
            var normalizedMode = string.IsNullOrWhiteSpace(mode) ? "avatar" : mode.Trim().ToLowerInvariant();
            var avatarPhotoLimit = normalizedMode == "realistic" ? 3 : 2;
            var avatarPhotos = avatar.PhotoUrls?.Where(url => !string.IsNullOrWhiteSpace(url)).Take(avatarPhotoLimit).ToList() ?? new List<string>();

            if (normalizedMode == "avatar" && !string.IsNullOrWhiteSpace(avatar.GeneratedAvatarImageUrl))
            {
                var generatedAvatar = await ResolveReferenceImageAsync(avatar.GeneratedAvatarImageUrl);
                if (!string.IsNullOrWhiteSpace(generatedAvatar))
                {
                    references.Add(generatedAvatar);
                }
            }

            if (avatarPhotos.Count == 0 && !string.IsNullOrWhiteSpace(avatar.PhotoUrl))
            {
                avatarPhotos.Add(avatar.PhotoUrl);
            }

            foreach (var photo in avatarPhotos)
            {
                var encoded = await ResolveReferenceImageAsync(photo);
                if (!string.IsNullOrWhiteSpace(encoded))
                {
                    references.Add(encoded);
                }
            }

            foreach (var product in products.Take(4))
            {
                var encoded = await ResolveReferenceImageAsync(product.ImageUrl);
                if (!string.IsNullOrWhiteSpace(encoded))
                {
                    references.Add(encoded);
                }
            }

            return references;
        }

        private async Task<string?> ResolveReferenceImageAsync(string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return null;
            }

            if (source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return source;
            }

            try
            {
                if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    var client = _httpClientFactory.CreateClient();
                    var bytes = await client.GetByteArrayAsync(source);
                    var extension = Path.GetExtension(source).ToLowerInvariant();
                    var mimeType = extension switch
                    {
                        ".jpg" => "image/jpeg",
                        ".jpeg" => "image/jpeg",
                        ".png" => "image/png",
                        ".webp" => "image/webp",
                        _ => "image/jpeg"
                    };
                    return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
                }

                var relativePath = source.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var filePath = Path.Combine(GetWebRootPath(), relativePath);
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var bytesFromDisk = await File.ReadAllBytesAsync(filePath);
                var extensionFromDisk = Path.GetExtension(filePath).ToLowerInvariant();
                var diskMimeType = extensionFromDisk switch
                {
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    ".svg" => "image/svg+xml",
                    _ => string.Empty
                };

                return string.IsNullOrWhiteSpace(diskMimeType)
                    ? null
                    : $"data:{diskMimeType};base64,{Convert.ToBase64String(bytesFromDisk)}";
            }
            catch
            {
                return null;
            }
        }

        private static string BuildTryOnPrompt(Avatar avatar, List<Clothing> products, GenerateTryOnPreviewRequest request, string mode, int referenceCount)
        {
            var productList = string.Join(", ", products.Select(product => $"{product.Category}: {product.Name} ({product.Color})"));
            var normalizedMode = string.IsNullOrWhiteSpace(mode) ? "avatar" : mode.Trim().ToLowerInvariant();
            var style = string.IsNullOrWhiteSpace(request.Style) ? "casual editorial" : request.Style.Trim();
            var occasion = string.IsNullOrWhiteSpace(request.Occasion) ? "casual" : request.Occasion.Trim();
            var boardId = string.IsNullOrWhiteSpace(request.BoardId) ? string.Empty : request.BoardId.Trim().ToLowerInvariant();
            var palette = request.Palette != null && request.Palette.Count > 0
                ? string.Join(", ", request.Palette.Where(color => !string.IsNullOrWhiteSpace(color)).Take(4))
                : "soft neutrals";
            var garmentDirectives = BuildGarmentDirectives(products);
            var boardDirectives = BuildBoardDirection(boardId, style, occasion, palette);
            var referenceDirectives = BuildReferenceDirectives(referenceCount, normalizedMode);
            var compositionDirectives = BuildCompositionDirectives(products);
            var silhouetteDirectives = BuildSilhouetteDirectives(avatar, normalizedMode);
            var fitAnchors = BuildFitAnchors(avatar, products);

            if (normalizedMode == "realistic")
            {
                return $"""
Create a photorealistic full-body fashion image using the real user photos as the person reference.

Identity rules:
- Use the real person from the uploaded photos, not an illustrated avatar.
- Preserve the same face, hair, skin tone, body proportions, silhouette, and natural appearance from the user photos.
- Do not beautify, slim down, bulk up, or stylize the person away from the real photos.
- Keep the body reading natural, moderate, and respectful.
- Keep the person recognizable as the same individual in the references.
- {referenceDirectives}
- {silhouetteDirectives}

Wardrobe rules:
- Dress the same person using the selected products as the wardrobe references.
- Selected pieces: {productList}
- Style direction: {style}
- Occasion: {occasion}
- Reference palette: {palette}
- Match the chosen garments as closely as possible in category, color, fit, layering, and overall visual impression.
- Respect the intended visual direction of the outfit without turning it into costume or editorial fantasy.
- {boardDirectives}
- {garmentDirectives}
- {compositionDirectives}
- {fitAnchors}

Composition rules:
- Full body visible from head to toe.
- Front-facing fashion photo, relaxed stance, realistic lighting.
- Clean studio-style or neutral indoor background.
- Single person only, no text, no watermark, no duplicate limbs, no extra props.
- Final result should look like a realistic outfit photo for a shopping try-on.
""";
            }

            return $"""
Create a highly faithful full-body virtual try-on image using the same person from the avatar reference photos.

Identity rules:
- Preserve the exact same identity from the reference photos.
- Match the same face, hairstyle, hairline, hair color, skin tone, shoulder width, torso shape, waist, hips, leg proportions, and overall silhouette.
- Do not beautify, slim down, bulk up, age up, age down, or stylize the person away from the reference.
- Keep the body reading natural and moderate.
- Keep the face and body recognizably consistent with the source person.
- {referenceDirectives}
- {silhouetteDirectives}

Wardrobe rules:
- Dress the same person using the selected clothing items as the wardrobe reference.
- Selected pieces: {productList}
- Style direction: {style}
- Occasion: {occasion}
- Reference palette: {palette}
- The final outfit must clearly resemble the selected products in category, color, fit, and overall visual language.
- If any garment image is incomplete, infer missing portions conservatively and keep the result realistic.
- Do not invent additional garments, logos, props, jewelry, bags, hats, or accessories unless they are explicitly among the selected products.
- {boardDirectives}
- {garmentDirectives}
- {compositionDirectives}
- {fitAnchors}

Composition rules:
- Full body visible from head to toe.
- Front-facing stance, arms relaxed, natural posture.
- The outfit must be cleanly visible and centered for a mobile app try-on experience.
- Use a light studio or transparent background.
- Single person only, no collage, no duplicated limbs, no extra people, no text, no watermarks.
- Output should look like a polished virtual fitting preview, not an illustration or fashion sketch.
""";
        }

        private static string BuildBoardDirection(string boardId, string style, string occasion, string palette)
        {
            return boardId switch
            {
                "winter" => $"Keep the image refined, layered, and softly structured, with a cooler and more polished mood. Use the palette {palette} as tonal guidance.",
                "social" => $"Keep the silhouette aligned, formal, and clean, with a polished presence suitable for {occasion}. Use the palette {palette} conservatively.",
                "smart-casual" => $"Balance relaxed and tailored elements so the result feels elevated but approachable. Use the palette {palette} for a modern smart-casual reading.",
                "emo" => $"Keep the mood darker and more expressive, with attitude and contrast, but still faithful to the real selected products and the person. Use the palette {palette} to guide tone.",
                _ => $"Keep the result wearable, contemporary, and natural for a {style} outfit. Use the palette {palette} to guide the overall mood."
            };
        }

        private static string BuildGarmentDirectives(List<Clothing> products)
        {
            var hasDress = products.Any(product => product.Category.Equals("dress", StringComparison.OrdinalIgnoreCase));
            var hasTop = products.Any(product => product.Category.Equals("top", StringComparison.OrdinalIgnoreCase));
            var hasBottom = products.Any(product => product.Category.Equals("bottom", StringComparison.OrdinalIgnoreCase));
            var hasShoes = products.Any(product => product.Category.Equals("shoes", StringComparison.OrdinalIgnoreCase));
            var hasAccessory = products.Any(product => product.Category.Equals("accessory", StringComparison.OrdinalIgnoreCase));

            var directives = new List<string>();

            if (hasDress)
            {
                directives.Add("Treat the dress as the dominant garment and avoid adding separate top or bottom layers unless clearly required by the references.");
            }

            if (hasTop && hasBottom)
            {
                directives.Add("Keep the top and bottom clearly separated and believable in their layering, waistline, and proportions.");
            }

            if (hasShoes)
            {
                directives.Add("Ensure the footwear is visible and proportionate near the feet, not cropped away or replaced.");
            }

            if (hasAccessory)
            {
                directives.Add("Only include the selected accessory if it remains subtle and clearly belongs to the chosen outfit.");
            }

            if (directives.Count == 0)
            {
                directives.Add("Keep the selected garments accurate and avoid inventing extra fashion elements.");
            }

            return string.Join(" ", directives);
        }

        private static string BuildReferenceDirectives(int referenceCount, string mode)
        {
            if (referenceCount >= 5)
            {
                return "Use the multiple reference images to keep identity, body proportions, and garment placement consistent across likely different angles.";
            }

            if (referenceCount >= 3)
            {
                return mode == "realistic"
                    ? "Use the available reference images to keep the same face, hair, and silhouette coherent, even if the angles differ."
                    : "Use the available references to preserve a consistent identity and body reading in the try-on result.";
            }

            return "Be conservative with any inference not fully visible in the references, and prefer identity fidelity over stylization.";
        }

        private static string BuildCompositionDirectives(List<Clothing> products)
        {
            var hasStructuredUpper = products.Any(product =>
                product.Category.Equals("top", StringComparison.OrdinalIgnoreCase) &&
                (product.Name.Contains("shirt", StringComparison.OrdinalIgnoreCase) ||
                 product.Name.Contains("blazer", StringComparison.OrdinalIgnoreCase) ||
                 product.Name.Contains("classic", StringComparison.OrdinalIgnoreCase)));

            var hasDenimOrRelaxedBottom = products.Any(product =>
                product.Category.Equals("bottom", StringComparison.OrdinalIgnoreCase) &&
                (product.Name.Contains("denim", StringComparison.OrdinalIgnoreCase) ||
                 product.Description.Contains("casual", StringComparison.OrdinalIgnoreCase) ||
                 product.Description.Contains("reto", StringComparison.OrdinalIgnoreCase)));

            var notes = new List<string>();

            if (hasStructuredUpper)
            {
                notes.Add("Keep the upper silhouette cleaner and more structured around shoulders and torso.");
            }

            if (hasDenimOrRelaxedBottom)
            {
                notes.Add("Keep the lower silhouette more natural and wearable, without over-tailoring the leg shape.");
            }

            if (products.Count >= 3)
            {
                notes.Add("Show enough spacing and visual clarity so each selected category still reads as part of one coherent outfit.");
            }

            return notes.Count == 0
                ? "Keep the outfit readable, balanced, and faithful to the selected pieces."
                : string.Join(" ", notes);
        }

        private static string BuildSilhouetteDirectives(Avatar avatar, string mode)
        {
            var notes = new List<string>();

            if (avatar.Chest > 0 && avatar.Waist > 0)
            {
                var chestToWaistDelta = avatar.Chest - avatar.Waist;
                if (chestToWaistDelta >= 10)
                {
                    notes.Add("Maintain a moderately tapered torso and avoid widening the waist or making the chest blocky.");
                }
                else
                {
                    notes.Add("Keep the torso straight and moderate, with no extra bulk added through the ribcage or abdomen.");
                }
            }

            if (avatar.Height > 0 && avatar.Weight > 0)
            {
                var bmi = avatar.Weight / Math.Pow(avatar.Height / 100d, 2);
                if (bmi < 26)
                {
                    notes.Add("Overall body mass should stay lean-to-moderate and never puffier than the references.");
                }
                else
                {
                    notes.Add("Keep the body softly built but controlled, without extra fullness in the face, arms, or waist.");
                }
            }

            if (mode == "avatar" && !string.IsNullOrWhiteSpace(avatar.GeneratedAvatarImageUrl))
            {
                notes.Add("Use the generated avatar silhouette as the primary body-shape anchor, then keep face and proportions aligned with the original photos.");
            }

            return notes.Count == 0
                ? "Keep the silhouette faithful, moderate, and free of extra body mass."
                : string.Join(" ", notes);
        }

        private static string BuildFitAnchors(Avatar avatar, List<Clothing> products)
        {
            var notes = new List<string>();

            if (products.Any(product => product.Category.Equals("top", StringComparison.OrdinalIgnoreCase)))
            {
                notes.Add("Tops should sit naturally on the shoulders and chest, without inflating the torso.");
            }

            if (products.Any(product => product.Category.Equals("bottom", StringComparison.OrdinalIgnoreCase)))
            {
                notes.Add("Bottoms should meet the real waist position and hip width, without lowering or widening the midsection.");
            }

            if (products.Any(product => product.Category.Equals("dress", StringComparison.OrdinalIgnoreCase)))
            {
                notes.Add("If the outfit uses a dress, keep the dress line faithful to the person's true torso and hip shape instead of smoothing the body into a mannequin.");
            }

            if (products.Any(product => product.Category.Equals("shoes", StringComparison.OrdinalIgnoreCase)))
            {
                notes.Add("Footwear should remain proportionate to the legs and feet, not oversized or cropped.");
            }

            if (avatar.Waist > 0)
            {
                notes.Add($"Use the waist anchor of about {avatar.Waist:0.#} cm only to preserve proportion, not to exaggerate softness.");
            }

            return notes.Count == 0
                ? "Keep garment fit natural and proportional to the real body."
                : string.Join(" ", notes);
        }

        private async Task<string> SaveTryOnPngAsync(Guid avatarId, string imageBase64)
        {
            var generatedDirectory = Path.Combine(EnsureLooksPreviewDirectory(), "generated");
            Directory.CreateDirectory(generatedDirectory);

            var fileName = $"try-on-{avatarId:N}-{DateTime.UtcNow:yyyyMMddHHmmss}.png";
            var filePath = Path.Combine(generatedDirectory, fileName);
            var bytes = Convert.FromBase64String(imageBase64);
            await File.WriteAllBytesAsync(filePath, bytes);

            return $"/uploads/looks/generated/{fileName}";
        }

        private async Task<string> CreateLocalTryOnPreviewAsync(Avatar avatar, List<Clothing> products, string style)
        {
            var generatedDirectory = Path.Combine(EnsureLooksPreviewDirectory(), "generated");
            Directory.CreateDirectory(generatedDirectory);

            var fileName = $"try-on-fallback-{avatar.Id:N}.svg";
            var filePath = Path.Combine(generatedDirectory, fileName);
            var productCards = string.Join(
                Environment.NewLine,
                products.Take(3).Select((product, index) =>
                    $@"<g transform=""translate({56 + (index * 134)} 438)"">
  <rect width=""110"" height=""126"" rx=""24"" fill=""#ffffff"" fill-opacity=""0.92""/>
  <image href=""{product.ImageUrl}"" x=""12"" y=""12"" width=""86"" height=""70"" preserveAspectRatio=""xMidYMid slice"" />
  <text x=""55"" y=""96"" text-anchor=""middle"" fill=""#203540"" font-family=""Segoe UI, sans-serif"" font-size=""11"" font-weight=""700"">{EscapeSvgText(product.Name)}</text>
  <text x=""55"" y=""112"" text-anchor=""middle"" fill=""#60737D"" font-family=""Segoe UI, sans-serif"" font-size=""10"">{EscapeSvgText(product.Category)}</text>
</g>"));

            var heroImage = avatar.GeneratedAvatarImageUrl ?? avatar.PhotoUrl ?? avatar.PhotoUrls.FirstOrDefault();
            var heroTag = string.IsNullOrWhiteSpace(heroImage)
                ? string.Empty
                : $@"<image href=""../..{heroImage}"" x=""74"" y=""56"" width=""292"" height=""338"" preserveAspectRatio=""xMidYMid meet"" />";

            var svg = $"""
<svg width="440" height="640" viewBox="0 0 440 640" fill="none" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="bg" x1="38" y1="26" x2="392" y2="610" gradientUnits="userSpaceOnUse">
      <stop stop-color="#F8FBFC"/>
      <stop offset="1" stop-color="#EAEDE8"/>
    </linearGradient>
  </defs>
  <rect x="18" y="18" width="404" height="604" rx="36" fill="url(#bg)"/>
  <rect x="38" y="38" width="364" height="564" rx="30" fill="#FFFFFF" fill-opacity="0.82" stroke="#D7E5EA"/>
  <text x="54" y="78" fill="#203540" font-family="Segoe UI, sans-serif" font-size="16" font-weight="700">Preview de provador</text>
  <text x="54" y="102" fill="#647883" font-family="Segoe UI, sans-serif" font-size="12">{EscapeSvgText(style)}</text>
  <rect x="54" y="124" width="332" height="280" rx="28" fill="#F0F4F5"/>
  {heroTag}
  <rect x="54" y="420" width="332" height="160" rx="30" fill="#EEF3F4"/>
  {productCards}
</svg>
""";

            await File.WriteAllTextAsync(filePath, svg);
            return $"/uploads/looks/generated/{fileName}";
        }

        private string EnsureLooksPreviewDirectory()
        {
            var previewDirectory = Path.Combine(GetWebRootPath(), "uploads", "looks");
            Directory.CreateDirectory(previewDirectory);
            return previewDirectory;
        }

        private string GetWebRootPath()
        {
            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            return webRootPath;
        }

        private static string EscapeSvgText(string? value)
        {
            return (value ?? string.Empty)
                .Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal);
        }

        private async Task EnsureSavedLookCapacityAsync(User user)
        {
            var plan = AccountPlanCatalog.Resolve(user.AccountPlan);
            var currentLooks = await _context.Looks.CountAsync(look => look.UserId == user.Id);
            if (currentLooks >= plan.SavedLooks)
            {
                throw new InvalidOperationException($"Seu plano {plan.Id} permite salvar ate {FormatLimit(plan.SavedLooks)} looks.");
            }
        }

        private async Task EnsureUsageLimitAsync(Guid userId, string metricType, int limit, string message)
        {
            var now = DateTime.UtcNow;
            var periodKey = AccountPlanCatalog.GetPeriodKeyForMetric(metricType, now);
            var currentUsage = await _context.UserUsageRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(record => record.UserId == userId && record.MetricType == metricType && record.PeriodKey == periodKey);

            if ((currentUsage?.Used ?? 0) >= limit)
            {
                throw new InvalidOperationException(message);
            }
        }

        private async Task IncrementUsageAsync(Guid userId, string metricType, DateTime now)
        {
            var periodKey = AccountPlanCatalog.GetPeriodKeyForMetric(metricType, now);
            var record = await _context.UserUsageRecords
                .FirstOrDefaultAsync(existingRecord => existingRecord.UserId == userId && existingRecord.MetricType == metricType && existingRecord.PeriodKey == periodKey);

            if (record == null)
            {
                record = new UserUsageRecord
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    MetricType = metricType,
                    PeriodKey = periodKey,
                    Used = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.UserUsageRecords.Add(record);
            }

            record.Used += 1;
            record.UpdatedAt = now;
        }

        private static string FormatLimit(int limit)
        {
            return limit >= 9999 ? "ilimitados" : limit.ToString();
        }

        private static string NormalizeCollectionId(string value)
        {
            return value.Trim().ToLowerInvariant().Replace(" ", "-");
        }

        private static string BuildShareSlug(string value, Guid lookId)
        {
            var baseSlug = NormalizeCollectionId(value);
            var cleaned = new string(baseSlug.Where(character => char.IsLetterOrDigit(character) || character == '-').ToArray()).Trim('-');
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                cleaned = "look";
            }

            return $"{cleaned}-{lookId.ToString("N")[..8]}";
        }

        private static string BuildProfileSlug(string firstName, string lastName, Guid userId)
        {
            var baseSlug = NormalizeCollectionId($"{firstName}-{lastName}");
            var cleaned = new string(baseSlug.Where(character => char.IsLetterOrDigit(character) || character == '-').ToArray()).Trim('-');
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                cleaned = "stylescan-user";
            }

            return $"{cleaned}-{userId.ToString("N")[..8]}";
        }
    }
}
