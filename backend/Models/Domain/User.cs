using System;
using System.Collections.Generic;

namespace StyleScan.Backend.Models.Domain
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? Bio { get; set; }
        public string? PublicProfileSlug { get; set; }
        public string AccountPlan { get; set; } = AccountPlanType.Free;
        public string SubscriptionStatus { get; set; } = "inactive";
        public string? SubscriptionProvider { get; set; }
        public string? SubscriptionReference { get; set; }
        public string? PendingAccountPlan { get; set; }
        public DateTime? SubscriptionStartedAt { get; set; }
        public DateTime? SubscriptionCurrentPeriodEndsAt { get; set; }
        public string? LastPaymentId { get; set; }
        public string? LastPaymentStatus { get; set; }
        public string? LastPaymentStatusDetail { get; set; }
        public DateTime? LastPaymentUpdatedAt { get; set; }
        public DateTime? LastWebhookReceivedAt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<Avatar> Avatars { get; set; } = new List<Avatar>();
        public ICollection<Look> Looks { get; set; } = new List<Look>();
        public ICollection<UserPreference> Preferences { get; set; } = new List<UserPreference>();
        public ICollection<UserUsageRecord> UsageRecords { get; set; } = new List<UserUsageRecord>();
    }
}
