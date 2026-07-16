using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using HouseholdId = Matrix.Population.Domain.ValueObjects.HouseholdId;
using PersonEntity = Matrix.Population.Domain.Entities.Person;
using ResidentialBuildingId = Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects.ResidentialBuildingId;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class HouseholdCommutePressureProfileBuilder
    {
        internal static async Task<CityHouseholdCommutePressureProfile?> BuildAsync(
            CityId cityId,
            HouseholdId householdId,
            IReadOnlyCollection<PersonEntity> householdResidents,
            EducationParticipationProjectionIndex educationParticipation,
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
            IReadOnlyDictionary<Guid, IReadOnlyList<CityAnchorId>> destinationAnchorsByResidentId =
                householdResidents
                   .Where(resident => resident.IsAlive)
                   .ToDictionary(
                        keySelector: resident => resident.Id.Value,
                        elementSelector: resident => ResolveDestinationAnchorIds(
                            resident: resident,
                            educationParticipation: educationParticipation));

            await commuteRoutingService.PreloadAnchorCommutesAsync(
                cityId: cityId.Value,
                requests: destinationAnchorsByResidentId.Values
                   .SelectMany(destinationAnchorIds => destinationAnchorIds)
                   .Select(destinationAnchorId => new CityPopulationCommuteRouteRequest(
                        ResidentialBuildingId: residentialBuildingId.Value,
                        DestinationAnchorId: destinationAnchorId,
                        Profile: CityPopulationCommuteRoutingProfiles.Pedestrian))
                   .ToArray(),
                cancellationToken: cancellationToken);

            foreach (PersonEntity householdResident in householdResidents)
            {
                if (!householdResident.IsAlive)
                    continue;

                IReadOnlyList<CityAnchorId> destinationAnchorIds =
                    destinationAnchorsByResidentId[householdResident.Id.Value];
                if (destinationAnchorIds.Count == 0)
                    continue;

                decimal residentAccessibilityDeficit = 0m;
                decimal residentTravelFatigue = 0m;
                bool hasBlockedRoute = false;
                foreach (CityAnchorId destinationAnchorId in destinationAnchorIds)
                {
                    CityPopulationCommuteContext commute = await commuteRoutingService.ResolveAnchorCommuteAsync(
                        cityId: cityId.Value,
                        residentialBuildingId: residentialBuildingId,
                        destinationAnchorId: destinationAnchorId,
                        cancellationToken: cancellationToken);
                    residentAccessibilityDeficit += 1m - commute.AccessibilityIndex;
                    hasBlockedRoute |= !commute.IsAccessible;

                    decimal travelFatigue = commute.EstimatedTravelTimeMinutes.HasValue
                        ? decimal.Clamp(
                            value: commute.EstimatedTravelTimeMinutes.Value / 90m,
                            min: 0m,
                            max: 1m)
                        : commute.IsAccessible
                            ? 0m
                            : 1m;
                    residentTravelFatigue += travelFatigue;
                }

                routedResidentCount++;
                accessibilityDeficitTotal += residentAccessibilityDeficit / destinationAnchorIds.Count;
                travelFatigueTotal += residentTravelFatigue / destinationAnchorIds.Count;
                if (hasBlockedRoute)
                    blockedRouteCount++;
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

        private static IReadOnlyList<CityAnchorId> ResolveDestinationAnchorIds(
            PersonEntity resident,
            EducationParticipationProjectionIndex educationParticipation)
        {
            List<CityAnchorId> destinationAnchorIds = [];
            if (resident.Employment.Status == EmploymentStatus.Employed &&
                resident.Employment.Job?.WorkplaceAnchorId is
                { } workplaceAnchorId)
                destinationAnchorIds.Add(workplaceAnchorId);

            EducationParticipationProjection? projection = educationParticipation.FindCurrent(resident);
            if (projection is
                {
                    IsEnrolled: true,
                    InstitutionAnchorId: { } institutionAnchorId
                })
            {
                CityAnchorId anchorId = CityAnchorId.From(institutionAnchorId);
                if (!destinationAnchorIds.Contains(anchorId))
                    destinationAnchorIds.Add(anchorId);
            }

            return destinationAnchorIds;
        }
    }
}
