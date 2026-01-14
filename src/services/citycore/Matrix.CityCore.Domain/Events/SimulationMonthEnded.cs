namespace Matrix.CityCore.Domain.Events
{
    public sealed record class SimulationMonthEnded(
        int Year,
        int Month) : ICityDomainEvent;
}
