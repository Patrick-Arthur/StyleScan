namespace StyleScan.Backend.Models.DTOs.User
{
    public class ConfirmSubscriptionRequest
    {
        public string PlanId { get; set; } = string.Empty;
        public string CheckoutId { get; set; } = string.Empty;
        public string Provider { get; set; } = "mercado-pago";
    }
}
