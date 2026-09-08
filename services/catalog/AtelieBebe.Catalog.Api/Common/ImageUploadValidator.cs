using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.Catalog.Api.Common;

/// <summary>Shared validation for admin image-upload endpoints (site images, product photos, gallery).</summary>
public static class ImageUploadValidator
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp",
    };

    private const long MaxFileSizeBytes = 8 * 1024 * 1024; // 8 MB

    /// <summary>Validates the file and returns its lowercase extension (including the leading dot).</summary>
    public static string ValidateAndGetExtension(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            throw new ConflictException("Formato de imagem não suportado. Use JPG, PNG ou WEBP.");

        if (file.Length == 0)
            throw new ConflictException("O arquivo enviado está vazio.");
        if (file.Length > MaxFileSizeBytes)
            throw new ConflictException("A imagem deve ter no máximo 8MB.");

        return extension.ToLowerInvariant();
    }
}
