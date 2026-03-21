using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;

namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Cities
{
    public interface ICityProvisioningService
    {
        Task<CityProvisioningView> CreateCityAsync(
            CreateCityRequestDto request,
            CancellationToken cancellationToken = default);

        Task<CityProvisioningView> RetryPopulationBootstrapAsync(
            Guid cityId,
            int? plannedPeopleCountOverride = null,
            CancellationToken cancellationToken = default);

        Task<CityCreatedView> CreateCitySkeletonAsync(
            CreateCityRequestDto request,
            CancellationToken cancellationToken = default);

        Task<CityProvisioningView> ProvisionCreatedCityAsync(
            Guid cityId,
            string simulationKind,
            Guid populationOperationId,
            Guid economyOperationId,
            CancellationToken cancellationToken = default,
            int? plannedPeopleCountOverride = null);
    }
}
