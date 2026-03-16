using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
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
using PersonId = Matrix.Population.Domain.ValueObjects.PersonId;
using HouseholdId = Matrix.Population.Domain.ValueObjects.HouseholdId;
using DistrictId = Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects.DistrictId;
using ResidentialBuildingId = Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects.ResidentialBuildingId;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class AdvanceCityPopulationCommandHandler(
        ICityPopulationPersonReadRepository personReadRepository,
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        ICityPopulationEnvironmentRepository cityPopulationEnvironmentRepository,
        ICityPopulationHouseholdFinancialStressStateRepository householdFinancialStressStateRepository,
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
        CityIllnessAutonomyPolicy illnessAutonomyPolicy,
        PersonNeedsProgressionPolicy personNeedsProgressionPolicy,
        CityPopulationWeatherExposurePolicy weatherExposurePolicy,
        ILogger<AdvanceCityPopulationCommandHandler> logger,
        IUnitOfWork unitOfWork)
        : IRequestHandler<AdvanceCityPopulationCommand, AdvanceCityPopulationResult>
    {
        private const int EconomyHouseholdCashflowBatchSize = 500;
        private const int EconomyWorkplaceSyncBatchSize = 500;
        private const int EconomyWorkplacePayrollBatchSize = 500;

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
            bool shouldAdvanceWeatherExposureCheckpoint = ShouldAdvanceWeatherExposureCheckpoint(
                weatherExposureState: weatherExposureState,
                fromSimTimeUtc: request.FromSimTimeUtc,
                toSimTimeUtc: request.ToSimTimeUtc);
            List<CityWeatherExposureSegment> exposureSegments =
                shouldAdvanceWeatherExposureCheckpoint && weatherExposureState is not null
                    ? BuildExposureSegments(
                        weatherExposureState: weatherExposureState,
                        fromSimTimeUtc: request.FromSimTimeUtc,
                        toSimTimeUtc: request.ToSimTimeUtc)
                    : [];
            bool requiresWeatherExposure = exposureSegments.Count > 0;
            IReadOnlyCollection<PersonEntity>? personsSnapshot = null;
            IReadOnlyCollection<HouseholdEntity>? householdsSnapshot = null;
            List<CityPopulationActivityWriteModel> pendingActivityEntries = [];
            CityEconomyDailySettlementSnapshot? pendingEconomySettlement = null;
            List<ClassicCityHouseholdCashflowSettlementItemV1> pendingHouseholdCashflowItems = [];
            List<ClassicCityWorkplacePayrollSettlementItemV1> pendingWorkplacePayrollItems = [];

            if ((requiresDateProgression || requiresNeedsProgression || requiresWeatherExposure) && environment is null)
                logger.LogWarning(
                    message:
                    "Advancing city population without synced environment for cityId={CityId}. Climate adaptation will be neutral and needs progression will use UTC fallback.",
                    request.CityId);

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
                        IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId =
                            await personReadRepository.ListHousingStatusesByHouseholdAsync(
                                cityId: cityId,
                                cancellationToken: ct);
                        IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>
                            financialStressByHouseholdId =
                                (await householdFinancialStressStateRepository.ListByCityAsync(
                                    cityId: cityId,
                                    cancellationToken: ct))
                               .ToDictionary(x => x.HouseholdId);
                        var householdsById = (await householdWriteRepository.ListByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct)).ToDictionary(
                            keySelector: x => x.Id,
                            elementSelector: x => x);
                        Dictionary<EducationLevel, List<EducationInstitutionId>> institutionPools =
                            BuildEducationInstitutionPools(residents);
                        Dictionary<string, List<WorkplaceId>> workplacePools = BuildWorkplacePools(residents);

                        foreach (PersonEntity person in residents)
                        {
                            ResidentLifecycleSnapshot beforeSnapshot = CreateResidentSnapshot(person);

                            if (ApplyProgressionNeedsExposureAndIllness(
                                    person: person,
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
                                    financialStressByHouseholdId: financialStressByHouseholdId,
                                    marriageDomainService: marriageDomainService,
                                    educationAutonomyPolicy: educationAutonomyPolicy,
                                    employmentAutonomyPolicy: employmentAutonomyPolicy,
                                    householdPressurePolicy: householdPressurePolicy,
                                    illnessAutonomyPolicy: illnessAutonomyPolicy,
                                    healthcareAutonomyPolicy: healthcareAutonomyPolicy,
                                    institutionPools: institutionPools,
                                    workplacePools: workplacePools,
                                    personNeedsProgressionPolicy: personNeedsProgressionPolicy,
                                    weatherExposurePolicy: weatherExposurePolicy))
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
                            pendingEconomySettlement = ApplyHouseholdCashflowSettlement(
                                householdsById: householdsById,
                                residentsByHouseholdId: residentsByHouseholdId,
                                housingByHouseholdId: housingByHouseholdId,
                                previousDate: previousDate,
                                currentDate: toDate,
                                householdCashflowPolicy: householdCashflowPolicy,
                                cashflowItems: pendingHouseholdCashflowItems,
                                workplacePayrollItems: pendingWorkplacePayrollItems);

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
                                housingAutonomyPolicy: housingAutonomyPolicy,
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
                        IReadOnlyCollection<ClassicCityHouseholdPlacement> placementsSnapshot =
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
                                     batchSize: EconomyHouseholdCashflowBatchSize))
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
                             BuildHouseholdCashflowSettlementBatches(
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
                             BuildWorkplacePayrollSettlementBatches(
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

            return new AdvanceCityPopulationResult(
                Status: AdvanceCityPopulationStatus.Applied,
                AffectedPeopleCount: affectedPeopleCount);
        }

        private static bool ApplyProgressionNeedsExposureAndIllness(
            PersonEntity person,
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
            IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState> financialStressByHouseholdId,
            MarriageDomainService marriageDomainService,
            CityEducationAutonomyPolicy educationAutonomyPolicy,
            CityEmploymentAutonomyPolicy employmentAutonomyPolicy,
            CityHouseholdPressurePolicy householdPressurePolicy,
            CityIllnessAutonomyPolicy illnessAutonomyPolicy,
            CityHealthcareAutonomyPolicy healthcareAutonomyPolicy,
            IDictionary<EducationLevel, List<EducationInstitutionId>> institutionPools,
            IDictionary<string, List<WorkplaceId>> workplacePools,
            PersonNeedsProgressionPolicy personNeedsProgressionPolicy,
            CityPopulationWeatherExposurePolicy weatherExposurePolicy)
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
                ApplyTimeProgression(
                    person: person,
                    householdsById: householdsById,
                    residentsByHouseholdId: residentsByHouseholdId,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    housingByHouseholdId: housingByHouseholdId,
                    educationAutonomyPolicy: educationAutonomyPolicy,
                    employmentAutonomyPolicy: employmentAutonomyPolicy,
                    institutionPools: institutionPools,
                    workplacePools: workplacePools))
                changed = true;
            if (requiresDateProgression &&
                ApplyHouseholdPressureProgression(
                    person: person,
                    residentsByHouseholdId: residentsByHouseholdId,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    housingByHouseholdId: housingByHouseholdId,
                    financialStressByHouseholdId: financialStressByHouseholdId,
                    householdPressurePolicy: householdPressurePolicy))
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
                ApplyIllnessProgression(
                    person: person,
                    residentsById: residentsById,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    housingByHouseholdId: housingByHouseholdId,
                    exposureSegments: exposureSegments,
                    marriageDomainService: marriageDomainService,
                    illnessAutonomyPolicy: illnessAutonomyPolicy,
                    healthcareAutonomyPolicy: healthcareAutonomyPolicy))
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

        private static bool ApplyTimeProgression(
            PersonEntity person,
            IReadOnlyDictionary<HouseholdId, HouseholdEntity> householdsById,
            IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> residentsByHouseholdId,
            DateOnly previousDate,
            DateOnly currentDate,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            CityEducationAutonomyPolicy educationAutonomyPolicy,
            CityEmploymentAutonomyPolicy employmentAutonomyPolicy,
            IDictionary<EducationLevel, List<EducationInstitutionId>> institutionPools,
            IDictionary<string, List<WorkplaceId>> workplacePools)
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
            if (!householdsById.TryGetValue(
                    key: person.HouseholdId,
                    value: out HouseholdEntity? household))
                return false;
            if (educationAutonomyPolicy.Apply(
                    person: person,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    institutionPools: institutionPools))
                changed = true;
            if (employmentAutonomyPolicy.Apply(
                    person: person,
                    household: household,
                    householdResidents: householdResidents,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    housingStatus: housingStatus,
                    workplacePools: workplacePools))
                changed = true;
            if (person.GetAgeGroup(currentDate) != AgeGroup.Senior)
                return changed;
            if (person.Employment.Status is not (EmploymentStatus.Employed or EmploymentStatus.Student))
                return changed;
            person.Retire(currentDate);
            return true;
        }

        private static CityEconomyDailySettlementSnapshot? ApplyHouseholdCashflowSettlement(
            IReadOnlyDictionary<HouseholdId, HouseholdEntity> householdsById,
            IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> residentsByHouseholdId,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            DateOnly previousDate,
            DateOnly currentDate,
            CityHouseholdCashflowPolicy householdCashflowPolicy,
            ICollection<ClassicCityHouseholdCashflowSettlementItemV1> cashflowItems,
            ICollection<ClassicCityWorkplacePayrollSettlementItemV1> workplacePayrollItems)
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
                    currentDate: currentDate);

                grossPayroll = grossPayroll.Add(cashflow.GrossIncome.Multiply(daysElapsed));
                incomeTax = incomeTax.Add(cashflow.TaxWithheld.Multiply(daysElapsed));
                netPayroll = netPayroll.Add(cashflow.TakeHomeIncome.Multiply(daysElapsed));
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

                foreach (PersonEntity resident in residents)
                {
                    CityResidentIncomeSettlementProfile residentIncome = householdCashflowPolicy.BuildResidentIncome(
                        resident: resident,
                        currentDate: currentDate);
                    Money residentGrossIncomeForPeriod = residentIncome.GrossIncome.Multiply(daysElapsed);
                    Money residentTaxForPeriod = residentIncome.TaxWithheld.Multiply(daysElapsed);
                    Money residentNetIncomeForPeriod = residentIncome.NetIncome.Multiply(daysElapsed);

                    if (resident.Employment.Status == EmploymentStatus.Employed &&
                        resident.Employment.Job is
                            { } job &&
                        residentNetIncomeForPeriod.IsPositive)
                    {
                        workplacePayrollItems.Add(
                            new ClassicCityWorkplacePayrollSettlementItemV1(
                                HouseholdId: householdId.Value,
                                HouseholdExternalReferenceCode: BuildHouseholdExternalReferenceCode(householdId),
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
                            ExternalReferenceCode: BuildHouseholdExternalReferenceCode(householdId),
                            GrossPayrollAmount: supportGrossIncomeForPeriod.Amount,
                            IncomeTaxAmount: supportIncomeTaxForPeriod.Amount,
                            NetPayrollAmount: supportNetIncomeForPeriod.Amount,
                            RetailTurnoverAmount: retailTurnoverForPeriod.Amount,
                            RetailTaxAmount: retailTaxForPeriod.Amount));

                household.ApplyDailyCashflow(
                    takeHomeIncome: cashflow.TakeHomeIncome,
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

        private static ClassicCityHouseholdCashflowSettlementBatchV1[] BuildHouseholdCashflowSettlementBatches(
            Guid cityId,
            DateOnly currentDate,
            int settledDays,
            IReadOnlyCollection<ClassicCityHouseholdCashflowSettlementItemV1> items,
            string correlationId,
            DateTimeOffset occurredAtUtc)
        {
            if (items.Count == 0 || settledDays <= 0)
                return [];

            ClassicCityHouseholdCashflowSettlementBatchV1[] batches = items
               .Chunk(EconomyHouseholdCashflowBatchSize)
               .Select((
                    chunk,
                    index) => new ClassicCityHouseholdCashflowSettlementBatchV1(
                    CityId: cityId,
                    CurrentDate: currentDate,
                    SettledDays: settledDays,
                    BatchNumber: index + 1,
                    TotalBatches: 0,
                    Households: chunk,
                    CorrelationId: correlationId,
                    OccurredAtUtc: occurredAtUtc))
               .ToArray();

            for (int i = 0; i < batches.Length; i++)
                batches[i] = batches[i] with
                {
                    TotalBatches = batches.Length
                };

            return batches;
        }

        private static ClassicCityWorkplacePayrollSettlementBatchV1[] BuildWorkplacePayrollSettlementBatches(
            Guid cityId,
            DateOnly currentDate,
            int settledDays,
            IReadOnlyCollection<ClassicCityWorkplacePayrollSettlementItemV1> items,
            string correlationId,
            DateTimeOffset occurredAtUtc)
        {
            if (items.Count == 0 || settledDays <= 0)
                return [];

            ClassicCityWorkplacePayrollSettlementBatchV1[] batches = items
               .Chunk(EconomyWorkplacePayrollBatchSize)
               .Select((
                    chunk,
                    index) => new ClassicCityWorkplacePayrollSettlementBatchV1(
                    CityId: cityId,
                    CurrentDate: currentDate,
                    SettledDays: settledDays,
                    BatchNumber: index + 1,
                    TotalBatches: 0,
                    Payrolls: chunk,
                    CorrelationId: correlationId,
                    OccurredAtUtc: occurredAtUtc))
               .ToArray();

            for (int i = 0; i < batches.Length; i++)
                batches[i] = batches[i] with
                {
                    TotalBatches = batches.Length
                };

            return batches;
        }

        private static string BuildHouseholdExternalReferenceCode(HouseholdId householdId)
        {
            return $"classic-city-household:{householdId.Value:N}";
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

        private static bool ApplyHouseholdPressureProgression(
            PersonEntity person,
            IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> residentsByHouseholdId,
            DateOnly previousDate,
            DateOnly currentDate,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState> financialStressByHouseholdId,
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

            return householdPressurePolicy.Apply(
                resident: person,
                householdResidents: householdResidents,
                housingStatus: housingStatus,
                financialStressState: financialStressState,
                previousDate: previousDate,
                currentDate: currentDate);
        }

        private static bool ApplyIllnessProgression(
            PersonEntity person,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            DateOnly previousDate,
            DateOnly currentDate,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            IReadOnlyCollection<CityWeatherExposureSegment> exposureSegments,
            MarriageDomainService marriageDomainService,
            CityIllnessAutonomyPolicy illnessAutonomyPolicy,
            CityHealthcareAutonomyPolicy healthcareAutonomyPolicy)
        {
            HousingStatus? housingStatus = housingByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out HousingStatus resolvedHousingStatus)
                ? resolvedHousingStatus
                : null;
            bool hadAdverseExposure = exposureSegments.Any(x => x.Kind == CityWeatherExposureKind.Adverse);
            bool wasAlive = person.IsAlive;
            IReadOnlyCollection<PersonEntity> householdResidents = residentsById.Values
               .Where(x => x.HouseholdId == person.HouseholdId)
               .ToArray();
            double healthcareSupportStrength = healthcareAutonomyPolicy.ResolveSupportStrength(
                resident: person,
                householdResidents: householdResidents,
                housingStatus: housingStatus,
                currentDate: currentDate);

            bool changed = illnessAutonomyPolicy.Apply(
                person: person,
                householdResidents: householdResidents,
                previousDate: previousDate,
                currentDate: currentDate,
                housingStatus: housingStatus,
                hadAdverseWeatherExposure: hadAdverseExposure,
                healthcareSupportStrength: healthcareSupportStrength);

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
            CityHousingAutonomyPolicy housingAutonomyPolicy,
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

            IReadOnlyList<CityHousingAutonomyDecision> decisions = housingAutonomyPolicy.Plan(
                households: householdsById,
                residents: residentsById.Values.ToArray(),
                housingStatuses: housingStatuses,
                financialStressStates: financialStressByHouseholdId,
                previousDate: previousDate,
                currentDate: currentDate);

            if (decisions.Count == 0)
                return 0;

            var residentsByHousehold = residentsById.Values
               .Where(x => x.IsAlive)
               .GroupBy(x => x.HouseholdId)
               .ToDictionary(
                    keySelector: x => x.Key,
                    elementSelector: x => x.ToList());

            var placementsByHousehold = placements.ToDictionary(
                keySelector: x => x.HouseholdId,
                elementSelector: x => x);

            List<(DistrictId DistrictId, ResidentialBuildingId ResidentialBuildingId)> housingPool =
                BuildHousingOpportunityPool(placements);

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

                PersonEntity anchorResident = SelectHousingAnchorResident(
                    householdResidents: householdResidents,
                    currentDate: currentDate);

                switch (decision.Type)
                {
                    case CityHousingAutonomyDecisionType.FindHousing:
                        if (placement.HousingStatus == HousingStatus.Housed ||
                            housingPool.Count == 0)
                            continue;

                        (DistrictId districtId, ResidentialBuildingId residentialBuildingId) opportunity =
                            SelectHousingOpportunity(
                                householdId: placement.HouseholdId,
                                currentDate: currentDate,
                                housingPool: housingPool);

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

        private static bool ShouldAdvanceWeatherExposureCheckpoint(
            CityPopulationWeatherExposureState? weatherExposureState,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc)
        {
            if (weatherExposureState is null)
                return false;
            DateTimeOffset effectiveFrom = Max(
                left: fromSimTimeUtc,
                right: weatherExposureState.LastExposureProcessedAtSimTimeUtc);
            return toSimTimeUtc > effectiveFrom;
        }

        private static List<CityWeatherExposureSegment> BuildExposureSegments(
            CityPopulationWeatherExposureState weatherExposureState,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc)
        {
            var segments = new List<CityWeatherExposureSegment>();
            DateTimeOffset effectiveFrom = Max(
                left: fromSimTimeUtc,
                right: weatherExposureState.LastExposureProcessedAtSimTimeUtc);
            if (toSimTimeUtc <= effectiveFrom)
                return segments;

            if (weatherExposureState.HasPreviousWeather &&
                weatherExposureState.PreviousWeather is WeatherImpactProfile previousWeather &&
                weatherExposureState.PreviousWeatherEffectiveAtSimTimeUtc.HasValue &&
                effectiveFrom < weatherExposureState.CurrentWeatherEffectiveAtSimTimeUtc)
            {
                DateTimeOffset previousStart = Max(
                    left: effectiveFrom,
                    right: weatherExposureState.PreviousWeatherEffectiveAtSimTimeUtc.Value);
                DateTimeOffset previousEnd = Min(
                    left: toSimTimeUtc,
                    right: weatherExposureState.CurrentWeatherEffectiveAtSimTimeUtc);
                if (previousEnd > previousStart && CityWeatherExposureRules.IsAdverseExposureWeather(previousWeather))
                    segments.Add(
                        new CityWeatherExposureSegment(
                            Kind: CityWeatherExposureKind.Adverse,
                            Weather: previousWeather,
                            EffectStartedAtSimTimeUtc: weatherExposureState.PreviousWeatherEffectiveAtSimTimeUtc.Value,
                            IntervalStartSimTimeUtc: previousStart,
                            IntervalEndSimTimeUtc: previousEnd));
            }

            DateTimeOffset currentStart = Max(
                left: effectiveFrom,
                right: weatherExposureState.CurrentWeatherEffectiveAtSimTimeUtc);
            if (toSimTimeUtc > currentStart &&
                CityWeatherExposureRules.IsAdverseExposureWeather(weatherExposureState.CurrentWeather))
                segments.Add(
                    new CityWeatherExposureSegment(
                        Kind: CityWeatherExposureKind.Adverse,
                        Weather: weatherExposureState.CurrentWeather,
                        EffectStartedAtSimTimeUtc: weatherExposureState.CurrentWeatherEffectiveAtSimTimeUtc,
                        IntervalStartSimTimeUtc: currentStart,
                        IntervalEndSimTimeUtc: toSimTimeUtc));

            if (toSimTimeUtc > currentStart &&
                weatherExposureState.HasRecoverySource &&
                weatherExposureState.RecoverySourceWeather is WeatherImpactProfile recoverySourceWeather &&
                weatherExposureState.RecoveryStartedAtSimTimeUtc.HasValue &&
                CityWeatherExposureRules.IsRecoveryWeather(weatherExposureState.CurrentWeather))
            {
                DateTimeOffset recoveryStart = Max(
                    left: currentStart,
                    right: weatherExposureState.RecoveryStartedAtSimTimeUtc.Value);
                if (toSimTimeUtc > recoveryStart)
                    segments.Add(
                        new CityWeatherExposureSegment(
                            Kind: CityWeatherExposureKind.Recovery,
                            Weather: weatherExposureState.CurrentWeather,
                            EffectStartedAtSimTimeUtc: weatherExposureState.RecoveryStartedAtSimTimeUtc.Value,
                            IntervalStartSimTimeUtc: recoveryStart,
                            IntervalEndSimTimeUtc: toSimTimeUtc,
                            SourceWeather: recoverySourceWeather));
            }

            return segments;
        }

        private static DateTimeOffset Max(
            DateTimeOffset left,
            DateTimeOffset right)
        {
            return left >= right
                ? left
                : right;
        }

        private static DateTimeOffset Min(
            DateTimeOffset left,
            DateTimeOffset right)
        {
            return left <= right
                ? left
                : right;
        }

        private static Dictionary<EducationLevel, List<EducationInstitutionId>> BuildEducationInstitutionPools(
            IEnumerable<PersonEntity> persons)
        {
            var pools = new Dictionary<EducationLevel, List<EducationInstitutionId>>();
            foreach (PersonEntity person in persons)
            {
                if (person.Education.CurrentInstitutionId is not
                    { } institutionId)
                    continue;
                EducationLevel level = person.Education.Level;
                if (!pools.TryGetValue(
                        key: level,
                        value: out List<EducationInstitutionId>? levelPool))
                {
                    levelPool = [];
                    pools[level] = levelPool;
                }

                if (!levelPool.Contains(institutionId))
                    levelPool.Add(institutionId);
            }

            return pools;
        }

        private static Dictionary<string, List<WorkplaceId>> BuildWorkplacePools(IEnumerable<PersonEntity> persons)
        {
            var pools = new Dictionary<string, List<WorkplaceId>>(StringComparer.OrdinalIgnoreCase);
            foreach (PersonEntity person in persons)
            {
                if (person.Employment.Status != EmploymentStatus.Employed ||
                    person.Employment.Job is not
                        { } job)
                    continue;
                if (!pools.TryGetValue(
                        key: job.Title,
                        value: out List<WorkplaceId>? titlePool))
                {
                    titlePool = [];
                    pools[job.Title] = titlePool;
                }

                if (!titlePool.Contains(job.WorkplaceId))
                    titlePool.Add(job.WorkplaceId);
            }

            return pools;
        }

        private static List<(DistrictId districtId, ResidentialBuildingId residentialBuildingId)>
            BuildHousingOpportunityPool(IEnumerable<ClassicCityHouseholdPlacement> placements)
        {
            return placements
               .Where(x => x.HousingStatus == HousingStatus.Housed &&
                           x.DistrictId.HasValue &&
                           x.ResidentialBuildingId.HasValue)
               .Select(x => (x.DistrictId!.Value, x.ResidentialBuildingId!.Value))
               .Distinct()
               .ToList();
        }

        private static PersonEntity SelectHousingAnchorResident(
            IReadOnlyCollection<PersonEntity> householdResidents,
            DateOnly currentDate)
        {
            return householdResidents
               .OrderByDescending(x => x.GetAgeGroup(currentDate) is AgeGroup.Adult or AgeGroup.Senior)
               .ThenByDescending(x => x.GetAge(currentDate)
                   .Years)
               .ThenBy(x => x.Id.Value)
               .First();
        }

        private static (DistrictId districtId, ResidentialBuildingId residentialBuildingId) SelectHousingOpportunity(
            HouseholdId householdId,
            DateOnly currentDate,
            IReadOnlyList<(DistrictId districtId, ResidentialBuildingId residentialBuildingId)> housingPool)
        {
            int index = GetStableInt(
                householdId: householdId,
                currentDate: currentDate,
                salt: 1_123,
                modulus: housingPool.Count);

            return housingPool[index];
        }

        private static int GetStableInt(
            HouseholdId householdId,
            DateOnly currentDate,
            int salt,
            int modulus)
        {
            if (modulus <= 0)
                return 0;

            unchecked
            {
                byte[] bytes = householdId.Value.ToByteArray();
                int hash = 17;
                for (int i = 0; i < bytes.Length; i++)
                    hash = (hash * 31) + bytes[i];

                hash = (hash * 31) + currentDate.DayNumber;
                hash = (hash * 31) + salt;

                return (int)(Math.Abs((long)hash) % modulus);
            }
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
