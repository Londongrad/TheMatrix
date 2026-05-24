using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Weather;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using PersonEntity = Matrix.Population.Domain.Entities.Person;
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;
using EducationInstitutionId = Matrix.Population.Domain.ValueObjects.EducationInstitutionId;
using WorkplaceId = Matrix.Population.Domain.ValueObjects.WorkplaceId;
using Job = Matrix.Population.Domain.ValueObjects.Job;
using PersonId = Matrix.Population.Domain.ValueObjects.PersonId;
using HouseholdId = Matrix.Population.Domain.ValueObjects.HouseholdId;
using DistrictId = Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects.DistrictId;
using ResidentialBuildingId = Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects.ResidentialBuildingId;
using CityEducationInstitutionBinding = Matrix.Population.Domain.Scenarios.ClassicCity.Models.CityEducationInstitutionBinding;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class AdvanceCityPopulationCommandHandler(
        ICityPopulationPersonReadRepository personReadRepository,
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationAnchorCatalogRepository cityPopulationAnchorCatalogRepository,
        ICityPopulationCostOfLivingStateRepository cityPopulationCostOfLivingStateRepository,
        ICityPopulationEssentialsStateRepository cityPopulationEssentialsStateRepository,
        ICityPopulationServiceQualityStateRepository cityPopulationServiceQualityStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        ICityPopulationEmployerFinancialStressStateRepository employerFinancialStressStateRepository,
        ICityPopulationEnvironmentRepository cityPopulationEnvironmentRepository,
        ICityPopulationHouseholdFinancialStressStateRepository householdFinancialStressStateRepository,
        ICityPopulationLivingConditionsStateRepository cityPopulationLivingConditionsStateRepository,
        ICityDistrictUtilityConditionsClient districtUtilityConditionsClient,
        ICityPopulationCommuteRoutingService commuteRoutingService,
        ICityPopulationCommuteTripSyncService commuteTripSyncService,
        ICityPopulationActivityJournalService cityPopulationActivityJournalService,
        ICityEconomySettlementOutboxWriter cityEconomySettlementOutboxWriter,
        ICityPopulationProgressionStateRepository progressionStateRepository,
        ICityPopulationSummaryProjectionService cityPopulationSummaryProjectionService,
        ICityPopulationWeatherExposureStateRepository weatherExposureStateRepository,
        IHouseholdWriteRepository householdWriteRepository,
        MarriageDomainService marriageDomainService,
        PopulationBirthDomainService populationBirthDomainService,
        IPersonWriteRepository personWriteRepository,
        CityBirthAutonomyPolicy birthAutonomyPolicy,
        CityCivilRegistryAutonomyPolicy civilRegistryAutonomyPolicy,
        CityEducationAutonomyPolicy educationAutonomyPolicy,
        CityEmploymentAutonomyPolicy employmentAutonomyPolicy,
        CityHealthcareAutonomyPolicy healthcareAutonomyPolicy,
        CityHouseholdCashflowPolicy householdCashflowPolicy,
        CityHouseholdPressurePolicy householdPressurePolicy,
        CityHousingAutonomyPolicy housingAutonomyPolicy,
        CityHouseholdIndependenceAutonomyPolicy householdIndependenceAutonomyPolicy,
        CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
        CityPopulationDistrictImpactPolicy districtImpactPolicy,
        CityPopulationHealthcarePressurePolicy healthcarePressurePolicy,
        CityIllnessAutonomyPolicy illnessAutonomyPolicy,
        CityPopulationLivingConditionsPressurePolicy livingConditionsPressurePolicy,
        CityPopulationParticipationPolicy participationPolicy,
        PersonNeedsProgressionPolicy personNeedsProgressionPolicy,
        CityPopulationWeatherExposurePolicy weatherExposurePolicy,
        TimeProvider timeProvider,
        ILogger<AdvanceCityPopulationCommandHandler> logger,
        IUnitOfWork unitOfWork)
        : IRequestHandler<AdvanceCityPopulationCommand, AdvanceCityPopulationResult>
    {
        private const int EconomyHouseholdSyncBatchSize = 500;
        private const int EconomyWorkplaceSyncBatchSize = 500;

        public async Task<AdvanceCityPopulationResult> Handle(
            AdvanceCityPopulationCommand request,
            CancellationToken cancellationToken)
        {
            var cityId = CityId.From(request.CityId);
            var fromDate = DateOnly.FromDateTime(request.FromSimTimeUtc.UtcDateTime);
            var toDate = DateOnly.FromDateTime(request.ToSimTimeUtc.UtcDateTime);
            DateTimeOffset handledAtUtc = timeProvider.GetUtcNow();
            CityPopulationProgressionState? state = await progressionStateRepository.GetByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            CityPopulationArchiveState? archiveState = await cityPopulationArchiveStateRepository.GetByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            CityPopulationCostOfLivingState? costOfLivingState =
                await cityPopulationCostOfLivingStateRepository.GetByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            CityPopulationEssentialsState? essentialsState =
                await cityPopulationEssentialsStateRepository.GetByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            CityPopulationServiceQualityState? serviceQualityState =
                await cityPopulationServiceQualityStateRepository.GetByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            CityPopulationLivingConditionsState? livingConditionsState =
                await cityPopulationLivingConditionsStateRepository.GetByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            CityPopulationDeletionState? deletionState =
                await cityPopulationDeletionStateRepository.GetByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            CityPopulationEnvironment? environment = await cityPopulationEnvironmentRepository.GetByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            CityPopulationWeatherExposureState? weatherExposureState =
                await weatherExposureStateRepository.GetByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

            if (state is not null)
            {
                if (request.TickId <= state.LastProcessedTickId)
                    return new AdvanceCityPopulationResult(
                        Status: AdvanceCityPopulationStatus.Duplicate,
                        AffectedPeopleCount: 0);
                if (toDate < state.LastProcessedDate)
                    return new AdvanceCityPopulationResult(
                        Status: AdvanceCityPopulationStatus.OutOfOrder,
                        AffectedPeopleCount: 0);
            }

            if (deletionState is not null)
                return new AdvanceCityPopulationResult(
                    Status: AdvanceCityPopulationStatus.CityDeleted,
                    AffectedPeopleCount: 0);
            if (archiveState is not null)
                return new AdvanceCityPopulationResult(
                    Status: AdvanceCityPopulationStatus.CityArchived,
                    AffectedPeopleCount: 0);

            DateOnly previousDate = state?.LastProcessedDate ?? fromDate;
            int affectedPeopleCount = 0;
            bool requiresDateProgression = state is null || toDate > previousDate;
            bool requiresNeedsProgression = request.ToSimTimeUtc > request.FromSimTimeUtc;
            bool shouldAdvanceWeatherExposureCheckpoint = CityPopulationWeatherExposurePlanner.ShouldAdvanceCheckpoint(
                weatherExposureState: weatherExposureState,
                fromSimTimeUtc: request.FromSimTimeUtc,
                toSimTimeUtc: request.ToSimTimeUtc);
            List<CityWeatherExposureSegment> exposureSegments =
                shouldAdvanceWeatherExposureCheckpoint && weatherExposureState is not null
                    ? CityPopulationWeatherExposurePlanner.BuildSegments(
                        weatherExposureState: weatherExposureState,
                        fromSimTimeUtc: request.FromSimTimeUtc,
                        toSimTimeUtc: request.ToSimTimeUtc)
                    : [];
            bool requiresWeatherExposure = exposureSegments.Count > 0;
            IReadOnlyCollection<PersonEntity>? personsSnapshot = null;
            IReadOnlyCollection<HouseholdEntity>? householdsSnapshot = null;
            IReadOnlyCollection<ClassicCityHouseholdPlacement>? placementsSnapshot = null;
            List<CityPopulationActivityWriteModel> pendingActivityEntries = [];
            CityEconomyDailySettlementSnapshot? pendingEconomySettlement = null;
            List<ClassicCityHouseholdCashflowSettlementItemV1> pendingHouseholdCashflowItems = [];
            List<ClassicCityWorkplacePayrollSettlementItemV1> pendingWorkplacePayrollItems = [];
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> districtUtilityConditionsByDistrictId =
                new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>();

            if ((requiresDateProgression || requiresNeedsProgression || requiresWeatherExposure) && environment is null)
                logger.LogWarning(
                    message:
                    "Advancing city population without synced environment for cityId={CityId}. Climate adaptation will be neutral and needs progression will use UTC fallback.",
                    request.CityId);

            if (requiresDateProgression || requiresNeedsProgression || requiresWeatherExposure)
            {
                try
                {
                    districtUtilityConditionsByDistrictId =
                        await districtUtilityConditionsClient.GetByCityAsync(
                            cityId: cityId.Value,
                            cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Failed to load district utility conditions for cityId={CityId}. Falling back to synthetic district impact.",
                        request.CityId);
                }
            }

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    if (requiresDateProgression || requiresNeedsProgression || requiresWeatherExposure)
                    {
                        var residents = (await personReadRepository.ListByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct)).ToList();
                        personsSnapshot = residents;
                        var personsById = residents.ToDictionary(
                            keySelector: x => x.Id,
                            elementSelector: x => x);
                        var residentsByHouseholdId = residents
                           .GroupBy(x => x.HouseholdId)
                           .ToDictionary(
                                keySelector: x => x.Key,
                                elementSelector: x => (IReadOnlyCollection<PersonEntity>)x.ToList());
                        placementsSnapshot = await householdWriteRepository.ListPlacementsByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);
                        IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId = placementsSnapshot
                           .ToDictionary(
                                keySelector: x => x.HouseholdId,
                                elementSelector: x => x.HousingStatus);
                        IReadOnlyDictionary<HouseholdId, DistrictId?> districtByHouseholdId = placementsSnapshot
                           .ToDictionary(
                                keySelector: x => x.HouseholdId,
                                elementSelector: x => x.DistrictId);
                        IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId =
                            placementsSnapshot.ToDictionary(
                                keySelector: x => x.HouseholdId,
                                elementSelector: x => x.ResidentialBuildingId);
                        IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>
                            financialStressByHouseholdId =
                                (await householdFinancialStressStateRepository.ListByCityAsync(
                                    cityId: cityId,
                                    cancellationToken: ct))
                               .ToDictionary(x => x.HouseholdId);
                        IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState>
                            employerStressByWorkplaceId =
                                (await employerFinancialStressStateRepository.ListByCityAsync(
                                    cityId: cityId,
                                    cancellationToken: ct))
                               .ToDictionary(x => x.WorkplaceId);
                        IReadOnlyList<CityPopulationAnchorCatalogItem> workplaceAnchors =
                            await cityPopulationAnchorCatalogRepository.ListByCityAsync(
                                cityId: cityId,
                                type: CityAnchorType.Workplace,
                                cancellationToken: ct);
                        IReadOnlyList<CityPopulationAnchorCatalogItem> schoolAnchors =
                            await cityPopulationAnchorCatalogRepository.ListByCityAsync(
                                cityId: cityId,
                                type: CityAnchorType.School,
                                cancellationToken: ct);
                        IReadOnlyList<CityPopulationAnchorCatalogItem> hospitalAnchors =
                            await cityPopulationAnchorCatalogRepository.ListByCityAsync(
                                cityId: cityId,
                                type: CityAnchorType.Hospital,
                                cancellationToken: ct);
                        CityPopulationHealthcarePressureProfile healthcarePressureProfile =
                            healthcarePressurePolicy.Evaluate(
                                residents: residents,
                                serviceQualityState: serviceQualityState,
                                livingConditionsState: livingConditionsState,
                                essentialsState: essentialsState);
                        var householdsById = (await householdWriteRepository.ListByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct)).ToDictionary(
                            keySelector: x => x.Id,
                            elementSelector: x => x);
                        Dictionary<EducationLevel, List<CityEducationInstitutionBinding>> institutionPools =
                            ResidentPlacementPoolBuilder.BuildEducationInstitutionPools(residents);
                        Dictionary<string, List<Job>> workplacePools =
                            ResidentPlacementPoolBuilder.BuildWorkplacePools(residents);

                        foreach (PersonEntity person in residents)
                        {
                            ResidentProgressionActivityCollector.Snapshot beforeSnapshot =
                                ResidentProgressionActivityCollector.Capture(person);

                            if (await ApplyProgressionNeedsExposureAndIllnessAsync(
                                    person: person,
                                    cityId: cityId,
                                    residentsById: personsById,
                                    householdsById: householdsById,
                                    residentsByHouseholdId: residentsByHouseholdId,
                                    previousDate: previousDate,
                                    fromSimTimeUtc: request.FromSimTimeUtc,
                                    toSimTimeUtc: request.ToSimTimeUtc,
                                    currentDate: toDate,
                                    requiresDateProgression: requiresDateProgression,
                                    requiresNeedsProgression: requiresNeedsProgression,
                                    environment: environment,
                                    exposureSegments: exposureSegments,
                                    housingByHouseholdId: housingByHouseholdId,
                                    districtByHouseholdId: districtByHouseholdId,
                                    residentialBuildingByHouseholdId: residentialBuildingByHouseholdId,
                                    employerStressByWorkplaceId: employerStressByWorkplaceId,
                                    workplaceAnchors: workplaceAnchors,
                                    schoolAnchors: schoolAnchors,
                                    financialStressByHouseholdId: financialStressByHouseholdId,
                                    costOfLivingState: costOfLivingState,
                                    essentialsState: essentialsState,
                                    livingConditionsState: livingConditionsState,
                                    districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId,
                                    districtImpactPolicy: districtImpactPolicy,
                                    serviceQualityState: serviceQualityState,
                                    healthcarePressureProfile: healthcarePressureProfile,
                                    marriageDomainService: marriageDomainService,
                                    educationAutonomyPolicy: educationAutonomyPolicy,
                                    employmentAutonomyPolicy: employmentAutonomyPolicy,
                                    householdPressurePolicy: householdPressurePolicy,
                                    illnessAutonomyPolicy: illnessAutonomyPolicy,
                                    healthcareAutonomyPolicy: healthcareAutonomyPolicy,
                                    anchorSelectionPolicy: anchorSelectionPolicy,
                                    hospitalAnchors: hospitalAnchors,
                                    livingConditionsPressurePolicy: livingConditionsPressurePolicy,
                                    institutionPools: institutionPools,
                                    workplacePools: workplacePools,
                                    personNeedsProgressionPolicy: personNeedsProgressionPolicy,
                                    weatherExposurePolicy: weatherExposurePolicy,
                                    commuteRoutingService: commuteRoutingService,
                                    cancellationToken: ct))
                            {
                                affectedPeopleCount++;
                                ResidentProgressionActivityCollector.Collect(
                                    cityId: cityId,
                                    currentDate: toDate,
                                    before: beforeSnapshot,
                                    resident: person,
                                    residentsById: personsById,
                                    activityEntries: pendingActivityEntries,
                                    occurredAtUtc: handledAtUtc);
                            }
                        }

                        if (requiresDateProgression)
                            pendingEconomySettlement = await HouseholdCashflowSettlementStep.ApplyAsync(
                                cityId: cityId,
                                householdsById: householdsById,
                                residentsByHouseholdId: residentsByHouseholdId,
                                housingByHouseholdId: housingByHouseholdId,
                                residentialBuildingIdByHouseholdId: placementsSnapshot
                                   .GroupBy(x => x.HouseholdId)
                                   .ToDictionary(
                                        keySelector: x => x.Key,
                                        elementSelector: x => x.Select(y => y.ResidentialBuildingId)
                                           .FirstOrDefault()),
                                previousDate: previousDate,
                                currentDate: toDate,
                                householdCashflowPolicy: householdCashflowPolicy,
                                costOfLivingState: costOfLivingState,
                                essentialsState: essentialsState,
                                livingConditionsState: livingConditionsState,
                                districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId,
                                districtByHouseholdId: districtByHouseholdId,
                                districtImpactPolicy: districtImpactPolicy,
                                participationPolicy: participationPolicy,
                                commuteRoutingService: commuteRoutingService,
                                cashflowItems: pendingHouseholdCashflowItems,
                                workplacePayrollItems: pendingWorkplacePayrollItems,
                                cancellationToken: ct);

                        if (requiresDateProgression)
                        {
                            affectedPeopleCount += await BirthAutonomyStep.ApplyAsync(
                                cityId: cityId,
                                residentsById: personsById,
                                housingStatusesByHouseholdId: housingByHouseholdId,
                                previousDate: previousDate,
                                currentDate: toDate,
                                birthAutonomyPolicy: birthAutonomyPolicy,
                                populationBirthDomainService: populationBirthDomainService,
                                personWriteRepository: personWriteRepository,
                                householdWriteRepository: householdWriteRepository,
                                activityEntries: pendingActivityEntries,
                                residents: residents,
                                occurredAtUtc: handledAtUtc,
                                cancellationToken: ct);

                            affectedPeopleCount += await CivilRegistryAutonomyStep.ApplyAsync(
                                cityId: cityId,
                                residentsById: personsById,
                                previousDate: previousDate,
                                currentDate: toDate,
                                householdWriteRepository: householdWriteRepository,
                                marriageDomainService: marriageDomainService,
                                civilRegistryAutonomyPolicy: civilRegistryAutonomyPolicy,
                                activityEntries: pendingActivityEntries,
                                occurredAtUtc: handledAtUtc,
                                cancellationToken: ct);

                            affectedPeopleCount += await ApplyHouseholdIndependenceAutonomyAsync(
                                cityId: cityId,
                                residentsById: personsById,
                                previousDate: previousDate,
                                currentDate: toDate,
                                householdWriteRepository: householdWriteRepository,
                                householdIndependenceAutonomyPolicy: householdIndependenceAutonomyPolicy,
                                activityEntries: pendingActivityEntries,
                                occurredAtUtc: handledAtUtc,
                                cancellationToken: ct);

                            affectedPeopleCount += await ApplyHousingAutonomyAsync(
                                cityId: cityId,
                                residentsById: personsById,
                                previousDate: previousDate,
                                currentDate: toDate,
                                householdWriteRepository: householdWriteRepository,
                                financialStressByHouseholdId: financialStressByHouseholdId,
                                costOfLivingState: costOfLivingState,
                                serviceQualityState: serviceQualityState,
                                districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId,
                                housingAutonomyPolicy: housingAutonomyPolicy,
                                anchorSelectionPolicy: anchorSelectionPolicy,
                                cityPopulationAnchorCatalogRepository: cityPopulationAnchorCatalogRepository,
                                commuteRoutingService: commuteRoutingService,
                                activityEntries: pendingActivityEntries,
                                occurredAtUtc: handledAtUtc,
                                cancellationToken: ct);
                        }
                    }

                    DateTimeOffset updatedAtUtc = handledAtUtc;
                    if (state is null)
                    {
                        var newState = CityPopulationProgressionState.Create(
                            cityId: cityId,
                            lastProcessedTickId: request.TickId,
                            lastProcessedDate: toDate,
                            updatedAtUtc: updatedAtUtc);
                        await progressionStateRepository.AddAsync(
                            state: newState,
                            cancellationToken: ct);
                    }
                    else
                        state.MarkProcessed(
                            tickId: request.TickId,
                            processedDate: toDate,
                            updatedAtUtc: updatedAtUtc);

                    if (shouldAdvanceWeatherExposureCheckpoint && weatherExposureState is not null)
                        weatherExposureState.MarkExposureProcessed(
                            processedAtSimTimeUtc: request.ToSimTimeUtc,
                            updatedAtUtc: updatedAtUtc);

                    if (personsSnapshot is not null)
                    {
                        householdsSnapshot =
                            await householdWriteRepository.ListByCityAsync(
                                cityId: cityId,
                                cancellationToken: ct);
                        placementsSnapshot ??=
                            await householdWriteRepository.ListPlacementsByCityAsync(
                                cityId: cityId,
                                cancellationToken: ct);

                        await cityPopulationSummaryProjectionService.UpdateAsync(
                            cityId: cityId,
                            currentDate: toDate,
                            persons: personsSnapshot,
                            householdPlacements: placementsSnapshot,
                            cancellationToken: ct);

                        foreach (CityPopulationActivityWriteModel activityEntry in pendingActivityEntries)
                            await cityPopulationActivityJournalService.RecordAsync(
                                entry: activityEntry,
                                cancellationToken: ct);

                        foreach (ClassicCityHouseholdAccountSyncBatchV1 batch in
                                 ClassicCityHouseholdAccountSyncBatchFactory.Build(
                                     cityId: cityId.Value,
                                     households: householdsSnapshot,
                                     placements: placementsSnapshot,
                                     correlationId: $"classic-city:{cityId.Value:N}:tick:{request.TickId}:households",
                                     occurredAtUtc: updatedAtUtc,
                                     batchSize: EconomyHouseholdSyncBatchSize))
                            await cityEconomySettlementOutboxWriter.AddClassicCityHouseholdAccountSyncBatchAsync(
                                batch: batch,
                                cancellationToken: ct);
                    }

                    if (pendingEconomySettlement is not null)
                    {
                        string economySettlementCorrelationId =
                            $"classic-city:{cityId.Value:N}:tick:{request.TickId:N}:economy-settlement";
                        await cityEconomySettlementOutboxWriter.AddCityDailySettlementAsync(
                            settlement: new CityEconomyDailySettlementV1(
                                CityId: cityId.Value,
                                TickId: request.TickId,
                                CurrentDate: pendingEconomySettlement.CurrentDate,
                                SettledDays: pendingEconomySettlement.SettledDays,
                                HouseholdCount: pendingEconomySettlement.HouseholdCount,
                                ResidentCount: pendingEconomySettlement.ResidentCount,
                                GrossPayrollAmount: pendingEconomySettlement.GrossPayroll.Amount,
                                IncomeTaxAmount: pendingEconomySettlement.IncomeTax.Amount,
                                NetPayrollAmount: pendingEconomySettlement.NetPayroll.Amount,
                                RetailTurnoverAmount: pendingEconomySettlement.RetailTurnover.Amount,
                                RetailTaxAmount: pendingEconomySettlement.RetailTax.Amount,
                                HousingSpendAmount: pendingEconomySettlement.HousingSpend.Amount,
                                CorrelationId: economySettlementCorrelationId,
                                OccurredAtUtc: updatedAtUtc),
                            cancellationToken: ct);
                    }

                    foreach (ClassicCityHouseholdCashflowSettlementBatchV1 batch in
                             ClassicCityEconomySettlementBatchFactory.BuildHouseholdCashflowSettlementBatches(
                                 cityId: cityId.Value,
                                 currentDate: toDate,
                                 settledDays: Math.Max(
                                     val1: 0,
                                     val2: toDate.DayNumber - previousDate.DayNumber),
                                 items: pendingHouseholdCashflowItems,
                                 correlationId:
                                 $"classic-city:{cityId.Value:N}:tick:{request.TickId}:household-cashflow",
                                 occurredAtUtc: updatedAtUtc))
                        await cityEconomySettlementOutboxWriter.AddClassicCityHouseholdCashflowSettlementBatchAsync(
                            batch: batch,
                            cancellationToken: ct);

                    foreach (ClassicCityWorkplacePayrollSettlementBatchV1 batch in
                             ClassicCityEconomySettlementBatchFactory.BuildWorkplacePayrollSettlementBatches(
                                 cityId: cityId.Value,
                                 currentDate: toDate,
                                 settledDays: Math.Max(
                                     val1: 0,
                                     val2: toDate.DayNumber - previousDate.DayNumber),
                                 items: pendingWorkplacePayrollItems,
                                 correlationId:
                                 $"classic-city:{cityId.Value:N}:tick:{request.TickId}:workplace-payroll",
                                 occurredAtUtc: updatedAtUtc))
                        await cityEconomySettlementOutboxWriter.AddClassicCityWorkplacePayrollSettlementBatchAsync(
                            batch: batch,
                            cancellationToken: ct);

                    if (personsSnapshot is not null)
                        foreach (ClassicCityWorkplaceBusinessSyncBatchV1 batch in
                                 ClassicCityWorkplaceBusinessSyncBatchFactory.Build(
                                     cityId: cityId.Value,
                                     persons: personsSnapshot,
                                     correlationId: $"classic-city:{cityId.Value:N}:tick:{request.TickId}:workplaces",
                                     occurredAtUtc: updatedAtUtc,
                                     batchSize: EconomyWorkplaceSyncBatchSize))
                            await cityEconomySettlementOutboxWriter.AddClassicCityWorkplaceBusinessSyncBatchAsync(
                                batch: batch,
                                cancellationToken: ct);

                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            if (personsSnapshot is not null && placementsSnapshot is not null)
            {
                try
                {
                    await commuteTripSyncService.SyncAsync(
                        cityId: cityId.Value,
                        tickId: request.TickId,
                        currentDate: toDate,
                        currentSimTimeUtc: request.ToSimTimeUtc,
                        residents: personsSnapshot,
                        householdPlacements: placementsSnapshot,
                        hospitalAnchors: await cityPopulationAnchorCatalogRepository.ListByCityAsync(
                            cityId: cityId,
                            type: CityAnchorType.Hospital,
                            cancellationToken: cancellationToken),
                        anchorSelectionPolicy: anchorSelectionPolicy,
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Failed to sync population commute trips for cityId={CityId} at tickId={TickId}.",
                        request.CityId,
                        request.TickId);
                }
            }

            return new AdvanceCityPopulationResult(
                Status: AdvanceCityPopulationStatus.Applied,
                AffectedPeopleCount: affectedPeopleCount);
        }

        private static async Task<bool> ApplyProgressionNeedsExposureAndIllnessAsync(
            PersonEntity person,
            CityId cityId,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            IReadOnlyDictionary<HouseholdId, HouseholdEntity> householdsById,
            IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> residentsByHouseholdId,
            DateOnly previousDate,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc,
            DateOnly currentDate,
            bool requiresDateProgression,
            bool requiresNeedsProgression,
            CityPopulationEnvironment? environment,
            IReadOnlyCollection<CityWeatherExposureSegment> exposureSegments,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            IReadOnlyDictionary<HouseholdId, DistrictId?> districtByHouseholdId,
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId,
            IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState> employerStressByWorkplaceId,
            IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState> financialStressByHouseholdId,
            CityPopulationCostOfLivingState? costOfLivingState,
            CityPopulationEssentialsState? essentialsState,
            CityPopulationLivingConditionsState? livingConditionsState,
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> districtUtilityConditionsByDistrictId,
            CityPopulationDistrictImpactPolicy districtImpactPolicy,
            CityPopulationServiceQualityState? serviceQualityState,
            CityPopulationHealthcarePressureProfile healthcarePressureProfile,
            MarriageDomainService marriageDomainService,
            CityEducationAutonomyPolicy educationAutonomyPolicy,
            CityEmploymentAutonomyPolicy employmentAutonomyPolicy,
            CityHouseholdPressurePolicy householdPressurePolicy,
            CityIllnessAutonomyPolicy illnessAutonomyPolicy,
            CityHealthcareAutonomyPolicy healthcareAutonomyPolicy,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> hospitalAnchors,
            CityPopulationLivingConditionsPressurePolicy livingConditionsPressurePolicy,
            IDictionary<EducationLevel, List<CityEducationInstitutionBinding>> institutionPools,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> workplaceAnchors,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> schoolAnchors,
            IDictionary<string, List<Job>> workplacePools,
            PersonNeedsProgressionPolicy personNeedsProgressionPolicy,
            CityPopulationWeatherExposurePolicy weatherExposurePolicy,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            CancellationToken cancellationToken)
        {
            bool changed = false;
            if (requiresNeedsProgression &&
                ResidentNeedsProgressionStep.Apply(
                    person: person,
                    residentsById: residentsById,
                    fromSimTimeUtc: fromSimTimeUtc,
                    toSimTimeUtc: toSimTimeUtc,
                    currentDate: currentDate,
                    environment: environment,
                    marriageDomainService: marriageDomainService,
                    personNeedsProgressionPolicy: personNeedsProgressionPolicy))
                changed = true;
            if (requiresDateProgression &&
                await ResidentTimeProgressionStep.ApplyAsync(
                    cityId: cityId,
                    person: person,
                    householdsById: householdsById,
                    residentsByHouseholdId: residentsByHouseholdId,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    housingByHouseholdId: housingByHouseholdId,
                    districtByHouseholdId: districtByHouseholdId,
                    residentialBuildingByHouseholdId: residentialBuildingByHouseholdId,
                    employerStressByWorkplaceId: employerStressByWorkplaceId,
                    costOfLivingState: costOfLivingState,
                    serviceQualityState: serviceQualityState,
                    educationAutonomyPolicy: educationAutonomyPolicy,
                    employmentAutonomyPolicy: employmentAutonomyPolicy,
                    institutionPools: institutionPools,
                    workplaceAnchors: workplaceAnchors,
                    schoolAnchors: schoolAnchors,
                    workplacePools: workplacePools,
                    commuteRoutingService: commuteRoutingService,
                    cancellationToken: cancellationToken))
                changed = true;
            if (requiresDateProgression &&
                await ResidentHouseholdPressureProgressionStep.ApplyAsync(
                    cityId: cityId,
                    person: person,
                    residentsByHouseholdId: residentsByHouseholdId,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    housingByHouseholdId: housingByHouseholdId,
                    residentialBuildingByHouseholdId: residentialBuildingByHouseholdId,
                    financialStressByHouseholdId: financialStressByHouseholdId,
                    commuteRoutingService: commuteRoutingService,
                    cancellationToken: cancellationToken,
                    householdPressurePolicy: householdPressurePolicy))
                changed = true;
            if (requiresDateProgression &&
                ResidentLivingConditionsProgressionStep.Apply(
                    person: person,
                    residentsById: residentsById,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    housingByHouseholdId: housingByHouseholdId,
                    districtByHouseholdId: districtByHouseholdId,
                    livingConditionsState: livingConditionsState,
                    districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId,
                    essentialsState: essentialsState,
                    districtImpactPolicy: districtImpactPolicy,
                    livingConditionsPressurePolicy: livingConditionsPressurePolicy,
                    marriageDomainService: marriageDomainService))
                changed = true;
            if (exposureSegments.Count > 0 &&
                ResidentWeatherExposureStep.Apply(
                    person: person,
                    residentsById: residentsById,
                    currentDate: currentDate,
                    environment: environment,
                    exposureSegments: exposureSegments,
                    marriageDomainService: marriageDomainService,
                    weatherExposurePolicy: weatherExposurePolicy))
                changed = true;
            if (requiresDateProgression &&
                await ResidentIllnessProgressionStep.ApplyAsync(
                    person: person,
                    cityId: cityId,
                    residentsById: residentsById,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    housingByHouseholdId: housingByHouseholdId,
                    districtByHouseholdId: districtByHouseholdId,
                    residentialBuildingByHouseholdId: residentialBuildingByHouseholdId,
                    exposureSegments: exposureSegments,
                    livingConditionsState: livingConditionsState,
                    districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId,
                    essentialsState: essentialsState,
                    serviceQualityState: serviceQualityState,
                    healthcarePressureProfile: healthcarePressureProfile,
                    marriageDomainService: marriageDomainService,
                    illnessAutonomyPolicy: illnessAutonomyPolicy,
                    healthcareAutonomyPolicy: healthcareAutonomyPolicy,
                    anchorSelectionPolicy: anchorSelectionPolicy,
                    hospitalAnchors: hospitalAnchors,
                    districtImpactPolicy: districtImpactPolicy,
                    livingConditionsPressurePolicy: livingConditionsPressurePolicy,
                    commuteRoutingService: commuteRoutingService,
                    cancellationToken: cancellationToken))
                changed = true;
            return changed;
        }

        private static async Task<int> ApplyHouseholdIndependenceAutonomyAsync(
            CityId cityId,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            DateOnly previousDate,
            DateOnly currentDate,
            IHouseholdWriteRepository householdWriteRepository,
            CityHouseholdIndependenceAutonomyPolicy householdIndependenceAutonomyPolicy,
            ICollection<CityPopulationActivityWriteModel> activityEntries,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<ClassicCityHouseholdPlacement> placements =
                await householdWriteRepository.ListPlacementsByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

            if (placements.Count == 0)
                return 0;

            var housingStatuses = placements.ToDictionary(
                keySelector: x => x.HouseholdId,
                elementSelector: x => x.HousingStatus);

            IReadOnlyList<CityHouseholdIndependenceAutonomyDecision> decisions =
                householdIndependenceAutonomyPolicy.Plan(
                    residents: residentsById.Values.ToArray(),
                    housingStatuses: housingStatuses,
                    previousDate: previousDate,
                    currentDate: currentDate);

            if (decisions.Count == 0)
                return 0;

            int affectedResidents = 0;

            foreach (CityHouseholdIndependenceAutonomyDecision decision in decisions)
            {
                if (!residentsById.TryGetValue(
                        key: decision.ResidentId,
                        value: out PersonEntity? resident) ||
                    resident.HouseholdId != decision.SourceHouseholdId)
                    continue;

                if (!await ClassicCityHouseholdAutonomySupport.MoveResidentIntoIndependentHouseholdAsync(
                        cityId: cityId,
                        resident: resident,
                        householdWriteRepository: householdWriteRepository,
                        createdAtUtc: occurredAtUtc,
                        cancellationToken: cancellationToken))
                    continue;

                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentFormedIndependentHousehold(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        source: CityPopulationActivitySource.Autonomy,
                        occurredAtUtc: occurredAtUtc));
                affectedResidents++;
            }

            return affectedResidents;
        }

        private static async Task<int> ApplyHousingAutonomyAsync(
            CityId cityId,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            DateOnly previousDate,
            DateOnly currentDate,
            IHouseholdWriteRepository householdWriteRepository,
            IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState> financialStressByHouseholdId,
            CityPopulationCostOfLivingState? costOfLivingState,
            CityPopulationServiceQualityState? serviceQualityState,
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> districtUtilityConditionsByDistrictId,
            CityHousingAutonomyPolicy housingAutonomyPolicy,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
            ICityPopulationAnchorCatalogRepository cityPopulationAnchorCatalogRepository,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            ICollection<CityPopulationActivityWriteModel> activityEntries,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<ClassicCityHouseholdPlacement> placements =
                await householdWriteRepository.ListPlacementsByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

            if (placements.Count == 0)
                return 0;

            var housingStatuses = placements.ToDictionary(
                keySelector: x => x.HouseholdId,
                elementSelector: x => x.HousingStatus);
            var householdsById = (await householdWriteRepository.ListByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken)).ToDictionary(
                keySelector: x => x.Id,
                elementSelector: x => x);
            var residentsByHousehold = residentsById.Values
               .Where(x => x.IsAlive)
               .GroupBy(x => x.HouseholdId)
               .ToDictionary(
                    keySelector: x => x.Key,
                    elementSelector: x => x.ToList());
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId =
                placements.ToDictionary(
                    keySelector: x => x.HouseholdId,
                    elementSelector: x => x.ResidentialBuildingId);
            IReadOnlyDictionary<HouseholdId, CityDistrictUtilityConditionsSnapshot> districtUtilityConditionsByHouseholdId =
                placements
                   .Where(x => x.DistrictId.HasValue)
                   .Select(x => new
                    {
                        x.HouseholdId,
                        Snapshot = ClassicCityHousingOpportunityPlanner.ResolveDistrictUtilityConditions(
                            districtId: x.DistrictId,
                            districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId)
                    })
                   .Where(x => x.Snapshot is not null)
                   .ToDictionary(
                        keySelector: x => x.HouseholdId,
                        elementSelector: x => x.Snapshot!);
            var commutePressureProfilesByHouseholdId = new Dictionary<HouseholdId, CityHouseholdCommutePressureProfile>();

            foreach (ClassicCityHouseholdPlacement placement in placements)
            {
                if (!residentsByHousehold.TryGetValue(
                        key: placement.HouseholdId,
                        value: out List<PersonEntity>? householdResidents) ||
                    householdResidents.Count == 0)
                    continue;

                CityHouseholdCommutePressureProfile? commutePressureProfile =
                    await HouseholdCommutePressureProfileBuilder.BuildAsync(
                        cityId: cityId,
                        householdId: placement.HouseholdId,
                        householdResidents: householdResidents,
                        residentialBuildingByHouseholdId: residentialBuildingByHouseholdId,
                        commuteRoutingService: commuteRoutingService,
                        cancellationToken: cancellationToken);

                if (commutePressureProfile is not null)
                    commutePressureProfilesByHouseholdId[placement.HouseholdId] = commutePressureProfile;
            }

            IReadOnlyList<CityHousingAutonomyDecision> decisions = housingAutonomyPolicy.Plan(
                households: householdsById,
                residents: residentsById.Values.ToArray(),
                housingStatuses: housingStatuses,
                financialStressStates: financialStressByHouseholdId,
                commutePressureProfiles: commutePressureProfilesByHouseholdId,
                districtUtilityConditionsByHouseholdId: districtUtilityConditionsByHouseholdId,
                previousDate: previousDate,
                currentDate: currentDate,
                costOfLivingState: costOfLivingState,
                serviceQualityState: serviceQualityState);

            if (decisions.Count == 0)
                return 0;

            var placementsByHousehold = placements.ToDictionary(
                keySelector: x => x.HouseholdId,
                elementSelector: x => x);
            IReadOnlyList<CityPopulationAnchorCatalogItem> hospitalAnchors =
                await cityPopulationAnchorCatalogRepository.ListByCityAsync(
                    cityId: cityId,
                    type: CityAnchorType.Hospital,
                    cancellationToken: cancellationToken);

            List<(DistrictId DistrictId, ResidentialBuildingId ResidentialBuildingId)> housingPool =
                ClassicCityHousingOpportunityPlanner.BuildHousingOpportunityPool(placements);

            int affectedResidents = 0;

            foreach (CityHousingAutonomyDecision decision in decisions)
            {
                if (!placementsByHousehold.TryGetValue(
                        key: decision.HouseholdId,
                        value: out ClassicCityHouseholdPlacement? placement) ||
                    !residentsByHousehold.TryGetValue(
                        key: decision.HouseholdId,
                        value: out List<PersonEntity>? householdResidents) ||
                    householdResidents.Count == 0)
                    continue;

                PersonEntity anchorResident = ClassicCityHousingOpportunityPlanner.SelectHousingAnchorResident(
                    householdResidents: householdResidents,
                    currentDate: currentDate);

                switch (decision.Type)
                {
                    case CityHousingAutonomyDecisionType.FindHousing:
                        if (placement.HousingStatus == HousingStatus.Housed ||
                            housingPool.Count == 0)
                            continue;

                        (DistrictId districtId, ResidentialBuildingId residentialBuildingId) opportunity =
                            await ClassicCityHousingOpportunityPlanner.SelectHousingOpportunityAsync(
                                cityId: cityId,
                                householdId: placement.HouseholdId,
                                currentDate: currentDate,
                                housingPool: housingPool,
                                householdResidents: householdResidents,
                                hospitalAnchors: hospitalAnchors,
                                districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId,
                                anchorSelectionPolicy: anchorSelectionPolicy,
                                commuteRoutingService: commuteRoutingService,
                                cancellationToken: cancellationToken);

                        placement.Relocate(
                            cityId: cityId,
                            districtId: opportunity.districtId,
                            residentialBuildingId: opportunity.residentialBuildingId);
                        activityEntries.Add(
                            ClassicCityActivityFactory.HouseholdFoundHousing(
                                cityId: cityId.Value,
                                currentDate: currentDate,
                                resident: anchorResident,
                                source: CityPopulationActivitySource.Autonomy,
                                occurredAtUtc: occurredAtUtc));
                        affectedResidents += householdResidents.Count;
                        break;

                    case CityHousingAutonomyDecisionType.LoseHousing:
                        if (placement.HousingStatus != HousingStatus.Housed)
                            continue;

                        placement.BecomeHomeless(cityId);
                        activityEntries.Add(
                            ClassicCityActivityFactory.HouseholdLostHousing(
                                cityId: cityId.Value,
                                currentDate: currentDate,
                                resident: anchorResident,
                                source: CityPopulationActivitySource.Autonomy,
                                occurredAtUtc: occurredAtUtc));
                        affectedResidents += householdResidents.Count;
                        break;
                }
            }

            return affectedResidents;
        }

    }
}
