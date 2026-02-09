using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.UseCases.Population.ApplyCityWeatherImpact;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.Population.SyncCityWeatherExposureState
{
    public sealed class SyncCityWeatherExposureStateCommandHandler(
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        ICityPopulationWeatherExposureStateRepository weatherExposureStateRepository,
        IProcessedIntegrationMessageRepository processedIntegrationMessageRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<SyncCityWeatherExposureStateCommand, SyncCityWeatherExposureStateResult>
    {
        public Task<SyncCityWeatherExposureStateResult> Handle(
            SyncCityWeatherExposureStateCommand request,
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

            WeatherImpactSnapshotInput currentState = GuardHelper.AgainstNull(
                value: request.CurrentState,
                errorFactory: ApplicationErrorsFactory.Required,
                propertyName: nameof(request.CurrentState));

            DateTimeOffset occurredOnUtc = NormalizeOccurredOnUtc(request.OccurredOnUtc);
            WeatherImpactProfile currentWeather = CreateWeatherImpactProfile(currentState);
            var cityId = CityId.From(request.CityId);

            return unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    bool markedAsProcessed = await processedIntegrationMessageRepository.TryMarkProcessedAsync(
                        consumer: consumerName,
                        messageId: request.IntegrationMessageId,
                        processedAtUtc: DateTimeOffset.UtcNow,
                        cancellationToken: ct);

                    if (!markedAsProcessed)
                        return new SyncCityWeatherExposureStateResult(
                            Status: SyncCityWeatherExposureStateStatus.Duplicate);

                    CityPopulationDeletionState? deletionState =
                        await cityPopulationDeletionStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);

                    if (deletionState is not null)
                        return new SyncCityWeatherExposureStateResult(
                            Status: SyncCityWeatherExposureStateStatus.CityDeleted);

                    CityPopulationArchiveState? archiveState =
                        await cityPopulationArchiveStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);

                    if (archiveState is not null)
                        return new SyncCityWeatherExposureStateResult(
                            Status: SyncCityWeatherExposureStateStatus.CityArchived);

                    CityPopulationWeatherExposureState? state = await weatherExposureStateRepository.GetByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);

                    DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;

                    if (state is null)
                    {
                        var newState = CityPopulationWeatherExposureState.Create(
                            cityId: cityId,
                            currentWeather: currentWeather,
                            currentWeatherEffectiveAtSimTimeUtc: request.AtSimTimeUtc,
                            occurredOnUtc: occurredOnUtc,
                            updatedAtUtc: updatedAtUtc);

                        await weatherExposureStateRepository.AddAsync(
                            state: newState,
                            cancellationToken: ct);
                        await unitOfWork.SaveChangesAsync(ct);

                        return new SyncCityWeatherExposureStateResult(
                            Status: SyncCityWeatherExposureStateStatus.Applied);
                    }

                    if (!state.CanApplyWeatherUpdate(
                            atSimTimeUtc: request.AtSimTimeUtc,
                            occurredOnUtc: occurredOnUtc))
                        return new SyncCityWeatherExposureStateResult(
                            Status: SyncCityWeatherExposureStateStatus.OutOfOrder);

                    state.ApplyWeatherUpdate(
                        currentWeather: currentWeather,
                        currentWeatherEffectiveAtSimTimeUtc: request.AtSimTimeUtc,
                        occurredOnUtc: occurredOnUtc,
                        updatedAtUtc: updatedAtUtc);

                    await unitOfWork.SaveChangesAsync(ct);

                    return new SyncCityWeatherExposureStateResult(Status: SyncCityWeatherExposureStateStatus.Applied);
                },
                cancellationToken: cancellationToken);
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
