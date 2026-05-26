namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    DeleteCitySystemsData
{
    public enum DeleteCitySystemsDataStatus
    {
        Applied = 0,
        Duplicate = 1,
        Stale = 2
    }

    public sealed record DeleteCitySystemsDataResult(DeleteCitySystemsDataStatus Status);
}
