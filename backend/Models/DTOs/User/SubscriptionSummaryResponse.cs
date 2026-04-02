using System;

namespace StyleScan.Backend.Models.DTOs.User
{
    public class SubscriptionSummaryResponse
    {
        public string Status { get; set; } = string.Empty;
        public string? Provider { get; set; }
        public string? Reference { get; set; }
        public string? PendingPlanId { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CurrentPeriodEndsAt { get; set; }
        public string? LastPaymentId { get; set; }
        public string? LastPaymentStatus { get; set; }
        public string? LastPaymentStatusDetail { get; set; }
        public DateTime? LastPaymentUpdatedAt { get; set; }
        public DateTime? LastWebhookReceivedAt { get; set; }
    }
}
