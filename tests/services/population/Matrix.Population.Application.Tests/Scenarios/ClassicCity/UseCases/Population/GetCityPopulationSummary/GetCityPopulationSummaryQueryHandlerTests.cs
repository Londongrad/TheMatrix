using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary;

public sealed class GetCityPopulationSummaryQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenSummaryDoesNotExist_ReturnsNullAfterEnsuringProjectionExists()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityId domainCityId = CityId.From(cityId);
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var summaryReadRepository = new FakeCityPopulationSummaryReadRepository();
        var handler = new GetCityPopulationSummaryQueryHandler(summaryProjectionService, summaryReadRepository);

        CityPopulationSummaryDto? result = await handler.Handle(new GetCityPopulationSummaryQuery(cityId), CancellationToken.None);

        Assert.Null(result);
        Assert.Single(summaryProjectionService.EnsuredCityIds);
        Assert.Equal(domainCityId, summaryProjectionService.EnsuredCityIds[0]);
        Assert.Equal(domainCityId, summaryReadRepository.RequestedCityId);
    }

    [Fact]
    public async Task Handle_WhenSummaryExists_MapsOptionalSectionsAndRoundsMetrics()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        DateTimeOffset archivedAtUtc = new(2048, 4, 29, 1, 2, 3, TimeSpan.Zero);
        DateTimeOffset deletedAtUtc = new(2048, 4, 30, 4, 5, 6, TimeSpan.Zero);
        DateTimeOffset environmentUpdatedAtUtc = new(2048, 5, 1, 7, 8, 9, TimeSpan.Zero);
        DateTimeOffset simulationUpdatedAtUtc = new(2048, 5, 2, 10, 11, 12, TimeSpan.Zero);
        DateTimeOffset weatherEffectiveAtUtc = new(2048, 5, 3, 13, 14, 15, TimeSpan.Zero);
        DateTimeOffset lastWeatherOccurredOnUtc = new(2048, 5, 3, 14, 15, 16, TimeSpan.Zero);
        DateTimeOffset lastExposureProcessedAtUtc = new(2048, 5, 3, 15, 16, 17, TimeSpan.Zero);
        DateTimeOffset lastWeatherImpactAppliedAtUtc = new(2048, 5, 3, 16, 17, 18, TimeSpan.Zero);
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var summaryReadRepository = new FakeCityPopulationSummaryReadRepository
        {
            Summary = new CityPopulationSummaryReadModel(
                CityId: cityId,
                CurrentDate: new DateOnly(2048, 5, 3),
                IsArchived: true,
                ArchivedAtUtc: archivedAtUtc,
                IsDeleted: true,
                DeletedAtUtc: deletedAtUtc,
                ClimateZone: PopulationClimateZone.Continental,
                Hemisphere: PopulationHemisphere.Northern,
                UtcOffsetMinutes: 180,
                EnvironmentUpdatedAtUtc: environmentUpdatedAtUtc,
                LastProcessedTickId: 42,
                LastProcessedDate: new DateOnly(2048, 5, 2),
                SimulationUpdatedAtUtc: simulationUpdatedAtUtc,
                CurrentWeatherType: PopulationWeatherType.Storm,
                CurrentWeatherSeverity: PopulationWeatherSeverity.Severe,
                IsWeatherRecoveryActive: true,
                CurrentWeatherEffectiveAtSimTimeUtc: weatherEffectiveAtUtc,
                LastWeatherOccurredOnUtc: lastWeatherOccurredOnUtc,
                LastExposureProcessedAtSimTimeUtc: lastExposureProcessedAtUtc,
                LastWeatherImpactAppliedAtSimTimeUtc: lastWeatherImpactAppliedAtUtc,
                HouseholdCount: 34,
                HousedHouseholdCount: 30,
                HomelessHouseholdCount: 4,
                ResidentCount: 120,
                DeceasedCount: 5,
                HousedResidentCount: 110,
                HomelessResidentCount: 10,
                ChildCount: 12,
                YouthCount: 18,
                AdultCount: 70,
                SeniorCount: 20,
                EmployedCount: 61,
                StudentCount: 16,
                UnemployedCount: 23,
                RetiredCount: 20,
                AverageHealth: 70.125m,
                AverageHappiness: 64.994m,
                AverageEnergy: 58.235m,
                AverageStress: 31.005m,
                AverageSocialNeed: 22.444m,
                ActiveIllnessCount: 11,
                SevereIllnessCount: 3,
                MedicalLoadIndex: 0.335m,
                TriagePressureIndex: 0.665m,
                RecoverySupportIndex: 0.995m,
                WorkforceCommuteAccessibilityIndex: 0.784m,
                WorkforceAttendanceIndex: 0.671m,
                WorkforceProductivityIndex: 0.555m,
                StudentCommuteAccessibilityIndex: 0.501m,
                StudentAttendanceIndex: null)
        };
        var handler = new GetCityPopulationSummaryQueryHandler(summaryProjectionService, summaryReadRepository);

        CityPopulationSummaryDto result = Assert.IsType<CityPopulationSummaryDto>(
            await handler.Handle(new GetCityPopulationSummaryQuery(cityId), CancellationToken.None));

        Assert.Equal(cityId, result.CityId);
        Assert.Equal("2048-05-03", result.CurrentDate);
        Assert.Equal(archivedAtUtc.ToString("O"), result.Lifecycle.ArchivedAtUtc);
        Assert.Equal(deletedAtUtc.ToString("O"), result.Lifecycle.DeletedAtUtc);
        Assert.NotNull(result.Environment);
        Assert.Equal("Continental", result.Environment!.ClimateZone);
        Assert.Equal("Northern", result.Environment.Hemisphere);
        Assert.Equal(180, result.Environment.UtcOffsetMinutes);
        Assert.Equal(environmentUpdatedAtUtc.ToString("O"), result.Environment.UpdatedAtUtc);
        Assert.NotNull(result.Simulation);
        Assert.Equal(42, result.Simulation!.LastProcessedTickId);
        Assert.Equal("2048-05-02", result.Simulation.LastProcessedDate);
        Assert.Equal(simulationUpdatedAtUtc.ToString("O"), result.Simulation.UpdatedAtUtc);
        Assert.NotNull(result.Weather);
        Assert.Equal("Storm", result.Weather!.CurrentType);
        Assert.Equal("Severe", result.Weather.CurrentSeverity);
        Assert.True(result.Weather.IsRecoveryActive);
        Assert.Equal(weatherEffectiveAtUtc.ToString("O"), result.Weather.CurrentWeatherEffectiveAtSimTimeUtc);
        Assert.Equal(lastWeatherOccurredOnUtc.ToString("O"), result.Weather.LastWeatherOccurredOnUtc);
        Assert.Equal(lastExposureProcessedAtUtc.ToString("O"), result.Weather.LastExposureProcessedAtSimTimeUtc);
        Assert.Equal(lastWeatherImpactAppliedAtUtc.ToString("O"), result.Weather.LastWeatherImpactAppliedAtSimTimeUtc);
        Assert.Equal(34, result.Housing.HouseholdCount);
        Assert.Equal(30, result.Housing.HousedHouseholdCount);
        Assert.Equal(4, result.Housing.HomelessHouseholdCount);
        Assert.Equal(120, result.Residents.ResidentCount);
        Assert.Equal(70.13m, result.Residents.AverageHealth);
        Assert.Equal(64.99m, result.Residents.AverageHappiness);
        Assert.Equal(58.24m, result.Residents.AverageEnergy);
        Assert.Equal(31.01m, result.Residents.AverageStress);
        Assert.Equal(22.44m, result.Residents.AverageSocialNeed);
        Assert.Equal(0.34m, result.Residents.MedicalLoadIndex);
        Assert.Equal(0.67m, result.Residents.TriagePressureIndex);
        Assert.Equal(1.00m, result.Residents.RecoverySupportIndex);
        Assert.Equal(0.78m, result.Residents.WorkforceCommuteAccessibilityIndex);
        Assert.Equal(0.67m, result.Residents.WorkforceAttendanceIndex);
        Assert.Equal(0.56m, result.Residents.WorkforceProductivityIndex);
        Assert.Equal(0.50m, result.Residents.StudentCommuteAccessibilityIndex);
        Assert.Null(result.Residents.StudentAttendanceIndex);
    }
}
