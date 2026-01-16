namespace Matrix.CityCore.Infrastructure.Public
{
    public interface ICityCoreClockAppService
    {
        Task BootstrapAsync(
            Guid cityId,
            DateTimeOffset startSimTimeUtc,
            CancellationToken cancellationToken);

        Task<CityClockView?> GetClockAsync(
            Guid cityId,
            CancellationToken cancellationToken);

        Task<bool> PauseAsync(
            Guid cityId,
            CancellationToken cancellationToken);

        Task<bool> ResumeAsync(
            Guid cityId,
            CancellationToken cancellationToken);

        Task<bool> SetSpeedAsync(
            Guid cityId,
            decimal multiplier,
            CancellationToken cancellationToken);

        Task<bool> JumpAsync(
            Guid cityId,
            DateTimeOffset newSimTimeUtc,
            CancellationToken cancellationToken);
    }
}
