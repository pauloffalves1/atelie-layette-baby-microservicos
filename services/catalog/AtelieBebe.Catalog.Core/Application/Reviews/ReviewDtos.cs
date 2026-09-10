namespace AtelieBebe.Catalog.Core.Application.Reviews;

public sealed record ProductReviewDto(
    Guid Id,
    Guid ProductId,
    string CustomerName,
    int Rating,
    string? Comment,
    string? PhotoUrl,
    DateTime CreatedAt);

public sealed record CreateReviewRequest(int Rating, string? Comment, string? PhotoUrl = null);

/// <summary>Homepage testimonial — a best approved review plus enough product info to link back to it.</summary>
public sealed record FeaturedReviewDto(
    Guid Id,
    string ProductName,
    string ProductSlug,
    string CustomerName,
    int Rating,
    string? Comment,
    string? PhotoUrl,
    DateTime CreatedAt);

/// <summary>Admin moderation queue row — includes the product name since reviews don't carry it themselves.</summary>
public sealed record AdminProductReviewDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string CustomerName,
    int Rating,
    string? Comment,
    string? PhotoUrl,
    bool Approved,
    DateTime CreatedAt);

/// <summary>Tells the frontend whether to show "write a review" for the current customer/product pair.</summary>
public sealed record ReviewEligibilityDto(bool HasPurchased, bool AlreadyReviewed);
