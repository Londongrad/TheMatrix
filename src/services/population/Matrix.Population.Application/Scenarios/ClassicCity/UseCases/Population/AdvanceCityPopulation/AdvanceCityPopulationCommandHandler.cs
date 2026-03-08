using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.Common;
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

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class AdvanceCityPopulationCommandHandler(
        ICityPopulationPersonReadRepository personReadRepository,
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        ICityPopulationEnvironmentRepository cityPopulationEnvironmentRepository,
        ICityPopulationActivityJournalService cityPopulationActivityJournalService,
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
        PersonNeedsProgressionPolicy personNeedsProgressionPolicy,
        CityPopulationWeatherExposurePolicy weatherExposurePolicy,
        ILogger<AdvanceCityPopulationCommandHandler> logger,
        IUnitOfWork unitOfWork)
        : IRequestHandler<AdvanceCityPopulationCommand, AdvanceCityPopulationResult>
    {
        public async Task<AdvanceCityPopulationResult> Handle(AdvanceCityPopulationCommand request, CancellationToken cancellationToken)
        {
            var cityId = CityId.From(request.CityId);
            var fromDate = DateOnly.FromDateTime(request.FromSimTimeUtc.UtcDateTime);
            var toDate = DateOnly.FromDateTime(request.ToSimTimeUtc.UtcDateTime);
            CityPopulationProgressionState? state = await progressionStateRepository.GetByCityAsync(cityId, cancellationToken);
            CityPopulationArchiveState? archiveState = await cityPopulationArchiveStateRepository.GetByCityAsync(cityId, cancellationToken);
            CityPopulationDeletionState? deletionState = await cityPopulationDeletionStateRepository.GetByCityAsync(cityId, cancellationToken);
            CityPopulationEnvironment? environment = await cityPopulationEnvironmentRepository.GetByCityAsync(cityId, cancellationToken);
            CityPopulationWeatherExposureState? weatherExposureState = await weatherExposureStateRepository.GetByCityAsync(cityId, cancellationToken);

            if (state is not null)
            {
                if (request.TickId <= state.LastProcessedTickId)
                    return new AdvanceCityPopulationResult(AdvanceCityPopulationStatus.Duplicate, 0);
                if (toDate < state.LastProcessedDate)
                    return new AdvanceCityPopulationResult(AdvanceCityPopulationStatus.OutOfOrder, 0);
            }

            if (deletionState is not null)
                return new AdvanceCityPopulationResult(AdvanceCityPopulationStatus.CityDeleted, 0);
            if (archiveState is not null)
                return new AdvanceCityPopulationResult(AdvanceCityPopulationStatus.CityArchived, 0);

            DateOnly previousDate = state?.LastProcessedDate ?? fromDate;
            int affectedPeopleCount = 0;
            bool requiresDateProgression = state is null || toDate > previousDate;
            bool requiresNeedsProgression = request.ToSimTimeUtc > request.FromSimTimeUtc;
            bool shouldAdvanceWeatherExposureCheckpoint = ShouldAdvanceWeatherExposureCheckpoint(weatherExposureState, request.FromSimTimeUtc, request.ToSimTimeUtc);
            List<CityWeatherExposureSegment> exposureSegments = shouldAdvanceWeatherExposureCheckpoint && weatherExposureState is not null
                ? BuildExposureSegments(weatherExposureState, request.FromSimTimeUtc, request.ToSimTimeUtc)
                : [];
            bool requiresWeatherExposure = exposureSegments.Count > 0;
            IReadOnlyCollection<PersonEntity>? personsSnapshot = null;
            List<CityPopulationActivityWriteModel> pendingActivityEntries = [];

            if ((requiresDateProgression || requiresNeedsProgression || requiresWeatherExposure) && environment is null)
                logger.LogWarning("Advancing city population without synced environment for cityId={CityId}. Climate adaptation will be neutral and needs progression will use UTC fallback.", request.CityId);

            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                if (requiresDateProgression || requiresNeedsProgression || requiresWeatherExposure)
                {
                    List<PersonEntity> residents = (await personReadRepository.ListByCityAsync(cityId, ct)).ToList();
                    personsSnapshot = residents;
                    var personsById = residents.ToDictionary(x => x.Id, x => x);
                    Dictionary<EducationLevel, List<EducationInstitutionId>> institutionPools = BuildEducationInstitutionPools(residents);
                    Dictionary<string, List<WorkplaceId>> workplacePools = BuildWorkplacePools(residents);

                    foreach (PersonEntity person in residents)
                    {
                        ResidentLifecycleSnapshot beforeSnapshot = CreateResidentSnapshot(person);

                        if (ApplyProgressionNeedsAndExposure(person, personsById, previousDate, request.FromSimTimeUtc, request.ToSimTimeUtc, toDate, requiresDateProgression, requiresNeedsProgression, environment, exposureSegments, marriageDomainService, educationAutonomyPolicy, employmentAutonomyPolicy, institutionPools, workplacePools, personNeedsProgressionPolicy, weatherExposurePolicy))
                        {
                            affectedPeopleCount++;
                            CollectResidentProgressionActivity(cityId, toDate, beforeSnapshot, person, personsById, pendingActivityEntries);
                        }
                    }

                    if (requiresDateProgression)
                    {
                        affectedPeopleCount += await ApplyBirthAutonomyAsync(
                            cityId,
                            personsById,
                            previousDate,
                            toDate,
                            birthAutonomyPolicy,
                            populationBirthDomainService,
                            personWriteRepository,
                            householdWriteRepository,
                            pendingActivityEntries,
                            residents,
                            ct);

                        affectedPeopleCount += await ApplyCivilRegistryAutonomyAsync(
                            cityId,
                            personsById,
                            previousDate,
                            toDate,
                            householdWriteRepository,
                            marriageDomainService,
                            civilRegistryAutonomyPolicy,
                            pendingActivityEntries,
                            ct);
                    }
                }

                DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;
                if (state is null)
                {
                    var newState = CityPopulationProgressionState.Create(cityId, request.TickId, toDate, updatedAtUtc);
                    await progressionStateRepository.AddAsync(newState, ct);
                }
                else
                    state.MarkProcessed(request.TickId, toDate, updatedAtUtc);

                if (shouldAdvanceWeatherExposureCheckpoint && weatherExposureState is not null)
                    weatherExposureState.MarkExposureProcessed(request.ToSimTimeUtc, updatedAtUtc);

                if (personsSnapshot is not null)
                {
                    await cityPopulationSummaryProjectionService.UpdateAsync(cityId, toDate, personsSnapshot, ct);

                    foreach (CityPopulationActivityWriteModel activityEntry in pendingActivityEntries)
                        await cityPopulationActivityJournalService.RecordAsync(activityEntry, ct);
                }

                await unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);

            return new AdvanceCityPopulationResult(AdvanceCityPopulationStatus.Applied, affectedPeopleCount);
        }

        private static bool ApplyProgressionNeedsAndExposure(PersonEntity person, IReadOnlyDictionary<PersonId, PersonEntity> residentsById, DateOnly previousDate, DateTimeOffset fromSimTimeUtc, DateTimeOffset toSimTimeUtc, DateOnly currentDate, bool requiresDateProgression, bool requiresNeedsProgression, CityPopulationEnvironment? environment, IReadOnlyCollection<CityWeatherExposureSegment> exposureSegments, MarriageDomainService marriageDomainService, CityEducationAutonomyPolicy educationAutonomyPolicy, CityEmploymentAutonomyPolicy employmentAutonomyPolicy, IDictionary<EducationLevel, List<EducationInstitutionId>> institutionPools, IDictionary<string, List<WorkplaceId>> workplacePools, PersonNeedsProgressionPolicy personNeedsProgressionPolicy, CityPopulationWeatherExposurePolicy weatherExposurePolicy)
        {
            bool changed = false;
            if (requiresNeedsProgression && ApplyNeedsProgression(person, residentsById, fromSimTimeUtc, toSimTimeUtc, currentDate, environment, marriageDomainService, personNeedsProgressionPolicy))
                changed = true;
            if (requiresDateProgression && ApplyTimeProgression(person, previousDate, currentDate, educationAutonomyPolicy, employmentAutonomyPolicy, institutionPools, workplacePools))
                changed = true;
            if (exposureSegments.Count > 0 && ApplyWeatherExposure(person, residentsById, currentDate, environment, exposureSegments, marriageDomainService, weatherExposurePolicy))
                changed = true;
            return changed;
        }

        private static bool ApplyNeedsProgression(PersonEntity person, IReadOnlyDictionary<PersonId, PersonEntity> residentsById, DateTimeOffset fromSimTimeUtc, DateTimeOffset toSimTimeUtc, DateOnly currentDate, CityPopulationEnvironment? environment, MarriageDomainService marriageDomainService, PersonNeedsProgressionPolicy personNeedsProgressionPolicy)
        {
            int utcOffsetMinutes = environment?.UtcOffsetMinutes ?? 0;
            PersonNeedsProgressionEffect effect = personNeedsProgressionPolicy.Calculate(person, fromSimTimeUtc, toSimTimeUtc, utcOffsetMinutes);
            bool wasAlive = person.IsAlive;
            bool changed = person.ApplyNeedsProgression(effect, currentDate);
            if (wasAlive && !person.IsAlive)
                changed = ClassicCityWidowhoodSupport.TryRegisterWidowhood(person, residentsById, marriageDomainService) || changed;
            return changed;
        }

        private static bool ApplyTimeProgression(PersonEntity person, DateOnly previousDate, DateOnly currentDate, CityEducationAutonomyPolicy educationAutonomyPolicy, CityEmploymentAutonomyPolicy employmentAutonomyPolicy, IDictionary<EducationLevel, List<EducationInstitutionId>> institutionPools, IDictionary<string, List<WorkplaceId>> workplacePools)
        {
            bool changed = false;
            if (!person.IsAlive)
                return false;
            if (educationAutonomyPolicy.Apply(person, previousDate, currentDate, institutionPools))
                changed = true;
            if (employmentAutonomyPolicy.Apply(person, previousDate, currentDate, workplacePools))
                changed = true;
            if (person.GetAgeGroup(currentDate) != AgeGroup.Senior)
                return changed;
            if (person.Employment.Status is not (EmploymentStatus.Employed or EmploymentStatus.Student))
                return changed;
            person.Retire(currentDate);
            return true;
        }

        private static bool ApplyWeatherExposure(PersonEntity person, IReadOnlyDictionary<PersonId, PersonEntity> residentsById, DateOnly currentDate, CityPopulationEnvironment? environment, IReadOnlyCollection<CityWeatherExposureSegment> exposureSegments, MarriageDomainService marriageDomainService, CityPopulationWeatherExposurePolicy weatherExposurePolicy)
        {
            if (exposureSegments.Count == 0)
                return false;
            int totalHealthDelta = 0;
            int totalHappinessDelta = 0;
            foreach (CityWeatherExposureSegment segment in exposureSegments)
            {
                PersonWeatherImpact impact = weatherExposurePolicy.Calculate(person, currentDate, segment, environment);
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
                person.ChangeHealth(totalHealthDelta, currentDate);
                changed = previousHealth != person.Health.Value || wasAlive != person.IsAlive;
                if (wasAlive && !person.IsAlive)
                    changed = ClassicCityWidowhoodSupport.TryRegisterWidowhood(person, residentsById, marriageDomainService) || changed;
            }
            if (totalHappinessDelta != 0 && person.IsAlive)
            {
                int previousHappiness = person.Happiness.Value;
                person.ChangeHappiness(totalHappinessDelta);
                changed = changed || previousHappiness != person.Happiness.Value;
            }
            return changed;
        }

        private static async Task<int> ApplyBirthAutonomyAsync(
            CityId cityId,
            IDictionary<PersonId, PersonEntity> residentsById,
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
                previousDate: previousDate,
                currentDate: currentDate);

            if (decisions.Count == 0)
                return 0;

            int affectedResidents = 0;

            foreach (CityBirthAutonomyDecision decision in decisions)
            {
                if (!residentsById.TryGetValue(decision.MotherId, out PersonEntity? mother))
                    continue;

                PersonEntity? father = null;
                if (decision.FatherId is not null &&
                    !residentsById.TryGetValue(decision.FatherId.Value, out father))
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

        private static async Task<int> ApplyCivilRegistryAutonomyAsync(CityId cityId, IReadOnlyDictionary<PersonId, PersonEntity> residentsById, DateOnly previousDate, DateOnly currentDate, IHouseholdWriteRepository householdWriteRepository, MarriageDomainService marriageDomainService, CityCivilRegistryAutonomyPolicy civilRegistryAutonomyPolicy, ICollection<CityPopulationActivityWriteModel> activityEntries, CancellationToken cancellationToken)
        {
            IReadOnlyList<CityCivilRegistryAutonomyDecision> decisions = civilRegistryAutonomyPolicy.Plan(residentsById.Values.ToArray(), previousDate, currentDate);
            if (decisions.Count == 0)
                return 0;
            int affectedResidents = 0;
            foreach (CityCivilRegistryAutonomyDecision decision in decisions)
            {
                if (!residentsById.TryGetValue(decision.FirstResidentId, out PersonEntity? firstResident) || !residentsById.TryGetValue(decision.SecondResidentId, out PersonEntity? secondResident))
                    continue;
                switch (decision.Type)
                {
                    case CityCivilRegistryAutonomyDecisionType.Marriage:
                        marriageDomainService.RegisterMarriage(firstResident, secondResident, currentDate);
                        await ClassicCityCivilRegistryHouseholdSupport.MergeSpousesIntoSharedHouseholdAsync(cityId, firstResident, secondResident, householdWriteRepository, cancellationToken);
                        activityEntries.Add(ClassicCityActivityFactory.ResidentsMarried(cityId.Value, currentDate, firstResident, secondResident, CityPopulationActivitySource.Autonomy));
                        affectedResidents += 2;
                        break;
                    case CityCivilRegistryAutonomyDecisionType.Divorce:
                        if (firstResident.SpouseId != secondResident.Id || secondResident.SpouseId != firstResident.Id)
                            continue;
                        marriageDomainService.RegisterDivorce(firstResident, secondResident, currentDate);
                        await ClassicCityCivilRegistryHouseholdSupport.SeparateDivorcedSpousesAsync(cityId, firstResident, secondResident, householdWriteRepository, cancellationToken);
                        activityEntries.Add(ClassicCityActivityFactory.ResidentsDivorced(cityId.Value, currentDate, firstResident, secondResident, CityPopulationActivitySource.Autonomy));
                        affectedResidents += 2;
                        break;
                }
            }
            return affectedResidents;
        }

        private static bool ShouldAdvanceWeatherExposureCheckpoint(CityPopulationWeatherExposureState? weatherExposureState, DateTimeOffset fromSimTimeUtc, DateTimeOffset toSimTimeUtc)
        {
            if (weatherExposureState is null)
                return false;
            DateTimeOffset effectiveFrom = Max(fromSimTimeUtc, weatherExposureState.LastExposureProcessedAtSimTimeUtc);
            return toSimTimeUtc > effectiveFrom;
        }

        private static List<CityWeatherExposureSegment> BuildExposureSegments(CityPopulationWeatherExposureState weatherExposureState, DateTimeOffset fromSimTimeUtc, DateTimeOffset toSimTimeUtc)
        {
            var segments = new List<CityWeatherExposureSegment>();
            DateTimeOffset effectiveFrom = Max(fromSimTimeUtc, weatherExposureState.LastExposureProcessedAtSimTimeUtc);
            if (toSimTimeUtc <= effectiveFrom)
                return segments;

            if (weatherExposureState.HasPreviousWeather && weatherExposureState.PreviousWeather is WeatherImpactProfile previousWeather && weatherExposureState.PreviousWeatherEffectiveAtSimTimeUtc.HasValue && effectiveFrom < weatherExposureState.CurrentWeatherEffectiveAtSimTimeUtc)
            {
                DateTimeOffset previousStart = Max(effectiveFrom, weatherExposureState.PreviousWeatherEffectiveAtSimTimeUtc.Value);
                DateTimeOffset previousEnd = Min(toSimTimeUtc, weatherExposureState.CurrentWeatherEffectiveAtSimTimeUtc);
                if (previousEnd > previousStart && CityWeatherExposureRules.IsAdverseExposureWeather(previousWeather))
                    segments.Add(new CityWeatherExposureSegment(CityWeatherExposureKind.Adverse, previousWeather, weatherExposureState.PreviousWeatherEffectiveAtSimTimeUtc.Value, previousStart, previousEnd));
            }

            DateTimeOffset currentStart = Max(effectiveFrom, weatherExposureState.CurrentWeatherEffectiveAtSimTimeUtc);
            if (toSimTimeUtc > currentStart && CityWeatherExposureRules.IsAdverseExposureWeather(weatherExposureState.CurrentWeather))
                segments.Add(new CityWeatherExposureSegment(CityWeatherExposureKind.Adverse, weatherExposureState.CurrentWeather, weatherExposureState.CurrentWeatherEffectiveAtSimTimeUtc, currentStart, toSimTimeUtc));

            if (toSimTimeUtc > currentStart && weatherExposureState.HasRecoverySource && weatherExposureState.RecoverySourceWeather is WeatherImpactProfile recoverySourceWeather && weatherExposureState.RecoveryStartedAtSimTimeUtc.HasValue && CityWeatherExposureRules.IsRecoveryWeather(weatherExposureState.CurrentWeather))
            {
                DateTimeOffset recoveryStart = Max(currentStart, weatherExposureState.RecoveryStartedAtSimTimeUtc.Value);
                if (toSimTimeUtc > recoveryStart)
                    segments.Add(new CityWeatherExposureSegment(CityWeatherExposureKind.Recovery, weatherExposureState.CurrentWeather, weatherExposureState.RecoveryStartedAtSimTimeUtc.Value, recoveryStart, toSimTimeUtc, recoverySourceWeather));
            }

            return segments;
        }

        private static DateTimeOffset Max(DateTimeOffset left, DateTimeOffset right) => left >= right ? left : right;
        private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right) => left <= right ? left : right;

        private static Dictionary<EducationLevel, List<EducationInstitutionId>> BuildEducationInstitutionPools(IEnumerable<PersonEntity> persons)
        {
            var pools = new Dictionary<EducationLevel, List<EducationInstitutionId>>();
            foreach (PersonEntity person in persons)
            {
                if (person.Education.CurrentInstitutionId is not { } institutionId)
                    continue;
                EducationLevel level = person.Education.Level;
                if (!pools.TryGetValue(level, out List<EducationInstitutionId>? levelPool))
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
                if (person.Employment.Status != EmploymentStatus.Employed || person.Employment.Job is not { } job)
                    continue;
                if (!pools.TryGetValue(job.Title, out List<WorkplaceId>? titlePool))
                {
                    titlePool = [];
                    pools[job.Title] = titlePool;
                }
                if (!titlePool.Contains(job.WorkplaceId))
                    titlePool.Add(job.WorkplaceId);
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
                EducationLevel: person.EducationLevel);
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
                activityEntries.Add(ClassicCityActivityFactory.ResidentDied(cityId.Value, currentDate, resident, CityPopulationActivitySource.Autonomy));

            if (before.MaritalStatus != MaritalStatus.Widowed && resident.MaritalStatus == MaritalStatus.Widowed)
            {
                string deceasedName = before.SpouseId is not null && residentsById.TryGetValue(before.SpouseId.Value, out PersonEntity? spouse)
                    ? spouse.Name.ToString()
                    : "their spouse";

                activityEntries.Add(ClassicCityActivityFactory.ResidentBecameWidowed(cityId.Value, currentDate, resident, deceasedName, CityPopulationActivitySource.Autonomy));
            }

            if (before.EducationLevel != resident.EducationLevel && resident.EducationLevel > before.EducationLevel)
                activityEntries.Add(ClassicCityActivityFactory.ResidentGraduated(cityId.Value, currentDate, resident, CityPopulationActivitySource.Autonomy));

            if (before.EmploymentStatus != EmploymentStatus.Student && resident.Employment.Status == EmploymentStatus.Student)
                activityEntries.Add(ClassicCityActivityFactory.ResidentEnrolled(cityId.Value, currentDate, resident, CityPopulationActivitySource.Autonomy));
            else if (before.EmploymentStatus == EmploymentStatus.Student && resident.Employment.Status != EmploymentStatus.Student)
                activityEntries.Add(ClassicCityActivityFactory.ResidentWithdrewFromStudy(cityId.Value, currentDate, resident, CityPopulationActivitySource.Autonomy));

            if (before.EmploymentStatus != EmploymentStatus.Employed && resident.Employment.Status == EmploymentStatus.Employed)
                activityEntries.Add(ClassicCityActivityFactory.ResidentHired(cityId.Value, currentDate, resident, CityPopulationActivitySource.Autonomy));
            else if (before.EmploymentStatus == EmploymentStatus.Employed && resident.Employment.Status == EmploymentStatus.Unemployed)
                activityEntries.Add(ClassicCityActivityFactory.ResidentFired(cityId.Value, currentDate, resident, before.JobTitle, CityPopulationActivitySource.Autonomy));
            else if (before.EmploymentStatus != EmploymentStatus.Retired && resident.Employment.Status == EmploymentStatus.Retired)
                activityEntries.Add(ClassicCityActivityFactory.ResidentRetired(cityId.Value, currentDate, resident, CityPopulationActivitySource.Autonomy));
        }

        private sealed record ResidentLifecycleSnapshot(
            bool IsAlive,
            MaritalStatus MaritalStatus,
            PersonId? SpouseId,
            EmploymentStatus EmploymentStatus,
            string? JobTitle,
            EducationLevel EducationLevel);
    }
}
