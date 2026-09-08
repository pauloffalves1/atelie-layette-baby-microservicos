namespace AtelieBebe.Orders.Core.Domain.Enums;

/// <summary>
/// Payment confirmation state, tracked independently of OrderStatus (which is about
/// fulfillment/production, not whether the customer has actually paid).
/// </summary>
public enum PaymentStatus
{
    Pendente = 0,
    Pago = 1,
    Recusado = 2,
}
