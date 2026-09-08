using System.Linq;

namespace AtelieBebe.SharedKernel.Web;

/// <summary>Builds human-readable "campo: antes → depois" fragments for admin audit log details.</summary>
public static class AuditDiff
{
    /// <summary>Null when the two values are equal — nothing to report for this field.</summary>
    public static string? Field(string name, object? oldValue, object? newValue)
    {
        if (Equals(oldValue, newValue)) return null;
        return $"{name}: {Format(oldValue)} → {Format(newValue)}";
    }

    /// <summary>Combines Field() results, dropping unchanged ones; a plain fallback if nothing changed.</summary>
    public static string Join(params string?[] fields)
    {
        var changes = fields.Where(f => f is not null).ToList();
        return changes.Count > 0 ? string.Join("; ", changes) : "sem alterações";
    }

    private static string Format(object? value) => value switch
    {
        null => "(vazio)",
        "" => "(vazio)",
        bool b => b ? "sim" : "não",
        decimal d => d.ToString("0.00"),
        _ => value.ToString() ?? "",
    };
}
