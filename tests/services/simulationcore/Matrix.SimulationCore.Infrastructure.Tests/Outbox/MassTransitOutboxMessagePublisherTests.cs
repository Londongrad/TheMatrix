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
}
