using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using AtelieBebe.Orders.Core.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AtelieBebe.Orders.Core.Infrastructure.Payments;

/// <summary>
/// Charges directly against PagBank's Order API (POST /orders) instead of the hosted Checkout
/// API — the checkout flow used to redirect to a PagBank-hosted page, but this account's token
/// doesn't have "allowlist" access to the Checkout API in production (confirmed via PagBank
/// support: HTTP 403 "allowlist_access_required" on POST /checkouts). The direct Order API isn't
/// gated the same way. The card itself is encrypted client-side (see GetCardEncryptionPublicKeyAsync
/// and the frontend's use of PagBank's JS SDK), so the raw card number/CVV never reaches this
/// service — only the encrypted blob does.
///
/// Docs: https://developer.pagbank.com.br/reference/criar-chave-publica,
/// https://developer.pagbank.com.br/reference/criar-pagar-pedido-com-cartao,
/// https://developer.pagbank.com.br/reference/consultar-pedido
/// </summary>
public sealed partial class PagBankGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly PagBankOptions _options;
    private readonly AppUrlOptions _appUrls;
    private readonly ILogger<PagBankGateway> _logger;

    // The public key is reusable across charges (per PagBank's docs) — cached process-wide (this
    // gateway is registered as a typed HttpClient, i.e. transient, so an instance field would be
    // re-created and re-fetched on every single request) to avoid minting a new key on every
    // checkout page load.
    private static readonly SemaphoreSlim PublicKeyLock = new(1, 1);
    private static string? _cachedPublicKey;
    private static DateTime _cachedPublicKeyAtUtc;
    private static readonly TimeSpan PublicKeyTtl = TimeSpan.FromHours(1);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.Token);

    public PagBankGateway(HttpClient httpClient, IOptions<PagBankOptions> options, IOptions<AppUrlOptions> appUrls, ILogger<PagBankGateway> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _appUrls = appUrls.Value;
        _logger = logger;
    }

    public async Task<string?> GetCardEncryptionPublicKeyAsync(CancellationToken ct = default)
    {
        if (!IsConfigured) return null;

        if (_cachedPublicKey is not null && DateTime.UtcNow - _cachedPublicKeyAtUtc < PublicKeyTtl)
            return _cachedPublicKey;

        await PublicKeyLock.WaitAsync(ct);
        try
        {
            if (_cachedPublicKey is not null && DateTime.UtcNow - _cachedPublicKeyAtUtc < PublicKeyTtl)
                return _cachedPublicKey;

            using var request = new HttpRequestMessage(HttpMethod.Post, "public-keys")
            {
                Content = JsonContent.Create(new { type = "card" }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);

            var response = await _httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Falha ao obter chave pública do PagBank (HTTP {Status}): {Body}", (int)response.StatusCode, body);
                return null;
            }

            var json = JsonDocument.Parse(body).RootElement;
            var publicKey = json.TryGetProperty("public_key", out var pkEl) ? pkEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                _logger.LogError("Resposta do PagBank sem 'public_key' ao criar chave pública.");
                return null;
            }

            _cachedPublicKey = publicKey;
            _cachedPublicKeyAtUtc = DateTime.UtcNow;
            return publicKey;
        }
        finally
        {
            PublicKeyLock.Release();
        }
    }

    public async Task<ThreeDsSession?> CreateThreeDsSessionAsync(CancellationToken ct = default)
    {
        if (!IsConfigured) return null;

        // Lives on a different host than every other call here (sdk.pagseguro.com, not
        // api.pagseguro.com) — same sandbox/production split, just a different subdomain.
        var host = _options.Sandbox ? "https://sandbox.sdk.pagseguro.com" : "https://sdk.pagseguro.com";
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{host}/checkout-sdk/sessions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);

        var response = await _httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Falha ao criar sessão 3DS no PagBank (HTTP {Status}): {Body}", (int)response.StatusCode, body);
            return null;
        }

        var json = JsonDocument.Parse(body).RootElement;
        var session = json.TryGetProperty("session", out var sessionEl) ? sessionEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(session))
        {
            _logger.LogError("Resposta do PagBank sem 'session' ao criar sessão 3DS.");
            return null;
        }

        return new ThreeDsSession(session, _options.Sandbox ? "SANDBOX" : "PROD");
    }

    public async Task<CardChargeResult?> ChargeCardAsync(
        Guid orderId, string description, decimal amount,
        string customerName, string customerEmail, string customerTaxId, string? customerPhone,
        string encryptedCard, int installments, string? threeDsAuthenticationId, CancellationToken ct = default)
    {
        if (!IsConfigured) return null;

        var paymentMethod = new Dictionary<string, object>
        {
            ["type"] = "CREDIT_CARD",
            ["installments"] = Math.Max(1, installments),
            ["capture"] = true,
            ["card"] = new { encrypted = encryptedCard },
            ["holder"] = new { name = customerName, tax_id = OnlyDigits(customerTaxId) },
        };
        // Absent (rather than sent as null) when the frontend's 3DS challenge didn't complete — we
        // still charge the card either way, just without the fraud-liability shift 3DS provides.
        if (!string.IsNullOrWhiteSpace(threeDsAuthenticationId))
            paymentMethod["authentication_method"] = new { type = "THREEDS", id = threeDsAuthenticationId };

        var payload = new
        {
            reference_id = orderId.ToString(),
            customer = BuildCustomer(customerName, customerEmail, customerTaxId, customerPhone),
            items = new[] { new { reference_id = "item-1", name = description, quantity = 1, unit_amount = ToCents(amount) } },
            notification_urls = new[] { $"{_appUrls.ApiPublicUrl}/api/payments/pagbank/webhook" },
            charges = new[]
            {
                new
                {
                    reference_id = orderId.ToString(),
                    description,
                    amount = new { value = ToCents(amount), currency = "BRL" },
                    payment_method = paymentMethod,
                },
            },
        };

        var (order, error) = await PostOrderAsync(payload, ct);
        if (order is null)
        {
            _logger.LogError("Falha ao cobrar cartão no PagBank para o pedido {OrderId}: {Error}", orderId, error);
            return null;
        }

        var orderExternalId = order.Value.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;

        if (!order.Value.TryGetProperty("charges", out var charges) || charges.GetArrayLength() == 0)
            return new CardChargeResult(false, "pending", orderExternalId, null);

        var charge = charges[0];
        var status = charge.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
        var approved = status == "PAID";

        string? declineReason = null;
        if (!approved && charge.TryGetProperty("payment_response", out var paymentResponse) &&
            paymentResponse.TryGetProperty("message", out var messageEl))
            declineReason = messageEl.GetString();

        return new CardChargeResult(approved, NormalizeChargeStatus(status), orderExternalId, declineReason);
    }

    public async Task<PixCharge?> CreatePixChargeAsync(
        Guid orderId, string description, decimal amount,
        string customerName, string customerEmail, string customerTaxId, string? customerPhone,
        CancellationToken ct = default)
    {
        if (!IsConfigured) return null;

        // PIX modeled as a charge's payment_method (matching PagBank's own example), not the older
        // top-level qr_codes array — same order-creation endpoint either way, so ExternalId still
        // ends up being the PagBank *order* id (ORDE_...), keeping GetPaymentAsync/the webhook
        // (both keyed on that order id) working unchanged.
        var payload = new
        {
            reference_id = orderId.ToString(),
            customer = BuildCustomer(customerName, customerEmail, customerTaxId, customerPhone),
            items = new[] { new { reference_id = "item-1", name = description, quantity = 1, unit_amount = ToCents(amount) } },
            notification_urls = new[] { $"{_appUrls.ApiPublicUrl}/api/payments/pagbank/webhook" },
            charges = new[]
            {
                new
                {
                    reference_id = orderId.ToString(),
                    description,
                    amount = new { value = ToCents(amount), currency = "BRL" },
                    payment_method = new
                    {
                        type = "PIX",
                        pix = new { expiration_date = DateTimeOffset.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:sszzz") },
                    },
                },
            },
        };

        var (order, error) = await PostOrderAsync(payload, ct);
        if (order is null)
        {
            _logger.LogError("Falha ao gerar cobrança PIX no PagBank para o pedido {OrderId}: {Error}", orderId, error);
            return null;
        }

        var orderExternalId = order.Value.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (orderExternalId is null || !order.Value.TryGetProperty("charges", out var charges) || charges.GetArrayLength() == 0)
        {
            _logger.LogError("Resposta do PagBank sem 'charges' ao gerar cobrança PIX para o pedido {OrderId}.", orderId);
            return null;
        }

        var charge = charges[0];
        var qrCodeText = charge.TryGetProperty("qr_code", out var qrCodeEl) && qrCodeEl.TryGetProperty("text", out var textEl)
            ? textEl.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(qrCodeText))
        {
            _logger.LogError("Resposta do PagBank sem 'qr_code.text' ao gerar cobrança PIX para o pedido {OrderId}.", orderId);
            return null;
        }

        string? qrCodeImageUrl = null;
        if (charge.TryGetProperty("links", out var links))
        {
            foreach (var link in links.EnumerateArray())
            {
                if (link.TryGetProperty("rel", out var rel) && rel.GetString() == "QRCODE.PNG" && link.TryGetProperty("href", out var href))
                {
                    qrCodeImageUrl = href.GetString();
                    break;
                }
            }
        }

        return new PixCharge(orderExternalId, qrCodeText!, qrCodeImageUrl);
    }

    public async Task<PaymentDetails?> GetPaymentAsync(string paymentId, CancellationToken ct = default)
    {
        if (!IsConfigured) return null;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"orders/{paymentId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Falha ao consultar pedido {PaymentId} no PagBank (HTTP {Status}): {Body}", paymentId, (int)response.StatusCode, errorBody);
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var externalReference = json.TryGetProperty("reference_id", out var refEl) ? refEl.GetString() : null;

        if (!json.TryGetProperty("charges", out var charges) || charges.GetArrayLength() == 0)
            return new PaymentDetails("pending", externalReference);

        // Last charge is the most recent — PagBank appends a new one per retry attempt.
        var lastCharge = charges[charges.GetArrayLength() - 1];
        var chargeStatus = lastCharge.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;

        return new PaymentDetails(NormalizeChargeStatus(chargeStatus), externalReference);
    }

    private async Task<(JsonElement? Order, string? Error)> PostOrderAsync(object payload, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "orders") { Content = JsonContent.Create(payload) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);

        var response = await _httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            return (null, $"HTTP {(int)response.StatusCode}: {body}");

        return (JsonDocument.Parse(body).RootElement, null);
    }

    private static object BuildCustomer(string name, string email, string taxId, string? phone)
    {
        var phones = ParsePhone(phone);
        return phones is null
            ? new { name, email, tax_id = OnlyDigits(taxId) }
            : new { name, email, tax_id = OnlyDigits(taxId), phones = new[] { phones } };
    }

    private static object? ParsePhone(string? phone)
    {
        var digits = OnlyDigits(phone);
        if (digits.Length < 10) return null;

        // Brazilian numbers: first 2 digits are the area code (DDD), the rest is the subscriber number.
        return new { country = "55", area = digits[..2], number = digits[2..], type = "MOBILE" };
    }

    private static int ToCents(decimal amount) => (int)Math.Round(amount * 100, MidpointRounding.AwayFromZero);

    private static string OnlyDigits(string? value) => value is null ? "" : NonDigitRegex().Replace(value, "");

    private static string NormalizeChargeStatus(string? chargeStatus) => chargeStatus switch
    {
        "PAID" => "approved",
        "DECLINED" or "CANCELED" => "rejected",
        _ => chargeStatus ?? "pending",
    };

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitRegex();
}
