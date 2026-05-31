using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;

namespace Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed record ClassicCitySetupSessionView(
        Guid SessionId,
        string ScenarioKind,
        string Status,
        string CurrentStepId,
        ClassicCitySetupDraftDto Draft,
        Guid? CityId,
        CityProvisioningView? Provisioning,
        string? FailureCode,
        string? FailureMessage,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc,
        DateTimeOffset? LaunchQueuedAtUtc,
        DateTimeOffset? StartedAtUtc,
        DateTimeOffset? CompletedAtUtc);
}
