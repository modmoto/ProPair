namespace ProPair.Domain.Common;

public interface IAggregateRoot
{
    long Version { get; }
    void SetPersistedVersion(long version);
}

public abstract class AggregateRoot<TId>(TId id) : IAggregateRoot where TId : notnull
{
    private readonly List<IDomainEvent> _events = [];

    public TId Id { get; } = id;

    /// <summary>Optimistic concurrency version, maintained by the persistence adapter.</summary>
    public long Version { get; private set; }
    public void SetPersistedVersion(long version) => Version = version;
    public IReadOnlyList<IDomainEvent> DomainEvents => _events;

    protected void Raise(IDomainEvent domainEvent) => _events.Add(domainEvent);
    public void ClearDomainEvents() => _events.Clear();
}

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}

public sealed class DomainException(string message) : Exception(message);
