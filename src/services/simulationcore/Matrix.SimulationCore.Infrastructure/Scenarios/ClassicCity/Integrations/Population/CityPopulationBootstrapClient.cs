using System.Net.Http.Json;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;

namespace Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Integrations.Population
{
    internal sealed class CityPopulationBootstrapClient(HttpClient client) : ICityPopulationBootstrapClient
    {
        private const string InitializeEndpoint = "/api/population/init";
        private readonly HttpClient _client = client;

        public async Task<CityPopulationBootstrapSummary> InitializeAsync(
            CityPopulationBootstrapInitializationRequest request,
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: InitializeEndpoint,
                value: new InitializeCityPopulationRequest(
                    CityId: request.CityId,
                    CurrentDate: request.CurrentDate,
                    CreatedAtUtc: request.CreatedAtUtc,
                    PeopleCount: request.PeopleCount,
                    RandomSeed: request.RandomSeed,
                    Environment: new CityPopulationEnvironmentDto(
                        ClimateZone: request.Environment.ClimateZone,
                        Hemisphere: request.Environment.Hemisphere,
                        UtcOffsetMinutes: request.Environment.UtcOffsetMinutes),
                    Tuning: new CityPopulationBootstrapTuningDto(
                        HousingPressurePercent: request.Tuning.HousingPressurePercent,
                        EconomicStabilityPercent: request.Tuning.EconomicStabilityPercent,
                        SocialVolatilityPercent: request.Tuning.SocialVolatilityPercent,
                        FamilyFormationPercent: request.Tuning.FamilyFormationPercent),
                    CityAnchors: request.CityAnchors
                       .Select(x => new CityAnchorSeedDto(
                            CityAnchorId: x.CityAnchorId,
                            DistrictId: x.DistrictId,
                            AccessRoadNodeId: x.AccessRoadNodeId,
                            Name: x.Name,
                            Type: x.Type,
                            Capacity: x.Capacity,
                            PositionX: x.PositionX,
                            PositionY: x.PositionY,
                            CreatedAtUtc: x.CreatedAtUtc))
                       .ToArray(),
                    ResidentialBuildings: request.ResidentialBuildings
                       .Select(x => new ResidentialBuildingSeedDto(
                            ResidentialBuildingId: x.ResidentialBuildingId,
                            DistrictId: x.DistrictId,
                            ResidentCapacity: x.ResidentCapacity))
                       .ToArray()),
                cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    message: $"Population bootstrap request failed with status code {(int)response.StatusCode}.",
                    inner: null,
                    statusCode: response.StatusCode);

            CityPopulationBootstrapSummaryDto? payload =
                await response.Content.ReadFromJsonAsync<CityPopulationBootstrapSummaryDto>(
                    cancellationToken: cancellationToken);

            if (payload is null)
                throw new InvalidOperationException("Population bootstrap response was empty.");

            return new CityPopulationBootstrapSummary(
                CityId: payload.CityId,
                RequestedPeopleCount: payload.RequestedPeopleCount,
                GeneratedPeopleCount: payload.GeneratedPeopleCount,
                HouseholdCount: payload.HouseholdCount,
                HousedHouseholdCount: payload.HousedHouseholdCount,
                HomelessHouseholdCount: payload.HomelessHouseholdCount,
                HousedPeopleCount: payload.HousedPeopleCount,
                HomelessPeopleCount: payload.HomelessPeopleCount);
        }
    }
}
