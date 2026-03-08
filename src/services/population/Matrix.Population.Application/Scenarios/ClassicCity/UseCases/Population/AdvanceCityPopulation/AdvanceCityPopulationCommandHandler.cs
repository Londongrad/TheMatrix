using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
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

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class AdvanceCityPopulationCommandHandler(
        ICityPopulationPersonReadRepository personReadRepository,
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        ICityPopulationEnvironmentRepository cityPopulationEnvironmentRepository,
        ICityPopulationProgressionStateRepository progressionStateRepository,
        ICityPopulationSummaryProjectionService cityPopulationSummaryProjectionService,
        ICityPopulationWeatherExposureStateRepository weatherExposureStateRepository,
        IHouseholdWriteRepository householdWriteRepository,
        MarriageDomainService marriageDomainService,
        CityCivilRegistryAutonomyPolicy civilRegistryAutonomyPolicy,
        CityEducationAutonomyPolicy educationAutonomyPolicy,
        CityEmploymentAutonomyPolicy employmentAutonomyPolicy,
        PersonNeedsProgressionPolicy personNeedsProgressionPolicy,
        CityPopulationWeatherExposurePolicy weatherExposurePolicy,
        ILogger<AdvanceCityPopulationCommandHandler> logger,
        IUnitOfWork unitOfWork)
        : IRequestHandler<AdvanceCityPopulationCommand, AdvanceCityPopulationResult>
    {
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
            CityPopulationDeletionState? deletionState = await cityPopulationDeletionStateRepository.GetByCityAsync(
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
                        personsSnapshot = await personReadRepository.ListByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);
                        var personsById = personsSnapshot.ToDictionary(
                            keySelector: x => x.Id,
                            elementSelector: x => x);
                        Dictionary<EducationLevel, List<Matrix.Population.Domain.ValueObjects.EducationInstitutionId>> institutionPools =
                            BuildEducationInstitutionPools(personsSnapshot);
                        Dictionary<string, List<Matrix.Population.Domain.ValueObjects.WorkplaceId>> workplacePools =
                            BuildWorkplacePools(personsSnapshot);

                        foreach (PersonEntity person in personsSnapshot)
                            if (ApplyProgressionNeedsAndExposure(
                                    person: person,
                                    residentsById: personsById,
                                    previousDate: previousDate,
                                    fromSimTimeUtc: request.FromSimTimeUtc,
                                    toSimTimeUtc: request.ToSimTimeUtc,
                                    currentDate: toDate,
                                    requiresDateProgression: requiresDateProgression,
                                    requiresNeedsProgression: requiresNeedsProgression,
                                    environment: environment,
                                    exposureSegments: exposureSegments,
                                    marriageDomainService: marriageDomainService,
                                    educationAutonomyPolicy: educationAutonomyPolicy,
                                    employmentAutonomyPolicy: employmentAutonomyPolicy,
                                    institutionPools: institutionPools,
                                    workplacePools: workplacePools,
                                    personNeedsProgressionPolicy: personNeedsProgressionPolicy,
                                    weatherExposurePolicy: weatherExposurePolicy))
                                affectedPeopleCount++;

                        if (requiresDateProgression)
                            affectedPeopleCount += await ApplyCivilRegistryAutonomyAsync(
                                cityId: cityId,
                                residentsById: personsById,
                                previousDate: previousDate,
                                currentDate: toDate,
                                householdWriteRepository: householdWriteRepository,
                                marriageDomainService: marriageDomainService,
                                civilRegistryAutonomyPolicy: civilRegistryAutonomyPolicy,
                                cancellationToken: ct);
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
                        await cityPopulationSummaryProjectionService.UpdateAsync(
                            cityId: cityId,
                            currentDate: toDate,
                            persons: personsSnapshot,
                            cancellationToken: ct);
                    }

                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            return new AdvanceCityPopulationResult(
                Status: AdvanceCityPopulationStatus.Applied,
                AffectedPeopleCount: affectedPeopleCount);
        }

        private static bool ApplyProgressionNeedsAndExposure(
            PersonEntity person,
            IReadOnlyDictionary<Matrix.Population.Domain.ValueObjects.PersonId, PersonEntity> residentsById,
            DateOnly previousDate,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc,
            DateOnly currentDate,
            bool requiresDateProgression,
            bool requiresNeedsProgression,
            CityPopulationEnvironment? environment,
            IReadOnlyCollection<CityWeatherExposureSegment> exposureSegments,
            MarriageDomainService marriageDomainService,
            CityEducationAutonomyPolicy educationAutonomyPolicy,
            CityEmploymentAutonomyPolicy employmentAutonomyPolicy,
            IDictionary<EducationLevel, List<Matrix.Population.Domain.ValueObjects.EducationInstitutionId>> institutionPools,
            IDictionary<string, List<Matrix.Population.Domain.ValueObjects.WorkplaceId>> workplacePools,
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
                    previousDate: previousDate,
                    currentDate: currentDate,
                    educationAutonomyPolicy: educationAutonomyPolicy,
                    employmentAutonomyPolicy: employmentAutonomyPolicy,
                    institutionPools: institutionPools,
                    workplacePools: workplacePools))
                changed = true;

            if (exposureSegments.Count > 0)
                if (ApplyWeatherExposure(
                        person: person,
                        residentsById: residentsById,
                        currentDate: currentDate,
                        environment: environment,
                        exposureSegments: exposureSegments,
                        marriageDomainService: marriageDomainService,
                        weatherExposurePolicy: weatherExposurePolicy))
                    changed = true;

            return changed;
        }

        private static bool ApplyNeedsProgression(
            PersonEntity person,
            IReadOnlyDictionary<Matrix.Population.Domain.ValueObjects.PersonId, PersonEntity> residentsById,
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
            DateOnly previousDate,
            DateOnly currentDate,
            CityEducationAutonomyPolicy educationAutonomyPolicy,
            CityEmploymentAutonomyPolicy employmentAutonomyPolicy,
            IDictionary<EducationLevel, List<Matrix.Population.Domain.ValueObjects.EducationInstitutionId>> institutionPools,
            IDictionary<string, List<Matrix.Population.Domain.ValueObjects.WorkplaceId>> workplacePools)
        {
            bool changed = false;

            if (!person.IsAlive)
                return false;

            if (educationAutonomyPolicy.Apply(
                    person: person,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    institutionPools: institutionPools))
                changed = true;

            if (employmentAutonomyPolicy.Apply(
                    person: person,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    workplacePools: workplacePools))
                changed = true;

            if (person.GetAgeGroup(currentDate) != AgeGroup.Senior)
                return changed;

            if (person.Employment.Status is not (EmploymentStatus.Employed or EmploymentStatus.Student))
                return changed;

            person.Retire(currentDate);
            return true;
        }

        private static bool ApplyWeatherExposure(
            PersonEntity person,
            IReadOnlyDictionary<Matrix.Population.Domain.ValueObjects.PersonId, PersonEntity> residentsById,
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

        private static async Task<int> ApplyCivilRegistryAutonomyAsync(
            CityId cityId,
            IReadOnlyDictionary<Matrix.Population.Domain.ValueObjects.PersonId, PersonEntity> residentsById,
            DateOnly previousDate,
            DateOnly currentDate,
            IHouseholdWriteRepository householdWriteRepository,
            MarriageDomainService marriageDomainService,
            CityCivilRegistryAutonomyPolicy civilRegistryAutonomyPolicy,
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
                if (!residentsById.TryGetValue(decision.FirstResidentId, out PersonEntity? firstResident) ||
                    !residentsById.TryGetValue(decision.SecondResidentId, out PersonEntity? secondResident))
                    continue;

                switch (decision.Type)
                {
                    case CityCivilRegistryAutonomyDecisionType.Marriage:
                    {
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

                        affectedResidents += 2;
                        break;
                    }
                    case CityCivilRegistryAutonomyDecisionType.Divorce:
                    {
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

                        affectedResidents += 2;
                        break;
                    }
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

                if (previousEnd > previousStart &&
                    CityWeatherExposureRules.IsAdverseExposureWeather(previousWeather))
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

        private static Dictionary<EducationLevel, List<Matrix.Population.Domain.ValueObjects.EducationInstitutionId>> BuildEducationInstitutionPools(
            IEnumerable<PersonEntity> persons)
        {
            var pools = new Dictionary<EducationLevel, List<Matrix.Population.Domain.ValueObjects.EducationInstitutionId>>();

            foreach (PersonEntity person in persons)
            {
                if (person.Education.CurrentInstitutionId is not { } institutionId)
                    continue;

                EducationLevel level = person.Education.Level;

                if (!pools.TryGetValue(level, out List<Matrix.Population.Domain.ValueObjects.EducationInstitutionId>? levelPool))
                {
                    levelPool = [];
                    pools[level] = levelPool;
                }

                if (!levelPool.Contains(institutionId))
                    levelPool.Add(institutionId);
            }

            return pools;
        }

        private static Dictionary<string, List<Matrix.Population.Domain.ValueObjects.WorkplaceId>> BuildWorkplacePools(
            IEnumerable<PersonEntity> persons)
        {
            var pools =
                new Dictionary<string, List<Matrix.Population.Domain.ValueObjects.WorkplaceId>>(StringComparer.OrdinalIgnoreCase);

            foreach (PersonEntity person in persons)
            {
                if (person.Employment.Status != EmploymentStatus.Employed || person.Employment.Job is not { } job)
                    continue;

                if (!pools.TryGetValue(job.Title, out List<Matrix.Population.Domain.ValueObjects.WorkplaceId>? titlePool))
                {
                    titlePool = [];
                    pools[job.Title] = titlePool;
                }

                if (!titlePool.Contains(job.WorkplaceId))
                    titlePool.Add(job.WorkplaceId);
            }

            return pools;
        }
    }
}
