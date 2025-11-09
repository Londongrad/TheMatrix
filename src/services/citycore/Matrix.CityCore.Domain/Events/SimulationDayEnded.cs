namespace Matrix.CityCore.Domain.Events
{
    public sealed record class SimulationDayEnded(DateOnly Day) : ICityDomainEvent;
}
