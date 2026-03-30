namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views
{
    public sealed record CityPopulationBootstrapView(
        Guid OperationId,
        string Status,
        int? PlannedPeopleCount,
        int? ResidentialCapacity,
        CityPopulationBootstrapSummaryView? Summary,
        string? FailureCode);
}
