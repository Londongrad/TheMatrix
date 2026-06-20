using System.Text.Json;
using Matrix.Resources.Infrastructure.Outbox;
using Matrix.Resources.Infrastructure.Outbox.RabbitMq;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Outbox;
using Xunit;

namespace Matrix.Resources.Infrastructure.Tests.Outbox
{
    public sealed class MassTransitOutboxMessagePublisherTests
    {
        [Fact]
        public async Task PublishAsync_WhenMessageTypeIsUnsupported_ThrowsNotSupportedException()
        {
            var publisher = CreatePublisher();

            NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(()
                => publisher.PublishAsync(
                    messageId: Guid.Parse("f0529b17-74bb-4e28-b0e1-0b436eeb2b60"),
                    type: "resources.unknown.v1",
                    payloadJson: "{}",
                    cancellationToken: CancellationToken.None));

            Assert.Contains(
                expectedSubstring: "not supported",
                actualString: exception.Message);
        }

        [Fact]
        public async Task PublishAsync_WhenPayloadDeserializesToNull_ThrowsInvalidOperationException()
        {
            var publisher = CreatePublisher();

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => publisher.PublishAsync(
                    messageId: Guid.Parse("0a4ae14d-2067-4fa4-9b57-ebd69f1ec11d"),
                    type: ClassicCityOutboxEventTypes.ClassicCityStockpileSnapshotV1,
                    payloadJson: "null",
                    cancellationToken: CancellationToken.None));

            Assert.Contains(
                expectedSubstring: "Failed to deserialize outbox payload",
                actualString: exception.Message);
        }

        [Fact]
        public async Task PublishAsync_WhenPayloadJsonIsMalformed_ThrowsJsonException()
        {
            var publisher = CreatePublisher();

            await Assert.ThrowsAsync<JsonException>(() => publisher.PublishAsync(
                messageId: Guid.Parse("56131248-e854-4ecc-a7f3-31ddb4d3d2b5"),
                type: ClassicCityOutboxEventTypes.ClassicCityOperationalExpenseIncurredV1,
                payloadJson: "{",
                cancellationToken: CancellationToken.None));
        }

        private static MassTransitOutboxMessagePublisher CreatePublisher()
        {
            var registry = new OutboxEventTypeRegistry([new ClassicCityOutboxEventTypeContributor()]);
            return new MassTransitOutboxMessagePublisher(
                publishEndpoint: null!,
                eventTypeRegistry: registry);
        }
    }
}
