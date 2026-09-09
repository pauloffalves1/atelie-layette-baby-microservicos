using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AtelieBebe.Orders.Core.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AtelieBebe.Orders.Core.Infrastructure.Payments;

/// <summary>
/// Creates hosted Checkout links via PagBank's Checkout API (restricted to PIX and credit card —
/// see <c>payment_methods</c> below) and resolves payment status by re-querying PagBank's Orders
/// API for the order a webhook points at — the webhook body is only ever used for its <c>id</c>,
/// never trusted for the actual charge status, same defensive pattern used for every gateway here.
///
/// PagBank splits "did the checkout page get used" (a Checkout resource, status ACTIVE/INACTIVE/
/// EXPIRED — never tells you if money moved) from "did the money move" (an Order resource,
/// created once the customer pays, with a <c>charges[]</c> array carrying the real status) — so a
/// checkout-status webhook is ignored by construction here: only an Order id (from an
/// ORDER-origin webhook) resolves to anything via GetPaymentAsync's GET /orders/{id} call; a
/// checkout id 404s there and the webhook safely no-ops.
///
/// Docs: https://developer.pagbank.com.br/reference/criar-checkout,
/// https://developer.pagbank.com.br/reference/consultar-pedido
/// </summary>
public sealed class PagBankGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly PagBankOptions _options;
    private readonly AppUrlOptions _appUrls;
    private readonly ILogger<PagBankGateway> _logger;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.Token);

    public PagBankGateway(HttpClient httpClient, IOptions<PagBankOptions> options, IOptions<AppUrlOptions> appUrls, ILogger<PagBankGateway> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _appUrls = appUrls.Value;
        _logger = logger;
    }

    public async Task<PaymentPreference?> CreatePreferenceAsync(Guid orderId, string description, decimal amount, string customerEmail, CancellationToken ct = default)
    {
        if (!IsConfigured) return null;

        var orderUrl = $"{_appUrls.PublicUrl}/pedido/{orderId}";
        var payload = new
        {
            reference_id = orderId.ToString(),
            customer = new { email = customerEmail },
            // Lets PagBank's own hosted page collect/validate name, CPF, phone etc. — we already
            // captured that at our own checkout, but re-entering it here risks a 400 from a format
            // PagBank doesn't accept, so we send only what we're sure of and let the customer
            // confirm/fill the rest on their page.
            customer_modifiable = true,
            items = new[]
            {
                new { reference_id = "item-1", name = description, quantity = 1, unit_amount = (int)Math.Round(amount * 100, MidpointRounding.AwayFromZero) },
            },
            // Ateliê only wants to offer PIX and credit card.
            payment_methods = new object[] { new { type = "CREDIT_CARD" }, new { type = "PIX" } },
            redirect_url = orderUrl,
            notification_urls = new[] { $"{_appUrls.ApiPublicUrl}/api/payments/pagbank/webhook" },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "checkouts") { Content = JsonContent.Create(payload) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Falha ao criar checkout no PagBank (HTTP {Status}): {Body}", (int)response.StatusCode, body);
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (!json.TryGetProperty("links", out var links))
        {
            _logger.LogError("Resposta do PagBank sem 'links' ao criar checkout para o pedido {OrderId}.", orderId);
            return null;
        }

        string? payUrl = null;
        foreach (var link in links.EnumerateArray())
        {
            if (link.TryGetProperty("rel", out var rel) && rel.GetString() == "PAY" && link.TryGetProperty("href", out var href))
            {
                payUrl = href.GetString();
                break;
            }
        }

        if (string.IsNullOrEmpty(payUrl))
        {
            _logger.LogError("Resposta do PagBank sem link 'PAY' ao criar checkout para o pedido {OrderId}.", orderId);
            return null;
        }

        var checkoutId = json.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        return new PaymentPreference(payUrl, checkoutId);
    }

    public async Task<PaymentDetails?> GetPaymentAsync(string paymentId, CancellationToken ct = default)
    {
        if (!IsConfigured) return null;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"orders/{paymentId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Falha ao consultar pedido {PaymentId} no PagBank (HTTP {Status}): {Body}", paymentId, (int)response.StatusCode, body);
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var externalReference = json.TryGetProperty("reference_id", out var refEl) ? refEl.GetString() : null;

        if (!json.TryGetProperty("charges", out var charges) || charges.GetArrayLength() == 0)
            return new PaymentDetails("pending", externalReference);

        // Last charge is the most recent — PagBank appends a new one per retry attempt.
        var lastCharge = charges[charges.GetArrayLength() - 1];
        var chargeStatus = lastCharge.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;

        var normalizedStatus = chargeStatus switch
        {
            "PAID" => "approved",
            "DECLINED" or "CANCELED" => "rejected",
            _ => chargeStatus ?? "pending",
        };

        return new PaymentDetails(normalizedStatus, externalReference);
    }
}
