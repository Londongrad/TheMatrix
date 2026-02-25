namespace Matrix.ApiGateway.Services.CityCore.Scenarios.ClassicCity.SetupSessions
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
    }
}
