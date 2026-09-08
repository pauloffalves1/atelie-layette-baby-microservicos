using AtelieBebe.Backoffice.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Common;
using AtelieBebe.Backoffice.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Backoffice.Core.Application.Audit;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IBackofficeUnitOfWork _unitOfWork;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IBackofficeUnitOfWork unitOfWork, ILogger<AuditLogService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task RecordAsync(Guid adminId, string adminName, string action, string details, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(RecordAsync));
        try
        {
            _unitOfWork.AuditLogs.Add(AuditLog.Create(adminId, adminName, action, details));
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(RecordAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(RecordAsync));
            throw;
        }
    }

    public async Task<PagedResult<AuditLogDto>> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ListAsync));
        try
        {
            var (normalizedPage, normalizedPageSize) = Pagination.Normalize(page, pageSize);
            var (items, totalItems) = await _unitOfWork.AuditLogs.ListAsync(normalizedPage, normalizedPageSize, ct);

            var result = new PagedResult<AuditLogDto>(
                items.Select(a => new AuditLogDto(a.Id, a.AdminName, a.Action, a.Details, a.CreatedAt)).ToList(),
                normalizedPage, normalizedPageSize, totalItems);

            _logger.LogInformation("Saindo de {Method}", nameof(ListAsync));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ListAsync));
            throw;
        }
    }
}
