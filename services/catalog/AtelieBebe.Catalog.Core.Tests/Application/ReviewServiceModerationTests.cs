using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.Catalog.Core.Application.Reviews;
using AtelieBebe.Catalog.Core.Domain.Entities;
using AtelieBebe.Catalog.Core.Domain.Repositories;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AtelieBebe.Catalog.Core.Tests.Application;

/// <summary>Covers the review pre-screen wired into review creation (Requisito 20, RF29) — the
/// screener is an application-layer collaborator (<see cref="IReviewModerationScreener"/>), so
/// these tests mock it rather than exercising the real Anthropic call; the fallback-on-error
/// behavior itself lives inside the concrete screener and is out of scope for these tests.</summary>
public class ReviewServiceModerationTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IProductReviewRepository _reviewRepository = Substitute.For<IProductReviewRepository>();
    private readonly ICatalogUnitOfWork _unitOfWork = Substitute.For<ICatalogUnitOfWork>();
    private readonly IOrdersServiceClient _ordersServiceClient = Substitute.For<IOrdersServiceClient>();
    private readonly IReviewModerationScreener _screener = Substitute.For<IReviewModerationScreener>();
    private readonly Product _product = Product.Create("Kit Ombro e Boca Nuvem", "kit-ombro-e-boca-nuvem", "Descrição", Money.FromReais(129.90m), "Kit Ombro e Boca", null);

    public ReviewServiceModerationTests()
    {
        _unitOfWork.Products.Returns(_productRepository);
        _unitOfWork.ProductReviews.Returns(_reviewRepository);
        _productRepository.GetByIdAsync(_product.Id, Arg.Any<CancellationToken>()).Returns(_product);
        _ordersServiceClient.CustomerHasPurchasedProductAsync(Arg.Any<Guid>(), _product.Id, Arg.Any<CancellationToken>()).Returns(true);
        _reviewRepository.ExistsAsync(_product.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    private ReviewService BuildService() => new(
        _unitOfWork,
        _ordersServiceClient,
        _screener,
        Substitute.For<ILogger<ReviewService>>());

    [Fact]
    public async Task CreateAsync_ScreenerFlagsComment_SetsModerationFlagOnReview()
    {
        _screener.ScreenAsync("comentário impróprio", Arg.Any<CancellationToken>()).Returns("agressivo");
        var request = new CreateReviewRequest(Rating: 3, Comment: "comentário impróprio");

        ProductReview? saved = null;
        _reviewRepository.Add(Arg.Do<ProductReview>(r => saved = r));

        var dto = await BuildService().CreateAsync(_product.Id, Guid.NewGuid(), "Cliente Teste", request);

        Assert.Equal("agressivo", saved?.ModerationFlag);
        Assert.Equal(dto.ProductId, saved?.ProductId);
    }

    [Fact]
    public async Task CreateAsync_ScreenerFindsNothing_LeavesModerationFlagNull()
    {
        _screener.ScreenAsync("ótimo produto", Arg.Any<CancellationToken>()).Returns((string?)null);
        var request = new CreateReviewRequest(Rating: 5, Comment: "ótimo produto");

        ProductReview? saved = null;
        _reviewRepository.Add(Arg.Do<ProductReview>(r => saved = r));

        await BuildService().CreateAsync(_product.Id, Guid.NewGuid(), "Cliente Teste", request);

        Assert.Null(saved?.ModerationFlag);
    }

    [Fact]
    public async Task CreateAsync_NoComment_NeverCallsScreener()
    {
        var request = new CreateReviewRequest(Rating: 4, Comment: null);

        await BuildService().CreateAsync(_product.Id, Guid.NewGuid(), "Cliente Teste", request);

        await _screener.DidNotReceive().ScreenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ModerationFlag_IsNeverExposedOnThePublicCustomerFacingDto()
    {
        // ProductReviewDto is what ListByProductAsync/CreateAsync return to customers/visitors —
        // ModerationFlag must stay confined to AdminProductReviewDto (the admin-only shape).
        var publicDtoProperties = typeof(ProductReviewDto).GetProperties().Select(p => p.Name);

        Assert.DoesNotContain("ModerationFlag", publicDtoProperties);
    }
}
