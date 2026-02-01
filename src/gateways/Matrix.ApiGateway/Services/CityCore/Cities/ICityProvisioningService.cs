using Matrix.ApiGateway.Contracts.CityCore.Cities;

namespace Matrix.ApiGateway.Services.CityCore.Cities
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
