using System.Globalization;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.Population.GetCityPopulationSummary
{
    public sealed class GetCityPopulationSummaryQueryHandler(
        ICityPopulationSummaryReadRepository summaryReadRepository)
        : IRequestHandler<GetCityPopulationSummaryQuery, CityPopulationSummaryDto?>
    {
        public async Task<CityPopulationSummaryDto?> Handle(
            GetCityPopulationSummaryQuery request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            CityPopulationSummaryReadModel? summary = await summaryReadRepository.GetByCityIdAsync(
                cityId: CityId.From(request.CityId),
                currentDate: request.CurrentDate,
                cancellationToken: cancellationToken);

            if (summary is null)
                return null;

            CityPopulationSummaryLifecycleDto lifecycle = new(
                IsArchived: summary.IsArchived,
                ArchivedAtUtc: FormatTimestamp(summary.ArchivedAtUtc),
                IsDeleted: summary.IsDeleted,
                DeletedAtUtc: FormatTimestamp(summary.DeletedAtUtc));

            CityPopulationSummaryEnvironmentDto? environment = null;
            if (summary.ClimateZone.HasValue &&
                summary.Hemisphere.HasValue &&
                summary.UtcOffsetMinutes.HasValue &&
                summary.EnvironmentUpdatedAtUtc.HasValue)
            {
                environment = new CityPopulationSummaryEnvironmentDto(
                    ClimateZone: summary.ClimateZone.Value.ToString(),
                    Hemisphere: summary.Hemisphere.Value.ToString(),
                    UtcOffsetMinutes: summary.UtcOffsetMinutes.Value,
                    UpdatedAtUtc: FormatTimestamp(summary.EnvironmentUpdatedAtUtc) !);
            }

            CityPopulationSummarySimulationDto? simulation = null;
            if (summary.LastProcessedTickId.HasValue &&
                summary.LastProcessedDate.HasValue &&
                summary.SimulationUpdatedAtUtc.HasValue)
            {
                simulation = new CityPopulationSummarySimulationDto(
                    LastProcessedTickId: summary.LastProcessedTickId.Value,
                    LastProcessedDate: FormatDate(summary.LastProcessedDate.Value),
                    UpdatedAtUtc: FormatTimestamp(summary.SimulationUpdatedAtUtc) !);
            }

            CityPopulationSummaryWeatherDto? weather = null;
            if (summary.CurrentWeatherType.HasValue &&
                summary.CurrentWeatherSeverity.HasValue &&
                summary.CurrentWeatherEffectiveAtSimTimeUtc.HasValue &&
                summary.LastWeatherOccurredOnUtc.HasValue &&
                summary.LastExposureProcessedAtSimTimeUtc.HasValue)
            {
                weather = new CityPopulationSummaryWeatherDto(
                    CurrentType: summary.CurrentWeatherType.Value.ToString(),
                    CurrentSeverity: summary.CurrentWeatherSeverity.Value.ToString(),
                    IsRecoveryActive: summary.IsWeatherRecoveryActive,
                    CurrentWeatherEffectiveAtSimTimeUtc: FormatTimestamp(summary.CurrentWeatherEffectiveAtSimTimeUtc) !,
                    LastWeatherOccurredOnUtc: FormatTimestamp(summary.LastWeatherOccurredOnUtc) !,
                    LastExposureProcessedAtSimTimeUtc: FormatTimestamp(summary.LastExposureProcessedAtSimTimeUtc) !,
                    LastWeatherImpactAppliedAtSimTimeUtc: FormatTimestamp(summary.LastWeatherImpactAppliedAtSimTimeUtc));
            }

            CityPopulationSummaryHousingDto housing = new(
                HouseholdCount: summary.HouseholdCount,
                HousedHouseholdCount: summary.HousedHouseholdCount,
                HomelessHouseholdCount: summary.HomelessHouseholdCount);

            CityPopulationSummaryResidentsDto residents = new(
                ResidentCount: summary.ResidentCount,
                DeceasedCount: summary.DeceasedCount,
                HousedResidentCount: summary.HousedResidentCount,
                HomelessResidentCount: summary.HomelessResidentCount,
                ChildCount: summary.ChildCount,
                YouthCount: summary.YouthCount,
                AdultCount: summary.AdultCount,
                SeniorCount: summary.SeniorCount,
                EmployedCount: summary.EmployedCount,
                StudentCount: summary.StudentCount,
                UnemployedCount: summary.UnemployedCount,
                RetiredCount: summary.RetiredCount,
                AverageHealth: RoundMetric(summary.AverageHealth),
                AverageHappiness: RoundMetric(summary.AverageHappiness));

            return new CityPopulationSummaryDto(
                CityId: summary.CityId,
                CurrentDate: FormatDate(summary.CurrentDate),
                Lifecycle: lifecycle,
                Environment: environment,
                Simulation: simulation,
                Weather: weather,
                Housing: housing,
                Residents: residents);
        }

        private static string FormatDate(DateOnly value)
        {
            return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static string? FormatTimestamp(DateTimeOffset? value)
        {
            return value?.ToString("O", CultureInfo.InvariantCulture);
        }

        private static decimal? RoundMetric(decimal? value)
        {
            return value.HasValue
                ? decimal.Round(value.Value, 2, MidpointRounding.AwayFromZero)
                : null;
        }
    }
}
