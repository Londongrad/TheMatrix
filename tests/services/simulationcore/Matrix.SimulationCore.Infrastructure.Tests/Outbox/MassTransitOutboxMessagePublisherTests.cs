using Matrix.SimulationCore.Infrastructure.Outbox;
using Matrix.SimulationCore.Infrastructure.Outbox.RabbitMq;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox;

public sealed class MassTransitOutboxMessagePublisherTests
{
    [Fact]
    public async Task PublishAsync_WhenMessageTypeIsUnsupported_ThrowsNotSupportedException()
    {
        var publisher = new MassTransitOutboxMessagePublisher(null!);

        NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => publisher.PublishAsync(
                messageId: Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                type: "simulationcore.unknown.v1",
                payloadJson: "{}",
                cancellationToken: CancellationToken.None));

        Assert.Contains("not supported", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_WhenPayloadDeserializesToNull_ThrowsInvalidOperationException()
    {
        var publisher = new MassTransitOutboxMessagePublisher(null!);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(
                messageId: Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                type: IntegrationEventTypes.CityCreatedV1,
                payloadJson: "null",
                cancellationToken: CancellationToken.None));

        Assert.Contains("Failed to deserialize outbox payload", exception.Message);
    }
}
