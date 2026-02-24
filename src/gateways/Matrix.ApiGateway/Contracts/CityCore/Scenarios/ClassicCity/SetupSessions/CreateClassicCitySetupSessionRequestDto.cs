namespace Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed record CreateClassicCitySetupSessionRequestDto(
        string CurrentStepId,
        ClassicCitySetupDraftDto Draft);
}
