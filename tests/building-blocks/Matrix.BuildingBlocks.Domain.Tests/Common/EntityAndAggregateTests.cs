using Matrix.BuildingBlocks.Domain.Events;
using Matrix.BuildingBlocks.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.BuildingBlocks.Domain.Tests.Common
{
    public sealed class EntityAndAggregateTests
    {
        [Fact]
        public void EntityEquality_WhenTypeAndIdMatch_ReturnsTrue()
        {
            var id = Guid.NewGuid();
            TestEntity left = new(id);
            TestEntity right = new(id);

            Assert.Equal(
                expected: left,
                actual: right);
            Assert.Equal(
                expected: left.GetHashCode(),
                actual: right.GetHashCode());
        }

        [Fact]
        public void EntityEquality_WhenConcreteTypesDiffer_ReturnsFalse()
        {
            var id = Guid.NewGuid();
            TestEntity entity = new(id);
            AnotherTestEntity other = new(id);

            Assert.False(entity.Equals(other));
        }

        [Fact]
        public void AggregateRoot_WhenEventIsRaised_TracksAndClearsDomainEvents()
        {
            TestAggregate aggregate = new(Guid.NewGuid());
            TestDomainEvent domainEvent = new("created");

            aggregate.Raise(domainEvent);

            Assert.Single(aggregate.DomainEvents);
            Assert.Same(
                expected: domainEvent,
                actual: aggregate.DomainEvents.Single());

            aggregate.ClearDomainEvents();

            Assert.Empty(aggregate.DomainEvents);
        }

        [Fact]
        public void DomainEventBase_WhenCreated_SetsOccurredAtUtc()
        {
            DateTimeOffset before = TimeProvider.System.GetUtcNow()
               .AddSeconds(-1);
            IDomainEvent domainEvent = new TestDomainEvent("created");
            DateTimeOffset after = TimeProvider.System.GetUtcNow()
               .AddSeconds(1);

            Assert.InRange(
                actual: domainEvent.OccurredAtUtc,
                low: before,
                high: after);
        }
    }
}
