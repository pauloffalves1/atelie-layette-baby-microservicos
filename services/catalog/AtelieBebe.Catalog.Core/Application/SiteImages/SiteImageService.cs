using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.Catalog.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Catalog.Core.Application.SiteImages;

public sealed class SiteImageService : ISiteImageService
{
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly ILogger<SiteImageService> _logger;

    public SiteImageService(ICatalogUnitOfWork unitOfWork, ILogger<SiteImageService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SiteImageDto>> ListAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ListAsync));
        try
        {
            var images = await _unitOfWork.SiteImages.ListAsync(ct);
            var result = images.Select(ToDto).ToList();

            _logger.LogInformation("Saindo de {Method}", nameof(ListAsync));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ListAsync));
            throw;
        }
    }

    public async Task<SiteImageDto> SetImageAsync(string key, string url, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(SetImageAsync));
        try
        {
            var existing = await _unitOfWork.SiteImages.GetByKeyAsync(key, ct);

            if (existing is null)
            {
                existing = SiteImage.Create(key, url);
                _unitOfWork.SiteImages.Add(existing);
            }
            else
            {
                existing.UpdateUrl(url);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(SetImageAsync));
            return ToDto(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(SetImageAsync));
            throw;
        }
    }

    private static SiteImageDto ToDto(SiteImage s) => new(s.Key, s.Url, s.UpdatedAt);
}
