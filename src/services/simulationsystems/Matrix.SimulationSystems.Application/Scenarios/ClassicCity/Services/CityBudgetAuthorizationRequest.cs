namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services
{
    public sealed record CityBudgetAuthorizationRequest(
        Guid CityId,
        string Category,
        string OperationKind,
        string RequestedIntensity,
        decimal EstimatedAmount,
        bool EmergencyOverrideRequested);
}
