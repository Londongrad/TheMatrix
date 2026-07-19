using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Integration;
using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Weather;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using DistrictId = Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects.DistrictId;
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;
using HouseholdId = Matrix.Population.Domain.ValueObjects.HouseholdId;
using Job = Matrix.Population.Domain.ValueObjects.Job;
using PersonEntity = Matrix.Population.Domain.Entities.Person;
using PersonId = Matrix.Population.Domain.ValueObjects.PersonId;
using ResidentialBuildingId = Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects.ResidentialBuildingId;
using WorkplaceId = Matrix.Population.Domain.ValueObjects.WorkplaceId;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class AdvanceCityPopulationCommandHandler(
        ICityPopulationPersonReadRepository personReadRepository,
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationAnchorCatalogRepository cityPopulationAnchorCatalogRepository,
        ICityPopulationCostOfLivingStateRepository cityPopulationCostOfLivingStateRepository,
        ICityPopulationEssentialsStateRepository cityPopulationEssentialsStateRepository,
        ICityPopulationServiceQualityStateRepository cityPopulationServiceQualityStateRepository,
        ICityHealthcarePressureSnapshotRepository healthcarePressureSnapshotRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        ICityPopulationEmployerFinancialStressStateRepository employerFinancialStressStateRepository,
        ICityPopulationEnvironmentRepository cityPopulationEnvironmentRepository,
        ICityPopulationHouseholdFinancialStressStateRepository householdFinancialStressStateRepository,
        ICityPopulationLivingConditionsStateRepository cityPopulationLivingConditionsStateRepository,
        IEducationParticipationProjectionRepository educationParticipationProjectionRepository,
        ICityDistrictUtilityConditionsClient districtUtilityConditionsClient,
        ICityPopulationCommuteRoutingService commuteRoutingService,
        ICityPopulationCommuteTripSyncService commuteTripSyncService,
        ICityPopulationActivityJournalService cityPopulationActivityJournalService,
        ICityEconomySettlementOutboxWriter cityEconomySettlementOutboxWriter,
        IPopulationResidentFactsOutboxWriter residentFactsOutboxWriter,
        IPopulationResidentHealthRiskOutboxWriter residentHealthRiskOutboxWriter,
        IPopulationResidentVitalStateOutboxWriter residentVitalStateOutboxWriter,
        ICityPopulationProgressionStateRepository progressionStateRepository,
        ICityPopulationPendingWeatherImpactRepository pendingWeatherImpactRepository,
        ICityPopulationSummaryProjectionService cityPopulationSummaryProjectionService,
        ICityPopulationWeatherExposureStateRepository weatherExposureStateRepository,
        IHouseholdWriteRepository householdWriteRepository,
        MarriageDomainService marriageDomainService,
        PopulationBirthDomainService populationBirthDomainService,
        IPersonWriteRepository personWriteRepository,
        CityBirthAutonomyPolicy birthAutonomyPolicy,
        CityCivilRegistryAutonomyPolicy civilRegistryAutonomyPolicy,
        CityEmploymentAutonomyPolicy employmentAutonomyPolicy,
        CityHealthcareAutonomyPolicy healthcareAutonomyPolicy,
        CityHouseholdCashflowPolicy householdCashflowPolicy,
        CityHouseholdPressurePolicy householdPressurePolicy,
        CityHousingAutonomyPolicy housingAutonomyPolicy,
        CityHouseholdIndependenceAutonomyPolicy householdIndependenceAutonomyPolicy,
        CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
        CityPopulationDistrictImpactPolicy districtImpactPolicy,
        CityPopulationLivingConditionsPressurePolicy livingConditionsPressurePolicy,
        CityPopulationParticipationPolicy participationPolicy,
        PersonNeedsProgressionPolicy personNeedsProgressionPolicy,
        CityPopulationWeatherImpactPolicy weatherImpactPolicy,
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
            List<PersonEntity> registeredResidents = [];
            List<PersonEntity> residentLifecycleChanges = [];
            IReadOnlyCollection<PopulationResidentHealthRiskSnapshot> pendingHealthRiskSnapshots = [];
            Dictionary<PersonId, int> pendingHealthAdjustmentsByResidentId = [];
            IReadOnlyList<CityPopulationPendingWeatherImpact> pendingWeatherImpacts = [];
            CityEconomyDailySettlementSnapshot? pendingEconomySettlement = null;
            List<ClassicCityHouseholdCashflowSettlementItemV1> pendingHouseholdCashflowItems = [];
            List<ClassicCityWorkplacePayrollSettlementItemV1> pendingWorkplacePayrollItems = [];
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>
                districtUtilityConditionsByDistrictId =
                    new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>();

            if ((requiresDateProgression || requiresNeedsProgression || requiresWeatherExposure) && environment is null)
                logger.LogWarning(
                    message:
                    "Advancing city population without synced environment for cityId={CityId}. Climate adaptation will be neutral and needs progression will use UTC fallback.",
                    request.CityId);

            if (requiresDateProgression || requiresNeedsProgression || requiresWeatherExposure)
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
                        exception: ex,
                        message:
                        "Failed to load district utility conditions for cityId={CityId}. Falling back to synthetic district impact.",
                        request.CityId);
                }

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    pendingWeatherImpacts = await pendingWeatherImpactRepository.ListByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);
                    bool requiresPendingWeatherImpact = pendingWeatherImpacts.Count > 0;

                    if (requiresDateProgression || requiresNeedsProgression || requiresWeatherExposure ||
                        requiresPendingWeatherImpact)
                    {
                        AdvanceCityPopulationWorkingSet workingSet =
                            await AdvanceCityPopulationWorkingSetLoader.LoadAsync(
                                cityId: cityId,
                                currentDate: toDate,
                                personReadRepository: personReadRepository,
                                householdWriteRepository: householdWriteRepository,
                                householdFinancialStressStateRepository: householdFinancialStressStateRepository,
                                employerFinancialStressStateRepository: employerFinancialStressStateRepository,
                                cityPopulationAnchorCatalogRepository: cityPopulationAnchorCatalogRepository,
                                educationParticipationProjectionRepository: educationParticipationProjectionRepository,
                                includeEducationParticipation: requiresDateProgression,
                                includeActiveEducationParticipation: requiresNeedsProgression,
                                cancellationToken: ct);
                        List<PersonEntity> residents = workingSet.Residents;
                        personsSnapshot = residents;
                        placementsSnapshot = workingSet.Placements;
                        Dictionary<PersonId, PersonEntity> personsById = workingSet.ResidentsById;
                        Dictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> residentsByHouseholdId =
                            workingSet.ResidentsByHouseholdId;
                        EducationParticipationProjectionIndex educationParticipation =
                            workingSet.EducationParticipation;
                        IReadOnlyDictionary<PersonId, PersonRoutineProfile> routineProfilesByResidentId =
                            workingSet.RoutineProfilesByResidentId;
                        var commutePressureProfilesByHouseholdId =
                            new Dictionary<HouseholdId, CityHouseholdCommutePressureProfile?>();
                        IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId =
                            workingSet.HousingByHouseholdId;
                        IReadOnlyDictionary<HouseholdId, DistrictId?> districtByHouseholdId =
                            workingSet.DistrictByHouseholdId;
                        IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId =
                            workingSet.ResidentialBuildingByHouseholdId;
                        IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>
                            financialStressByHouseholdId = workingSet.FinancialStressByHouseholdId;
                        IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState>
                            employerStressByWorkplaceId = workingSet.EmployerStressByWorkplaceId;
                        IReadOnlyList<CityPopulationAnchorCatalogItem> workplaceAnchors = workingSet.WorkplaceAnchors;
                        Dictionary<HouseholdId, HouseholdEntity> householdsById = workingSet.HouseholdsById;
                        Dictionary<string, List<Job>> workplacePools = workingSet.WorkplacePools;

                        foreach (PersonEntity person in residents)
                        {
                            ResidentProgressionActivityCollector.Snapshot beforeSnapshot =
                                ResidentProgressionActivityCollector.Capture(person);

                            ResidentProgressionStepResult progressionResult =
                                await ResidentProgressionStep.ApplyAsync(
                                    person: person,
                                    cityId: cityId,
                                    householdsById: householdsById,
                                    residentsByHouseholdId: residentsByHouseholdId,
                                    educationParticipation: educationParticipation,
                                    routineProfilesByResidentId: routineProfilesByResidentId,
                                    economicContextsByResidentId: workingSet.EconomicContextsByResidentId,
                                    commutePressureProfilesByHouseholdId: commutePressureProfilesByHouseholdId,
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
                                    financialStressByHouseholdId: financialStressByHouseholdId,
                                    costOfLivingState: costOfLivingState,
                                    essentialsState: essentialsState,
                                    livingConditionsState: livingConditionsState,
                                    districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId,
                                    districtImpactPolicy: districtImpactPolicy,
                                    employmentAutonomyPolicy: employmentAutonomyPolicy,
                                    householdPressurePolicy: householdPressurePolicy,
                                    livingConditionsPressurePolicy: livingConditionsPressurePolicy,
                                    workplacePools: workplacePools,
                                    personNeedsProgressionPolicy: personNeedsProgressionPolicy,
                                    weatherExposurePolicy: weatherExposurePolicy,
                                    commuteRoutingService: commuteRoutingService,
                                    cancellationToken: ct);
                            int pendingWeatherHealthDelta = PendingWeatherHealthImpactStep.CalculateHealthDelta(
                                person: person,
                                pendingImpacts: pendingWeatherImpacts,
                                weatherImpactPolicy: weatherImpactPolicy);
                            int externalHealthDelta = Math.Clamp(
                                progressionResult.ExternalHealthDelta + pendingWeatherHealthDelta,
                                -100,
                                100);
                            if (externalHealthDelta != 0)
                                pendingHealthAdjustmentsByResidentId[person.Id] =
                                    externalHealthDelta;
                            if (progressionResult.HasAnyEffect || pendingWeatherHealthDelta != 0)
                            {
                                affectedPeopleCount++;
                                if (beforeSnapshot.IsAlive != person.IsAlive)
                                    residentLifecycleChanges.Add(person);

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

                        if (requiresDateProgression || pendingHealthAdjustmentsByResidentId.Count > 0)
                        {
                            IReadOnlyList<CityPopulationAnchorCatalogItem> hospitalAnchors =
                                await cityPopulationAnchorCatalogRepository.ListByCityAsync(
                                    cityId: cityId,
                                    type: CityAnchorType.Hospital,
                                    cancellationToken: ct);
                            ClassicCityHealthcarePressureSnapshot? healthcarePressureSnapshot =
                                await healthcarePressureSnapshotRepository.GetByCityAsync(
                                    cityId: cityId,
                                    cancellationToken: ct);
                            CityPopulationHealthcarePressureProfile healthcarePressureProfile =
                                healthcarePressureSnapshot?.Pressure ??
                                CityPopulationHealthcarePressureProfile.Baseline;
                            pendingHealthRiskSnapshots =
                                await ClassicCityResidentHealthRiskSnapshotFactory.BuildAsync(
                                    cityId: cityId,
                                    residents: residents,
                                    residentsByHouseholdId: residentsByHouseholdId,
                                    routineProfilesByResidentId: workingSet.RoutineProfilesByResidentId,
                                    currentDate: toDate,
                                    housingByHouseholdId: housingByHouseholdId,
                                    districtByHouseholdId: districtByHouseholdId,
                                    residentialBuildingByHouseholdId: residentialBuildingByHouseholdId,
                                    hadAdverseWeatherExposure: exposureSegments.Any(segment =>
                                        segment.Kind == CityWeatherExposureKind.Adverse),
                                    livingConditionsState: livingConditionsState,
                                    districtUtilityConditionsByDistrictId:
                                    districtUtilityConditionsByDistrictId,
                                    essentialsState: essentialsState,
                                    serviceQualityState: serviceQualityState,
                                    healthcarePressureProfile: healthcarePressureProfile,
                                    healthcareAutonomyPolicy: healthcareAutonomyPolicy,
                                    anchorSelectionPolicy: anchorSelectionPolicy,
                                    hospitalAnchors: hospitalAnchors,
                                    districtImpactPolicy: districtImpactPolicy,
                                    livingConditionsPressurePolicy: livingConditionsPressurePolicy,
                                    commuteRoutingService: commuteRoutingService,
                                    cancellationToken: ct);
                            pendingHealthRiskSnapshots = pendingHealthRiskSnapshots
                               .Select(snapshot => snapshot with
                                {
                                    ExternalHealthDelta = pendingHealthAdjustmentsByResidentId.TryGetValue(
                                        PersonId.From(snapshot.ResidentId),
                                        out int healthDelta)
                                        ? healthDelta
                                        : 0
                                })
                               .ToArray();
                        }

                        if (requiresDateProgression)
                            pendingEconomySettlement = await HouseholdCashflowSettlementStep.ApplyAsync(
                                cityId: cityId,
                                householdsById: householdsById,
                                residentsByHouseholdId: residentsByHouseholdId,
                                economicContextsByResidentId: workingSet.EconomicContextsByResidentId,
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
                                routineProfilesByResidentId: workingSet.RoutineProfilesByResidentId,
                                housingStatusesByHouseholdId: housingByHouseholdId,
                                previousDate: previousDate,
                                currentDate: toDate,
                                birthAutonomyPolicy: birthAutonomyPolicy,
                                populationBirthDomainService: populationBirthDomainService,
                                personWriteRepository: personWriteRepository,
                                householdWriteRepository: householdWriteRepository,
                                activityEntries: pendingActivityEntries,
                                residents: residents,
                                registeredResidents: registeredResidents,
                                occurredAtUtc: handledAtUtc,
                                cancellationToken: ct);

                            affectedPeopleCount += await CivilRegistryAutonomyStep.ApplyAsync(
                                cityId: cityId,
                                residentsById: personsById,
                                routineProfilesByResidentId: workingSet.RoutineProfilesByResidentId,
                                previousDate: previousDate,
                                currentDate: toDate,
                                householdWriteRepository: householdWriteRepository,
                                marriageDomainService: marriageDomainService,
                                civilRegistryAutonomyPolicy: civilRegistryAutonomyPolicy,
                                activityEntries: pendingActivityEntries,
                                occurredAtUtc: handledAtUtc,
                                cancellationToken: ct);

                            affectedPeopleCount += await HouseholdIndependenceAutonomyStep.ApplyAsync(
                                cityId: cityId,
                                residentsById: personsById,
                                routineProfilesByResidentId: workingSet.RoutineProfilesByResidentId,
                                previousDate: previousDate,
                                currentDate: toDate,
                                householdWriteRepository: householdWriteRepository,
                                householdIndependenceAutonomyPolicy: householdIndependenceAutonomyPolicy,
                                activityEntries: pendingActivityEntries,
                                occurredAtUtc: handledAtUtc,
                                cancellationToken: ct);

                            affectedPeopleCount += await HousingAutonomyStep.ApplyAsync(
                                cityId: cityId,
                                residentsById: personsById,
                                routineProfilesByResidentId: workingSet.RoutineProfilesByResidentId,
                                economicContextsByResidentId: workingSet.EconomicContextsByResidentId,
                                educationParticipation: educationParticipation,
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

                    if (registeredResidents.Count > 0 || residentLifecycleChanges.Count > 0)
                    {
                        PersonEntity[] residentFactChanges = registeredResidents
                           .Concat(residentLifecycleChanges)
                           .ToArray();

                        foreach (PopulationResidentFactsBatchV1 batch in PopulationResidentFactsBatchFactory.Build(
                                     simulationHostId: request.CityId,
                                     sourceRevision: request.TickId,
                                     residents: residentFactChanges,
                                     correlationId:
                                     $"population:{request.CityId:N}:tick:{request.TickId}:resident-facts",
                                     synchronizedAtUtc: updatedAtUtc))
                            await residentFactsOutboxWriter.AddResidentFactsBatchAsync(
                                batch: batch,
                                cancellationToken: ct);
                    }

                    if (registeredResidents.Count > 0)
                        foreach (PopulationResidentVitalStateBatchV1 batch in
                                 PopulationResidentVitalStateBatchFactory.Build(
                                     simulationHostId: request.CityId,
                                     sourceRevision: request.TickId,
                                     residents: registeredResidents,
                                     correlationId:
                                     $"population:{request.CityId:N}:tick:{request.TickId}:vital-state",
                                     observedAtUtc: updatedAtUtc))
                            await residentVitalStateOutboxWriter.AddResidentVitalStateBatchAsync(
                                batch: batch,
                                cancellationToken: ct);

                    foreach (PopulationResidentHealthRiskBatchV1 batch in
                             PopulationResidentHealthRiskBatchFactory.Build(
                                 simulationHostId: request.CityId,
                                 sourceRevision: request.TickId,
                                 previousDate: previousDate,
                                 currentDate: toDate,
                                 residents: pendingHealthRiskSnapshots,
                                 correlationId:
                                 $"population:{request.CityId:N}:tick:{request.TickId}:health-risk",
                                 observedAtUtc: updatedAtUtc))
                        await residentHealthRiskOutboxWriter.AddResidentHealthRiskBatchAsync(
                            batch: batch,
                            cancellationToken: ct);

                    if (pendingWeatherImpacts.Count > 0)
                        pendingWeatherImpactRepository.RemoveRange(pendingWeatherImpacts);

                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            if (personsSnapshot is not null && placementsSnapshot is not null)
                try
                {
                    await commuteTripSyncService.SyncAsync(
                        cityId: cityId.Value,
                        tickId: request.TickId,
                        currentSimTimeUtc: request.ToSimTimeUtc,
                        residents: personsSnapshot,
                        householdPlacements: placementsSnapshot,
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        exception: ex,
                        message: "Failed to sync population commute trips for cityId={CityId} at tickId={TickId}.",
                        request.CityId,
                        request.TickId);
                }

            return new AdvanceCityPopulationResult(
                Status: AdvanceCityPopulationStatus.Applied,
                AffectedPeopleCount: affectedPeopleCount);
        }
    }
}
