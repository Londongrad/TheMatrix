using System.Text.Json;
using MassTransit;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;

namespace Matrix.CityCore.Infrastructure.Outbox.RabbitMq
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

            return publishEndpoint.Publish(
                message: evt,
                messageType: clrType,
                publishPipe: Pipe.Execute<PublishContext>(context => { context.MessageId = messageId; }),
                cancellationToken: cancellationToken);
        }
    }
}
