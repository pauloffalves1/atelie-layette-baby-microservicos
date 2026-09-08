using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AtelieBebe.Notifications.Worker.Abstractions;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AtelieBebe.Notifications.Worker.Infrastructure;

/// <summary>
/// Sends transactional e-mails through Resend (api.resend.com/emails). Independent of, and a lot
/// simpler than, the WhatsApp channel — no message templates need pre-approval, any HTML works —
/// but still requires FromEmail's domain to be verified in the Resend dashboard before send works.
/// </summary>
public sealed class ResendEmailSender : IEmailSender
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
    private readonly ResendOptions _options;
    private readonly AdminNotificationOptions _adminOptions;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(HttpClient httpClient, IOptions<ResendOptions> options, IOptions<AdminNotificationOptions> adminOptions, ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _adminOptions = adminOptions.Value;
        _logger = logger;
    }

    public Task SendOrderCreatedAsync(Guid orderId, string customerName, string customerEmail, decimal total, CancellationToken ct = default) =>
        SendAsync(
            customerEmail,
            $"Recebemos seu pedido #{ShortId(orderId)}!",
            Wrap($"""
                <p>Olá, {customerName}!</p>
                <p>Recebemos seu pedido <strong>#{ShortId(orderId)}</strong> no valor de <strong>{FormatMoney(total)}</strong> e já estamos cuidando dele com carinho.</p>
                <p>Você pode acompanhar o status a qualquer momento na sua conta.</p>
                """),
            ct);

    public Task SendOrderStatusChangedAsync(Guid orderId, string customerName, string customerEmail, string oldStatus, string newStatus, CancellationToken ct = default) =>
        SendAsync(
            customerEmail,
            $"Pedido #{ShortId(orderId)} — {Label(newStatus)}",
            Wrap($"""
                <p>Olá, {customerName}!</p>
                <p>Seu pedido <strong>#{ShortId(orderId)}</strong> mudou de status: agora está <strong>{Label(newStatus)}</strong>.</p>
                """),
            ct);

    public Task SendWelcomeMessageAsync(Guid customerId, string name, string email, CancellationToken ct = default) =>
        SendAsync(
            email,
            "Bem-vinda(o) ao Ateliê Layette Baby!",
            Wrap($"""
                <p>Olá, {name}!</p>
                <p>Sua conta foi criada com sucesso. Que bom ter você por aqui — esperamos que ame as peças tanto quanto amamos fazê-las.</p>
                """),
            ct);

    public Task SendContactAcknowledgementAsync(Guid messageId, string name, string email, CancellationToken ct = default) =>
        SendAsync(
            email,
            "Recebemos sua mensagem",
            Wrap($"""
                <p>Olá, {name}!</p>
                <p>Recebemos sua mensagem e vamos responder em breve pelo WhatsApp ou e-mail.</p>
                """),
            ct);

    public Task SendNewOrderAdminAlertAsync(Guid orderId, string customerName, decimal total, CancellationToken ct = default) =>
        SendAsync(
            _adminOptions.Email,
            $"Novo pedido #{ShortId(orderId)} — {FormatMoney(total)}",
            Wrap($"""
                <p>Novo pedido recebido!</p>
                <p><strong>#{ShortId(orderId)}</strong> — {customerName} — <strong>{FormatMoney(total)}</strong></p>
                <p>Acesse o painel administrativo para ver os detalhes.</p>
                """),
            ct);

    public Task SendPasswordResetAsync(string name, string email, string resetUrl, CancellationToken ct = default) =>
        SendAsync(
            email,
            "Redefinição de senha",
            Wrap($"""
                <p>Olá, {name}!</p>
                <p>Recebemos um pedido para redefinir sua senha. Clique no link abaixo para escolher uma nova (válido por 1 hora):</p>
                <p><a href="{resetUrl}">{resetUrl}</a></p>
                <p>Se você não pediu essa redefinição, pode ignorar este e-mail — sua senha continua a mesma.</p>
                """),
            ct);

    public Task SendEmailVerificationAsync(string name, string email, string verificationUrl, CancellationToken ct = default) =>
        SendAsync(
            email,
            "Confirme seu e-mail",
            Wrap($"""
                <p>Olá, {name}!</p>
                <p>Para confirmar que este é o seu e-mail, clique no link abaixo (válido por 24 horas):</p>
                <p><a href="{verificationUrl}">{verificationUrl}</a></p>
                <p>Se você não criou uma conta no Ateliê Layette Baby, pode ignorar este e-mail.</p>
                """),
            ct);

    public Task SendProductBackInStockAsync(string customerName, string customerEmail, string productName, string productUrl, CancellationToken ct = default) =>
        SendAsync(
            customerEmail,
            $"{productName} está disponível de novo!",
            Wrap($"""
                <p>Olá, {customerName}!</p>
                <p>O produto <strong>{productName}</strong>, que está na sua lista de favoritos, voltou a ficar disponível.</p>
                <p><a href="{productUrl}">Ver produto</a></p>
                """),
            ct);

    public Task SendAbandonedCartReminderAsync(string customerName, string customerEmail, IReadOnlyList<AbandonedCartItem> items, string shopUrl, CancellationToken ct = default)
    {
        var itemsHtml = string.Join("", items.Select(i =>
            $"""<li><a href="{i.ProductUrl}">{i.ProductName}</a> — {i.Quantity}x</li>"""));

        return SendAsync(
            customerEmail,
            "Você esqueceu algo no seu carrinho!",
            Wrap($"""
                <p>Olá, {customerName}!</p>
                <p>Notamos que você deixou alguns itens no carrinho e ainda não finalizou o pedido:</p>
                <ul>{itemsHtml}</ul>
                <p><a href="{shopUrl}">Voltar à loja</a></p>
                """),
            ct);
    }

    private async Task SendAsync(string toEmail, string subject, string html, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Resend não configurado: defina Resend:ApiKey (dotnet user-secrets).");

        if (string.IsNullOrWhiteSpace(toEmail))
            throw new InvalidOperationException($"Não é possível enviar o e-mail '{subject}': nenhum endereço de destino informado.");

        var payload = new { from = _options.FromEmail, to = new[] { toEmail }, subject, html };

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails") { Content = JsonContent.Create(payload) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Falha ao enviar e-mail via Resend ('{subject}', HTTP {(int)response.StatusCode}): {body}");
        }

        _logger.LogInformation("[Resend] E-mail '{Subject}' enviado para {Email}.", subject, toEmail);
    }

    private static string Wrap(string bodyHtml) => $"""
        <div style="font-family: Arial, sans-serif; color: #4a3f3a; max-width: 480px; margin: 0 auto;">
          <h2 style="color: #e8a2ad;">Ateliê Layette Baby</h2>
          {bodyHtml}
          <p style="margin-top: 2rem; font-size: 0.85rem; color: #8a7f7a;">Fraldas de ombro e boca bordadas com carinho.</p>
        </div>
        """;

    private static string ShortId(Guid id) => id.ToString()[..8];

    private static string FormatMoney(decimal amount) => "R$ " + amount.ToString("0.00", CultureInfo.GetCultureInfo("pt-BR"));

    private static string Label(string status) => StatusLabels.TryGetValue(status, out var label) ? label : status;
}
