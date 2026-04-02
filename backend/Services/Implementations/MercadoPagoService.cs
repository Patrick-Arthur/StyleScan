using System;
using System.Linq;
using System.Security.Cryptography;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using StyleScan.Backend.Models.Domain;
using StyleScan.Backend.Models.DTOs.User;
using StyleScan.Backend.Services.Interfaces;
using StyleScan.Backend.Services.Support;

namespace StyleScan.Backend.Services.Implementations
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public MercadoPagoService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public bool IsConfigured()
        {
            var accessToken = _configuration["ExternalApis:MercadoPago:AccessToken"];
            return !string.IsNullOrWhiteSpace(accessToken) && !accessToken.Contains("your-", StringComparison.OrdinalIgnoreCase);
        }

        public bool HasWebhookSecret()
        {
            var secret = _configuration["ExternalApis:MercadoPago:WebhookSecret"];
            return !string.IsNullOrWhiteSpace(secret) && !secret.Contains("your-", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<SubscriptionCheckoutResponse> CreateSubscriptionCheckoutAsync(User user, string checkoutId, string planId)
        {
            var plan = AccountPlanCatalog.Resolve(planId);
            if (!IsConfigured())
            {
                return BuildFallbackCheckout(checkoutId, plan);
            }

            var baseUrl = (_configuration["ExternalApis:MercadoPago:BaseUrl"] ?? "https://api.mercadopago.com").TrimEnd('/');
            var accessToken = _configuration["ExternalApis:MercadoPago:AccessToken"] ?? string.Empty;
            var frontBaseUrl = ResolveFrontBaseUrl();
            var notificationUrl = _configuration["ExternalApis:MercadoPago:NotificationUrl"];

            var successUrl = BuildReturnUrl(frontBaseUrl, "success", plan.Id, checkoutId);
            var failureUrl = BuildReturnUrl(frontBaseUrl, "failure", plan.Id, checkoutId);
            var pendingUrl = BuildReturnUrl(frontBaseUrl, "pending", plan.Id, checkoutId);

            var payload = new
            {
                items = new[]
                {
                    new
                    {
                        title = $"StyleScan - {plan.DisplayName}",
                        quantity = 1,
                        currency_id = "BRL",
                        unit_price = plan.MonthlyPrice,
                        description = $"Assinatura mensal do plano {plan.DisplayName}"
                    }
                },
                payer = new
                {
                    email = user.Email,
                    first_name = user.FirstName,
                    last_name = user.LastName
                },
                payment_methods = new
                {
                    excluded_payment_types = new[]
                    {
                        new
                        {
                            id = "ticket"
                        }
                    },
                    installments = 1,
                    default_installments = 1
                },
                external_reference = checkoutId,
                statement_descriptor = "STYLESCAN",
                auto_return = "approved",
                back_urls = new
                {
                    success = successUrl,
                    failure = failureUrl,
                    pending = pendingUrl
                },
                metadata = new
                {
                    user_id = user.Id,
                    plan_id = plan.Id,
                    checkout_id = checkoutId
                },
                notification_url = string.IsNullOrWhiteSpace(notificationUrl) ? null : notificationUrl
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/checkout/preferences");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            }), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Mercado Pago retornou erro ao criar checkout: {content}");
            }

            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var preferenceId = root.TryGetProperty("id", out var idElement) ? idElement.GetString() : checkoutId;
            var initPoint = root.TryGetProperty("init_point", out var initPointElement) ? initPointElement.GetString() : string.Empty;
            var sandboxInitPoint = root.TryGetProperty("sandbox_init_point", out var sandboxElement) ? sandboxElement.GetString() : initPoint;

            return new SubscriptionCheckoutResponse
            {
                CheckoutId = checkoutId,
                PlanId = plan.Id,
                Status = "pending",
                Provider = "mercado-pago",
                CheckoutUrl = initPoint ?? string.Empty,
                SandboxCheckoutUrl = sandboxInitPoint,
                PreferenceId = preferenceId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                Message = "Checkout Mercado Pago criado com sucesso.",
                IsLiveCheckout = true
            };
        }

        public async Task<MercadoPagoPaymentInfo?> GetPaymentAsync(string paymentId)
        {
            if (!IsConfigured() || string.IsNullOrWhiteSpace(paymentId))
            {
                return null;
            }

            var baseUrl = (_configuration["ExternalApis:MercadoPago:BaseUrl"] ?? "https://api.mercadopago.com").TrimEnd('/');
            var accessToken = _configuration["ExternalApis:MercadoPago:AccessToken"] ?? string.Empty;

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/v1/payments/{paymentId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Mercado Pago retornou erro ao consultar pagamento: {content}");
            }

            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            return new MercadoPagoPaymentInfo
            {
                PaymentId = root.TryGetProperty("id", out var idElement) ? idElement.ToString() : paymentId,
                Status = root.TryGetProperty("status", out var statusElement) ? statusElement.GetString() ?? string.Empty : string.Empty,
                StatusDetail = root.TryGetProperty("status_detail", out var detailElement) ? detailElement.GetString() ?? string.Empty : string.Empty,
                ExternalReference = root.TryGetProperty("external_reference", out var referenceElement) ? referenceElement.GetString() ?? string.Empty : string.Empty,
                PayerEmail = TryReadNestedString(root, "payer", "email"),
                ApprovedAt = TryReadDateTime(root, "date_approved")
            };
        }

        public bool IsWebhookSignatureValid(string? signatureHeader, string? requestId, string? dataId)
        {
            if (!HasWebhookSecret())
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(signatureHeader) ||
                string.IsNullOrWhiteSpace(requestId) ||
                string.IsNullOrWhiteSpace(dataId))
            {
                return false;
            }

            var parts = signatureHeader
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
                .Where(part => part.Length == 2)
                .ToDictionary(part => part[0].Trim().ToLowerInvariant(), part => part[1].Trim(), StringComparer.OrdinalIgnoreCase);

            if (!parts.TryGetValue("ts", out var timestamp) || string.IsNullOrWhiteSpace(timestamp))
            {
                return false;
            }

            if (!parts.TryGetValue("v1", out var receivedSignature) || string.IsNullOrWhiteSpace(receivedSignature))
            {
                return false;
            }

            var manifest = $"id:{dataId};request-id:{requestId};ts:{timestamp};";
            var secret = _configuration["ExternalApis:MercadoPago:WebhookSecret"] ?? string.Empty;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
            var expectedSignature = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return string.Equals(expectedSignature, receivedSignature.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildReturnUrl(string frontBaseUrl, string status, string planId, string checkoutId)
        {
            return QueryHelpers.AddQueryString(frontBaseUrl.TrimEnd('/'), new System.Collections.Generic.Dictionary<string, string?>
            {
                ["mpStatus"] = status,
                ["plan"] = planId,
                ["checkoutId"] = checkoutId
            });
        }

        private static string? TryReadNestedString(JsonElement root, string parentProperty, string childProperty)
        {
            if (root.TryGetProperty(parentProperty, out var parentElement) &&
                parentElement.ValueKind == JsonValueKind.Object &&
                parentElement.TryGetProperty(childProperty, out var childElement))
            {
                return childElement.GetString();
            }

            return null;
        }

        private static DateTime? TryReadDateTime(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var property))
            {
                return null;
            }

            if (property.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(property.GetString(), out var parsedDateTime))
            {
                return parsedDateTime.Kind == DateTimeKind.Utc
                    ? parsedDateTime
                    : DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Utc);
            }

            return null;
        }

        private string ResolveFrontBaseUrl()
        {
            var configuredFrontBaseUrl = (_configuration["ExternalApis:MercadoPago:FrontBaseUrl"] ?? string.Empty).Trim();
            var publicFrontBaseUrl = (_configuration["ExternalApis:MercadoPago:PublicFrontBaseUrl"] ?? "https://stylescan.app").Trim();

            if (string.IsNullOrWhiteSpace(configuredFrontBaseUrl))
            {
                return publicFrontBaseUrl.TrimEnd('/');
            }

            if (Uri.TryCreate(configuredFrontBaseUrl, UriKind.Absolute, out var uri))
            {
                if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
                {
                    return publicFrontBaseUrl.TrimEnd('/');
                }

                return configuredFrontBaseUrl.TrimEnd('/');
            }

            return publicFrontBaseUrl.TrimEnd('/');
        }

        private static SubscriptionCheckoutResponse BuildFallbackCheckout(string checkoutId, AccountPlanDefinition plan)
        {
            return new SubscriptionCheckoutResponse
            {
                CheckoutId = checkoutId,
                PlanId = plan.Id,
                Status = "pending",
                Provider = "mercado-pago",
                CheckoutUrl = $"/user/upgrade/checkout?plan={plan.Id}&checkoutId={checkoutId}",
                SandboxCheckoutUrl = $"/user/upgrade/checkout?plan={plan.Id}&checkoutId={checkoutId}",
                PreferenceId = checkoutId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                Message = "Checkout preparado em modo de homologacao. Configure o Access Token para abrir o Mercado Pago real.",
                IsLiveCheckout = false
            };
        }
    }
}
