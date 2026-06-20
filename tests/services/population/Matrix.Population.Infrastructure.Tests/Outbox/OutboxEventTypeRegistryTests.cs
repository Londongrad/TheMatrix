using Matrix.Population.Infrastructure.Outbox;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Outbox
{
    public sealed class OutboxEventTypeRegistryTests
    {
        [Fact]
        public void Resolve_WhenContributorContainsEventType_ReturnsClrType()
        {
            var registry = new OutboxEventTypeRegistry(
                contributors:
                [
                    new StubContributor(
                        new Dictionary<string, Type>
                        {
                            ["population.test-event.v1"] = typeof(TestEvent)
                        })
                ]);

            Assert.Equal(
                expected: typeof(TestEvent),
                actual: registry.Resolve("population.test-event.v1"));
            Assert.Equal(
                expected: 1,
                actual: registry.Count);
        }

        [Fact]
        public void Constructor_WhenContributorsRepeatEventType_ThrowsInvalidOperationException()
        {
            IOutboxEventTypeContributor[] contributors =
            [
                new StubContributor(
                    new Dictionary<string, Type>
                    {
                        ["population.test-event.v1"] = typeof(TestEvent)
                    }),
                new StubContributor(
                    new Dictionary<string, Type>
                    {
                        ["population.test-event.v1"] = typeof(OtherTestEvent)
                    })
            ];

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => new OutboxEventTypeRegistry(contributors));

            Assert.Contains(
                expectedSubstring: "population.test-event.v1",
                actualString: exception.Message);
        }

        [Fact]
        public void Resolve_WhenEventTypeIsUnknown_ThrowsNotSupportedException()
        {
            var registry = new OutboxEventTypeRegistry(contributors: []);

            NotSupportedException exception = Assert.Throws<NotSupportedException>(
                () => registry.Resolve("population.unknown.v1"));

            Assert.Contains(
                expectedSubstring: "population.unknown.v1",
                actualString: exception.Message);
        }

        private sealed record TestEvent;

        private sealed record OtherTestEvent;

        private sealed class StubContributor(IReadOnlyDictionary<string, Type> eventTypes)
            : IOutboxEventTypeContributor
        {
            public IReadOnlyDictionary<string, Type> EventTypes { get; } = eventTypes;
        }
    }
}
