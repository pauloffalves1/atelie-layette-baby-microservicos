using AtelieBebe.Backoffice.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Common;
using AtelieBebe.Backoffice.Core.Domain.Entities;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Backoffice.Core.Application.Contact;

public sealed class ContactService : IContactService
{
    private readonly IBackofficeUnitOfWork _unitOfWork;
    private readonly ILogger<ContactService> _logger;

    public ContactService(IBackofficeUnitOfWork unitOfWork, ILogger<ContactService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SubmitAsync(SubmitContactRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(SubmitAsync));
        try
        {
            var message = ContactMessage.Create(request.Name, Email.Create(request.Email), request.Phone, request.Message);
            _unitOfWork.ContactMessages.Add(message);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(SubmitAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(SubmitAsync));
            throw;
        }
    }

    public async Task<PagedResult<ContactMessageDto>> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ListAsync));
        try
        {
            var (normalizedPage, normalizedPageSize) = Pagination.Normalize(page, pageSize);
            var (messages, totalItems) = await _unitOfWork.ContactMessages.ListAsync(normalizedPage, normalizedPageSize, ct);
            var items = messages.Select(m => new ContactMessageDto(m.Id, m.Name, m.Email.Value, m.Phone, m.Message, m.CreatedAt)).ToList();
            var result = new PagedResult<ContactMessageDto>(items, normalizedPage, normalizedPageSize, totalItems);

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
