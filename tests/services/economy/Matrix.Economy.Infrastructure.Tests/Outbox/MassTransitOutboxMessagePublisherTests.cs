using System.Text.Json;
using Matrix.Economy.Infrastructure.Outbox;
using Matrix.Economy.Infrastructure.Outbox.RabbitMq;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Outbox;
using Xunit;

namespace Matrix.Economy.Infrastructure.Tests.Outbox
{
    public sealed class MassTransitOutboxMessagePublisherTests
    {
        [Fact]
        public async Task PublishAsync_WhenMessageTypeIsUnsupported_ThrowsNotSupportedException()
        {
            var publisher = new MassTransitOutboxMessagePublisher(
                null!,
                new OutboxEventTypeRegistry([]));

            NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(()
                => publisher.PublishAsync(
                    messageId: Guid.Parse("f7fbdf2d-ac04-46d7-ac89-22bb5075fb3c"),
                    type: "economy.unknown.v1",
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
                    messageId: Guid.Parse("1bc95fa9-ff56-452a-94b1-13cefdad1164"),
                    type: ClassicCityOutboxEventTypes.ClassicCityCostOfLivingSnapshotV1,
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
                messageId: Guid.Parse("a0cec4fc-a853-45cb-a371-8c55295d3048"),
                type: ClassicCityOutboxEventTypes.ClassicCityEmployerFinancialStressBatchV1,
                payloadJson: "{",
                cancellationToken: CancellationToken.None));
        }

        private static MassTransitOutboxMessagePublisher CreatePublisher()
        {
            return new MassTransitOutboxMessagePublisher(
                null!,
                new OutboxEventTypeRegistry([new ClassicCityOutboxEventTypeContributor()]));
        }
    }
}
