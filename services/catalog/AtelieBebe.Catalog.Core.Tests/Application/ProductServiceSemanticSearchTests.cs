using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.Catalog.Core.Application.Products;
using AtelieBebe.Catalog.Core.Domain.Entities;
using AtelieBebe.Catalog.Core.Domain.Repositories;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AtelieBebe.Catalog.Core.Tests.Application;

/// <summary>Covers the storefront's semantic search orchestration (Requisito 19, RF28) — the
/// translator is an application-layer collaborator (<see cref="ISemanticSearchTranslator"/>), so
/// these tests mock it rather than exercising the real Anthropic call; the fallback-on-error
/// behavior itself lives inside the concrete translator and is out of scope for these tests.</summary>
public class ProductServiceSemanticSearchTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICatalogUnitOfWork _unitOfWork = Substitute.For<ICatalogUnitOfWork>();
    private readonly ISemanticSearchTranslator _translator = Substitute.For<ISemanticSearchTranslator>();

    public ProductServiceSemanticSearchTests()
    {
        _unitOfWork.Products.Returns(_productRepository);
        _productRepository.ListCategoriesAsync(null, false, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "Fralda de Boca", "Fralda de Ombro", "Kit Ombro e Boca" });
    }

    private ProductService BuildService() => new(
        _unitOfWork,
        Substitute.For<IOrdersServiceClient>(),
        _translator,
        new MemoryCache(new MemoryCacheOptions()),
        new ProductCacheInvalidator(),
        Substitute.For<ILogger<ProductService>>());

    private static Product CreateProduct(string name, string category) =>
        Product.Create(name, name.ToLowerInvariant().Replace(' ', '-'), "Descrição", Money.FromReais(59.90m), category, null);

    [Fact]
    public async Task SearchAsync_TranslatesQueryWithKnownCategories_AndReturnsMatchingProducts()
    {
        var filters = new ProductSearchFilters("Fralda de Boca", null, 80m, "algodão", OnlyOnPromotion: false);
        _translator.TranslateAsync("body de algodão até 80 reais", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(filters);

        var product = CreateProduct("Fralda de Boca Bordada", "Fralda de Boca");
        _productRepository.SearchAsync(filters, 1, 20, null, false, Arg.Any<CancellationToken>())
            .Returns(([product], 1));

        var result = await BuildService().SearchAsync("body de algodão até 80 reais", 1, 20);

        Assert.Equal(1, result.TotalItems);
        Assert.Equal(product.Id, Assert.Single(result.Items).Id);
        await _translator.Received(1).TranslateAsync(
            "body de algodão até 80 reais",
            Arg.Is<IReadOnlyList<string>>(c => c.Contains("Fralda de Boca") && c.Contains("Fralda de Ombro")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_NoMatches_ReturnsEmptyPageWithoutThrowing()
    {
        var filters = new ProductSearchFilters(null, null, null, "unicórnio", OnlyOnPromotion: false);
        _translator.TranslateAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(filters);
        _productRepository.SearchAsync(filters, 1, 20, null, false, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Product>(), 0));

        var result = await BuildService().SearchAsync("unicórnio", 1, 20);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
    }
}
