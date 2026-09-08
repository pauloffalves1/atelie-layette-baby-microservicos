using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.Catalog.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Catalog.Core.Application.Gallery;

public sealed class GalleryImageService : IGalleryImageService
{
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<GalleryImageService> _logger;

    public GalleryImageService(ICatalogUnitOfWork unitOfWork, IFileStorageService fileStorage, ILogger<GalleryImageService> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GalleryImageDto>> ListAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ListAsync));
        try
        {
            var images = await _unitOfWork.GalleryImages.ListAsync(ct);
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

    public async Task<GalleryImageDto> AddAsync(string url, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(AddAsync));
        try
        {
            var image = GalleryImage.Create(url);
            _unitOfWork.GalleryImages.Add(image);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(AddAsync));
            return ToDto(image);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(AddAsync));
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(DeleteAsync));
        try
        {
            var image = await _unitOfWork.GalleryImages.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Imagem da galeria", id);

            _unitOfWork.GalleryImages.Remove(image);
            await _unitOfWork.SaveChangesAsync(ct);
            await _fileStorage.DeleteAsync(image.Url, ct);

            _logger.LogInformation("Saindo de {Method}", nameof(DeleteAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(DeleteAsync));
            throw;
        }
    }

    private static GalleryImageDto ToDto(GalleryImage g) => new(g.Id, g.Url, g.CreatedAt);
}
