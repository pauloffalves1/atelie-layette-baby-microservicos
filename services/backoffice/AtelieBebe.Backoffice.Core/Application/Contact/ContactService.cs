using AtelieBebe.Backoffice.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Common;
using AtelieBebe.Backoffice.Core.Domain.Entities;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Backoffice.Core.Application.Contact;

public sealed class ContactService : IContactService
{
    private readonly IBackofficeUnitOfWork _unitOfWork;

    public ContactService(IBackofficeUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task SubmitAsync(SubmitContactRequest request, CancellationToken ct = default)
    {
        var message = ContactMessage.Create(request.Name, Email.Create(request.Email), request.Phone, request.Message);
        _unitOfWork.ContactMessages.Add(message);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<ContactMessageDto>> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var (normalizedPage, normalizedPageSize) = Pagination.Normalize(page, pageSize);
        var (messages, totalItems) = await _unitOfWork.ContactMessages.ListAsync(normalizedPage, normalizedPageSize, ct);
        var items = messages.Select(m => new ContactMessageDto(m.Id, m.Name, m.Email.Value, m.Phone, m.Message, m.CreatedAt)).ToList();
        return new PagedResult<ContactMessageDto>(items, normalizedPage, normalizedPageSize, totalItems);
    }
}
