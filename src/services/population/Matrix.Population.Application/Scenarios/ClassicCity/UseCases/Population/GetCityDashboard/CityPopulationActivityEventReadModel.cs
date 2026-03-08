namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard
{
    public sealed record CityPopulationActivityEventReadModel(
        Guid ActivityEventId,
        DateOnly CurrentDate,
        DateTimeOffset OccurredAtUtc,
        string EventType,
        string Source,
        string Severity,
        string Title,
        string Summary,
        Guid? PrimaryResidentId,
        Guid? SecondaryResidentId);
}
