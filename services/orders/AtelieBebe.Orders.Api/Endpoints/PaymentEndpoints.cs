using AtelieBebe.Orders.Core.Application.Abstractions;
using AtelieBebe.Orders.Core.Application.Orders;

namespace AtelieBebe.Orders.Api.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Pagamentos");

        // Public — the checkout page needs this before the customer is authenticated to anything.
        // The public key itself isn't secret (that's the point of asymmetric encryption); only
        // PagBank can decrypt what the frontend encrypts with it.
        group.MapGet("/pagbank/public-key", async (IPaymentGateway gateway, CancellationToken ct) =>
        {
            var publicKey = await gateway.GetCardEncryptionPublicKeyAsync(ct);
            return publicKey is null ? Results.NotFound() : Results.Ok(new { publicKey });
        });

        // Public, same reasoning as public-key above — fetched fresh per checkout attempt since
        // the session PagBank returns is only valid for 30 minutes.
        group.MapGet("/pagbank/3ds-session", async (IPaymentGateway gateway, CancellationToken ct) =>
        {
            var session = await gateway.CreateThreeDsSessionAsync(ct);
            return session is null ? Results.NotFound() : Results.Ok(new { session = session.Session, environment = session.Environment });
        });

        // PagBank posts the full Order object here whenever a charge's status changes — but the
        // body is only ever used to read the order's own "id" field; the actual status always
        // comes from re-fetching GET /orders/{id} (see PagBankGateway.GetPaymentAsync), never from
        // trusting this payload directly. A checkout-status ping (not an order) carries an id that
        // 404s at that endpoint and safely no-ops. Always returns 200 so PagBank doesn't keep
        // retrying a notification we can't make sense of.
        group.MapPost("/pagbank/webhook", async (HttpContext http, IOrderService service, CancellationToken ct) =>
        {
            string? orderId = null;
            try
            {
                var body = await http.Request.ReadFromJsonAsync<PagBankWebhookPayload>(ct);
                orderId = body?.Id;
            }
            catch
            {
                // Malformed/empty body — nothing to process, just acknowledge.
            }

            if (!string.IsNullOrWhiteSpace(orderId))
                await service.HandlePaymentWebhookAsync(orderId, ct);

            return Results.Ok();
        }).DisableAntiforgery();
    }

    /// <summary>
    /// Only mapped in Development (see Program.cs) — backs the fake payment page shown when
    /// FakePaymentGateway is in use, so the checkout → payment → confirmation flow can be
    /// previewed before real PagBank credentials exist. Never registered in production, so
    /// there's no route here to guard against even if someone finds the URL.
    /// </summary>
    public static void MapFakePaymentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/payments/pagbank/simulate/{orderId:guid}", async (Guid orderId, SimulatePaymentRequest request, IOrderService service, CancellationToken ct) =>
            Results.Ok(await service.SimulatePaymentAsync(orderId, request.Approved, ct)))
            .WithTags("Pagamentos");
    }

    private sealed record PagBankWebhookPayload(string? Id);
    public sealed record SimulatePaymentRequest(bool Approved);
}
