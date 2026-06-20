namespace Matrix.Resources.Infrastructure.Outbox
{
    public interface IOutboxEventTypeContributor
    {
        IReadOnlyDictionary<string, Type> EventTypes { get; }
    }
}
