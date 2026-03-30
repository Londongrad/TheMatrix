namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions
{
    public interface ICityPopulationBootstrapClient
    {
        Task<CityPopulationBootstrapSummary> InitializeAsync(
            CityPopulationBootstrapInitializationRequest request,
            CancellationToken cancellationToken);
    }
}
