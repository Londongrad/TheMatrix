using Matrix.CityCore.Contracts.Scenarios.ClassicCity.Cities.Requests;
using Matrix.CityCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.CityCore.Contracts.Scenarios.ClassicCity.Topology.Views;

namespace Matrix.ApiGateway.DownstreamClients.CityCore.Scenarios.ClassicCity.Cities
{
    public interface ICitiesApiClient
    {
        Task<CityCreatedView> CreateCityAsync(
            CreateCityRequest request,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SimulationKindCatalogItemView>> GetSimulationKindsAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CityListItemView>> ListCitiesAsync(
            bool includeArchived,
            CancellationToken cancellationToken = default);

        Task<CityView> GetCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<CityProvisioningStatusView> GetProvisioningStatusAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ResidentialBuildingView>> GetResidentialBuildingsAsync(
            Guid cityId,
            Guid? districtId = null,
            CancellationToken cancellationToken = default);

        Task<CityPopulationBootstrapRestartedView> RestartPopulationBootstrapAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task CompletePopulationBootstrapAsync(
            Guid cityId,
            CompleteCityPopulationBootstrapRequest request,
            CancellationToken cancellationToken = default);

        Task FailPopulationBootstrapAsync(
            Guid cityId,
            FailCityPopulationBootstrapRequest request,
            CancellationToken cancellationToken = default);

        Task UpdateEnvironmentAsync(
            Guid cityId,
            UpdateCityEnvironmentRequest request,
            CancellationToken cancellationToken = default);

        Task RenameCityAsync(
            Guid cityId,
            RenameCityRequest request,
            CancellationToken cancellationToken = default);

        Task ArchiveCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task DeleteCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);
    }
}
