using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing
{
    public sealed class CityPopulationCommuteRoutingService(ICityRouteResolutionClient routeResolutionClient)
        : ICityPopulationCommuteRoutingService
    {
        private readonly ICityRouteResolutionClient _routeResolutionClient = routeResolutionClient;
        private readonly Dictionary<CommuteRouteCacheKey, CityPopulationCommuteContext> _cache = [];

        public Task<CityPopulationCommuteContext> ResolveEmploymentCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            Person resident,
            CancellationToken cancellationToken)
        {
            CityAnchorId? workplaceAnchorId = resident.Employment.Status == EmploymentStatus.Employed
                ? resident.Employment.Job?.WorkplaceAnchorId
                : null;

            return ResolveAsync(
                cityId: cityId,
                residentialBuildingId: residentialBuildingId,
                destinationAnchorId: workplaceAnchorId,
                profile: CityPopulationCommuteRoutingProfiles.Pedestrian,
                cancellationToken: cancellationToken);
        }

        public Task<CityPopulationCommuteContext> ResolveEducationCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            Person resident,
            CancellationToken cancellationToken)
        {
            CityAnchorId? schoolAnchorId = resident.Employment.Status == EmploymentStatus.Student
                ? resident.Education.CurrentInstitutionAnchorId
                : null;

            return ResolveAsync(
                cityId: cityId,
                residentialBuildingId: residentialBuildingId,
                destinationAnchorId: schoolAnchorId,
                profile: CityPopulationCommuteRoutingProfiles.Pedestrian,
                cancellationToken: cancellationToken);
        }

        private async Task<CityPopulationCommuteContext> ResolveAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            CityAnchorId? destinationAnchorId,
            string profile,
            CancellationToken cancellationToken)
        {
            if (!residentialBuildingId.HasValue || !destinationAnchorId.HasValue)
                return CityPopulationCommuteContext.Neutral;

            var cacheKey = new CommuteRouteCacheKey(
                CityId: cityId,
                ResidentialBuildingId: residentialBuildingId.Value.Value,
                DestinationAnchorId: destinationAnchorId.Value.Value,
                Profile: profile);

            if (_cache.TryGetValue(
                    key: cacheKey,
                    value: out CityPopulationCommuteContext? cached))
            {
                return cached;
            }

            CityPopulationCommuteContext routeContext =
                await _routeResolutionClient.ResolveResidentialToAnchorAsync(
                    cityId: cityId,
                    residentialBuildingId: residentialBuildingId.Value,
                    cityAnchorId: destinationAnchorId.Value,
                    profile: profile,
                    cancellationToken: cancellationToken)
                ?? CityPopulationCommuteContext.Neutral;

            _cache[cacheKey] = routeContext;
            return routeContext;
        }

        private readonly record struct CommuteRouteCacheKey(
            Guid CityId,
            Guid ResidentialBuildingId,
            Guid DestinationAnchorId,
            string Profile);
    }
}
