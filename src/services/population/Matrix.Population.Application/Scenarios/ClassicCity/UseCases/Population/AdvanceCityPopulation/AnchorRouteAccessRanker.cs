using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class AnchorRouteAccessRanker
    {
        internal static async Task<IReadOnlyList<CityAnchorId>> RankAsync(
            CityId cityId,
            ResidentialBuildingId? residentialBuildingId,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> anchors,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            CancellationToken cancellationToken)
        {
            if (!residentialBuildingId.HasValue || anchors.Count == 0)
                return [];

            await commuteRoutingService.PreloadAnchorCommutesAsync(
                cityId: cityId.Value,
                requests: anchors
                   .Select(anchor => new CityPopulationCommuteRouteRequest(
                        ResidentialBuildingId: residentialBuildingId.Value,
                        DestinationAnchorId: anchor.CityAnchorId,
                        Profile: CityPopulationCommuteRoutingProfiles.Pedestrian))
                   .ToArray(),
                cancellationToken: cancellationToken);

            var rankedAnchors = new List<(CityAnchorId AnchorId, CityPopulationCommuteContext Commute)>(anchors.Count);
            foreach (CityPopulationAnchorCatalogItem anchor in anchors)
            {
                CityPopulationCommuteContext commute = await commuteRoutingService.ResolveAnchorCommuteAsync(
                    cityId: cityId.Value,
                    residentialBuildingId: residentialBuildingId,
                    destinationAnchorId: anchor.CityAnchorId,
                    cancellationToken: cancellationToken);
                rankedAnchors.Add((anchor.CityAnchorId, commute));
            }

            return rankedAnchors
               .OrderByDescending(x => x.Commute.IsAccessible)
               .ThenByDescending(x => x.Commute.AccessibilityIndex)
               .ThenByDescending(x => x.Commute.PassabilityIndex)
               .ThenBy(x => x.Commute.EstimatedTravelTimeMinutes ?? decimal.MaxValue)
               .Select(x => x.AnchorId)
               .ToArray();
        }
    }
}
