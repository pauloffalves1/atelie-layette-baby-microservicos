using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Orders.Core.Domain.Events;

public sealed record ReviewRequestItem(string ProductName, string ProductUrl);

/// <summary>Raised once per delivered order, a few days after delivery, inviting the customer to review what they bought — see ReviewRequestReminderProcessor.</summary>
public sealed record ReviewRequestDomainEvent(
    string CustomerName,
    string CustomerEmail,
    IReadOnlyList<ReviewRequestItem> Items) : DomainEventBase;
