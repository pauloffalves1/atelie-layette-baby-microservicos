namespace AtelieBebe.Catalog.Core.Application.Abstractions;

/// <summary>Drafts a sales description for a product, for the admin to review and edit before saving.</summary>
public interface IProductDescriptionGenerator
{
    Task<string> GenerateAsync(string name, string category, CancellationToken ct = default);
}
