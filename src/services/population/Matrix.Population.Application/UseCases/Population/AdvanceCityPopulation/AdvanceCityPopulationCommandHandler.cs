using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.UseCases.Population.AdvanceCityPopulation
{
    public sealed class AdvanceCityPopulationCommandHandler(
        IPersonWriteRepository personWriteRepository,
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        ICityPopulationEnvironmentRepository cityPopulationEnvironmentRepository,
        ICityPopulationProgressionStateRepository progressionStateRepository,
        ICityPopulationWeatherExposureStateRepository weatherExposureStateRepository,
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
            GuardHelper.Ensure(
                condition: request.FromSimTimeUtc.Offset == TimeSpan.Zero,
                value: request.FromSimTimeUtc,
                errorFactory: ApplicationErrorsFactory.TimestampMustBeUtc,
                propertyName: nameof(request.FromSimTimeUtc));

            GuardHelper.Ensure(
                condition: request.ToSimTimeUtc.Offset == TimeSpan.Zero,
                value: request.ToSimTimeUtc,
                errorFactory: ApplicationErrorsFactory.TimestampMustBeUtc,
                propertyName: nameof(request.ToSimTimeUtc));

            GuardHelper.AgainstNegativeNumber(
                value: request.TickId,
                errorFactory: ApplicationErrorsFactory.NumberMustNotBeNegative,
                propertyName: nameof(request.TickId));

            var cityId = CityId.From(request.CityId);
            DateOnly fromDate = DateOnly.FromDateTime(request.FromSimTimeUtc.UtcDateTime);
            DateOnly toDate = DateOnly.FromDateTime(request.ToSimTimeUtc.UtcDateTime);

            if (toDate < fromDate)
                throw ApplicationErrorsFactory.InvalidDateRange(
                    from: fromDate,
                    to: toDate,
                    fromName: nameof(request.FromSimTimeUtc),
                    toName: nameof(request.ToSimTimeUtc));

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
            CityPopulationWeatherExposureState? weatherExposureState = await weatherExposureStateRepository.GetByCityAsync(
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
            List<CityWeatherExposureSegment> exposureSegments = shouldAdvanceWeatherExposureCheckpoint && weatherExposureState is not null
                ? BuildExposureSegments(
                    weatherExposureState: weatherExposureState,
                    fromSimTimeUtc: request.FromSimTimeUtc,
                    toSimTimeUtc: request.ToSimTimeUtc)
                : [];
            bool requiresWeatherExposure = exposureSegments.Count > 0;

            if ((requiresDateProgression || requiresNeedsProgression || requiresWeatherExposure) && environment is null)
                logger.LogWarning(
                    message: "Advancing city population without synced environment for cityId={CityId}. Climate adaptation will be neutral and needs progression will use UTC fallback.",
                    request.CityId);

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    if (requiresDateProgression || requiresNeedsProgression || requiresWeatherExposure)
                    {
                        IReadOnlyCollection<PersonEntity> persons = await personWriteRepository.ListByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);

                        foreach (PersonEntity person in persons)
                            if (ApplyProgressionNeedsAndExposure(
                                person: person,
                                fromSimTimeUtc: request.FromSimTimeUtc,
                                toSimTimeUtc: request.ToSimTimeUtc,
                                currentDate: toDate,
                                requiresDateProgression: requiresDateProgression,
                                requiresNeedsProgression: requiresNeedsProgression,
                                environment: environment,
                                exposureSegments: exposureSegments,
                                personNeedsProgressionPolicy: personNeedsProgressionPolicy,
                                weatherExposurePolicy: weatherExposurePolicy))
                            affectedPeopleCount++;
                    }

                    DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;

                    if (state is null)
                    {
                        CityPopulationProgressionState newState = CityPopulationProgressionState.Create(
                            cityId: cityId,
                            lastProcessedTickId: request.TickId,
                            lastProcessedDate: toDate,
                            updatedAtUtc: updatedAtUtc);

                        await progressionStateRepository.AddAsync(
                            state: newState,
                            cancellationToken: ct);
                    }
                    else
                    {
                        state.MarkProcessed(
                            tickId: request.TickId,
                            processedDate: toDate,
                            updatedAtUtc: updatedAtUtc);
                    }

                    if (shouldAdvanceWeatherExposureCheckpoint && weatherExposureState is not null)
                        weatherExposureState.MarkExposureProcessed(
                            processedAtSimTimeUtc: request.ToSimTimeUtc,
                            updatedAtUtc: updatedAtUtc);

                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            return new AdvanceCityPopulationResult(
                Status: AdvanceCityPopulationStatus.Applied,
                AffectedPeopleCount: affectedPeopleCount);
        }

        private static bool ApplyProgressionNeedsAndExposure(
            PersonEntity person,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc,
            DateOnly currentDate,
            bool requiresDateProgression,
            bool requiresNeedsProgression,
            CityPopulationEnvironment? environment,
            IReadOnlyCollection<CityWeatherExposureSegment> exposureSegments,
            PersonNeedsProgressionPolicy personNeedsProgressionPolicy,
            CityPopulationWeatherExposurePolicy weatherExposurePolicy)
        {
            bool changed = false;

            if (requiresNeedsProgression &&
                ApplyNeedsProgression(
                    person: person,
                    fromSimTimeUtc: fromSimTimeUtc,
                    toSimTimeUtc: toSimTimeUtc,
                    currentDate: currentDate,
                    environment: environment,
                    personNeedsProgressionPolicy: personNeedsProgressionPolicy))
                changed = true;

            if (requiresDateProgression &&
                ApplyTimeProgression(
                    person: person,
                    currentDate: currentDate))
                changed = true;

            if (exposureSegments.Count > 0)
                if (ApplyWeatherExposure(
                        person: person,
                        currentDate: currentDate,
                        environment: environment,
                        exposureSegments: exposureSegments,
                        weatherExposurePolicy: weatherExposurePolicy))
                    changed = true;

            return changed;
        }

        private static bool ApplyNeedsProgression(
            PersonEntity person,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc,
            DateOnly currentDate,
            CityPopulationEnvironment? environment,
            PersonNeedsProgressionPolicy personNeedsProgressionPolicy)
        {
            int utcOffsetMinutes = environment?.UtcOffsetMinutes ?? 0;

            PersonNeedsProgressionEffect effect = personNeedsProgressionPolicy.Calculate(
                person: person,
                fromSimTimeUtc: fromSimTimeUtc,
                toSimTimeUtc: toSimTimeUtc,
                utcOffsetMinutes: utcOffsetMinutes);

            return person.ApplyNeedsProgression(
                effect: effect,
                currentDate: currentDate);
        }

        private static bool ApplyTimeProgression(
            PersonEntity person,
            DateOnly currentDate)
        {
            if (!person.IsAlive)
                return false;

            if (person.GetAgeGroup(currentDate) != AgeGroup.Senior)
                return false;

            if (person.Employment.Status is not (EmploymentStatus.Employed or EmploymentStatus.Student))
                return false;

            person.Retire(currentDate);
            return true;
        }

        private static bool ApplyWeatherExposure(
            PersonEntity person,
            DateOnly currentDate,
            CityPopulationEnvironment? environment,
            IReadOnlyCollection<CityWeatherExposureSegment> exposureSegments,
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
            }

            if (totalHappinessDelta != 0 && person.IsAlive)
            {
                int previousHappiness = person.Happiness.Value;

                person.ChangeHappiness(totalHappinessDelta);

                changed = changed || previousHappiness != person.Happiness.Value;
            }

            return changed;
        }

        private static bool ShouldAdvanceWeatherExposureCheckpoint(
            CityPopulationWeatherExposureState? weatherExposureState,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc)
        {
            if (weatherExposureState is null)
                return false;

            DateTimeOffset effectiveFrom = Max(
                fromSimTimeUtc,
                weatherExposureState.LastExposureProcessedAtSimTimeUtc);

            return toSimTimeUtc > effectiveFrom;
        }

        private static List<CityWeatherExposureSegment> BuildExposureSegments(
            CityPopulationWeatherExposureState weatherExposureState,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc)
        {
            var segments = new List<CityWeatherExposureSegment>();

            DateTimeOffset effectiveFrom = Max(
                fromSimTimeUtc,
                weatherExposureState.LastExposureProcessedAtSimTimeUtc);

            if (toSimTimeUtc <= effectiveFrom)
                return segments;

            if (weatherExposureState.HasPreviousWeather &&
                weatherExposureState.PreviousWeather is WeatherImpactProfile previousWeather &&
                weatherExposureState.PreviousWeatherEffectiveAtSimTimeUtc.HasValue &&
                effectiveFrom < weatherExposureState.CurrentWeatherEffectiveAtSimTimeUtc)
            {
                DateTimeOffset previousStart = Max(
                    effectiveFrom,
                    weatherExposureState.PreviousWeatherEffectiveAtSimTimeUtc.Value);
                DateTimeOffset previousEnd = Min(
                    toSimTimeUtc,
                    weatherExposureState.CurrentWeatherEffectiveAtSimTimeUtc);

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
                effectiveFrom,
                weatherExposureState.CurrentWeatherEffectiveAtSimTimeUtc);

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
                    currentStart,
                    weatherExposureState.RecoveryStartedAtSimTimeUtc.Value);

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
    }
}
