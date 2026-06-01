using System.Text.Json;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Matrix.SimulationCore.Infrastructure.Outbox.RabbitMq;
using Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Outbox;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox
{
    public sealed class MassTransitOutboxMessagePublisherTests
    {
        [Fact]
        public async Task PublishAsync_WhenMessageTypeIsUnsupported_ThrowsNotSupportedException()
        {
            MassTransitOutboxMessagePublisher publisher = CreatePublisher();

            NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(()
                => publisher.PublishAsync(
                    messageId: Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    type: "simulationcore.unknown.v1",
                    payloadJson: "{}",
                    cancellationToken: CancellationToken.None));

            Assert.Contains(
                expectedSubstring: "not supported",
                actualString: exception.Message);
        }

        [Fact]
        public async Task PublishAsync_WhenPayloadDeserializesToNull_ThrowsInvalidOperationException()
        {
            MassTransitOutboxMessagePublisher publisher = CreatePublisher();

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => publisher.PublishAsync(
                    messageId: Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    type: SimulationCoreEventTypes.ClassicCityCreatedV1,
                    payloadJson: "null",
                    cancellationToken: CancellationToken.None));

            Assert.Contains(
                expectedSubstring: "Failed to deserialize outbox payload",
                actualString: exception.Message);
        }

        [Fact]
        public async Task PublishAsync_WhenPayloadJsonIsMalformed_ThrowsJsonException()
        {
            MassTransitOutboxMessagePublisher publisher = CreatePublisher();

            await Assert.ThrowsAsync<JsonException>(() => publisher.PublishAsync(
                messageId: Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                type: SimulationCoreEventTypes.ClassicCityCreatedV1,
                payloadJson: "{",
                cancellationToken: CancellationToken.None));
        }

        private static MassTransitOutboxMessagePublisher CreatePublisher()
        {
            var registry = new OutboxEventTypeRegistry(
            [
                new SimulationCoreOutboxEventTypeContributor(),
                new ClassicCityOutboxEventTypeContributor()
            ]);

            return new MassTransitOutboxMessagePublisher(
                publishEndpoint: null!,
                eventTypeRegistry: registry);
        }
    }
}
