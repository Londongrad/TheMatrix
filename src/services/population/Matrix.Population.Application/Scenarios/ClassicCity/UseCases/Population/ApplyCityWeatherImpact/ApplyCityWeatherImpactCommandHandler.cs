using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact
{
    public sealed class ApplyCityWeatherImpactCommandHandler(
        IPersonWriteRepository personWriteRepository,
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        ICityPopulationEnvironmentRepository cityPopulationEnvironmentRepository,
        ICityPopulationWeatherImpactStateRepository weatherImpactStateRepository,
        IProcessedIntegrationMessageRepository processedIntegrationMessageRepository,
        CityPopulationWeatherImpactPolicy weatherImpactPolicy,
        ILogger<ApplyCityWeatherImpactCommandHandler> logger,
        IUnitOfWork unitOfWork)
        : IRequestHandler<ApplyCityWeatherImpactCommand, ApplyCityWeatherImpactResult>
    {
        public Task<ApplyCityWeatherImpactResult> Handle(
            ApplyCityWeatherImpactCommand request,
            CancellationToken cancellationToken)
        {
            GuardHelper.AgainstEmptyGuid(
                id: request.CityId,
                errorFactory: ApplicationErrorsFactory.EmptyId,
                propertyName: nameof(request.CityId));
            GuardHelper.AgainstEmptyGuid(
                id: request.IntegrationMessageId,
                errorFactory: ApplicationErrorsFactory.EmptyId,
                propertyName: nameof(request.IntegrationMessageId));

            string consumerName = GuardHelper.AgainstNullOrWhiteSpace(
                value: request.ConsumerName,
                errorFactory: ApplicationErrorsFactory.Required,
                propertyName: nameof(request.ConsumerName));
            GuardHelper.Ensure(
                condition: request.AtSimTimeUtc.Offset == TimeSpan.Zero,
                value: request.AtSimTimeUtc,
                errorFactory: ApplicationErrorsFactory.TimestampMustBeUtc,
                propertyName: nameof(request.AtSimTimeUtc));

            WeatherImpactSnapshotInput previousState = GuardHelper.AgainstNull(
                value: request.PreviousState,
                errorFactory: ApplicationErrorsFactory.Required,
                propertyName: nameof(request.PreviousState));
            WeatherImpactSnapshotInput currentState = GuardHelper.AgainstNull(
                value: request.CurrentState,
                errorFactory: ApplicationErrorsFactory.Required,
                propertyName: nameof(request.CurrentState));

            DateTimeOffset occurredOnUtc = NormalizeOccurredOnUtc(request.OccurredOnUtc);
            var cityId = CityId.From(request.CityId);
            var currentDate = DateOnly.FromDateTime(request.AtSimTimeUtc.UtcDateTime);
            WeatherImpactProfile previousWeather = CreateWeatherImpactProfile(previousState);
            WeatherImpactProfile currentWeather = CreateWeatherImpactProfile(currentState);

            return unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    bool markedAsProcessed = await processedIntegrationMessageRepository.TryMarkProcessedAsync(
                        consumer: consumerName,
                        messageId: request.IntegrationMessageId,
                        processedAtUtc: DateTimeOffset.UtcNow,
                        cancellationToken: ct);

                    if (!markedAsProcessed)
                        return new ApplyCityWeatherImpactResult(
                            Status: ApplyCityWeatherImpactStatus.Duplicate,
                            AffectedPeopleCount: 0);

                    CityPopulationDeletionState? deletionState =
                        await cityPopulationDeletionStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);

                    if (deletionState is not null)
                        return new ApplyCityWeatherImpactResult(
                            Status: ApplyCityWeatherImpactStatus.CityDeleted,
                            AffectedPeopleCount: 0);

                    CityPopulationArchiveState? archiveState =
                        await cityPopulationArchiveStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);

                    if (archiveState is not null)
                        return new ApplyCityWeatherImpactResult(
                            Status: ApplyCityWeatherImpactStatus.CityArchived,
                            AffectedPeopleCount: 0);

                    CityPopulationWeatherImpactState? state = await weatherImpactStateRepository.GetByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);
                    CityPopulationEnvironment? environment = await cityPopulationEnvironmentRepository.GetByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);

                    if (environment is null)
                        logger.LogWarning(
                            message:
                            "Applying city weather impact without synced environment for cityId={CityId}. Climate adaptation will be neutral.",
                            request.CityId);

                    if (IsOutOfOrder(
                            state: state,
                            atSimTimeUtc: request.AtSimTimeUtc,
                            occurredOnUtc: occurredOnUtc))
                        return new ApplyCityWeatherImpactResult(
                            Status: ApplyCityWeatherImpactStatus.OutOfOrder,
                            AffectedPeopleCount: 0);

                    int affectedPeopleCount = 0;
                    IReadOnlyCollection<PersonEntity> persons = await personWriteRepository.ListByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);

                    foreach (PersonEntity person in persons)
                        if (ApplyWeatherImpact(
                                person: person,
                                currentDate: currentDate,
                                previousWeather: previousWeather,
                                currentWeather: currentWeather,
                                environment: environment,
                                weatherImpactPolicy: weatherImpactPolicy))
                            affectedPeopleCount++;

                    DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;

                    if (state is null)
                    {
                        var newState = CityPopulationWeatherImpactState.Create(
                            cityId: cityId,
                            lastAppliedAtSimTimeUtc: request.AtSimTimeUtc,
                            lastAppliedOccurredOnUtc: occurredOnUtc,
                            updatedAtUtc: updatedAtUtc);

                        await weatherImpactStateRepository.AddAsync(
                            state: newState,
                            cancellationToken: ct);
                    }
                    else
                        state.MarkApplied(
                            atSimTimeUtc: request.AtSimTimeUtc,
                            occurredOnUtc: occurredOnUtc,
                            updatedAtUtc: updatedAtUtc);

                    await unitOfWork.SaveChangesAsync(ct);

                    return new ApplyCityWeatherImpactResult(
                        Status: ApplyCityWeatherImpactStatus.Applied,
                        AffectedPeopleCount: affectedPeopleCount);
                },
                cancellationToken: cancellationToken);
        }

        private static bool ApplyWeatherImpact(
            PersonEntity person,
            DateOnly currentDate,
            WeatherImpactProfile previousWeather,
            WeatherImpactProfile currentWeather,
            CityPopulationEnvironment? environment,
            CityPopulationWeatherImpactPolicy weatherImpactPolicy)
        {
            PersonWeatherImpact impact = weatherImpactPolicy.CalculateDifferential(
                person: person,
                currentDate: currentDate,
                previousWeather: previousWeather,
                currentWeather: currentWeather,
                environment: environment);

            if (!impact.HasEffect)
                return false;

            bool changed = false;

            if (impact.HealthDelta != 0)
            {
                int previousHealth = person.Health.Value;
                bool wasAlive = person.IsAlive;

                person.ChangeHealth(
                    delta: impact.HealthDelta,
                    currentDate: currentDate);

                changed = previousHealth != person.Health.Value || wasAlive != person.IsAlive;
            }

            if (impact.HappinessDelta != 0 && person.IsAlive)
            {
                int previousHappiness = person.Happiness.Value;

                person.ChangeHappiness(impact.HappinessDelta);

                changed = changed || previousHappiness != person.Happiness.Value;
            }

            return changed;
        }

        private static WeatherImpactProfile CreateWeatherImpactProfile(WeatherImpactSnapshotInput snapshot)
        {
            return new WeatherImpactProfile(
                Type: ParseWeatherType(snapshot.Type),
                Severity: ParseWeatherSeverity(snapshot.Severity),
                PrecipitationKind: ParsePrecipitationKind(snapshot.PrecipitationKind),
                TemperatureC: snapshot.TemperatureC,
                HumidityPercent: snapshot.HumidityPercent,
                WindSpeedKph: snapshot.WindSpeedKph,
                CloudCoveragePercent: snapshot.CloudCoveragePercent,
                PressureHpa: snapshot.PressureHpa);
        }

        private static bool IsOutOfOrder(
            CityPopulationWeatherImpactState? state,
            DateTimeOffset atSimTimeUtc,
            DateTimeOffset occurredOnUtc)
        {
            if (state is null)
                return false;

            if (atSimTimeUtc < state.LastAppliedAtSimTimeUtc)
                return true;

            return atSimTimeUtc == state.LastAppliedAtSimTimeUtc &&
                   occurredOnUtc <= state.LastAppliedOccurredOnUtc;
        }

        private static DateTimeOffset NormalizeOccurredOnUtc(DateTime occurredOnUtc)
        {
            GuardHelper.Ensure(
                condition: occurredOnUtc.Kind is DateTimeKind.Utc or DateTimeKind.Unspecified,
                value: occurredOnUtc,
                errorFactory: ApplicationErrorsFactory.TimestampMustBeUtc);

            return occurredOnUtc.Kind switch
            {
                DateTimeKind.Utc => new DateTimeOffset(occurredOnUtc),
                DateTimeKind.Unspecified => new DateTimeOffset(
                    DateTime.SpecifyKind(
                        value: occurredOnUtc,
                        kind: DateTimeKind.Utc)),
                _ => throw ApplicationErrorsFactory.TimestampMustBeUtc(
                    value: occurredOnUtc,
                    propertyName: nameof(occurredOnUtc))
            };
        }

        private static PopulationWeatherType ParseWeatherType(string value)
        {
            return Enum.TryParse(
                value: value,
                ignoreCase: true,
                result: out PopulationWeatherType parsed)
                ? parsed
                : PopulationWeatherType.Unknown;
        }

        private static PopulationWeatherSeverity ParseWeatherSeverity(string value)
        {
            return Enum.TryParse(
                value: value,
                ignoreCase: true,
                result: out PopulationWeatherSeverity parsed)
                ? parsed
                : PopulationWeatherSeverity.Unknown;
        }

        private static PopulationPrecipitationKind ParsePrecipitationKind(string value)
        {
            return Enum.TryParse(
                value: value,
                ignoreCase: true,
                result: out PopulationPrecipitationKind parsed)
                ? parsed
                : PopulationPrecipitationKind.Unknown;
        }
    }
}
