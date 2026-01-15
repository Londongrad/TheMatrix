namespace Matrix.CityCore.Domain.Events
{
    /// <summary>
    ///     Marker interface for domain events.
    /// </summary>
    public interface IDomainEvent
    {
        DateTimeOffset OccurredAtUtc { get; }
    }
}
