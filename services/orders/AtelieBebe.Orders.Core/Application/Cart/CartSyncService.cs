using System.Text.Json;
using AtelieBebe.Orders.Core.Application.Abstractions;
using AtelieBebe.Orders.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Orders.Core.Application.Cart;

public sealed class CartSyncService : ICartSyncService
{
    private readonly IOrdersUnitOfWork _unitOfWork;
    private readonly ILogger<CartSyncService> _logger;

    public CartSyncService(IOrdersUnitOfWork unitOfWork, ILogger<CartSyncService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SaveAsync(Guid customerId, CartSyncRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(SaveAsync));
        try
        {
            var existing = await _unitOfWork.CartSnapshots.GetByCustomerAsync(customerId, ct);

            if (request.Items.Count == 0)
            {
                if (existing is not null)
                {
                    _unitOfWork.CartSnapshots.Remove(existing);
                    await _unitOfWork.SaveChangesAsync(ct);
                }
                _logger.LogInformation("Saindo de {Method}", nameof(SaveAsync));
                return;
            }

            var itemsJson = JsonSerializer.Serialize(request.Items);

            if (existing is null)
                _unitOfWork.CartSnapshots.Add(CartSnapshot.Create(customerId, itemsJson));
            else
                existing.ReplaceItems(itemsJson);

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(SaveAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(SaveAsync));
            throw;
        }
    }
}
