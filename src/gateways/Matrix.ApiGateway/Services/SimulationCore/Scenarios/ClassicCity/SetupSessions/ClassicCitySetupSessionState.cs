using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.SetupSessions;

namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed class ClassicCitySetupSessionState
    {
        public Guid SessionId { get; init; }
        public Guid? OwnerUserId { get; set; }
        public string ScenarioKind { get; set; } = "ClassicCity";
        public string Status { get; set; } = ClassicCitySetupSessionStatuses.Draft;
        public string CurrentStepId { get; set; } = ClassicCitySetupSteps.Scenario;
        public ClassicCitySetupDraftDto Draft { get; set; } = default!;
        public CreateCityRequestDto? LaunchRequest { get; set; }
        public ClassicCitySetupSessionLaunchAuthSnapshot? LaunchAuthContext { get; set; }
        public Guid? CityId { get; set; }
        public string? SimulationKind { get; set; }
        public CityProvisioningView? Provisioning { get; set; }
        public string? FailureCode { get; set; }
        public string? FailureMessage { get; set; }
        public DateTimeOffset CreatedAtUtc { get; init; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public DateTimeOffset? LaunchQueuedAtUtc { get; set; }
        public DateTimeOffset? StartedAtUtc { get; set; }
        public DateTimeOffset? CompletedAtUtc { get; set; }
    }
}
