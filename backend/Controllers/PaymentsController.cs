using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StyleScan.Backend.Data;
using StyleScan.Backend.Services.Interfaces;

namespace StyleScan.Backend.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/v1/payments/mercado-pago")]
    public class PaymentsController : ControllerBase
    {
        private readonly StyleScanDbContext _context;
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            StyleScanDbContext context,
            IMercadoPagoService mercadoPagoService,
            ILogger<PaymentsController> logger)
        {
            _context = context;
            _mercadoPagoService = mercadoPagoService;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook([FromBody] JsonElement payload)
        {
            return await ProcessWebhookAsync(payload);
        }

        [HttpGet("webhook")]
        public async Task<IActionResult> HandleWebhookGet()
        {
            return await ProcessWebhookAsync(default);
        }

        private async Task<IActionResult> ProcessWebhookAsync(JsonElement payload)
        {
            var topic = ResolveTopic(payload);
            var paymentId = ResolvePaymentId(payload);
            var signatureHeader = Request.Headers["x-signature"].ToString();
            var requestId = Request.Headers["x-request-id"].ToString();

            if (string.IsNullOrWhiteSpace(paymentId))
            {
                _logger.LogInformation("Mercado Pago webhook ignored because payment id was missing. Topic: {Topic}", topic);
                return Ok(new { received = true, ignored = true, reason = "payment_id_missing" });
            }

            if (!_mercadoPagoService.IsWebhookSignatureValid(signatureHeader, requestId, paymentId))
            {
                _logger.LogWarning("Mercado Pago webhook rejected due to invalid signature. PaymentId: {PaymentId}, RequestId: {RequestId}", paymentId, requestId);
                return Unauthorized(new { received = false, reason = "invalid_signature" });
            }

            if (!string.IsNullOrWhiteSpace(topic) &&
                !string.Equals(topic, "payment", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(topic, "payments", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Mercado Pago webhook ignored due to unsupported topic. Topic: {Topic}, PaymentId: {PaymentId}", topic, paymentId);
                return Ok(new { received = true, ignored = true, reason = "unsupported_topic", topic });
            }

            try
            {
                var payment = await _mercadoPagoService.GetPaymentAsync(paymentId);
                if (payment == null || string.IsNullOrWhiteSpace(payment.ExternalReference))
                {
                    _logger.LogWarning("Mercado Pago webhook could not resolve payment or external reference. PaymentId: {PaymentId}", paymentId);
                    return Ok(new { received = true, ignored = true, reason = "payment_not_resolved", paymentId });
                }

                var user = await _context.Users.FirstOrDefaultAsync(existingUser =>
                    existingUser.SubscriptionReference == payment.ExternalReference);

                if (user == null)
                {
                    _logger.LogWarning("Mercado Pago webhook could not match subscription reference. PaymentId: {PaymentId}, ExternalReference: {ExternalReference}", payment.PaymentId, payment.ExternalReference);
                    return Ok(new { received = true, ignored = true, reason = "subscription_not_found", paymentId });
                }

                var now = DateTime.UtcNow;
                user.SubscriptionProvider = "mercado-pago";
                user.SubscriptionStatus = payment.Status;
                user.LastPaymentId = payment.PaymentId;
                user.LastPaymentStatus = payment.Status;
                user.LastPaymentStatusDetail = payment.StatusDetail;
                user.LastPaymentUpdatedAt = payment.ApprovedAt ?? now;
                user.LastWebhookReceivedAt = now;
                user.UpdatedAt = now;

                if (string.Equals(payment.Status, "approved", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(user.PendingAccountPlan))
                {
                    user.AccountPlan = user.PendingAccountPlan;
                    user.PendingAccountPlan = null;
                    user.SubscriptionStartedAt ??= payment.ApprovedAt ?? now;
                    user.SubscriptionCurrentPeriodEndsAt = (payment.ApprovedAt ?? now).AddDays(30);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Mercado Pago webhook processed. UserId: {UserId}, PaymentId: {PaymentId}, Status: {Status}, Detail: {Detail}, PendingPlan: {PendingPlan}, ActivePlan: {ActivePlan}",
                    user.Id,
                    payment.PaymentId,
                    payment.Status,
                    payment.StatusDetail,
                    user.PendingAccountPlan,
                    user.AccountPlan);

                return Ok(new
                {
                    received = true,
                    processed = true,
                    paymentId = payment.PaymentId,
                    paymentStatus = payment.Status,
                    externalReference = payment.ExternalReference
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Mercado Pago webhook failed. PaymentId: {PaymentId}", paymentId);
                return Ok(new
                {
                    received = true,
                    processed = false,
                    paymentId,
                    error = exception.Message
                });
            }
        }

        private string ResolveTopic(JsonElement payload)
        {
            if (Request.Query.TryGetValue("topic", out var topic))
            {
                return topic.ToString();
            }

            if (Request.Query.TryGetValue("type", out var type))
            {
                return type.ToString();
            }

            if (payload.ValueKind == JsonValueKind.Object)
            {
                if (payload.TryGetProperty("topic", out var topicElement))
                {
                    return topicElement.GetString() ?? string.Empty;
                }

                if (payload.TryGetProperty("type", out var typeElement))
                {
                    return typeElement.GetString() ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private string ResolvePaymentId(JsonElement payload)
        {
            if (Request.Query.TryGetValue("data.id", out var flatDataId))
            {
                return flatDataId.ToString();
            }

            if (Request.Query.TryGetValue("id", out var id))
            {
                return id.ToString();
            }

            if (payload.ValueKind != JsonValueKind.Object)
            {
                return string.Empty;
            }

            if (payload.TryGetProperty("data", out var dataElement) &&
                dataElement.ValueKind == JsonValueKind.Object &&
                dataElement.TryGetProperty("id", out var dataIdElement))
            {
                return dataIdElement.ToString();
            }

            if (payload.TryGetProperty("resource", out var resourceElement))
            {
                var resource = resourceElement.GetString() ?? string.Empty;
                var segments = resource.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 0)
                {
                    return segments[^1];
                }
            }

            if (payload.TryGetProperty("id", out var idElement))
            {
                return idElement.ToString();
            }

            return string.Empty;
        }
    }
}
