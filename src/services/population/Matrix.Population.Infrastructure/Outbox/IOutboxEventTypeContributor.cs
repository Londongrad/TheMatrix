namespace Matrix.Population.Infrastructure.Outbox
{
    public interface IOutboxEventTypeContributor
    {
        IReadOnlyDictionary<string, Type> EventTypes { get; }
    }
}
