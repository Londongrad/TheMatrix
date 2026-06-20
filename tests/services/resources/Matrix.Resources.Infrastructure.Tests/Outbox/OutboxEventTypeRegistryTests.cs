using Matrix.Resources.Infrastructure.Outbox;
using Xunit;

namespace Matrix.Resources.Infrastructure.Tests.Outbox
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
                            ["resources.test-event.v1"] = typeof(TestEvent)
                        })
                ]);

            Assert.Equal(
                expected: typeof(TestEvent),
                actual: registry.Resolve("resources.test-event.v1"));
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
                        ["resources.test-event.v1"] = typeof(TestEvent)
                    }),
                new StubContributor(
                    new Dictionary<string, Type>
                    {
                        ["resources.test-event.v1"] = typeof(OtherTestEvent)
                    })
            ];

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => new OutboxEventTypeRegistry(contributors));

            Assert.Contains(
                expectedSubstring: "resources.test-event.v1",
                actualString: exception.Message);
        }

        [Fact]
        public void Resolve_WhenEventTypeIsUnknown_ThrowsNotSupportedException()
        {
            var registry = new OutboxEventTypeRegistry(contributors: []);

            NotSupportedException exception = Assert.Throws<NotSupportedException>(
                () => registry.Resolve("resources.unknown.v1"));

            Assert.Contains(
                expectedSubstring: "resources.unknown.v1",
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
