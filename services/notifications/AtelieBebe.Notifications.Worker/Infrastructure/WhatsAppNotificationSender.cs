using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AtelieBebe.Notifications.Worker.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AtelieBebe.Notifications.Worker.Infrastructure;

/// <summary>
/// Sends transactional notifications through the Meta WhatsApp Business Cloud API. Every message
/// here is business-initiated (outside any customer service window), so the Cloud API requires a
/// pre-approved message template per notification type — see spec/design.md, Requisito 16, for the
/// exact template names/variables that must exist and be approved in the Meta Business Manager.
/// </summary>
public sealed class WhatsAppNotificationSender : INotificationSender
{
    private static readonly IReadOnlyDictionary<string, string> StatusLabels = new Dictionary<string, string>
    {
        ["Recebido"] = "Recebido",
        ["EmProducao"] = "Em produção",
        ["Pronto"] = "Pronto",
        ["Enviado"] = "Enviado",
        ["Entregue"] = "Entregue",
        ["Cancelado"] = "Cancelado",
    };

    private readonly HttpClient _httpClient;
    private readonly WhatsAppOptions _options;
    private readonly AdminNotificationOptions _adminOptions;
    private readonly ILogger<WhatsAppNotificationSender> _logger;

    public WhatsAppNotificationSender(HttpClient httpClient, IOptions<WhatsAppOptions> options, IOptions<AdminNotificationOptions> adminOptions, ILogger<WhatsAppNotificationSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _adminOptions = adminOptions.Value;
        _logger = logger;
    }

    public Task SendOrderCreatedAsync(Guid orderId, string customerName, string customerPhone, decimal total, CancellationToken ct = default) =>
        SendTemplateAsync(customerPhone, "pedido_recebido", ct, customerName, ShortId(orderId), FormatMoney(total));

    public Task SendOrderStatusChangedAsync(Guid orderId, string customerName, string customerPhone, string oldStatus, string newStatus, CancellationToken ct = default) =>
        SendTemplateAsync(customerPhone, "pedido_status_atualizado", ct, customerName, ShortId(orderId), Label(newStatus));

    public Task SendWelcomeMessageAsync(Guid customerId, string name, string phone, CancellationToken ct = default) =>
        SendTemplateAsync(phone, "boas_vindas_cliente", ct, name);

    public Task SendContactAcknowledgementAsync(Guid messageId, string name, string phone, CancellationToken ct = default) =>
        SendTemplateAsync(phone, "confirmacao_contato", ct, name);

    public Task SendNewOrderAdminAlertAsync(Guid orderId, string customerName, decimal total, CancellationToken ct = default) =>
        SendTemplateAsync(_adminOptions.Phone, "novo_pedido_admin", ct, customerName, ShortId(orderId), FormatMoney(total));

    private async Task SendTemplateAsync(string toPhone, string templateName, CancellationToken ct, params string[] bodyParams)
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken) || string.IsNullOrWhiteSpace(_options.PhoneNumberId))
            throw new InvalidOperationException("WhatsApp não configurado: defina WhatsApp:AccessToken e WhatsApp:PhoneNumberId (dotnet user-secrets).");

        if (string.IsNullOrWhiteSpace(toPhone))
            throw new InvalidOperationException($"Não é possível enviar o template '{templateName}': nenhum número de WhatsApp de destino informado.");

        var payload = new
        {
            messaging_product = "whatsapp",
            to = WhatsAppPhoneFormatter.ToE164(toPhone),
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = "pt_BR" },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = bodyParams.Select(p => new { type = "text", text = p }).ToArray(),
                    },
                },
            },
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.ApiVersion}/{_options.PhoneNumberId}/messages")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Falha ao enviar WhatsApp (template '{templateName}', HTTP {(int)response.StatusCode}): {body}");
        }

        _logger.LogInformation("[WhatsApp] Template '{Template}' enviado para {Phone}.", templateName, payload.to);
    }

    private static string ShortId(Guid id) => id.ToString()[..8];

    private static string FormatMoney(decimal amount) => amount.ToString("0.00", CultureInfo.GetCultureInfo("pt-BR"));

    private static string Label(string status) => StatusLabels.TryGetValue(status, out var label) ? label : status;
}
