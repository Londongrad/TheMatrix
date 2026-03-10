using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common
{
    internal static class ClassicCityHouseholdAutonomySupport
    {
        public static async Task<bool> MoveResidentIntoIndependentHouseholdAsync(
            CityId cityId,
            Person resident,
            IHouseholdWriteRepository householdWriteRepository,
            CancellationToken cancellationToken)
        {
            Household sourceHousehold = await LoadHouseholdAsync(
                householdId: resident.HouseholdId,
                householdWriteRepository: householdWriteRepository,
                cancellationToken: cancellationToken);
            ClassicCityHouseholdPlacement sourcePlacement = await LoadPlacementAsync(
                householdId: sourceHousehold.Id,
                cityId: cityId,
                householdWriteRepository: householdWriteRepository,
                cancellationToken: cancellationToken);

            int sourceCount = await householdWriteRepository.CountResidentsAsync(
                householdId: sourceHousehold.Id,
                cancellationToken: cancellationToken);

            if (sourceCount <= 1)
                return false;

            HouseholdId newHouseholdId = HouseholdId.New();
            Household newHousehold = Household.Create(
                id: newHouseholdId,
                size: HouseholdSize.From(1),
                createdAtUtc: DateTimeOffset.UtcNow,
                cashReserve: sourceHousehold.ReleasePositiveReserveShare(0.32m));

            ClassicCityHouseholdPlacement newPlacement = ClassicCityHouseholdPlacement.CreateHomeless(
                householdId: newHouseholdId,
                cityId: cityId);

            resident.ChangeHousehold(newHouseholdId);
            sourceHousehold.Resize(HouseholdSize.From(sourceCount - 1));

            await householdWriteRepository.UpdateAsync(
                household: sourceHousehold,
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
