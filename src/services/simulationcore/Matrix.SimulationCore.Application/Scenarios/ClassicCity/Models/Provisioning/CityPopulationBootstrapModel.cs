namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning
{
    public sealed record CityPopulationBootstrapModel(
        Guid OperationId,
        string Status,
        int? PlannedPeopleCount,
        int? ResidentialCapacity,
        CityPopulationBootstrapSummaryModel? Summary,
        string? FailureCode);
}
