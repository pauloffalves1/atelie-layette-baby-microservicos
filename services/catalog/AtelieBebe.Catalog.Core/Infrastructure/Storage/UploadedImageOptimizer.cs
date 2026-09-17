using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.Catalog.Core.Domain.Entities;
using AtelieBebe.Catalog.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Catalog.Core.Infrastructure.Storage;

/// <summary>
/// One pass shortly after startup that converts photos uploaded before uploads were optimized
/// (JPG/PNG at up to 1600 px, no small copy) into the current WebP variants and repoints every
/// stored URL — product main photo and gallery, site images, "Dicas para o casal" gallery, review
/// photos — at the WebP file. Idempotent: already-optimized photos are skipped, so later restarts
/// cost a few file-existence checks. Original files are kept (a customer's saved cart or an old
/// e-mail may still link to them).
/// </summary>
public sealed class UploadedImageOptimizer : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(20);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UploadedImageOptimizer> _logger;

    public UploadedImageOptimizer(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<UploadedImageOptimizer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
            await OptimizeAllAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao otimizar as fotos já enviadas.");
        }
    }

    private async Task OptimizeAllAsync(CancellationToken ct)
    {
        var prefix = (_configuration["Uploads:PublicPath"] ?? "/api/uploads") + "/";
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        var urls = new HashSet<string>(StringComparer.Ordinal);
        urls.UnionWith(await db.Products.Where(p => p.ImageUrl != null && p.ImageUrl.StartsWith(prefix)).Select(p => p.ImageUrl!).ToListAsync(ct));
        urls.UnionWith(await db.Set<ProductImage>().Where(i => i.Url.StartsWith(prefix)).Select(i => i.Url).ToListAsync(ct));
        urls.UnionWith(await db.SiteImages.Where(i => i.Url.StartsWith(prefix)).Select(i => i.Url).ToListAsync(ct));
        urls.UnionWith(await db.GalleryImages.Where(i => i.Url.StartsWith(prefix)).Select(i => i.Url).ToListAsync(ct));
        urls.UnionWith(await db.ProductReviews.Where(r => r.PhotoUrl != null && r.PhotoUrl.StartsWith(prefix)).Select(r => r.PhotoUrl!).ToListAsync(ct));

        int converted = 0, failed = 0;
        foreach (var url in urls)
        {
            try
            {
                var optimizedUrl = await storage.OptimizeExistingAsync(url, ct);
                if (optimizedUrl is null || optimizedUrl == url) continue;

                await db.Products.Where(p => p.ImageUrl == url).ExecuteUpdateAsync(s => s.SetProperty(p => p.ImageUrl, optimizedUrl), ct);
                await db.Set<ProductImage>().Where(i => i.Url == url).ExecuteUpdateAsync(s => s.SetProperty(i => i.Url, optimizedUrl), ct);
                await db.SiteImages.Where(i => i.Url == url).ExecuteUpdateAsync(s => s.SetProperty(i => i.Url, optimizedUrl), ct);
                await db.GalleryImages.Where(i => i.Url == url).ExecuteUpdateAsync(s => s.SetProperty(i => i.Url, optimizedUrl), ct);
                await db.ProductReviews.Where(r => r.PhotoUrl == url).ExecuteUpdateAsync(s => s.SetProperty(r => r.PhotoUrl, optimizedUrl), ct);
                converted++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                _logger.LogWarning(ex, "Não foi possível otimizar a foto {Url}", url);
            }
        }

        _logger.LogInformation("Otimização de fotos: {Total} verificadas, {Converted} convertidas para WebP, {Failed} com falha", urls.Count, converted, failed);
    }
}
