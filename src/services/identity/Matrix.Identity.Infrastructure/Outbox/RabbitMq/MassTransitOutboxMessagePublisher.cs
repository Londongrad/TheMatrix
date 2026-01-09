using System.Text.Json;
using MassTransit;
using Matrix.Identity.Contracts.Internal.Events;
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
            // Минимальный вариант: поддерживаем нужные типы явно (без reflection).
            if (type == typeof(UserSecurityStateChangedV1).FullName)
            {
                UserSecurityStateChangedV1 evt = JsonSerializer.Deserialize<UserSecurityStateChangedV1>(
                                                     json: payloadJson,
                                                     options: Json) ??
                                                 throw new InvalidOperationException(
                                                     "Outbox payload deserialization failed.");

                // Publish = event broadcast всем подписчикам по типу сообщения :contentReference[oaicite:2]{index=2}
                return publishEndpoint.Publish(
                    message: evt,
                    cancellationToken: cancellationToken);
            }

            throw new NotSupportedException($"Outbox message type '{type}' is not supported.");
        }
    }
}
