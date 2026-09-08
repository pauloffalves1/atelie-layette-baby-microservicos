namespace AtelieBebe.SharedKernel.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected Entity() { }

    protected Entity(Guid id) => Id = id;

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}

/// <summary>
/// Marker for entities that are the entry point (root) of an aggregate.
/// Only aggregate roots are exposed through repositories; child entities
/// are reached and mutated exclusively through the root.
/// </summary>
public interface IAggregateRoot
{
}
