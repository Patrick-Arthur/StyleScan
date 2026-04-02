using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StyleScan.Backend.Data;
using StyleScan.Backend.Models.Domain;
using StyleScan.Backend.Models.DTOs.User;
using StyleScan.Backend.Services.Interfaces;
using StyleScan.Backend.Services.Support;
using System.Security.Claims;

namespace StyleScan.Backend.Controllers
{
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly StyleScanDbContext _context;
        private readonly IMercadoPagoService _mercadoPagoService;

        public UserController(StyleScanDbContext context, IMercadoPagoService mercadoPagoService)
        {
            _context = context;
            _mercadoPagoService = mercadoPagoService;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileResponse>> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(existingUser => existingUser.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "Usuario nao encontrado." });
            }

            EnsurePublicProfileSlug(user);
            await _context.SaveChangesAsync();

            var usage = await GetUsageResponsesAsync(user.Id);
            return Ok(MapToProfileResponse(user, usage));
        }

        [HttpGet("subscription/status")]
        public async Task<ActionResult<SubscriptionSummaryResponse>> GetSubscriptionStatus()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(existingUser => existingUser.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "Usuario nao encontrado." });
            }

            return Ok(MapSubscriptionSummary(user));
        }

        [HttpGet("gamification")]
        public async Task<ActionResult<GamificationSummaryResponse>> GetGamificationSummary()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(existingUser => existingUser.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "Usuario nao encontrado." });
            }

            return Ok(await BuildGamificationSummaryAsync(user.Id));
        }

        [HttpPut("profile")]
        public async Task<ActionResult<UserProfileResponse>> UpdateProfile([FromBody] UpdateUserProfileRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FirstOrDefaultAsync(existingUser => existingUser.Id == userId);
            if (user == null)
            {
                return NotFound(new { message = "Usuario nao encontrado." });
            }

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.DateOfBirth = request.DateOfBirth;
            user.Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var usage = await GetUsageResponsesAsync(user.Id);
            return Ok(MapToProfileResponse(user, usage));
        }

        [HttpPut("plan")]
        public async Task<ActionResult<UserProfileResponse>> UpdatePlan([FromBody] UpdateAccountPlanRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var normalizedPlan = request.PlanId.Trim().ToLowerInvariant();
            if (!AccountPlanCatalog.IsValid(normalizedPlan))
            {
                return BadRequest(new { message = "Plano informado nao e valido." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(existingUser => existingUser.Id == userId);
            if (user == null)
            {
                return NotFound(new { message = "Usuario nao encontrado." });
            }

            user.AccountPlan = normalizedPlan;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var usage = await GetUsageResponsesAsync(user.Id);
            return Ok(MapToProfileResponse(user, usage));
        }

        [HttpPost("subscription/checkout")]
        public async Task<ActionResult<SubscriptionCheckoutResponse>> CreateSubscriptionCheckout([FromBody] CreateSubscriptionCheckoutRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var normalizedPlan = request.PlanId.Trim().ToLowerInvariant();
            if (!AccountPlanCatalog.IsValid(normalizedPlan) || normalizedPlan == AccountPlanType.Free)
            {
                return BadRequest(new { message = "Plano informado nao e valido para assinatura." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(existingUser => existingUser.Id == userId);
            if (user == null)
            {
                return NotFound(new { message = "Usuario nao encontrado." });
            }

            var now = DateTime.UtcNow;
            var checkoutId = $"ssc_{Guid.NewGuid():N}";
            user.SubscriptionStatus = "pending";
            user.SubscriptionProvider = "mercado-pago";
            user.SubscriptionReference = checkoutId;
            user.PendingAccountPlan = normalizedPlan;
            user.LastPaymentId = null;
            user.LastPaymentStatus = "pending";
            user.LastPaymentStatusDetail = "checkout_created";
            user.LastPaymentUpdatedAt = now;
            user.LastWebhookReceivedAt = null;
            user.UpdatedAt = now;
            await _context.SaveChangesAsync();

            try
            {
                var checkout = await _mercadoPagoService.CreateSubscriptionCheckoutAsync(user, checkoutId, normalizedPlan);
                return Ok(checkout);
            }
            catch (InvalidOperationException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }

        [HttpPost("subscription/activate")]
        public async Task<ActionResult<UserProfileResponse>> ActivateSubscription([FromBody] ConfirmSubscriptionRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var normalizedPlan = request.PlanId.Trim().ToLowerInvariant();
            if (!AccountPlanCatalog.IsValid(normalizedPlan) || normalizedPlan == AccountPlanType.Free)
            {
                return BadRequest(new { message = "Plano informado nao e valido para assinatura." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(existingUser => existingUser.Id == userId);
            if (user == null)
            {
                return NotFound(new { message = "Usuario nao encontrado." });
            }

            var checkoutId = request.CheckoutId.Trim();
            if (string.IsNullOrWhiteSpace(checkoutId) || !string.Equals(user.SubscriptionReference, checkoutId, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Checkout informado nao confere com a assinatura pendente." });
            }

            var now = DateTime.UtcNow;
            user.AccountPlan = normalizedPlan;
            user.PendingAccountPlan = null;
            user.SubscriptionStatus = "active";
            user.SubscriptionProvider = string.IsNullOrWhiteSpace(request.Provider) ? "mercado-pago" : request.Provider.Trim().ToLowerInvariant();
            user.SubscriptionStartedAt = now;
            user.SubscriptionCurrentPeriodEndsAt = now.AddDays(30);
            user.UpdatedAt = now;
            await _context.SaveChangesAsync();

            var usage = await GetUsageResponsesAsync(user.Id);
            return Ok(MapToProfileResponse(user, usage));
        }

        [HttpPost("usage/register")]
        public async Task<ActionResult<UserProfileResponse>> RegisterUsage([FromBody] RegisterUsageRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var normalizedMetric = request.MetricType.Trim().ToLowerInvariant();
            if (!IsSupportedMetric(normalizedMetric))
            {
                return BadRequest(new { message = "Metrica de uso nao suportada." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(existingUser => existingUser.Id == userId);
            if (user == null)
            {
                return NotFound(new { message = "Usuario nao encontrado." });
            }

            var now = DateTime.UtcNow;
            var periodKey = AccountPlanCatalog.GetPeriodKeyForMetric(normalizedMetric, now);
            var usageRecord = await _context.UserUsageRecords.FirstOrDefaultAsync(record =>
                record.UserId == userId &&
                record.MetricType == normalizedMetric &&
                record.PeriodKey == periodKey);

            if (usageRecord == null)
            {
                usageRecord = new UserUsageRecord
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    MetricType = normalizedMetric,
                    PeriodKey = periodKey,
                    Used = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.UserUsageRecords.Add(usageRecord);
            }

            usageRecord.Used += 1;
            usageRecord.UpdatedAt = now;
            user.UpdatedAt = now;
            await _context.SaveChangesAsync();

            var usage = await GetUsageResponsesAsync(user.Id);
            return Ok(MapToProfileResponse(user, usage));
        }

        private async Task<List<AccountPlanUsageResponse>> GetUsageResponsesAsync(Guid userId)
        {
            var now = DateTime.UtcNow;
            var currentWeekKey = AccountPlanCatalog.GetWeekPeriodKey(now);
            var currentMonthKey = AccountPlanCatalog.GetMonthPeriodKey(now);

            var usage = await _context.UserUsageRecords
                .AsNoTracking()
                .Where(record => record.UserId == userId &&
                    (
                        record.PeriodKey == "lifetime" ||
                        record.PeriodKey == currentWeekKey ||
                        record.PeriodKey == currentMonthKey
                    ))
                .ToListAsync();

            return usage
                .OrderBy(record => record.MetricType)
                .Select(record => new AccountPlanUsageResponse
                {
                    MetricType = record.MetricType,
                    PeriodKey = record.PeriodKey,
                    Used = record.Used
                })
                .ToList();
        }

        private static UserProfileResponse MapToProfileResponse(Models.Domain.User user, List<AccountPlanUsageResponse> usage)
        {
            var plan = AccountPlanCatalog.Resolve(user.AccountPlan);
            return new UserProfileResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PublicProfileSlug = user.PublicProfileSlug ?? string.Empty,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                AccountPlan = plan.Id,
                Limits = AccountPlanCatalog.ToResponse(plan),
                Usage = usage,
                Subscription = MapSubscriptionSummary(user)
            };
        }

        private static SubscriptionSummaryResponse MapSubscriptionSummary(Models.Domain.User user)
        {
            return new SubscriptionSummaryResponse
            {
                Status = user.SubscriptionStatus,
                Provider = user.SubscriptionProvider,
                Reference = user.SubscriptionReference,
                PendingPlanId = user.PendingAccountPlan,
                StartedAt = user.SubscriptionStartedAt,
                CurrentPeriodEndsAt = user.SubscriptionCurrentPeriodEndsAt,
                LastPaymentId = user.LastPaymentId,
                LastPaymentStatus = user.LastPaymentStatus,
                LastPaymentStatusDetail = user.LastPaymentStatusDetail,
                LastPaymentUpdatedAt = user.LastPaymentUpdatedAt,
                LastWebhookReceivedAt = user.LastWebhookReceivedAt
            };
        }

        private static bool IsSupportedMetric(string metricType)
        {
            return metricType == UsageMetricType.AvatarTryOn
                || metricType == UsageMetricType.RealisticRender
                || metricType == UsageMetricType.SavedLook
                || metricType == UsageMetricType.AvatarSlot
                || metricType == UsageMetricType.SharedLook
                || metricType == UsageMetricType.PurchaseClick;
        }

        private static void EnsurePublicProfileSlug(Models.Domain.User user)
        {
            if (!string.IsNullOrWhiteSpace(user.PublicProfileSlug))
            {
                return;
            }

            var baseSlug = $"{user.FirstName}-{user.LastName}".Trim().ToLowerInvariant().Replace(" ", "-");
            var cleaned = new string(baseSlug.Where(character => char.IsLetterOrDigit(character) || character == '-').ToArray()).Trim('-');
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                cleaned = "stylescan-user";
            }

            user.PublicProfileSlug = $"{cleaned}-{user.Id.ToString("N")[..8]}";
        }

        private async Task<GamificationSummaryResponse> BuildGamificationSummaryAsync(Guid userId)
        {
            var currentWeekKey = AccountPlanCatalog.GetWeekPeriodKey(DateTime.UtcNow);
            var currentMonthKey = AccountPlanCatalog.GetMonthPeriodKey(DateTime.UtcNow);
            var usageRecords = await _context.UserUsageRecords
                .AsNoTracking()
                .Where(record => record.UserId == userId)
                .ToListAsync();

            var favoriteLooksCount = await _context.UserPreferences
                .AsNoTracking()
                .CountAsync(preference => preference.UserId == userId && preference.PreferenceKey == "favorite-look");

            var lookCount = await _context.Looks
                .AsNoTracking()
                .CountAsync(look => look.UserId == userId);

            var usageByMetric = usageRecords
                .GroupBy(record => record.MetricType)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Used));

            var weeklyAvatarTryOns = usageRecords
                .FirstOrDefault(record => record.MetricType == UsageMetricType.AvatarTryOn && record.PeriodKey == currentWeekKey)?.Used ?? 0;
            var weeklyShares = usageRecords
                .FirstOrDefault(record => record.MetricType == UsageMetricType.SharedLook && record.PeriodKey == currentWeekKey)?.Used ?? 0;
            var weeklyPurchaseClicks = usageRecords
                .FirstOrDefault(record => record.MetricType == UsageMetricType.PurchaseClick && record.PeriodKey == currentWeekKey)?.Used ?? 0;
            var monthlyRealisticRenders = usageRecords
                .FirstOrDefault(record => record.MetricType == UsageMetricType.RealisticRender && record.PeriodKey == currentMonthKey)?.Used ?? 0;

            var experiencePoints =
                (usageByMetric.GetValueOrDefault(UsageMetricType.AvatarTryOn) * 12) +
                (usageByMetric.GetValueOrDefault(UsageMetricType.RealisticRender) * 26) +
                (usageByMetric.GetValueOrDefault(UsageMetricType.SavedLook) * 20) +
                (usageByMetric.GetValueOrDefault(UsageMetricType.SharedLook) * 35) +
                (usageByMetric.GetValueOrDefault(UsageMetricType.PurchaseClick) * 18) +
                (favoriteLooksCount * 6);

            var currentLevel = Math.Max(1, (experiencePoints / 120) + 1);
            var levelBasePoints = (currentLevel - 1) * 120;
            var nextLevelPoints = currentLevel * 120;
            var progressPercent = Math.Round(((double)(experiencePoints - levelBasePoints) / Math.Max(nextLevelPoints - levelBasePoints, 1)) * 100, 1);
            var weeklyActions = weeklyAvatarTryOns + weeklyShares + weeklyPurchaseClicks;

            var badges = new List<GamificationBadgeResponse>();
            if (usageByMetric.GetValueOrDefault(UsageMetricType.AvatarTryOn) >= 1)
            {
                badges.Add(new GamificationBadgeResponse
                {
                    Title = "Primeiro provador",
                    Description = "Voce ja colocou o primeiro look para teste no avatar.",
                    Icon = "sparkles-outline"
                });
            }

            if (usageByMetric.GetValueOrDefault(UsageMetricType.RealisticRender) >= 1)
            {
                badges.Add(new GamificationBadgeResponse
                {
                    Title = "Foto de impacto",
                    Description = "Voce ja gerou uma imagem realista pronta para compartilhar.",
                    Icon = "camera-outline"
                });
            }

            if (usageByMetric.GetValueOrDefault(UsageMetricType.SharedLook) >= 1)
            {
                badges.Add(new GamificationBadgeResponse
                {
                    Title = "Look compartilhado",
                    Description = "Seu estilo ja saiu do app e foi para outras pessoas.",
                    Icon = "share-social-outline"
                });
            }

            if (usageByMetric.GetValueOrDefault(UsageMetricType.PurchaseClick) >= 1)
            {
                badges.Add(new GamificationBadgeResponse
                {
                    Title = "Intencao de compra",
                    Description = "Voce ja transformou um look em clique de compra real.",
                    Icon = "bag-handle-outline"
                });
            }

            if (lookCount >= 5)
            {
                badges.Add(new GamificationBadgeResponse
                {
                    Title = "Curador de estilo",
                    Description = "Seu acervo ja comecou a mostrar variedade de looks e ocasioes.",
                    Icon = "pricetags-outline"
                });
            }

            var missions = new List<GamificationMissionResponse>
            {
                new()
                {
                    Title = "Prove 3 looks na semana",
                    Description = "Use o studio para testar combinacoes novas.",
                    Progress = weeklyAvatarTryOns,
                    Goal = 3,
                    Completed = weeklyAvatarTryOns >= 3,
                    RewardPoints = 45
                },
                new()
                {
                    Title = "Compartilhe 1 resultado",
                    Description = "Publique um preview ou look que valha mostrar.",
                    Progress = weeklyShares,
                    Goal = 1,
                    Completed = weeklyShares >= 1,
                    RewardPoints = 35
                },
                new()
                {
                    Title = "Gere 1 foto realista no mes",
                    Description = "Valide como o look fica mais perto da vida real.",
                    Progress = monthlyRealisticRenders,
                    Goal = 1,
                    Completed = monthlyRealisticRenders >= 1,
                    RewardPoints = 40
                },
                new()
                {
                    Title = "Clique em 2 compras",
                    Description = "Transforme inspiracao em intencao de compra.",
                    Progress = weeklyPurchaseClicks,
                    Goal = 2,
                    Completed = weeklyPurchaseClicks >= 2,
                    RewardPoints = 30
                }
            };

            return new GamificationSummaryResponse
            {
                CurrentLevel = currentLevel,
                ExperiencePoints = experiencePoints,
                NextLevelPoints = nextLevelPoints,
                ProgressPercent = Math.Max(0, Math.Min(progressPercent, 100)),
                MomentumLabel = weeklyActions switch
                {
                    >= 8 => "Em alta",
                    >= 4 => "Ritmo firme",
                    >= 1 => "Comecando a ganhar tracao",
                    _ => "Pronto para a primeira jogada"
                },
                Badges = badges,
                Missions = missions
            };
        }
    }
}
