namespace Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed record UpdateClassicCitySetupSessionRequestDto(
        string CurrentStepId,
        ClassicCitySetupDraftDto Draft);
}
