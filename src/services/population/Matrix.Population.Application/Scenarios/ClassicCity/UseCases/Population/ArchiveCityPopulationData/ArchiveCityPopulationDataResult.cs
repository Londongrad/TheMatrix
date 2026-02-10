namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ArchiveCityPopulationData
{
    public enum ArchiveCityPopulationDataStatus
    {
        Applied = 0,
        Duplicate = 1,
        Stale = 2,
        CityDeleted = 3
    }

    public sealed record ArchiveCityPopulationDataResult(ArchiveCityPopulationDataStatus Status);
}
