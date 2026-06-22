using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Integration;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Errors;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common;
using Matrix.Population.Contracts.Events;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation
{
    public sealed class InitializeCityPopulationCommandHandler(
        IPersonWriteRepository personWriteRepository,
        IHouseholdWriteRepository householdWriteRepository,
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        ICityPopulationEnvironmentRepository cityPopulationEnvironmentRepository,
        ICityPopulationAnchorCatalogRepository cityPopulationAnchorCatalogRepository,
        ICityPopulationActivityJournalService cityPopulationActivityJournalService,
        ICityPopulationSummaryProjectionService cityPopulationSummaryProjectionService,
        ICityEconomySettlementOutboxWriter cityEconomySettlementOutboxWriter,
        IPopulationResidentFactsOutboxWriter residentFactsOutboxWriter,
        CityPopulationBootstrapGenerator generator,
        IUnitOfWork unitOfWork)
        : IRequestHandler<InitializeCityPopulationCommand, CityPopulationBootstrapSummaryDto>
    {
        private const int EconomyHouseholdSyncBatchSize = 500;
        private const int EconomyWorkplaceSyncBatchSize = 500;

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
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> cityAnchors = request.CityAnchors
               .Select(x => CityPopulationAnchorCatalogItem.Create(
                    cityId: cityId,
                    cityAnchorId: CityAnchorId.From(x.CityAnchorId),
                    districtId: DistrictId.From(x.DistrictId),
                    accessRoadNodeId: RoadNodeId.From(x.AccessRoadNodeId),
                    name: x.Name,
                    type: Enum.Parse<CityAnchorType>(
                        value: x.Type,
                        ignoreCase: true),
                    capacity: x.Capacity,
                    positionX: x.PositionX,
                    positionY: x.PositionY,
                    createdAtUtc: x.CreatedAtUtc))
               .ToArray();

            PopulationBootstrapResult result = generator.GenerateForCity(
                cityId: cityId,
                residentialBuildings: residentialBuildings,
                cityAnchors: cityAnchors,
                peopleCount: request.PeopleCount,
                currentDate: request.CurrentDate,
                createdAtUtc: request.CreatedAtUtc,
                tuning: new CityPopulationBootstrapTuning(
                    HousingPressurePercent: request.Tuning.HousingPressurePercent,
                    EconomicStabilityPercent: request.Tuning.EconomicStabilityPercent,
                    SocialVolatilityPercent: request.Tuning.SocialVolatilityPercent,
                    FamilyFormationPercent: request.Tuning.FamilyFormationPercent),
                randomSeed: request.RandomSeed);
            DateTimeOffset syncOccurredAtUtc = request.CreatedAtUtc;
            string syncCorrelationId = $"classic-city-population-init:{request.CityId:N}:{Guid.NewGuid():N}";
            ClassicCityHouseholdAccountSyncBatchV1[] householdAccountBatches =
                ClassicCityHouseholdAccountSyncBatchFactory.Build(
                    cityId: request.CityId,
                    households: result.Households,
                    placements: result.HouseholdPlacements,
                    correlationId: syncCorrelationId,
                    occurredAtUtc: syncOccurredAtUtc,
                    batchSize: EconomyHouseholdSyncBatchSize);
            ClassicCityWorkplaceBusinessSyncBatchV1[] workplaceBusinessBatches =
                ClassicCityWorkplaceBusinessSyncBatchFactory.Build(
                    cityId: request.CityId,
                    persons: result.Persons,
                    correlationId: $"{syncCorrelationId}:workplaces",
                    occurredAtUtc: syncOccurredAtUtc,
                    batchSize: EconomyWorkplaceSyncBatchSize);
            PopulationResidentFactsBatchV1[] residentFactsBatches =
                PopulationResidentFactsBatchFactory.Build(
                    simulationHostId: request.CityId,
                    sourceRevision: 0,
                    residents: result.Persons,
                    correlationId: $"{syncCorrelationId}:resident-facts",
                    synchronizedAtUtc: syncOccurredAtUtc);

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    DateTimeOffset updatedAtUtc = request.CreatedAtUtc;
                    CityPopulationEnvironment environment = CityPopulationEnvironmentMapper.Create(
                        cityId: request.CityId,
                        input: environmentInput,
                        createdAtUtc: updatedAtUtc);

                    await cityPopulationEnvironmentRepository.UpsertAsync(
                        environment: environment,
                        cancellationToken: ct);

                    await cityPopulationAnchorCatalogRepository.DeleteByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);

                    if (cityAnchors.Count > 0)
                        await cityPopulationAnchorCatalogRepository.AddRangeAsync(
                            items: cityAnchors,
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
                        includeCommuteMetrics: false,
                        cancellationToken: ct);

                    await cityPopulationActivityJournalService.RecordAsync(
                        entry: ClassicCityActivityFactory.PopulationInitialized(
                            cityId: request.CityId,
                            currentDate: request.CurrentDate,
                            requestedPeopleCount: request.PeopleCount,
                            generatedPeopleCount: result.Persons.Count,
                            householdCount: result.Households.Count,
                            occurredAtUtc: request.CreatedAtUtc),
                        cancellationToken: ct);

                    foreach (ClassicCityHouseholdAccountSyncBatchV1 batch in householdAccountBatches)
                        await cityEconomySettlementOutboxWriter.AddClassicCityHouseholdAccountSyncBatchAsync(
                            batch: batch,
                            cancellationToken: ct);

                    foreach (ClassicCityWorkplaceBusinessSyncBatchV1 batch in workplaceBusinessBatches)
                        await cityEconomySettlementOutboxWriter.AddClassicCityWorkplaceBusinessSyncBatchAsync(
                            batch: batch,
                            cancellationToken: ct);

                    foreach (PopulationResidentFactsBatchV1 batch in residentFactsBatches)
                        await residentFactsOutboxWriter.AddResidentFactsBatchAsync(
                            batch: batch,
                            cancellationToken: ct);

                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            int housedHouseholdCount = result.HouseholdPlacements.Count(x => x.HousingStatus == HousingStatus.Housed);
            int homelessHouseholdCount = result.HouseholdPlacements.Count - housedHouseholdCount;
            var housedHouseholdIds = result.HouseholdPlacements
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
