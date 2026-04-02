using System;

namespace StyleScan.Backend.Models.DTOs.User
{
    public class UserProfileResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PublicProfileSlug { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string AccountPlan { get; set; } = string.Empty;
        public PlanLimitsResponse Limits { get; set; } = new();
        public List<AccountPlanUsageResponse> Usage { get; set; } = new();
        public SubscriptionSummaryResponse Subscription { get; set; } = new();
    }
}
