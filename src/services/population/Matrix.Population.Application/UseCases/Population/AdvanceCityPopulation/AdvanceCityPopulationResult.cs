namespace Matrix.Population.Application.UseCases.Population.AdvanceCityPopulation
{
    public enum AdvanceCityPopulationStatus
    {
        Applied = 1,
        Duplicate = 2,
        OutOfOrder = 3,
        CityDeleted = 4,
        CityArchived = 5
    }

    public sealed record AdvanceCityPopulationResult(
        AdvanceCityPopulationStatus Status,
        int AffectedPeopleCount);
}
