using Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.SetupSessions;

namespace Matrix.ApiGateway.Services.CityCore.Scenarios.ClassicCity.SetupSessions
{
    public enum ClassicCitySetupSessionMutationStatus
    {
        Updated = 1,
        NotFound = 2,
        Conflict = 3,
        Invalid = 4
    }

    public sealed record ClassicCitySetupSessionMutationResult(
        ClassicCitySetupSessionMutationStatus Status,
        ClassicCitySetupSessionView? Session,
        string? ErrorCode,
        string? ErrorMessage);
}
