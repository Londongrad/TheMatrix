using System.Text.Json;
using Matrix.Identity.Contracts.Internal.Events;
using Matrix.Identity.Infrastructure.Outbox.RabbitMq;
using Xunit;

namespace Matrix.Identity.Infrastructure.Tests.Outbox;

public sealed class MassTransitOutboxMessagePublisherTests
{
    [Fact]
    public async Task PublishAsync_WhenMessageTypeIsUnsupported_ThrowsNotSupportedException()
    {
        var publisher = new MassTransitOutboxMessagePublisher(null!);

        NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => publisher.PublishAsync(
                messageId: Guid.Parse("f03f3571-3564-4f16-b0ad-57bbf31be8df"),
                type: "identity.unknown.v1",
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
                messageId: Guid.Parse("acb7a982-f2e5-4973-8a7b-faed9f7332fa"),
                type: InternalEventTypes.UserSecurityStateChangedV1,
                payloadJson: "null",
                cancellationToken: CancellationToken.None));

        Assert.Contains("Failed to deserialize outbox payload", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_WhenPayloadJsonIsMalformed_ThrowsJsonException()
    {
        var publisher = new MassTransitOutboxMessagePublisher(null!);

        await Assert.ThrowsAsync<JsonException>(
            () => publisher.PublishAsync(
                messageId: Guid.Parse("3818f69d-b5ae-450b-b76b-3a0959895351"),
                type: InternalEventTypes.UserSecurityStateChangedV1,
                payloadJson: "{",
                cancellationToken: CancellationToken.None));
    }
}
