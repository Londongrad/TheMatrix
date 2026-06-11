namespace Matrix.SimulationSystems.Infrastructure.Outbox
{
    public interface IOutboxEventTypeContributor
    {
        IReadOnlyDictionary<string, Type> EventTypes { get; }
    }
}
