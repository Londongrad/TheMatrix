namespace Matrix.SimulationCore.Infrastructure.Outbox
{
    public interface IOutboxEventTypeContributor
    {
        IReadOnlyDictionary<string, Type> EventTypes { get; }
    }
}
