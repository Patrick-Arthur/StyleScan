using System;

namespace StyleScan.Backend.Models.DTOs.User
{
    public class SubscriptionCheckoutResponse
    {
        public string CheckoutId { get; set; } = string.Empty;
        public string PlanId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
        public string? SandboxCheckoutUrl { get; set; }
        public string? PreferenceId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsLiveCheckout { get; set; }
    }
}
