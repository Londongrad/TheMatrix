using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Weather;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
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
                            BuildEducationInstitutionPools(residents);
                        Dictionary<string, List<Job>> workplacePools = BuildWorkplacePools(residents);

                        foreach (PersonEntity person in residents)
                        {
                            ResidentLifecycleSnapshot beforeSnapshot = CreateResidentSnapshot(person);

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
                                CollectResidentProgressionActivity(
                                    cityId: cityId,
                                    currentDate: toDate,
                                    before: beforeSnapshot,
                                    resident: person,
                                    residentsById: personsById,
                                    activityEntries: pendingActivityEntries);
                            }
                        }

                        if (requiresDateProgression)
                            pendingEconomySettlement = await ApplyHouseholdCashflowSettlementAsync(
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
                            affectedPeopleCount += await ApplyBirthAutonomyAsync(
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
                                cancellationToken: ct);

                            affectedPeopleCount += await ApplyCivilRegistryAutonomyAsync(
                                cityId: cityId,
                                residentsById: personsById,
                                previousDate: previousDate,
                                currentDate: toDate,
                                householdWriteRepository: householdWriteRepository,
                                marriageDomainService: marriageDomainService,
                                civilRegistryAutonomyPolicy: civilRegistryAutonomyPolicy,
                                activityEntries: pendingActivityEntries,
                                cancellationToken: ct);

                            affectedPeopleCount += await ApplyHouseholdIndependenceAutonomyAsync(
                                cityId: cityId,
                                residentsById: personsById,
                                previousDate: previousDate,
                                currentDate: toDate,
                                householdWriteRepository: householdWriteRepository,
                                householdIndependenceAutonomyPolicy: householdIndependenceAutonomyPolicy,
                                activityEntries: pendingActivityEntries,
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
                                cancellationToken: ct);
                        }
                    }

                    DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;
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
                ApplyNeedsProgression(
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
                await ApplyTimeProgressionAsync(
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
                await ApplyHouseholdPressureProgressionAsync(
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
                ApplyLivingConditionsProgression(
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
                ApplyWeatherExposure(
                    person: person,
                    residentsById: residentsById,
                    currentDate: currentDate,
                    environment: environment,
                    exposureSegments: exposureSegments,
                    marriageDomainService: marriageDomainService,
                    weatherExposurePolicy: weatherExposurePolicy))
                changed = true;
            if (requiresDateProgression &&
                await ApplyIllnessProgressionAsync(
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

        private static bool ApplyNeedsProgression(
            PersonEntity person,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc,
            DateOnly currentDate,
            CityPopulationEnvironment? environment,
            MarriageDomainService marriageDomainService,
            PersonNeedsProgressionPolicy personNeedsProgressionPolicy)
        {
            int utcOffsetMinutes = environment?.UtcOffsetMinutes ?? 0;
            PersonNeedsProgressionEffect effect = personNeedsProgressionPolicy.Calculate(
                person: person,
                fromSimTimeUtc: fromSimTimeUtc,
                toSimTimeUtc: toSimTimeUtc,
                utcOffsetMinutes: utcOffsetMinutes);
            bool wasAlive = person.IsAlive;
            bool changed = person.ApplyNeedsProgression(
                effect: effect,
                currentDate: currentDate);
            if (wasAlive && !person.IsAlive)
                changed = ClassicCityWidowhoodSupport.TryRegisterWidowhood(
                              deceased: person,
                              residentsById: residentsById,
                              marriageDomainService: marriageDomainService) ||
                          changed;
            return changed;
        }

        private static async Task<bool> ApplyTimeProgressionAsync(
            CityId cityId,
            PersonEntity person,
            IReadOnlyDictionary<HouseholdId, HouseholdEntity> householdsById,
            IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> residentsByHouseholdId,
            DateOnly previousDate,
            DateOnly currentDate,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            IReadOnlyDictionary<HouseholdId, DistrictId?> districtByHouseholdId,
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId,
            IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState> employerStressByWorkplaceId,
            CityPopulationCostOfLivingState? costOfLivingState,
            CityPopulationServiceQualityState? serviceQualityState,
            CityEducationAutonomyPolicy educationAutonomyPolicy,
            CityEmploymentAutonomyPolicy employmentAutonomyPolicy,
            IDictionary<EducationLevel, List<CityEducationInstitutionBinding>> institutionPools,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> workplaceAnchors,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> schoolAnchors,
            IDictionary<string, List<Job>> workplacePools,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            CancellationToken cancellationToken)
        {
            bool changed = false;
            if (!person.IsAlive)
                return false;
            IReadOnlyCollection<PersonEntity> householdResidents = residentsByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out IReadOnlyCollection<PersonEntity>? resolvedResidents)
                ? resolvedResidents
                : [person];
            HousingStatus? housingStatus = housingByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out HousingStatus resolvedHousingStatus)
                ? resolvedHousingStatus
                : null;
            ResidentialBuildingId? residentialBuildingId = residentialBuildingByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out ResidentialBuildingId? resolvedResidentialBuildingId)
                ? resolvedResidentialBuildingId
                : null;
            if (!householdsById.TryGetValue(
                    key: person.HouseholdId,
                    value: out HouseholdEntity? household))
                return false;
            IReadOnlyList<CityAnchorId> preferredSchoolAnchorIds = await RankAnchorIdsByRouteAccessAsync(
                cityId: cityId,
                residentialBuildingId: residentialBuildingId,
                anchors: schoolAnchors,
                commuteRoutingService: commuteRoutingService,
                cancellationToken: cancellationToken);
            IReadOnlyList<CityAnchorId> preferredWorkplaceAnchorIds = await RankAnchorIdsByRouteAccessAsync(
                cityId: cityId,
                residentialBuildingId: residentialBuildingId,
                anchors: workplaceAnchors,
                commuteRoutingService: commuteRoutingService,
                cancellationToken: cancellationToken);
            if (educationAutonomyPolicy.Apply(
                    person: person,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    institutionPools: institutionPools,
                    preferredDistrictId: districtByHouseholdId.TryGetValue(
                        key: person.HouseholdId,
                        value: out DistrictId? schoolDistrictId)
                        ? schoolDistrictId
                        : null,
                    schoolAnchors: schoolAnchors,
                    preferredInstitutionAnchorIds: preferredSchoolAnchorIds,
                    serviceQualityState: serviceQualityState))
                changed = true;
            if (employmentAutonomyPolicy.Apply(
                    person: person,
                    household: household,
                    householdResidents: householdResidents,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    housingStatus: housingStatus,
                    preferredDistrictId: districtByHouseholdId.TryGetValue(
                        key: person.HouseholdId,
                        value: out DistrictId? preferredDistrictId)
                        ? preferredDistrictId
                        : null,
                    workplaceAnchors: workplaceAnchors,
                    workplacePools: workplacePools,
                    employerStressByWorkplaceId: employerStressByWorkplaceId,
                    preferredWorkplaceAnchorIds: preferredWorkplaceAnchorIds,
                    costOfLivingState: costOfLivingState))
                changed = true;
            if (person.GetAgeGroup(currentDate) != AgeGroup.Senior)
                return changed;
            if (person.Employment.Status is not (EmploymentStatus.Employed or EmploymentStatus.Student))
                return changed;
            person.Retire(currentDate);
            return true;
        }

        private static async Task<IReadOnlyList<CityAnchorId>> RankAnchorIdsByRouteAccessAsync(
            CityId cityId,
            ResidentialBuildingId? residentialBuildingId,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> anchors,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            CancellationToken cancellationToken)
        {
            if (!residentialBuildingId.HasValue || anchors.Count == 0)
                return [];

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

        private static bool ApplyLivingConditionsProgression(
            PersonEntity person,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            DateOnly previousDate,
            DateOnly currentDate,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            IReadOnlyDictionary<HouseholdId, DistrictId?> districtByHouseholdId,
            CityPopulationLivingConditionsState? livingConditionsState,
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> districtUtilityConditionsByDistrictId,
            CityPopulationEssentialsState? essentialsState,
            CityPopulationDistrictImpactPolicy districtImpactPolicy,
            CityPopulationLivingConditionsPressurePolicy livingConditionsPressurePolicy,
            MarriageDomainService marriageDomainService)
        {
            HousingStatus? housingStatus = housingByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out HousingStatus resolvedHousingStatus)
                ? resolvedHousingStatus
                : null;
            DistrictId? districtId = districtByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out DistrictId? resolvedDistrictId)
                ? resolvedDistrictId
                : null;
            CityPopulationLivingConditionsContext districtLivingConditions = districtImpactPolicy.ResolveLivingConditions(
                districtId: districtId,
                livingConditionsState: livingConditionsState,
                districtUtilityConditions: ClassicCityHousingOpportunityPlanner.ResolveDistrictUtilityConditions(
                    districtId: districtId,
                    districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId));
            CityPopulationEssentialsContext districtEssentials = districtImpactPolicy.ResolveEssentials(
                districtId: districtId,
                essentialsState: essentialsState);
            CityPopulationLivingConditionsPressureEffect effect = livingConditionsPressurePolicy.Calculate(
                person: person,
                previousDate: previousDate,
                currentDate: currentDate,
                housingStatus: housingStatus,
                livingConditions: districtLivingConditions,
                essentials: districtEssentials);

            if (!effect.HasAnyEffect)
                return false;

            bool wasAlive = person.IsAlive;
            int previousHealth = person.Health.Value;
            int previousEnergy = person.Energy.Value;
            int previousStress = person.Stress.Value;
            int previousHappiness = person.Happiness.Value;

            if (effect.HealthDelta != 0)
                person.ChangeHealth(
                    delta: effect.HealthDelta,
                    currentDate: currentDate);

            if (person.IsAlive)
            {
                if (effect.EnergyDelta != 0)
                    person.ChangeEnergy(effect.EnergyDelta);
                if (effect.StressDelta != 0)
                    person.ChangeStress(effect.StressDelta);
                if (effect.HappinessDelta != 0)
                    person.ChangeHappiness(effect.HappinessDelta);
            }

            bool changed = previousHealth != person.Health.Value ||
                           previousEnergy != person.Energy.Value ||
                           previousStress != person.Stress.Value ||
                           previousHappiness != person.Happiness.Value ||
                           wasAlive != person.IsAlive;

            if (wasAlive && !person.IsAlive)
                changed = ClassicCityWidowhoodSupport.TryRegisterWidowhood(
                              deceased: person,
                              residentsById: residentsById,
                              marriageDomainService: marriageDomainService) ||
                          changed;

            return changed;
        }

        private static async Task<CityEconomyDailySettlementSnapshot?> ApplyHouseholdCashflowSettlementAsync(
            CityId cityId,
            IReadOnlyDictionary<HouseholdId, HouseholdEntity> householdsById,
            IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> residentsByHouseholdId,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingIdByHouseholdId,
            DateOnly previousDate,
            DateOnly currentDate,
            CityHouseholdCashflowPolicy householdCashflowPolicy,
            CityPopulationCostOfLivingState? costOfLivingState,
            CityPopulationEssentialsState? essentialsState,
            CityPopulationLivingConditionsState? livingConditionsState,
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> districtUtilityConditionsByDistrictId,
            IReadOnlyDictionary<HouseholdId, DistrictId?> districtByHouseholdId,
            CityPopulationDistrictImpactPolicy districtImpactPolicy,
            CityPopulationParticipationPolicy participationPolicy,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            ICollection<ClassicCityHouseholdCashflowSettlementItemV1> cashflowItems,
            ICollection<ClassicCityWorkplacePayrollSettlementItemV1> workplacePayrollItems,
            CancellationToken cancellationToken)
        {
            int daysElapsed = Math.Max(
                val1: 0,
                val2: currentDate.DayNumber - previousDate.DayNumber);
            if (daysElapsed <= 0)
                return null;

            Money grossPayroll = Money.Zero;
            Money incomeTax = Money.Zero;
            Money netPayroll = Money.Zero;
            Money retailTurnover = Money.Zero;
            Money retailTax = Money.Zero;
            Money housingSpend = Money.Zero;
            int settledHouseholdCount = 0;
            int settledResidentCount = 0;

            foreach ((HouseholdId householdId, HouseholdEntity household) in householdsById)
            {
                if (!residentsByHouseholdId.TryGetValue(
                        key: householdId,
                        value: out IReadOnlyCollection<PersonEntity>? residents) ||
                    residents.Count == 0)
                    continue;

                HousingStatus? housingStatus = housingByHouseholdId.TryGetValue(
                    key: householdId,
                    value: out HousingStatus resolvedHousingStatus)
                    ? resolvedHousingStatus
                    : null;
                CityHouseholdCashflowProfile cashflow = householdCashflowPolicy.Build(
                    householdResidents: residents,
                    housingStatus: housingStatus,
                    currentDate: currentDate,
                    costOfLivingState: costOfLivingState);

                Money retailTurnoverForPeriod = cashflow.RetailTurnover.Multiply(daysElapsed);
                retailTurnover = retailTurnover.Add(retailTurnoverForPeriod);
                Money retailTaxForPeriod = retailTurnoverForPeriod.Multiply(ResolveRetailTaxRate());
                retailTax = retailTax.Add(retailTaxForPeriod);
                housingSpend = housingSpend.Add(cashflow.HousingExpense.Multiply(daysElapsed));
                settledHouseholdCount++;
                settledResidentCount += cashflow.ResidentCount;

                Money supportGrossIncomeForPeriod = Money.Zero;
                Money supportIncomeTaxForPeriod = Money.Zero;
                Money supportNetIncomeForPeriod = Money.Zero;
                Money actualHouseholdNetIncomeForPeriod = Money.Zero;

                foreach (PersonEntity resident in residents)
                {
                    decimal incomeMultiplier = 1m;
                    DistrictId? districtId = districtByHouseholdId.TryGetValue(
                        key: resident.HouseholdId,
                        value: out DistrictId? resolvedDistrictId)
                        ? resolvedDistrictId
                        : null;
                    CityPopulationLivingConditionsContext districtLivingConditions = districtImpactPolicy.ResolveLivingConditions(
                        districtId: districtId,
                        livingConditionsState: livingConditionsState,
                        districtUtilityConditions: ClassicCityHousingOpportunityPlanner.ResolveDistrictUtilityConditions(
                            districtId: districtId,
                            districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId));
                    CityPopulationEssentialsContext districtEssentials = districtImpactPolicy.ResolveEssentials(
                        districtId: districtId,
                        essentialsState: essentialsState);
                    if (resident.Employment.Status == EmploymentStatus.Employed)
                    {
                        HousingStatus? residentHousingStatus = housingStatus;
                        ResidentialBuildingId? residentialBuildingId =
                            residentialBuildingIdByHouseholdId.TryGetValue(
                                key: resident.HouseholdId,
                                value: out ResidentialBuildingId? resolvedResidentialBuildingId)
                                ? resolvedResidentialBuildingId
                                : null;
                        CityPopulationCommuteContext employmentCommute =
                            await commuteRoutingService.ResolveEmploymentCommuteAsync(
                                cityId: cityId.Value,
                                residentialBuildingId: residentialBuildingId,
                                resident: resident,
                                cancellationToken: cancellationToken);
                        CityPopulationParticipationProfile employmentProfile =
                            participationPolicy.ResolveEmploymentProfile(
                                person: resident,
                                currentDate: currentDate,
                                housingStatus: residentHousingStatus,
                                livingConditions: districtLivingConditions,
                                essentials: districtEssentials,
                                commute: employmentCommute);
                        incomeMultiplier = employmentProfile.PayrollMultiplier;
                    }

                    CityResidentIncomeSettlementProfile residentIncome = householdCashflowPolicy.BuildResidentIncome(
                        resident: resident,
                        currentDate: currentDate,
                        costOfLivingState: costOfLivingState,
                        incomeMultiplier: incomeMultiplier);
                    Money residentGrossIncomeForPeriod = residentIncome.GrossIncome.Multiply(daysElapsed);
                    Money residentTaxForPeriod = residentIncome.TaxWithheld.Multiply(daysElapsed);
                    Money residentNetIncomeForPeriod = residentIncome.NetIncome.Multiply(daysElapsed);
                    actualHouseholdNetIncomeForPeriod = actualHouseholdNetIncomeForPeriod.Add(residentNetIncomeForPeriod);

                    if (resident.Employment.Status == EmploymentStatus.Employed &&
                        resident.Employment.Job is
                            { } job &&
                        residentNetIncomeForPeriod.IsPositive)
                    {
                        workplacePayrollItems.Add(
                            new ClassicCityWorkplacePayrollSettlementItemV1(
                                HouseholdId: householdId.Value,
                                HouseholdExternalReferenceCode: ClassicCityEconomySettlementBatchFactory
                                   .BuildHouseholdExternalReferenceCode(householdId),
                                WorkplaceId: job.WorkplaceId.Value,
                                WorkplaceExternalReferenceCode: ClassicCityWorkplaceBusinessSyncBatchFactory
                                   .BuildExternalReferenceCode(job.WorkplaceId),
                                JobTitle: job.Title,
                                GrossPayrollAmount: residentGrossIncomeForPeriod.Amount,
                                IncomeTaxAmount: residentTaxForPeriod.Amount,
                                NetPayrollAmount: residentNetIncomeForPeriod.Amount));
                        continue;
                    }

                    supportGrossIncomeForPeriod = supportGrossIncomeForPeriod.Add(residentGrossIncomeForPeriod);
                    supportIncomeTaxForPeriod = supportIncomeTaxForPeriod.Add(residentTaxForPeriod);
                    supportNetIncomeForPeriod = supportNetIncomeForPeriod.Add(residentNetIncomeForPeriod);
                }

                if (supportNetIncomeForPeriod.IsPositive || retailTurnoverForPeriod.IsPositive)
                    cashflowItems.Add(
                        new ClassicCityHouseholdCashflowSettlementItemV1(
                            HouseholdId: householdId.Value,
                            ExternalReferenceCode: ClassicCityEconomySettlementBatchFactory
                               .BuildHouseholdExternalReferenceCode(householdId),
                            GrossPayrollAmount: supportGrossIncomeForPeriod.Amount,
                            IncomeTaxAmount: supportIncomeTaxForPeriod.Amount,
                            NetPayrollAmount: supportNetIncomeForPeriod.Amount,
                            RetailTurnoverAmount: retailTurnoverForPeriod.Amount,
                            RetailTaxAmount: retailTaxForPeriod.Amount,
                            RetailStoreSpendAmount: cashflow.RetailStoreSpend.Multiply(daysElapsed).Amount,
                            ServiceSpendAmount: cashflow.ServiceSpend.Multiply(daysElapsed).Amount,
                            MunicipalSpendAmount: cashflow.MunicipalSpend.Multiply(daysElapsed).Amount));

                grossPayroll = grossPayroll.Add(supportGrossIncomeForPeriod);
                incomeTax = incomeTax.Add(supportIncomeTaxForPeriod);
                netPayroll = netPayroll.Add(supportNetIncomeForPeriod);

                household.ApplyDailyCashflow(
                    takeHomeIncome: Money.FromDecimal(actualHouseholdNetIncomeForPeriod.Amount / daysElapsed),
                    expenses: cashflow.DailyExpenses,
                    daysElapsed: daysElapsed);
            }

            return settledHouseholdCount == 0
                ? null
                : new CityEconomyDailySettlementSnapshot(
                    CurrentDate: currentDate,
                    SettledDays: daysElapsed,
                    HouseholdCount: settledHouseholdCount,
                    ResidentCount: settledResidentCount,
                    GrossPayroll: grossPayroll,
                    IncomeTax: incomeTax,
                    NetPayroll: netPayroll,
                    RetailTurnover: retailTurnover,
                    RetailTax: retailTax,
                    HousingSpend: housingSpend);
        }

        private static decimal ResolveRetailTaxRate()
        {
            return 0.08m;
        }

        private static bool ApplyWeatherExposure(
            PersonEntity person,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            DateOnly currentDate,
            CityPopulationEnvironment? environment,
            IReadOnlyCollection<CityWeatherExposureSegment> exposureSegments,
            MarriageDomainService marriageDomainService,
            CityPopulationWeatherExposurePolicy weatherExposurePolicy)
        {
            if (exposureSegments.Count == 0)
                return false;
            int totalHealthDelta = 0;
            int totalHappinessDelta = 0;
            foreach (CityWeatherExposureSegment segment in exposureSegments)
            {
                PersonWeatherImpact impact = weatherExposurePolicy.Calculate(
                    person: person,
                    currentDate: currentDate,
                    segment: segment,
                    environment: environment);
                totalHealthDelta += impact.HealthDelta;
                totalHappinessDelta += impact.HappinessDelta;
            }

            if (totalHealthDelta == 0 && totalHappinessDelta == 0)
                return false;
            bool changed = false;
            if (totalHealthDelta != 0)
            {
                int previousHealth = person.Health.Value;
                bool wasAlive = person.IsAlive;
                person.ChangeHealth(
                    delta: totalHealthDelta,
                    currentDate: currentDate);
                changed = previousHealth != person.Health.Value || wasAlive != person.IsAlive;
                if (wasAlive && !person.IsAlive)
                    changed = ClassicCityWidowhoodSupport.TryRegisterWidowhood(
                                  deceased: person,
                                  residentsById: residentsById,
                                  marriageDomainService: marriageDomainService) ||
                              changed;
            }

            if (totalHappinessDelta != 0 && person.IsAlive)
            {
                int previousHappiness = person.Happiness.Value;
                person.ChangeHappiness(totalHappinessDelta);
                changed = changed || previousHappiness != person.Happiness.Value;
            }

            return changed;
        }

        private static async Task<bool> ApplyHouseholdPressureProgressionAsync(
            CityId cityId,
            PersonEntity person,
            IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> residentsByHouseholdId,
            DateOnly previousDate,
            DateOnly currentDate,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId,
            IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState> financialStressByHouseholdId,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            CancellationToken cancellationToken,
            CityHouseholdPressurePolicy householdPressurePolicy)
        {
            if (!residentsByHouseholdId.TryGetValue(
                    key: person.HouseholdId,
                    value: out IReadOnlyCollection<PersonEntity>? householdResidents))
                return false;

            HousingStatus? housingStatus = housingByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out HousingStatus resolvedHousingStatus)
                ? resolvedHousingStatus
                : null;
            financialStressByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out CityPopulationHouseholdFinancialStressState? financialStressState);
            CityHouseholdCommutePressureProfile? commutePressureProfile = await BuildHouseholdCommutePressureProfileAsync(
                cityId: cityId,
                householdId: person.HouseholdId,
                householdResidents: householdResidents,
                residentialBuildingByHouseholdId: residentialBuildingByHouseholdId,
                commuteRoutingService: commuteRoutingService,
                cancellationToken: cancellationToken);

            return householdPressurePolicy.Apply(
                resident: person,
                householdResidents: householdResidents,
                housingStatus: housingStatus,
                financialStressState: financialStressState,
                commutePressureProfile: commutePressureProfile,
                previousDate: previousDate,
                currentDate: currentDate);
        }

        private static async Task<CityHouseholdCommutePressureProfile?> BuildHouseholdCommutePressureProfileAsync(
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

        private static async Task<bool> ApplyIllnessProgressionAsync(
            PersonEntity person,
            CityId cityId,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            DateOnly previousDate,
            DateOnly currentDate,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            IReadOnlyDictionary<HouseholdId, DistrictId?> districtByHouseholdId,
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId,
            IReadOnlyCollection<CityWeatherExposureSegment> exposureSegments,
            CityPopulationLivingConditionsState? livingConditionsState,
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> districtUtilityConditionsByDistrictId,
            CityPopulationEssentialsState? essentialsState,
            CityPopulationServiceQualityState? serviceQualityState,
            CityPopulationHealthcarePressureProfile healthcarePressureProfile,
            MarriageDomainService marriageDomainService,
            CityIllnessAutonomyPolicy illnessAutonomyPolicy,
            CityHealthcareAutonomyPolicy healthcareAutonomyPolicy,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> hospitalAnchors,
            CityPopulationDistrictImpactPolicy districtImpactPolicy,
            CityPopulationLivingConditionsPressurePolicy livingConditionsPressurePolicy,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            CancellationToken cancellationToken)
        {
            HousingStatus? housingStatus = housingByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out HousingStatus resolvedHousingStatus)
                ? resolvedHousingStatus
                : null;
            DistrictId? districtId = districtByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out DistrictId? resolvedDistrictId)
                ? resolvedDistrictId
                : null;
            bool hadAdverseExposure = exposureSegments.Any(x => x.Kind == CityWeatherExposureKind.Adverse);
            bool wasAlive = person.IsAlive;
            IReadOnlyCollection<PersonEntity> householdResidents = residentsById.Values
               .Where(x => x.HouseholdId == person.HouseholdId)
               .ToArray();
            CityDistrictUtilityConditionsSnapshot? districtUtilityConditions =
                ClassicCityHousingOpportunityPlanner.ResolveDistrictUtilityConditions(
                    districtId: districtId,
                    districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId);
            CityPopulationLivingConditionsContext districtLivingConditions = districtImpactPolicy.ResolveLivingConditions(
                districtId: districtId,
                livingConditionsState: livingConditionsState,
                districtUtilityConditions: districtUtilityConditions);
            CityPopulationEssentialsContext districtEssentials = districtImpactPolicy.ResolveEssentials(
                districtId: districtId,
                essentialsState: essentialsState);
            CityPopulationAnchorCatalogItem? primaryCareAnchor = anchorSelectionPolicy.SelectHospitalAnchor(
                anchors: hospitalAnchors,
                preferredDistrictId: districtId,
                stableKey: person.Id.Value);
            ResidentialBuildingId? residentialBuildingId = residentialBuildingByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out ResidentialBuildingId? resolvedResidentialBuildingId)
                ? resolvedResidentialBuildingId
                : null;
            CityPopulationCommuteContext healthcareCommute = await commuteRoutingService.ResolveHealthcareCommuteAsync(
                cityId: cityId.Value,
                residentialBuildingId: residentialBuildingId,
                healthcareAnchorId: primaryCareAnchor?.CityAnchorId,
                cancellationToken: cancellationToken);
            double healthcareSupportStrength = healthcareAutonomyPolicy.ResolveSupportStrength(
                resident: person,
                householdResidents: householdResidents,
                housingStatus: housingStatus,
                currentDate: currentDate,
                hasPrimaryCareAccess: primaryCareAnchor is not null,
                hasDistrictPrimaryCareAccess: primaryCareAnchor?.DistrictId == districtId,
                districtUtilityConditions: districtUtilityConditions,
                healthcareCommute: healthcareCommute,
                serviceQualityState: serviceQualityState,
                healthcarePressureProfile: healthcarePressureProfile) *
                  livingConditionsPressurePolicy.ResolveMedicineAccessStrength(
                      livingConditions: districtLivingConditions,
                      essentials: districtEssentials);
            double publicHealthRiskStrength = livingConditionsPressurePolicy.ResolvePublicHealthRiskStrength(
                  livingConditions: districtLivingConditions,
                  essentials: districtEssentials);

            bool changed = illnessAutonomyPolicy.Apply(
                person: person,
                householdResidents: householdResidents,
                previousDate: previousDate,
                currentDate: currentDate,
                housingStatus: housingStatus,
                hadAdverseWeatherExposure: hadAdverseExposure,
                healthcareSupportStrength: healthcareSupportStrength,
                publicHealthRiskStrength: publicHealthRiskStrength);

            if (wasAlive && !person.IsAlive)
                changed = ClassicCityWidowhoodSupport.TryRegisterWidowhood(
                              deceased: person,
                              residentsById: residentsById,
                              marriageDomainService: marriageDomainService) ||
                          changed;

            return changed;
        }

        private static async Task<int> ApplyBirthAutonomyAsync(
            CityId cityId,
            IDictionary<PersonId, PersonEntity> residentsById,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingStatusesByHouseholdId,
            DateOnly previousDate,
            DateOnly currentDate,
            CityBirthAutonomyPolicy birthAutonomyPolicy,
            PopulationBirthDomainService populationBirthDomainService,
            IPersonWriteRepository personWriteRepository,
            IHouseholdWriteRepository householdWriteRepository,
            ICollection<CityPopulationActivityWriteModel> activityEntries,
            ICollection<PersonEntity> residents,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityBirthAutonomyDecision> decisions = birthAutonomyPolicy.Plan(
                residents: residentsById.Values.ToArray(),
                housingStatuses: housingStatusesByHouseholdId,
                previousDate: previousDate,
                currentDate: currentDate);

            if (decisions.Count == 0)
                return 0;

            int affectedResidents = 0;

            foreach (CityBirthAutonomyDecision decision in decisions)
            {
                if (!residentsById.TryGetValue(
                        key: decision.MotherId,
                        value: out PersonEntity? mother))
                    continue;

                PersonEntity? father = null;
                if (decision.FatherId is not null &&
                    !residentsById.TryGetValue(
                        key: decision.FatherId.Value,
                        value: out father))
                    continue;

                HouseholdEntity? household = await householdWriteRepository.FindByIdAsync(
                    householdId: mother.HouseholdId,
                    cancellationToken: cancellationToken);

                if (household is null)
                    continue;

                PersonEntity newborn = populationBirthDomainService.RegisterBirth(
                    mother: mother,
                    father: father,
                    household: household,
                    newborn: decision.Newborn,
                    currentDate: currentDate);

                await personWriteRepository.AddAsync(
                    person: newborn,
                    cancellationToken: cancellationToken);
                await householdWriteRepository.UpdateAsync(
                    household: household,
                    cancellationToken: cancellationToken);

                residents.Add(newborn);
                residentsById[newborn.Id] = newborn;
                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentBorn(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: newborn,
                        mother: mother,
                        father: father,
                        source: CityPopulationActivitySource.Autonomy));
                affectedResidents++;
            }

            return affectedResidents;
        }

        private static async Task<int> ApplyHouseholdIndependenceAutonomyAsync(
            CityId cityId,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            DateOnly previousDate,
            DateOnly currentDate,
            IHouseholdWriteRepository householdWriteRepository,
            CityHouseholdIndependenceAutonomyPolicy householdIndependenceAutonomyPolicy,
            ICollection<CityPopulationActivityWriteModel> activityEntries,
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
                        cancellationToken: cancellationToken))
                    continue;

                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentFormedIndependentHousehold(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        source: CityPopulationActivitySource.Autonomy));
                affectedResidents++;
            }

            return affectedResidents;
        }

        private static async Task<int> ApplyCivilRegistryAutonomyAsync(
            CityId cityId,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            DateOnly previousDate,
            DateOnly currentDate,
            IHouseholdWriteRepository householdWriteRepository,
            MarriageDomainService marriageDomainService,
            CityCivilRegistryAutonomyPolicy civilRegistryAutonomyPolicy,
            ICollection<CityPopulationActivityWriteModel> activityEntries,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityCivilRegistryAutonomyDecision> decisions = civilRegistryAutonomyPolicy.Plan(
                residents: residentsById.Values.ToArray(),
                previousDate: previousDate,
                currentDate: currentDate);
            if (decisions.Count == 0)
                return 0;
            int affectedResidents = 0;
            foreach (CityCivilRegistryAutonomyDecision decision in decisions)
            {
                if (!residentsById.TryGetValue(
                        key: decision.FirstResidentId,
                        value: out PersonEntity? firstResident) ||
                    !residentsById.TryGetValue(
                        key: decision.SecondResidentId,
                        value: out PersonEntity? secondResident))
                    continue;
                switch (decision.Type)
                {
                    case CityCivilRegistryAutonomyDecisionType.Marriage:
                        marriageDomainService.RegisterMarriage(
                            person: firstResident,
                            spouse: secondResident,
                            currentDate: currentDate);
                        await ClassicCityCivilRegistryHouseholdSupport.MergeSpousesIntoSharedHouseholdAsync(
                            cityId: cityId,
                            firstResident: firstResident,
                            secondResident: secondResident,
                            householdWriteRepository: householdWriteRepository,
                            cancellationToken: cancellationToken);
                        activityEntries.Add(
                            ClassicCityActivityFactory.ResidentsMarried(
                                cityId: cityId.Value,
                                currentDate: currentDate,
                                firstResident: firstResident,
                                secondResident: secondResident,
                                source: CityPopulationActivitySource.Autonomy));
                        affectedResidents += 2;
                        break;
                    case CityCivilRegistryAutonomyDecisionType.Divorce:
                        if (firstResident.SpouseId != secondResident.Id || secondResident.SpouseId != firstResident.Id)
                            continue;
                        marriageDomainService.RegisterDivorce(
                            person: firstResident,
                            spouse: secondResident,
                            currentDate: currentDate);
                        await ClassicCityCivilRegistryHouseholdSupport.SeparateDivorcedSpousesAsync(
                            cityId: cityId,
                            firstResident: firstResident,
                            secondResident: secondResident,
                            householdWriteRepository: householdWriteRepository,
                            cancellationToken: cancellationToken);
                        activityEntries.Add(
                            ClassicCityActivityFactory.ResidentsDivorced(
                                cityId: cityId.Value,
                                currentDate: currentDate,
                                firstResident: firstResident,
                                secondResident: secondResident,
                                source: CityPopulationActivitySource.Autonomy));
                        affectedResidents += 2;
                        break;
                }
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
                    await BuildHouseholdCommutePressureProfileAsync(
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
                                source: CityPopulationActivitySource.Autonomy));
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
                                source: CityPopulationActivitySource.Autonomy));
                        affectedResidents += householdResidents.Count;
                        break;
                }
            }

            return affectedResidents;
        }

        private static Dictionary<EducationLevel, List<CityEducationInstitutionBinding>> BuildEducationInstitutionPools(
            IEnumerable<PersonEntity> persons)
        {
            var pools = new Dictionary<EducationLevel, List<CityEducationInstitutionBinding>>();
            foreach (PersonEntity person in persons)
            {
                if (person.Education.CurrentInstitutionId is not
                    { } institutionId)
                    continue;
                EducationLevel level = person.Education.Level;
                if (!pools.TryGetValue(
                        key: level,
                        value: out List<CityEducationInstitutionBinding>? levelPool))
                {
                    levelPool = [];
                    pools[level] = levelPool;
                }

                if (!levelPool.Any(x => x.InstitutionId == institutionId))
                    levelPool.Add(
                        new CityEducationInstitutionBinding(
                            InstitutionId: institutionId,
                            InstitutionAnchorId: person.Education.CurrentInstitutionAnchorId));
            }

            return pools;
        }

        private static Dictionary<string, List<Job>> BuildWorkplacePools(IEnumerable<PersonEntity> persons)
        {
            var pools = new Dictionary<string, List<Job>>(StringComparer.OrdinalIgnoreCase);
            foreach (PersonEntity person in persons)
            {
                if (person.Employment.Status != EmploymentStatus.Employed ||
                    person.Employment.Job is not
                        { } job)
                    continue;
                if (!pools.TryGetValue(
                        key: job.Title,
                        value: out List<Job>? titlePool))
                {
                    titlePool = [];
                    pools[job.Title] = titlePool;
                }

                if (!titlePool.Any(x => x.WorkplaceId == job.WorkplaceId))
                    titlePool.Add(job);
            }

            return pools;
        }

        private static ResidentLifecycleSnapshot CreateResidentSnapshot(PersonEntity person)
        {
            return new ResidentLifecycleSnapshot(
                IsAlive: person.IsAlive,
                MaritalStatus: person.MaritalStatus,
                SpouseId: person.SpouseId,
                EmploymentStatus: person.Employment.Status,
                JobTitle: person.Employment.Job?.Title,
                EducationLevel: person.EducationLevel,
                IllnessKind: person.CurrentIllnessKind?.ToString(),
                IllnessSeverity: person.CurrentIllnessSeverity?.ToString());
        }

        private static void CollectResidentProgressionActivity(
            CityId cityId,
            DateOnly currentDate,
            ResidentLifecycleSnapshot before,
            PersonEntity resident,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            ICollection<CityPopulationActivityWriteModel> activityEntries)
        {
            if (before.IsAlive && !resident.IsAlive)
                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentDied(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        source: CityPopulationActivitySource.Autonomy));

            if (before.MaritalStatus != MaritalStatus.Widowed && resident.MaritalStatus == MaritalStatus.Widowed)
            {
                string deceasedName = before.SpouseId is not null &&
                                      residentsById.TryGetValue(
                                          key: before.SpouseId.Value,
                                          value: out PersonEntity? spouse)
                    ? spouse.Name.ToString()
                    : "their spouse";

                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentBecameWidowed(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        deceasedName: deceasedName,
                        source: CityPopulationActivitySource.Autonomy));
            }

            if (before.EducationLevel != resident.EducationLevel && resident.EducationLevel > before.EducationLevel)
                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentGraduated(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        source: CityPopulationActivitySource.Autonomy));

            if (before.IllnessKind is null && resident.CurrentIllnessKind is not null)
                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentBecameIll(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        source: CityPopulationActivitySource.Autonomy));
            else
                if (before.IllnessKind is not null && resident.CurrentIllnessKind is null)
                    activityEntries.Add(
                        ClassicCityActivityFactory.ResidentRecoveredFromIllness(
                            cityId: cityId.Value,
                            currentDate: currentDate,
                            resident: resident,
                            previousIllnessKind: before.IllnessKind,
                            source: CityPopulationActivitySource.Autonomy));

            if (before.EmploymentStatus != EmploymentStatus.Student &&
                resident.Employment.Status == EmploymentStatus.Student)
                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentEnrolled(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        source: CityPopulationActivitySource.Autonomy));
            else
                if (before.EmploymentStatus == EmploymentStatus.Student &&
                    resident.Employment.Status != EmploymentStatus.Student)
                    activityEntries.Add(
                        ClassicCityActivityFactory.ResidentWithdrewFromStudy(
                            cityId: cityId.Value,
                            currentDate: currentDate,
                            resident: resident,
                            source: CityPopulationActivitySource.Autonomy));

            if (before.EmploymentStatus != EmploymentStatus.Employed &&
                resident.Employment.Status == EmploymentStatus.Employed)
                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentHired(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        source: CityPopulationActivitySource.Autonomy));
            else
                if (before.EmploymentStatus == EmploymentStatus.Employed &&
                    resident.Employment.Status == EmploymentStatus.Unemployed)
                    activityEntries.Add(
                        ClassicCityActivityFactory.ResidentFired(
                            cityId: cityId.Value,
                            currentDate: currentDate,
                            resident: resident,
                            previousJobTitle: before.JobTitle,
                            source: CityPopulationActivitySource.Autonomy));
                else
                    if (before.EmploymentStatus != EmploymentStatus.Retired &&
                        resident.Employment.Status == EmploymentStatus.Retired)
                        activityEntries.Add(
                            ClassicCityActivityFactory.ResidentRetired(
                                cityId: cityId.Value,
                                currentDate: currentDate,
                                resident: resident,
                                source: CityPopulationActivitySource.Autonomy));
        }

        private sealed record ResidentLifecycleSnapshot(
            bool IsAlive,
            MaritalStatus MaritalStatus,
            PersonId? SpouseId,
            EmploymentStatus EmploymentStatus,
            string? JobTitle,
            EducationLevel EducationLevel,
            string? IllnessKind,
            string? IllnessSeverity);
    }
}
