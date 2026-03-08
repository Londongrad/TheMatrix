using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Errors;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation
{
    public sealed class InitializeCityPopulationCommandHandler(
        IPersonWriteRepository personWriteRepository,
        IHouseholdWriteRepository householdWriteRepository,
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        ICityPopulationEnvironmentRepository cityPopulationEnvironmentRepository,
        ICityPopulationActivityJournalService cityPopulationActivityJournalService,
        ICityPopulationSummaryProjectionService cityPopulationSummaryProjectionService,
        CityPopulationBootstrapGenerator generator,
        IUnitOfWork unitOfWork)
        : IRequestHandler<InitializeCityPopulationCommand, CityPopulationBootstrapSummaryDto>
    {
        public async Task<CityPopulationBootstrapSummaryDto> Handle(
            InitializeCityPopulationCommand request,
            CancellationToken cancellationToken)
        {
            CityPopulationEnvironmentInput environmentInput = request.Environment!;

            var cityId = CityId.From(request.CityId);
            CityPopulationArchiveState? archiveState = await cityPopulationArchiveStateRepository.GetByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            CityPopulationDeletionState? deletionState = await cityPopulationDeletionStateRepository.GetByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (archiveState is not null)
                throw ClassicCityApplicationErrorsFactory.CannotInitializePopulationForArchivedCity(request.CityId);

            if (deletionState is not null)
                throw ClassicCityApplicationErrorsFactory.CannotInitializePopulationForDeletedCity(request.CityId);

            IReadOnlyCollection<ResidentialBuildingResidence> residentialBuildings = request.ResidentialBuildings
               .Select(x => new ResidentialBuildingResidence(
                    residentialBuildingId: ResidentialBuildingId.From(x.ResidentialBuildingId),
                    districtId: DistrictId.From(x.DistrictId),
                    residentCapacity: x.ResidentCapacity))
               .ToArray();

            PopulationBootstrapResult result = generator.GenerateForCity(
                cityId: cityId,
                residentialBuildings: residentialBuildings,
                peopleCount: request.PeopleCount,
                currentDate: request.CurrentDate,
                createdAtUtc: DateTimeOffset.UtcNow,
                tuning: new CityPopulationBootstrapTuning(
                    HousingPressurePercent: request.Tuning.HousingPressurePercent,
                    EconomicStabilityPercent: request.Tuning.EconomicStabilityPercent,
                    SocialVolatilityPercent: request.Tuning.SocialVolatilityPercent,
                    FamilyFormationPercent: request.Tuning.FamilyFormationPercent),
                randomSeed: request.RandomSeed);

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;
                    CityPopulationEnvironment environment = CityPopulationEnvironmentMapper.Create(
                        cityId: request.CityId,
                        input: environmentInput,
                        createdAtUtc: updatedAtUtc);

                    await cityPopulationEnvironmentRepository.UpsertAsync(
                        environment: environment,
                        cancellationToken: ct);

                    await householdWriteRepository.DeleteByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);

                    await householdWriteRepository.AddRangeAsync(
                        households: result.Households,
                        householdPlacements: result.HouseholdPlacements,
                        cancellationToken: ct);

                    await personWriteRepository.AddRangeAsync(
                        persons: result.Persons,
                        cancellationToken: ct);

                    await cityPopulationSummaryProjectionService.UpdateAsync(
                        cityId: cityId,
                        currentDate: request.CurrentDate,
                        persons: result.Persons,
                        householdPlacements: result.HouseholdPlacements,
                        cancellationToken: ct);

                    await cityPopulationActivityJournalService.RecordAsync(
                        entry: ClassicCityActivityFactory.PopulationInitialized(
                            cityId: request.CityId,
                            currentDate: request.CurrentDate,
                            requestedPeopleCount: request.PeopleCount,
                            generatedPeopleCount: result.Persons.Count,
                            householdCount: result.Households.Count),
                        cancellationToken: ct);

                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            int housedHouseholdCount = result.HouseholdPlacements.Count(x => x.HousingStatus == HousingStatus.Housed);
            int homelessHouseholdCount = result.HouseholdPlacements.Count - housedHouseholdCount;
            HashSet<HouseholdId> housedHouseholdIds = result.HouseholdPlacements
               .Where(x => x.HousingStatus == HousingStatus.Housed)
               .Select(x => x.HouseholdId)
               .ToHashSet();
            int housedPeopleCount = result.Households
               .Where(x => housedHouseholdIds.Contains(x.Id))
               .Sum(x => x.Size.Value);
            int homelessPeopleCount = result.Persons.Count - housedPeopleCount;

            return new CityPopulationBootstrapSummaryDto(
                CityId: request.CityId,
                RequestedPeopleCount: request.PeopleCount,
                GeneratedPeopleCount: result.Persons.Count,
                HouseholdCount: result.Households.Count,
                HousedHouseholdCount: housedHouseholdCount,
                HomelessHouseholdCount: homelessHouseholdCount,
                HousedPeopleCount: housedPeopleCount,
                HomelessPeopleCount: homelessPeopleCount);
        }
    }
}
