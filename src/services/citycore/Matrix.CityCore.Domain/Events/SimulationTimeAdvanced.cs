namespace Matrix.CityCore.Domain.Events
{
    public sealed record class SimulationTimeAdvanced(DateTime NewTime) : ICityDomainEvent;
}
