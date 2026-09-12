namespace AtelieBebe.Orders.Core.Application.Abstractions;

/// <summary>Pre-screens embroidery/personalization text before production starts.</summary>
public interface IEmbroideryModerationScreener
{
    /// <summary>Returns "ofensivo", "dado_pessoal", or null when the text looks fine (or the screen
    /// couldn't run) — never blocks the order, only flags the item for a human to double-check.</summary>
    Task<string?> ScreenAsync(string embroideryText, CancellationToken ct = default);
}
