using Matrix.Population.Contracts.Models;

namespace Matrix.ApiGateway.Contracts.CityCore.Cities
{
    public sealed record CityPopulationBootstrapView(
        string Status,
        int? PlannedPeopleCount,
        int? ResidentialCapacity,
        CityPopulationBootstrapSummaryDto? Summary,
        string? Error);
}
