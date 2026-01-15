namespace Matrix.CityCore.Domain.Events
{
    /// <summary>
    ///     Base implementation for domain events.
    /// </summary>
    public abstract record DomainEventBase : IDomainEvent
    {
        public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;
    }
}
