using Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.Cities;

namespace Matrix.ApiGateway.Services.CityCore.Scenarios.ClassicCity.Cities
{
    public interface ICityProvisioningService
    {
        Task<CityProvisioningView> CreateCityAsync(
            CreateCityRequestDto request,
            CancellationToken cancellationToken = default);

        Task<CityProvisioningView> RetryPopulationBootstrapAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);
    }
}
