using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.Common
{
    internal static class ClassicCityCivilRegistryHouseholdSupport
    {
        public static async Task<bool> MergeSpousesIntoSharedHouseholdAsync(
            CityId cityId,
            Person firstResident,
            Person secondResident,
            IHouseholdWriteRepository householdWriteRepository,
            CancellationToken cancellationToken)
        {
            if (firstResident.HouseholdId == secondResident.HouseholdId)
                return false;

            Household firstHousehold = await LoadHouseholdAsync(
                householdId: firstResident.HouseholdId,
                householdWriteRepository: householdWriteRepository,
                cancellationToken: cancellationToken);
            Household secondHousehold = await LoadHouseholdAsync(
                householdId: secondResident.HouseholdId,
                householdWriteRepository: householdWriteRepository,
                cancellationToken: cancellationToken);

            ClassicCityHouseholdPlacement firstPlacement = await LoadPlacementAsync(
                householdId: firstHousehold.Id,
                cityId: cityId,
                householdWriteRepository: householdWriteRepository,
                cancellationToken: cancellationToken);
            ClassicCityHouseholdPlacement secondPlacement = await LoadPlacementAsync(
                householdId: secondHousehold.Id,
                cityId: cityId,
                householdWriteRepository: householdWriteRepository,
                cancellationToken: cancellationToken);

            bool keepFirstHousehold = firstPlacement.HousingStatus == HousingStatus.Housed ||
                                      secondPlacement.HousingStatus != HousingStatus.Housed;

            Household targetHousehold = keepFirstHousehold
                ? firstHousehold
                : secondHousehold;
            Household sourceHousehold = keepFirstHousehold
                ? secondHousehold
                : firstHousehold;
            Person movedResident = keepFirstHousehold
                ? secondResident
                : firstResident;
            targetHousehold.ReceiveReserve(sourceHousehold.DrainReserve());

            int sourceCount = await householdWriteRepository.CountResidentsAsync(
                householdId: sourceHousehold.Id,
                cancellationToken: cancellationToken);
            int targetCount = await householdWriteRepository.CountResidentsAsync(
                householdId: targetHousehold.Id,
                cancellationToken: cancellationToken);

            movedResident.ChangeHousehold(targetHousehold.Id);
            targetHousehold.Resize(HouseholdSize.From(targetCount + 1));
            await householdWriteRepository.UpdateAsync(
                household: targetHousehold,
                cancellationToken: cancellationToken);

            if (sourceCount <= 1)
            {
                await householdWriteRepository.DeleteAsync(
                    household: sourceHousehold,
                    cancellationToken: cancellationToken);
            }
            else
            {
                sourceHousehold.Resize(HouseholdSize.From(sourceCount - 1));
                await householdWriteRepository.UpdateAsync(
                    household: sourceHousehold,
                    cancellationToken: cancellationToken);
            }

            return true;
        }

        public static async Task<bool> SeparateDivorcedSpousesAsync(
            CityId cityId,
            Person firstResident,
            Person secondResident,
            IHouseholdWriteRepository householdWriteRepository,
            CancellationToken cancellationToken)
        {
            if (firstResident.HouseholdId != secondResident.HouseholdId)
                return false;

            Household sharedHousehold = await LoadHouseholdAsync(
                householdId: firstResident.HouseholdId,
                householdWriteRepository: householdWriteRepository,
                cancellationToken: cancellationToken);
            ClassicCityHouseholdPlacement sharedPlacement = await LoadPlacementAsync(
                householdId: sharedHousehold.Id,
                cityId: cityId,
                householdWriteRepository: householdWriteRepository,
                cancellationToken: cancellationToken);

            int sharedCount = await householdWriteRepository.CountResidentsAsync(
                householdId: sharedHousehold.Id,
                cancellationToken: cancellationToken);

            if (sharedCount <= 1)
                return false;

            HouseholdId newHouseholdId = HouseholdId.New();
            Household newHousehold = Household.Create(
                id: newHouseholdId,
                size: HouseholdSize.From(1),
                createdAtUtc: DateTimeOffset.UtcNow,
                cashReserve: sharedHousehold.ReleasePositiveReserveShare(0.40m));
            ClassicCityHouseholdPlacement newPlacement = sharedPlacement.HousingStatus == HousingStatus.Housed &&
                                                         sharedPlacement.DistrictId.HasValue &&
                                                         sharedPlacement.ResidentialBuildingId.HasValue
                ? ClassicCityHouseholdPlacement.CreateHoused(
                    householdId: newHouseholdId,
                    cityId: cityId,
                    districtId: sharedPlacement.DistrictId.Value,
                    residentialBuildingId: sharedPlacement.ResidentialBuildingId.Value)
                : ClassicCityHouseholdPlacement.CreateHomeless(
                    householdId: newHouseholdId,
                    cityId: cityId);

            secondResident.ChangeHousehold(newHouseholdId);
            sharedHousehold.Resize(HouseholdSize.From(sharedCount - 1));

            await householdWriteRepository.UpdateAsync(
                household: sharedHousehold,
                cancellationToken: cancellationToken);
            await householdWriteRepository.AddAsync(
                household: newHousehold,
                householdPlacement: newPlacement,
                cancellationToken: cancellationToken);

            return true;
        }

        private static async Task<Household> LoadHouseholdAsync(
            HouseholdId householdId,
            IHouseholdWriteRepository householdWriteRepository,
            CancellationToken cancellationToken)
        {
            return await householdWriteRepository.FindByIdAsync(
                       householdId: householdId,
                       cancellationToken: cancellationToken) ??
                   throw ApplicationErrorsFactory.HouseholdNotFound(householdId.Value);
        }

        private static async Task<ClassicCityHouseholdPlacement> LoadPlacementAsync(
            HouseholdId householdId,
            CityId cityId,
            IHouseholdWriteRepository householdWriteRepository,
            CancellationToken cancellationToken)
        {
            ClassicCityHouseholdPlacement? placement = await householdWriteRepository.FindPlacementByHouseholdIdAsync(
                householdId: householdId,
                cancellationToken: cancellationToken);

            if (placement is null || placement.CityId != cityId)
            {
                throw ApplicationErrorsFactory.HouseholdPlacementNotFound(
                    householdId: householdId.Value,
                    cityId: cityId.Value);
            }

            return placement;
        }
    }
}
