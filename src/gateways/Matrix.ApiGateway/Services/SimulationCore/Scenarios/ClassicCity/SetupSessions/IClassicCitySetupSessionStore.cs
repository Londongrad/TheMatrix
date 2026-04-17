namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    public interface IClassicCitySetupSessionStore
    {
        Task<IReadOnlyList<ClassicCitySetupSessionState>> ListOwnedAsync(
            Guid ownerUserId,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid sessionId,
            Guid? ownerUserId,
            CancellationToken cancellationToken = default);

        Task<ClassicCitySetupSessionState?> GetAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        Task SaveAsync(
            ClassicCitySetupSessionState session,
            CancellationToken cancellationToken = default);

        Task<ClassicCitySetupSessionLockHandle?> TryAcquireLockAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        Task<ClassicCitySetupSessionLockHandle?> TryAcquireCreateLockAsync(
            Guid ownerUserId,
            CancellationToken cancellationToken = default);

        Task ReleaseLockAsync(
            Guid sessionId,
            ClassicCitySetupSessionLockHandle lockHandle,
            CancellationToken cancellationToken = default);

        Task ReleaseCreateLockAsync(
            Guid ownerUserId,
            ClassicCitySetupSessionLockHandle lockHandle,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Guid>> ListTrackedSessionIdsAsync(CancellationToken cancellationToken = default);

        Task UntrackAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);
    }
}
