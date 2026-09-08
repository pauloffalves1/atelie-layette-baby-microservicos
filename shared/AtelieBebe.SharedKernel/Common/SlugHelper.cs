using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AtelieBebe.SharedKernel.Common;

public static partial class SlugHelper
{
    public static string Slugify(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var withoutDiacritics = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        var slug = NonAlphaNumericRegex().Replace(withoutDiacritics, "-");
        return slug.Trim('-');
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlphaNumericRegex();
}
