using System.Linq;
using AtelieBebe.Backoffice.Core.Application.Abstractions;
using AtelieBebe.Backoffice.Core.Domain.Entities;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Backoffice.Core.Application.Newsletter;

public sealed class NewsletterService : INewsletterService
{
    private readonly IBackofficeUnitOfWork _unitOfWork;

    public NewsletterService(IBackofficeUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task SubscribeAsync(string email, CancellationToken ct = default)
    {
        var normalized = Email.Create(email);
        var existing = await _unitOfWork.NewsletterSubscribers.GetByEmailAsync(normalized.Value, ct);

        if (existing is null)
            _unitOfWork.NewsletterSubscribers.Add(NewsletterSubscriber.Create(normalized));
        else if (!existing.Active)
            existing.Reactivate();
        else
            return; // already an active subscriber, nothing to do

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<NewsletterSubscriberDto>> ListAsync(CancellationToken ct = default)
    {
        var subscribers = await _unitOfWork.NewsletterSubscribers.ListAsync(ct);
        return subscribers.Where(s => s.Active).Select(s => new NewsletterSubscriberDto(s.Id, s.Email.Value, s.CreatedAt)).ToList();
    }
}
