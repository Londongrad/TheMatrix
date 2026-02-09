using Matrix.Population.Contracts.Models;

namespace Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.Cities
{
    public sealed record CityPopulationBootstrapView(
        Guid OperationId,
        string Status,
        int? PlannedPeopleCount,
        int? ResidentialCapacity,
        CityPopulationBootstrapSummaryDto? Summary,
        string? FailureCode);
}
