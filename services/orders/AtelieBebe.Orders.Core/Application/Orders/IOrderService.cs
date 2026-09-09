using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Orders.Core.Application.Orders;

public interface IOrderService
{
    Task<OrderDto> CreateStoreOrderAsync(CreateStoreOrderRequest request, Guid? customerId, CancellationToken ct = default);
    Task<OrderDto> CreateCustomOrderAsync(CreateCustomOrderRequest request, Guid? customerId, CancellationToken ct = default);
    Task<PagedResult<OrderDto>> ListAsync(string? status, string? paymentStatus, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Every order matching the filters, unpaginated — backs the admin CSV export.</summary>
    Task<IReadOnlyList<OrderDto>> ExportAsync(string? status, string? paymentStatus, CancellationToken ct = default);
    Task<IReadOnlyList<OrderDto>> ListMineAsync(Guid customerId, CancellationToken ct = default);
    Task<OrderDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Guest order tracking — requires both the short order number and the customer's own e-mail to match.</summary>
    Task<OrderDto> LookupAsync(string shortId, string email, CancellationToken ct = default);
    Task<OrderDto> ChangeStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken ct = default);

    /// <summary>Re-queries the payment gateway for the given payment id and updates the matching order's PaymentStatus. Never throws on a malformed/unknown id — webhooks must always get a 200.</summary>
    Task HandlePaymentWebhookAsync(string paymentId, CancellationToken ct = default);

    /// <summary>Development-only: sets PaymentStatus directly, bypassing the gateway entirely — backs the fake payment page used to preview the checkout flow before real PagBank credentials exist.</summary>
    Task<OrderDto> SimulatePaymentAsync(Guid orderId, bool approved, CancellationToken ct = default);

    /// <summary>Admin action: creates a fresh PagBank checkout for an order (e.g. the customer never completed or abandoned the original one) and returns its URL, to open directly or send to the customer. Throws ConflictException when the gateway isn't configured or the order is already paid.</summary>
    Task<string> GeneratePixChargeAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>Admin action: attaches/clears the shipping carrier's tracking code, shown to the customer on the order page.</summary>
    Task<OrderDto> SetTrackingCodeAsync(Guid orderId, SetTrackingCodeRequest request, CancellationToken ct = default);
}
