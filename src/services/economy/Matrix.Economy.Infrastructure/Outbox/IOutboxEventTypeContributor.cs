namespace Matrix.Economy.Infrastructure.Outbox
{
    public interface IOutboxEventTypeContributor
    {
        IReadOnlyDictionary<string, Type> EventTypes { get; }
    }
}
