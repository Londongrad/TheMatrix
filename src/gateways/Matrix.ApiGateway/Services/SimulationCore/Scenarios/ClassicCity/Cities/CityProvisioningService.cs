using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;

namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Cities
{
    public sealed class CityProvisioningService(ICitiesApiClient citiesApiClient) : ICityProvisioningService
    {
        public Task<CityProvisioningView> CreateCityAsync(
            CreateCityRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return citiesApiClient.CreateProvisionedCityAsync(
                request: new CreateCityRequest(
                    Name: request.Name,
                    SimulationKind: request.SimulationKind,
                    ClimateZone: request.ClimateZone,
                    Hemisphere: request.Hemisphere,
                    UtcOffsetMinutes: request.UtcOffsetMinutes,
                    GenerationSeed: request.GenerationSeed,
                    SizeTier: request.SizeTier,
                    UrbanDensity: request.UrbanDensity,
                    DevelopmentLevel: request.DevelopmentLevel,
                    EconomyProfile: request.EconomyProfile,
                    PopulationOccupancyProfile: request.PopulationOccupancyProfile,
                    InitialWeatherMode: request.InitialWeatherMode,
                    InitialWeatherType: request.InitialWeatherType,
                    InitialWeatherSeverity: request.InitialWeatherSeverity,
                    InitialWeatherTemperatureC: request.InitialWeatherTemperatureC,
                    StartSimTimeUtc: request.StartSimTimeUtc,
                    SpeedMultiplier: request.SpeedMultiplier,
                    PlannedPeopleCount: request.PlannedPeopleCount,
                    ProvisioningCorrelationId: request.ProvisioningCorrelationId),
                cancellationToken: cancellationToken);
        }

        public Task<CityProvisioningView> RetryPopulationBootstrapAsync(
            Guid cityId,
            int? plannedPeopleCountOverride = null,
            CancellationToken cancellationToken = default)
        {
            return citiesApiClient.RetryPopulationBootstrapProvisioningAsync(
                cityId: cityId,
                request: new RetryCityPopulationBootstrapProvisioningRequest(
                    PlannedPeopleCountOverride: plannedPeopleCountOverride),
                cancellationToken: cancellationToken);
        }
    }
}
