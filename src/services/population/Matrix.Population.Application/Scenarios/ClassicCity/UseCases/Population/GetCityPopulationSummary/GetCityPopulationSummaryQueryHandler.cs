using System.Globalization;
using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary
{
    public sealed class GetCityPopulationSummaryQueryHandler(ICityPopulationSummaryReadRepository summaryReadRepository)
        : IRequestHandler<GetCityPopulationSummaryQuery, CityPopulationSummaryDto?>
    {
        public async Task<CityPopulationSummaryDto?> Handle(
            GetCityPopulationSummaryQuery request,
            CancellationToken cancellationToken)
        {
            request = GuardHelper.AgainstNull(
                value: request,
                errorFactory: ApplicationErrorsFactory.Required);

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
                environment = new CityPopulationSummaryEnvironmentDto(
                    ClimateZone: summary.ClimateZone.Value.ToString(),
                    Hemisphere: summary.Hemisphere.Value.ToString(),
                    UtcOffsetMinutes: summary.UtcOffsetMinutes.Value,
                    UpdatedAtUtc: FormatTimestamp(summary.EnvironmentUpdatedAtUtc) !);

            CityPopulationSummarySimulationDto? simulation = null;
            if (summary.LastProcessedTickId.HasValue &&
                summary.LastProcessedDate.HasValue &&
                summary.SimulationUpdatedAtUtc.HasValue)
                simulation = new CityPopulationSummarySimulationDto(
                    LastProcessedTickId: summary.LastProcessedTickId.Value,
                    LastProcessedDate: FormatDate(summary.LastProcessedDate.Value),
                    UpdatedAtUtc: FormatTimestamp(summary.SimulationUpdatedAtUtc) !);

            CityPopulationSummaryWeatherDto? weather = null;
            if (summary.CurrentWeatherType.HasValue &&
                summary.CurrentWeatherSeverity.HasValue &&
                summary.CurrentWeatherEffectiveAtSimTimeUtc.HasValue &&
                summary.LastWeatherOccurredOnUtc.HasValue &&
                summary.LastExposureProcessedAtSimTimeUtc.HasValue)
                weather = new CityPopulationSummaryWeatherDto(
                    CurrentType: summary.CurrentWeatherType.Value.ToString(),
                    CurrentSeverity: summary.CurrentWeatherSeverity.Value.ToString(),
                    IsRecoveryActive: summary.IsWeatherRecoveryActive,
                    CurrentWeatherEffectiveAtSimTimeUtc: FormatTimestamp(summary.CurrentWeatherEffectiveAtSimTimeUtc) !,
                    LastWeatherOccurredOnUtc: FormatTimestamp(summary.LastWeatherOccurredOnUtc) !,
                    LastExposureProcessedAtSimTimeUtc: FormatTimestamp(summary.LastExposureProcessedAtSimTimeUtc) !,
                    LastWeatherImpactAppliedAtSimTimeUtc: FormatTimestamp(
                        summary.LastWeatherImpactAppliedAtSimTimeUtc));

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
                AverageHappiness: RoundMetric(summary.AverageHappiness),
                AverageEnergy: RoundMetric(summary.AverageEnergy),
                AverageStress: RoundMetric(summary.AverageStress),
                AverageSocialNeed: RoundMetric(summary.AverageSocialNeed));

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
            return value.ToString(
                format: "yyyy-MM-dd",
                provider: CultureInfo.InvariantCulture);
        }

        private static string? FormatTimestamp(DateTimeOffset? value)
        {
            return value?.ToString(
                format: "O",
                formatProvider: CultureInfo.InvariantCulture);
        }

        private static decimal? RoundMetric(decimal? value)
        {
            return value.HasValue
                ? decimal.Round(
                    d: value.Value,
                    decimals: 2,
                    mode: MidpointRounding.AwayFromZero)
                : null;
        }
    }
}
