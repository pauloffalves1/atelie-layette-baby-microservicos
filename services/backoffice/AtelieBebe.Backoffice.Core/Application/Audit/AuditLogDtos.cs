namespace AtelieBebe.Backoffice.Core.Application.Audit;

public sealed record AuditLogDto(Guid Id, string AdminName, string Action, string Details, DateTime CreatedAt);
