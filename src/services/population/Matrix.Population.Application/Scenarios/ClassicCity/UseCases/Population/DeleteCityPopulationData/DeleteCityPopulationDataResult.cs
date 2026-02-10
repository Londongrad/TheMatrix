namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.DeleteCityPopulationData
{
    public enum DeleteCityPopulationDataStatus
    {
        Applied = 0,
        Duplicate = 1,
        Stale = 2
    }

    public sealed record DeleteCityPopulationDataResult(DeleteCityPopulationDataStatus Status);
}
