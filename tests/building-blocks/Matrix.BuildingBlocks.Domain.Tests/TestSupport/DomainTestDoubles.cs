using Matrix.BuildingBlocks.Domain.Common;
using Matrix.BuildingBlocks.Domain.Events;

namespace Matrix.BuildingBlocks.Domain.Tests.TestSupport;

internal enum TestMode
{
    Alpha = 1,
    Beta = 2
}

internal sealed class TestEntity(Guid id) : Entity<Guid>(id);

internal sealed class AnotherTestEntity(Guid id) : Entity<Guid>(id);

internal sealed record TestDomainEvent(string Name) : DomainEventBase;

internal sealed class TestAggregate(Guid id) : AggregateRoot<Guid>(id)
{
    public void Raise(IDomainEvent domainEvent)
    {
        AddDomainEvent(domainEvent);
    }
}
