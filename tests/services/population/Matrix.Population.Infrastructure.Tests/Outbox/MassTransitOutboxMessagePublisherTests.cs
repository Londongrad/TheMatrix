using System.Text.Json;
using Matrix.Population.Infrastructure.Outbox;
using Matrix.Population.Infrastructure.Outbox.RabbitMq;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Outbox;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Outbox
{
    public sealed class MassTransitOutboxMessagePublisherTests
    {
        [Fact]
        public async Task PublishAsync_WhenMessageTypeIsUnsupported_ThrowsNotSupportedException()
        {
            var publisher = new MassTransitOutboxMessagePublisher(null!);

            NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(()
                => publisher.PublishAsync(
                    messageId: Guid.Parse("271f7832-c8fe-42ec-a405-a8beec45a34a"),
                    type: "population.unknown.v1",
                    payloadJson: "{}",
                    cancellationToken: CancellationToken.None));

            Assert.Contains(
                expectedSubstring: "not supported",
                actualString: exception.Message);
        }

        [Fact]
        public async Task PublishAsync_WhenPayloadDeserializesToNull_ThrowsInvalidOperationException()
        {
            var publisher = new MassTransitOutboxMessagePublisher(null!);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => publisher.PublishAsync(
                    messageId: Guid.Parse("6b9282cb-c3f4-418f-95bd-b60670bd8f88"),
                    type: ClassicCityOutboxEventTypes.CityEconomyDailySettlementV1,
                    payloadJson: "null",
                    cancellationToken: CancellationToken.None));

            Assert.Contains(
                expectedSubstring: "Failed to deserialize outbox payload",
                actualString: exception.Message);
        }

        [Fact]
        public async Task PublishAsync_WhenPayloadJsonIsMalformed_ThrowsJsonException()
        {
            var publisher = new MassTransitOutboxMessagePublisher(null!);

            await Assert.ThrowsAsync<JsonException>(() => publisher.PublishAsync(
                messageId: Guid.Parse("8351fa7f-9c7a-4ca1-9816-6b69ea90d427"),
                type: ClassicCityOutboxEventTypes.ClassicCityHouseholdAccountSyncBatchV1,
                payloadJson: "{",
                cancellationToken: CancellationToken.None));
        }
    }
}
