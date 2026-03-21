namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    public interface IClassicCitySetupSessionStore
    {
        Task<ClassicCitySetupSessionState?> GetAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        Task SaveAsync(
            ClassicCitySetupSessionState session,
            CancellationToken cancellationToken = default);

        Task<ClassicCitySetupSessionLockHandle?> TryAcquireLockAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        Task ReleaseLockAsync(
            Guid sessionId,
            ClassicCitySetupSessionLockHandle lockHandle,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Guid>> ListTrackedSessionIdsAsync(CancellationToken cancellationToken = default);

        Task UntrackAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);
    }
}
