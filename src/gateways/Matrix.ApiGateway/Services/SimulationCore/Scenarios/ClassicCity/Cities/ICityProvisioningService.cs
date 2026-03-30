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
    }
}
