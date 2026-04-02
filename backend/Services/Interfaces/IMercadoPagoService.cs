using System.Threading.Tasks;
using StyleScan.Backend.Models.Domain;
using StyleScan.Backend.Models.DTOs.User;

namespace StyleScan.Backend.Services.Interfaces
{
    public interface IMercadoPagoService
    {
        bool IsConfigured();
        bool HasWebhookSecret();
        bool IsWebhookSignatureValid(string? signatureHeader, string? requestId, string? dataId);
        Task<SubscriptionCheckoutResponse> CreateSubscriptionCheckoutAsync(User user, string checkoutId, string planId);
        Task<MercadoPagoPaymentInfo?> GetPaymentAsync(string paymentId);
    }
}
