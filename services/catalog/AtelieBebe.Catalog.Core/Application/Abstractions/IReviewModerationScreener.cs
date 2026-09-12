namespace AtelieBebe.Catalog.Core.Application.Abstractions;

/// <summary>Pre-screens a review comment for spam/abuse before it reaches the admin's manual queue.</summary>
public interface IReviewModerationScreener
{
    /// <summary>Returns "spam", "agressivo", "dado_pessoal", or null when the comment looks fine
    /// (or the screen couldn't run) — never blocks the review, only flags it for faster triage.</summary>
    Task<string?> ScreenAsync(string comment, CancellationToken ct = default);
}
