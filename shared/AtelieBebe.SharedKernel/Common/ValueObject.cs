namespace AtelieBebe.SharedKernel.Common;

public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode() =>
        GetEqualityComponents().Aggregate(17, (hash, value) => HashCode.Combine(hash, value));

    // Intentionally no `==`/`!=` operator overloads: EF Core's query translator cannot push a
    // custom operator method through a value-converted property (e.g. `c.Email == someEmail` in a
    // LINQ query), which would throw at runtime. Plain `Equals()` is translated correctly by EF Core
    // and is what repositories use; callers writing domain code should call `.Equals(...)` too.
}
