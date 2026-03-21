using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;

namespace Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities
{
    public sealed record CityPopulationBootstrapView(
        Guid OperationId,
        string Status,
        int? PlannedPeopleCount,
        int? ResidentialCapacity,
        CityPopulationBootstrapSummaryDto? Summary,
        string? FailureCode);
}
