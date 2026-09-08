using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Backoffice.Core.Application.Contact;

public interface IContactService
{
    Task SubmitAsync(SubmitContactRequest request, CancellationToken ct = default);
    Task<PagedResult<ContactMessageDto>> ListAsync(int page, int pageSize, CancellationToken ct = default);
}
