using System.Linq;
using AtelieBebe.Backoffice.Core.Application.Abstractions;
using AtelieBebe.Backoffice.Core.Domain.Entities;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Backoffice.Core.Application.Newsletter;

public sealed class NewsletterService : INewsletterService
{
    private readonly IBackofficeUnitOfWork _unitOfWork;
    private readonly ILogger<NewsletterService> _logger;

    public NewsletterService(IBackofficeUnitOfWork unitOfWork, ILogger<NewsletterService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SubscribeAsync(string email, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(SubscribeAsync));
        try
        {
            var normalized = Email.Create(email);
            var existing = await _unitOfWork.NewsletterSubscribers.GetByEmailAsync(normalized.Value, ct);

            if (existing is null)
                _unitOfWork.NewsletterSubscribers.Add(NewsletterSubscriber.Create(normalized));
            else if (!existing.Active)
                existing.Reactivate();
            else
            {
                _logger.LogInformation("Saindo de {Method}", nameof(SubscribeAsync));
                return; // already an active subscriber, nothing to do
            }

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(SubscribeAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(SubscribeAsync));
            throw;
        }
    }

    public async Task<IReadOnlyList<NewsletterSubscriberDto>> ListAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ListAsync));
        try
        {
            var subscribers = await _unitOfWork.NewsletterSubscribers.ListAsync(ct);
            var result = subscribers.Where(s => s.Active).Select(s => new NewsletterSubscriberDto(s.Id, s.Email.Value, s.CreatedAt)).ToList();

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
