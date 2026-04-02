using System;

namespace StyleScan.Backend.Models.DTOs.User
{
    public class MercadoPagoPaymentInfo
    {
        public string PaymentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusDetail { get; set; } = string.Empty;
        public string ExternalReference { get; set; } = string.Empty;
        public string? PayerEmail { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}
