using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Models
{
    public sealed record CityPopulationActivityWriteModel(
        Guid CityId,
        DateOnly CurrentDate,
        DateTimeOffset OccurredAtUtc,
        CityPopulationActivityEventType EventType,
        CityPopulationActivitySource Source,
        CityPopulationActivitySeverity Severity,
        string Title,
        string Summary,
        Guid? PrimaryResidentId = null,
        Guid? SecondaryResidentId = null);
}
