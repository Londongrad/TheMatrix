using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Bootstrap.InitializeCityEconomy;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions
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
