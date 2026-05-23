using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using PersonEntity = Matrix.Population.Domain.Entities.Person;
using HouseholdId = Matrix.Population.Domain.ValueObjects.HouseholdId;
using ResidentialBuildingId = Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects.ResidentialBuildingId;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class HouseholdCommutePressureProfileBuilder
    {
        internal static async Task<CityHouseholdCommutePressureProfile?> BuildAsync(
            CityId cityId,
            HouseholdId householdId,
            IReadOnlyCollection<PersonEntity> householdResidents,
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            CancellationToken cancellationToken)
        {
            ResidentialBuildingId? residentialBuildingId = residentialBuildingByHouseholdId.TryGetValue(
                key: householdId,
                value: out ResidentialBuildingId? resolvedResidentialBuildingId)
                ? resolvedResidentialBuildingId
                : null;
            if (!residentialBuildingId.HasValue)
                return null;

            int routedResidentCount = 0;
            int blockedRouteCount = 0;
            decimal accessibilityDeficitTotal = 0m;
            decimal travelFatigueTotal = 0m;

            await commuteRoutingService.PreloadAnchorCommutesAsync(
                cityId: cityId.Value,
                requests: householdResidents
                   .Where(x => x.IsAlive)
                   .Select(x => x.Employment.Status == EmploymentStatus.Employed
                        ? x.Employment.Job?.WorkplaceAnchorId
                        : x.Employment.Status == EmploymentStatus.Student
                            ? x.Education.CurrentInstitutionAnchorId
                            : null)
                   .Where(x => x.HasValue)
                   .Select(x => new CityPopulationCommuteRouteRequest(
                        ResidentialBuildingId: residentialBuildingId.Value,
                        DestinationAnchorId: x!.Value,
                        Profile: CityPopulationCommuteRoutingProfiles.Pedestrian))
                   .ToArray(),
                cancellationToken: cancellationToken);

            foreach (PersonEntity householdResident in householdResidents)
            {
                if (!householdResident.IsAlive)
                    continue;

                CityAnchorId? destinationAnchorId = householdResident.Employment.Status == EmploymentStatus.Employed
                    ? householdResident.Employment.Job?.WorkplaceAnchorId
                    : householdResident.Employment.Status == EmploymentStatus.Student
                        ? householdResident.Education.CurrentInstitutionAnchorId
                        : null;
                if (!destinationAnchorId.HasValue)
                    continue;

                CityPopulationCommuteContext commute = await commuteRoutingService.ResolveAnchorCommuteAsync(
                    cityId: cityId.Value,
                    residentialBuildingId: residentialBuildingId,
                    destinationAnchorId: destinationAnchorId,
                    cancellationToken: cancellationToken);
                routedResidentCount++;
                accessibilityDeficitTotal += 1m - commute.AccessibilityIndex;
                if (!commute.IsAccessible)
                    blockedRouteCount++;

                decimal travelFatigue = commute.EstimatedTravelTimeMinutes.HasValue
                    ? decimal.Clamp(
                        value: commute.EstimatedTravelTimeMinutes.Value / 90m,
                        min: 0m,
                        max: 1m)
                    : commute.IsAccessible
                        ? 0m
                        : 1m;
                travelFatigueTotal += travelFatigue;
            }

            if (routedResidentCount == 0)
                return null;

            return new CityHouseholdCommutePressureProfile(
                RoutedResidentCount: routedResidentCount,
                BlockedRouteCount: blockedRouteCount,
                AccessibilityDeficitIndex: decimal.Round(
                    d: accessibilityDeficitTotal / routedResidentCount,
                    decimals: 4,
                    mode: MidpointRounding.AwayFromZero),
                TravelFatigueIndex: decimal.Round(
                    d: travelFatigueTotal / routedResidentCount,
                    decimals: 4,
                    mode: MidpointRounding.AwayFromZero));
        }
    }
}
