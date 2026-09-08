using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.Backoffice.Core.Domain.Entities;

/// <summary>An immutable record of an admin action, for accountability when more than one admin has access to the panel.</summary>
public sealed class AuditLog : Entity, IAggregateRoot
{
    public Guid AdminId { get; private set; }
    public string AdminName { get; private set; } = default!;
    public string Action { get; private set; } = default!;
    public string Details { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private AuditLog() { } // EF Core

    private AuditLog(Guid id, Guid adminId, string adminName, string action, string details) : base(id)
    {
        AdminId = adminId;
        AdminName = adminName;
        Action = action;
        Details = details;
        CreatedAt = DateTime.UtcNow;
    }

    public static AuditLog Create(Guid adminId, string adminName, string action, string details)
    {
        if (adminId == Guid.Empty)
            throw new DomainException("Administrador inválido.");
        if (string.IsNullOrWhiteSpace(action))
            throw new DomainException("A ação é obrigatória.");

        return new AuditLog(Guid.NewGuid(), adminId, adminName, action.Trim(), details?.Trim() ?? "");
    }
}
