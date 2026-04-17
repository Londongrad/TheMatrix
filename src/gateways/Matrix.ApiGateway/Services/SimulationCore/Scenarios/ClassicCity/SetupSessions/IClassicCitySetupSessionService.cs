using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.SetupSessions;

namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    public interface IClassicCitySetupSessionService
    {
        Task<IReadOnlyList<ClassicCitySetupSessionView>> ListDraftsAsync(
            CancellationToken cancellationToken = default);

        Task<ClassicCitySetupSessionView> CreateAsync(
            CreateClassicCitySetupSessionRequestDto request,
            CancellationToken cancellationToken = default);

        Task<ClassicCitySetupSessionView?> GetAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        Task<ClassicCitySetupSessionMutationResult> DeleteAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        Task<ClassicCitySetupSessionMutationResult> UpdateAsync(
            Guid sessionId,
            UpdateClassicCitySetupSessionRequestDto request,
            CancellationToken cancellationToken = default);

        Task<ClassicCitySetupSessionMutationResult> QueueLaunchAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        Task ProcessLaunchAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        Task ReconcileAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);
    }
}
