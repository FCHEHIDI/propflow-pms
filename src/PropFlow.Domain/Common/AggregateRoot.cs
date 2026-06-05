namespace PropFlow.Domain.Common;

public abstract class AggregateRoot
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid TenantId { get; protected set; }

    private readonly List<object> _domainEvents = [];
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(object @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
