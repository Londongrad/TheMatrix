using System.Text.Json;
using MassTransit;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;

namespace Matrix.Economy.Infrastructure.Outbox.RabbitMq
{
    public sealed class MassTransitOutboxMessagePublisher(
        IPublishEndpoint publishEndpoint,
        OutboxEventTypeRegistry eventTypeRegistry)
        : IOutboxMessagePublisher
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public Task PublishAsync(
            Guid messageId,
            string type,
            string payloadJson,
            CancellationToken cancellationToken)
        {
            Type clrType = eventTypeRegistry.Resolve(type);

            object evt = JsonSerializer.Deserialize(
                             json: payloadJson,
                             returnType: clrType,
                             options: Json) ??
                         throw new InvalidOperationException(
                             $"Failed to deserialize outbox payload for type '{type}'.");

            return publishEndpoint.Publish(
                message: evt,
                messageType: clrType,
                cancellationToken: cancellationToken);
        }
    }
}
