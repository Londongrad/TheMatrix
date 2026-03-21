namespace Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed record UpdateClassicCitySetupSessionRequestDto(
        string CurrentStepId,
        ClassicCitySetupDraftDto Draft);
}
