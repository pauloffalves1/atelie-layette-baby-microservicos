using AtelieBebe.Catalog.Core.Application.Products;

namespace AtelieBebe.Catalog.Core.Application.Abstractions;

/// <summary>Translates a free-text storefront search query into structured product filters via an LLM call.</summary>
public interface ISemanticSearchTranslator
{
    Task<ProductSearchFilters> TranslateAsync(string naturalLanguageQuery, IReadOnlyList<string> knownCategories, CancellationToken ct = default);
}
