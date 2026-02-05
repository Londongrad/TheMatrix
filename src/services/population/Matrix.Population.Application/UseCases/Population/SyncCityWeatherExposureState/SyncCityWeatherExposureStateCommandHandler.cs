using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.UseCases.Population.ApplyCityWeatherImpact;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.Population.SyncCityWeatherExposureState
{
    public sealed class SyncCityWeatherExposureStateCommandHandler(
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
            if (request.CityId == Guid.Empty)
                throw new ArgumentException("CityId cannot be empty.", nameof(request.CityId));

            if (request.IntegrationMessageId == Guid.Empty)
                throw new ArgumentException("IntegrationMessageId cannot be empty.", nameof(request.IntegrationMessageId));

            if (string.IsNullOrWhiteSpace(request.ConsumerName))
                throw new ArgumentException("ConsumerName is required.", nameof(request.ConsumerName));

            if (request.AtSimTimeUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException("AtSimTimeUtc must be UTC.", nameof(request.AtSimTimeUtc));

            ArgumentNullException.ThrowIfNull(request.CurrentState);

            DateTimeOffset occurredOnUtc = NormalizeOccurredOnUtc(request.OccurredOnUtc);
            WeatherImpactProfile currentWeather = CreateWeatherImpactProfile(request.CurrentState);
            var cityId = CityId.From(request.CityId);

            return unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    bool markedAsProcessed = await processedIntegrationMessageRepository.TryMarkProcessedAsync(
                        consumer: request.ConsumerName,
                        messageId: request.IntegrationMessageId,
                        processedAtUtc: DateTimeOffset.UtcNow,
                        cancellationToken: ct);

                    if (!markedAsProcessed)
                        return new SyncCityWeatherExposureStateResult(
                            Status: SyncCityWeatherExposureStateStatus.Duplicate);

                    CityPopulationDeletionState? deletionState = await cityPopulationDeletionStateRepository.GetByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);

                    if (deletionState is not null)
                        return new SyncCityWeatherExposureStateResult(
                            Status: SyncCityWeatherExposureStateStatus.CityDeleted);

                    CityPopulationWeatherExposureState? state = await weatherExposureStateRepository.GetByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);

                    DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;

                    if (state is null)
                    {
                        CityPopulationWeatherExposureState newState = CityPopulationWeatherExposureState.Create(
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

                    return new SyncCityWeatherExposureStateResult(
                        Status: SyncCityWeatherExposureStateStatus.Applied);
                },
                cancellationToken: cancellationToken);
        }

        private static DateTimeOffset NormalizeOccurredOnUtc(DateTime occurredOnUtc)
        {
            return occurredOnUtc.Kind switch
            {
                DateTimeKind.Utc => new DateTimeOffset(occurredOnUtc),
                DateTimeKind.Unspecified => new DateTimeOffset(
                    DateTime.SpecifyKind(
                        value: occurredOnUtc,
                        kind: DateTimeKind.Utc)),
                _ => throw new ArgumentException("OccurredOnUtc must be UTC.", nameof(occurredOnUtc))
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
