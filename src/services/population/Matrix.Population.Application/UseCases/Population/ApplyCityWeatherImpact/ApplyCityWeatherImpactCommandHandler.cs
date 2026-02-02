using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using MediatR;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.UseCases.Population.ApplyCityWeatherImpact
{
    public sealed class ApplyCityWeatherImpactCommandHandler(
        IPersonWriteRepository personWriteRepository,
        ICityPopulationWeatherImpactStateRepository weatherImpactStateRepository,
        IProcessedIntegrationMessageRepository processedIntegrationMessageRepository,
        CityPopulationWeatherImpactPolicy weatherImpactPolicy,
        IUnitOfWork unitOfWork)
        : IRequestHandler<ApplyCityWeatherImpactCommand, ApplyCityWeatherImpactResult>
    {
        public Task<ApplyCityWeatherImpactResult> Handle(
            ApplyCityWeatherImpactCommand request,
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

            DateTimeOffset occurredOnUtc = NormalizeOccurredOnUtc(request.OccurredOnUtc);
            var cityId = CityId.From(request.CityId);
            DateOnly currentDate = DateOnly.FromDateTime(request.AtSimTimeUtc.UtcDateTime);
            WeatherImpactProfile weather = CreateWeatherImpactProfile(request);

            return unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    bool markedAsProcessed = await processedIntegrationMessageRepository.TryMarkProcessedAsync(
                        consumer: request.ConsumerName,
                        messageId: request.IntegrationMessageId,
                        processedAtUtc: DateTimeOffset.UtcNow,
                        cancellationToken: ct);

                    if (!markedAsProcessed)
                        return new ApplyCityWeatherImpactResult(
                            Status: ApplyCityWeatherImpactStatus.Duplicate,
                            AffectedPeopleCount: 0);

                    CityPopulationWeatherImpactState? state = await weatherImpactStateRepository.GetByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);

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
                                weather: weather,
                                weatherImpactPolicy: weatherImpactPolicy))
                            affectedPeopleCount++;

                    DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;

                    if (state is null)
                    {
                        CityPopulationWeatherImpactState newState = CityPopulationWeatherImpactState.Create(
                            cityId: cityId,
                            lastAppliedAtSimTimeUtc: request.AtSimTimeUtc,
                            lastAppliedOccurredOnUtc: occurredOnUtc,
                            updatedAtUtc: updatedAtUtc);

                        await weatherImpactStateRepository.AddAsync(
                            state: newState,
                            cancellationToken: ct);
                    }
                    else
                    {
                        state.MarkApplied(
                            atSimTimeUtc: request.AtSimTimeUtc,
                            occurredOnUtc: occurredOnUtc,
                            updatedAtUtc: updatedAtUtc);
                    }

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
            WeatherImpactProfile weather,
            CityPopulationWeatherImpactPolicy weatherImpactPolicy)
        {
            PersonWeatherImpact impact = weatherImpactPolicy.Calculate(
                person: person,
                currentDate: currentDate,
                weather: weather);

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

        private static WeatherImpactProfile CreateWeatherImpactProfile(ApplyCityWeatherImpactCommand request)
        {
            return new WeatherImpactProfile(
                Type: ParseWeatherType(request.WeatherType),
                Severity: ParseWeatherSeverity(request.WeatherSeverity),
                PrecipitationKind: ParsePrecipitationKind(request.PrecipitationKind),
                TemperatureC: request.TemperatureC,
                HumidityPercent: request.HumidityPercent,
                WindSpeedKph: request.WindSpeedKph,
                CloudCoveragePercent: request.CloudCoveragePercent,
                PressureHpa: request.PressureHpa);
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
