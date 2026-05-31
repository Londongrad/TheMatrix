namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions
{
    public interface ICityEconomyBootstrapClient
    {
        Task<CityEconomyBootstrapResult> InitializeAsync(
            Guid cityId,
            string economyProfile,
            DateTimeOffset createdAtUtc,
            CancellationToken cancellationToken);
    }
}
