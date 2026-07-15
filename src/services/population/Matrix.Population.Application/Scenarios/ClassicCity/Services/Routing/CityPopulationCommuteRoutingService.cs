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
        private readonly Dictionary<CommuteRouteCacheKey, CityPopulationCommuteContext> _cache = [];
        private readonly ICityRouteResolutionClient _routeResolutionClient = routeResolutionClient;

        public async Task PreloadAnchorCommutesAsync(
            Guid cityId,
            IReadOnlyCollection<CityPopulationCommuteRouteRequest> requests,
            CancellationToken cancellationToken)
        {
            if (requests.Count == 0)
                return;

            var missingRoutesByCacheKey = new Dictionary<CommuteRouteCacheKey, CityRouteResolutionBatchRequestItem>();
            foreach (CityPopulationCommuteRouteRequest request in requests.Distinct())
            {
                CommuteRouteCacheKey cacheKey = BuildCacheKey(
                    cityId: cityId,
                    residentialBuildingId: request.ResidentialBuildingId,
                    destinationAnchorId: request.DestinationAnchorId,
                    profile: request.Profile);
                if (_cache.ContainsKey(cacheKey))
                    continue;

                missingRoutesByCacheKey[cacheKey] = new CityRouteResolutionBatchRequestItem(
                    ResidentialBuildingId: request.ResidentialBuildingId,
                    CityAnchorId: request.DestinationAnchorId,
                    Profile: request.Profile);
            }

            if (missingRoutesByCacheKey.Count == 0)
                return;

            IReadOnlyDictionary<CityRouteResolutionBatchRequestItem, CityPopulationCommuteContext?> resolvedRoutes =
                await _routeResolutionClient.ResolveResidentialToAnchorsAsync(
                    cityId: cityId,
                    requests: missingRoutesByCacheKey.Values.ToArray(),
                    cancellationToken: cancellationToken);

            foreach ((CommuteRouteCacheKey cacheKey, CityRouteResolutionBatchRequestItem request) in
                     missingRoutesByCacheKey)
                _cache[cacheKey] = resolvedRoutes.TryGetValue(
                    key: request,
                    value: out CityPopulationCommuteContext? routeContext)
                    ? routeContext ?? CityPopulationCommuteContext.Neutral
                    : CityPopulationCommuteContext.Neutral;
        }

        public Task<CityPopulationCommuteContext> ResolveAnchorCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            CityAnchorId? destinationAnchorId,
            CancellationToken cancellationToken)
        {
            return ResolveAsync(
                cityId: cityId,
                residentialBuildingId: residentialBuildingId,
                destinationAnchorId: destinationAnchorId,
                profile: CityPopulationCommuteRoutingProfiles.Pedestrian,
                cancellationToken: cancellationToken);
        }

        public Task<CityPopulationCommuteContext> ResolveEmploymentCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            Person resident,
            CancellationToken cancellationToken)
        {
            CityAnchorId? workplaceAnchorId = resident.Employment.Status == EmploymentStatus.Employed
                ? resident.Employment.Job?.WorkplaceAnchorId
                : null;

            return ResolveAnchorCommuteAsync(
                cityId: cityId,
                residentialBuildingId: residentialBuildingId,
                destinationAnchorId: workplaceAnchorId,
                cancellationToken: cancellationToken);
        }

        public Task<CityPopulationCommuteContext> ResolveHealthcareCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            CityAnchorId? healthcareAnchorId,
            CancellationToken cancellationToken)
        {
            return ResolveAnchorCommuteAsync(
                cityId: cityId,
                residentialBuildingId: residentialBuildingId,
                destinationAnchorId: healthcareAnchorId,
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

            CommuteRouteCacheKey cacheKey = BuildCacheKey(
                cityId: cityId,
                residentialBuildingId: residentialBuildingId.Value,
                destinationAnchorId: destinationAnchorId.Value,
                profile: profile);

            if (_cache.TryGetValue(
                    key: cacheKey,
                    value: out CityPopulationCommuteContext? cached))
                return cached;

            CityPopulationCommuteContext routeContext =
                await _routeResolutionClient.ResolveResidentialToAnchorAsync(
                    cityId: cityId,
                    residentialBuildingId: residentialBuildingId.Value,
                    cityAnchorId: destinationAnchorId.Value,
                    profile: profile,
                    cancellationToken: cancellationToken) ??
                CityPopulationCommuteContext.Neutral;

            _cache[cacheKey] = routeContext;
            return routeContext;
        }

        private static CommuteRouteCacheKey BuildCacheKey(
            Guid cityId,
            ResidentialBuildingId residentialBuildingId,
            CityAnchorId destinationAnchorId,
            string profile)
        {
            return new CommuteRouteCacheKey(
                CityId: cityId,
                ResidentialBuildingId: residentialBuildingId.Value,
                DestinationAnchorId: destinationAnchorId.Value,
                Profile: profile);
        }

        private readonly record struct CommuteRouteCacheKey(
            Guid CityId,
            Guid ResidentialBuildingId,
            Guid DestinationAnchorId,
            string Profile);
    }
}
