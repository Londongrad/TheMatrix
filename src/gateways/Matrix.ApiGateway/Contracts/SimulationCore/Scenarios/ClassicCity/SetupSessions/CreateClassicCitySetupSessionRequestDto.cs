namespace Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed record CreateClassicCitySetupSessionRequestDto(
        string CurrentStepId,
        ClassicCitySetupDraftDto Draft);
}
