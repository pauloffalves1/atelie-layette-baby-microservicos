namespace AtelieBebe.SharedKernel.Common;

/// <summary>
/// Normalizes paging inputs. The caller (an API endpoint) is responsible for choosing the
/// context-appropriate default page size when the query string omits it — C#'s optional-parameter
/// default already covers that case. This only guards against a caller-supplied value that is
/// present but out of range (zero, negative, or absurdly large).
/// </summary>
public static class Pagination
{
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize) =>
        (Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize));
}
