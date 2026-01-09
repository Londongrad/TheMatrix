using System.Text.Json;
using MassTransit;
using Matrix.Identity.Infrastructure.Outbox.Abstractions;

namespace Matrix.Identity.Infrastructure.Outbox.RabbitMq
{
    public sealed class MassTransitOutboxMessagePublisher(IPublishEndpoint publishEndpoint)
        : IOutboxMessagePublisher
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public Task PublishAsync(
            Guid messageId,
            string type,
            string payloadJson,
            CancellationToken cancellationToken)
        {
            if (!OutboxEventTypeMap.Map.TryGetValue(
                    key: type,
                    value: out Type? clrType))
                throw new NotSupportedException($"Outbox message type '{type}' is not supported.");

            object evt = JsonSerializer.Deserialize(
                             json: payloadJson,
                             returnType: clrType,
                             options: Json) ??
                         throw new InvalidOperationException(
                             $"Failed to deserialize outbox payload for type '{type}'.");

            // публикуем с CLR-типом
            return publishEndpoint.Publish(
                message: evt,
                messageType: clrType,
                cancellationToken: cancellationToken);
        }
    }
}
