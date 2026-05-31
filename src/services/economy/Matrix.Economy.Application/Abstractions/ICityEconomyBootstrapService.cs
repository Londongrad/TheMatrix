using Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy;

namespace Matrix.Economy.Application.Abstractions
{
    public interface ICityEconomyBootstrapService
    {
        Task<CityEconomyBootstrapResultDto> BootstrapAsync(
            Guid cityId,
            string scenarioKey,
            string? economyProfile,
            DateTimeOffset createdAtUtc,
            CancellationToken cancellationToken = default);
    }
}
