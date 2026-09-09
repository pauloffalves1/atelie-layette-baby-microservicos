using System.Globalization;
using System.Text;
using System.Text.Json;
using AtelieBebe.SharedKernel.Web;
using AtelieBebe.SharedKernel.Messaging;
using AtelieBebe.Orders.Core.Application.Orders;
using Microsoft.AspNetCore.RateLimiting;

namespace AtelieBebe.Orders.Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Encomendas");

        // Store checkout and custom-order submissions are open to guests too; when the caller
        // is an authenticated customer, the order is linked to their account automatically.
        group.MapPost("/store", async (CreateStoreOrderRequest request, HttpContext http, IOrderService service, CancellationToken ct) =>
        {
            var customerId = http.User.GetUserIdOrNull();
            return Results.Ok(await service.CreateStoreOrderAsync(request, customerId, ct));
        });

        group.MapPost("/custom", async (CreateCustomOrderRequest request, HttpContext http, IOrderService service, CancellationToken ct) =>
        {
            var customerId = http.User.GetUserIdOrNull();
            return Results.Ok(await service.CreateCustomOrderAsync(request, customerId, ct));
        });

        group.MapGet("/mine", async (HttpContext http, IOrderService service, CancellationToken ct) =>
            Results.Ok(await service.ListMineAsync(http.User.GetUserId(), ct)))
            .RequireAuthorization("CustomerOnly");

        group.MapGet("/{id:guid}", async (Guid id, IOrderService service, CancellationToken ct) =>
            Results.Ok(await service.GetByIdAsync(id, ct)));

        // Guest order tracking: the short id alone is guessable (8 hex chars), so this is
        // rate-limited like other secret-guessing endpoints (login, coupon validation).
        group.MapGet("/lookup", async (string orderNumber, string email, IOrderService service, CancellationToken ct) =>
            Results.Ok(await service.LookupAsync(orderNumber, email, ct)))
            .RequireRateLimiting("auth");

        var adminGroup = app.MapGroup("/api/admin/orders").WithTags("Encomendas (admin)").RequireAuthorization("AdminOnly");

        adminGroup.MapGet("/", async (string? status, string? paymentStatus, IOrderService service, CancellationToken ct, int page = 1, int pageSize = 20) =>
            Results.Ok(await service.ListAsync(status, paymentStatus, page, pageSize, ct)));

        adminGroup.MapPatch("/{id:guid}/status", async (Guid id, UpdateOrderStatusRequest request, HttpContext http, IOrderService service, AdminAuditPublisher auditPublisher, CancellationToken ct) =>
        {
            var before = await service.GetByIdAsync(id, ct);
            var updated = await service.ChangeStatusAsync(id, request, ct);
            await auditPublisher.PublishAsync(http.User.GetUserId(), http.User.GetName(), "OrderStatusChanged", $"Pedido #{id.ToString()[..8]}: {before.Status} → {updated.Status}", ct);
            return Results.Ok(updated);
        });

        adminGroup.MapPatch("/{id:guid}/tracking-code", async (Guid id, SetTrackingCodeRequest request, HttpContext http, IOrderService service, AdminAuditPublisher auditPublisher, CancellationToken ct) =>
        {
            var before = await service.GetByIdAsync(id, ct);
            var updated = await service.SetTrackingCodeAsync(id, request, ct);
            var oldCode = string.IsNullOrWhiteSpace(before.TrackingCode) ? "(vazio)" : before.TrackingCode;
            var newCode = string.IsNullOrWhiteSpace(updated.TrackingCode) ? "(vazio)" : updated.TrackingCode;
            await auditPublisher.PublishAsync(http.User.GetUserId(), http.User.GetName(), "OrderTrackingCodeSet", $"Pedido #{id.ToString()[..8]}: {oldCode} → {newCode}", ct);
            return Results.Ok(updated);
        });

        // Lets an admin (re)generate a PIX charge for an order — e.g. the customer abandoned checkout,
        // or the order was created before the gateway was configured. Returns the copy-paste code to
        // relay to the customer (over WhatsApp, say) since there's no card form on the admin side.
        adminGroup.MapPost("/{id:guid}/payment-link", async (Guid id, IOrderService service, CancellationToken ct) =>
            Results.Ok(new { pixQrCodeText = await service.GeneratePixChargeAsync(id, ct) }));

        adminGroup.MapGet("/export", async (string? status, string? paymentStatus, IOrderService service, CancellationToken ct) =>
        {
            var orders = await service.ExportAsync(status, paymentStatus, ct);
            var csv = BuildCsv(orders);
            var fileName = $"encomendas-{DateTime.UtcNow:yyyy-MM-dd}.csv";
            return Results.File(new UTF8Encoding(true).GetBytes(csv), "text/csv", fileName);
        });
    }

    /// <summary>
    /// One row per item (not per order) so each embroidery job — with its own text/quantity/thread
    /// color — is its own line; order-level columns (customer, totals, status) repeat on every row
    /// of the same order. An order with no items still gets exactly one row, with blank item columns.
    /// </summary>
    private static string BuildCsv(IReadOnlyList<OrderDto> orders)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var sb = new StringBuilder();
        sb.AppendLine("Pedido;Data;Cliente;E-mail;Telefone;Tipo;Status;Pagamento;Produto;Quantidade;Bordado;Cor da linha;Subtotal;Frete;Cupom;Desconto do cupom;Total;Código de rastreio");

        foreach (var o in orders)
        {
            var orderColumns = new[]
            {
                o.Id.ToString()[..8],
                o.CreatedAt.ToString("dd/MM/yyyy HH:mm", culture),
                Escape(o.CustomerName),
                Escape(o.CustomerEmail),
                Escape(o.CustomerPhone ?? ""),
                o.Type,
                o.Status,
                o.PaymentStatus,
            };
            var totalsColumns = new[]
            {
                o.ItemsTotal.ToString("0.00", culture),
                o.ShippingCost.ToString("0.00", culture),
                Escape(o.CouponCode ?? ""),
                o.CouponDiscountAmount.ToString("0.00", culture),
                o.Total.ToString("0.00", culture),
                Escape(o.TrackingCode ?? ""),
            };

            var items = o.Items.Count > 0 ? o.Items : new[] { (OrderItemDto?)null }.Cast<OrderItemDto>();
            foreach (var item in items)
            {
                var (embroideryText, threadColor) = ParseItemOptions(item?.OptionsJson);
                var itemColumns = new[]
                {
                    Escape(item?.ProductName ?? ""),
                    item is null ? "" : item.Quantity.ToString(culture),
                    Escape(embroideryText ?? ""),
                    Escape(threadColor ?? ""),
                };

                sb.AppendLine(string.Join(';', orderColumns.Concat(itemColumns).Concat(totalsColumns)));
            }
        }

        return sb.ToString();
    }

    private static (string? EmbroideryText, string? ThreadColor) ParseItemOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson)) return (null, null);

        try
        {
            using var doc = JsonDocument.Parse(optionsJson);
            var embroideryText = doc.RootElement.TryGetProperty("embroideryText", out var e) ? e.GetString() : null;
            var threadColor = doc.RootElement.TryGetProperty("threadColor", out var c) ? c.GetString() : null;
            return (embroideryText, threadColor);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static string Escape(string value) =>
        value.Contains(';') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
