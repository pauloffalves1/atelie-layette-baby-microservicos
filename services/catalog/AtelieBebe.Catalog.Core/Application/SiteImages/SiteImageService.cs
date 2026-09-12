using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.Catalog.Core.Domain.Entities;
using AtelieBebe.SharedKernel.Exceptions;
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

    public async Task<SiteImageDto> AddImageAsync(string key, string url, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(AddImageAsync));
        try
        {
            var existingForKey = await _unitOfWork.SiteImages.ListByKeyAsync(key, ct);
            var nextSortOrder = existingForKey.Count == 0 ? 0 : existingForKey.Max(s => s.SortOrder) + 1;

            var image = SiteImage.Create(key, url, nextSortOrder);
            _unitOfWork.SiteImages.Add(image);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(AddImageAsync));
            return ToDto(image);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(AddImageAsync));
            throw;
        }
    }

    public async Task DeleteImageAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(DeleteImageAsync));
        try
        {
            var image = await _unitOfWork.SiteImages.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Imagem do site", id);

            _unitOfWork.SiteImages.Remove(image);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(DeleteImageAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(DeleteImageAsync));
            throw;
        }
    }

    public async Task MoveImageAsync(Guid id, MoveDirection direction, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(MoveImageAsync));
        try
        {
            var image = await _unitOfWork.SiteImages.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Imagem do site", id);

            var siblings = await _unitOfWork.SiteImages.ListByKeyAsync(image.Key, ct);
            var index = siblings.ToList().FindIndex(s => s.Id == id);
            var neighborIndex = direction == MoveDirection.Up ? index - 1 : index + 1;

            if (neighborIndex < 0 || neighborIndex >= siblings.Count)
            {
                _logger.LogInformation("Saindo de {Method} (sem vizinho, nada a fazer)", nameof(MoveImageAsync));
                return;
            }

            var neighbor = siblings[neighborIndex];
            var imageOrder = image.SortOrder;
            var neighborOrder = neighbor.SortOrder;
            image.SetSortOrder(neighborOrder);
            neighbor.SetSortOrder(imageOrder);

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(MoveImageAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(MoveImageAsync));
            throw;
        }
    }

    private static SiteImageDto ToDto(SiteImage s) => new(s.Id, s.Key, s.Url, s.SortOrder, s.UpdatedAt);
}
