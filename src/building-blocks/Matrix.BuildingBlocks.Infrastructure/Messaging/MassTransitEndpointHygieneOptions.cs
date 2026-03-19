namespace Matrix.BuildingBlocks.Infrastructure.Messaging
{
    public sealed class MassTransitEndpointHygieneOptions
    {
        public const string SectionName = "RabbitMq:EndpointHygiene";

        public bool DiscardSkippedMessages { get; init; } = true;
    }
}
