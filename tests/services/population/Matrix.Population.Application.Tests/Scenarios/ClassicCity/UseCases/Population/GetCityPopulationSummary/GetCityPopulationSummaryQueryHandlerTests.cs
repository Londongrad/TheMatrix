using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary
{
    public sealed class GetCityPopulationSummaryQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenSummaryDoesNotExist_ReturnsNullAfterEnsuringProjectionExists()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var domainCityId = CityId.From(cityId);
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var summaryReadRepository = new FakeCityPopulationSummaryReadRepository();
            var handler = new GetCityPopulationSummaryQueryHandler(
                summaryProjectionService: summaryProjectionService,
                summaryReadRepository: summaryReadRepository);

            CityPopulationSummaryDto? result = await handler.Handle(
                request: new GetCityPopulationSummaryQuery(cityId),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Single(summaryProjectionService.EnsuredCityIds);
            Assert.Equal(
                expected: domainCityId,
                actual: summaryProjectionService.EnsuredCityIds[0]);
            Assert.Equal(
                expected: domainCityId,
                actual: summaryReadRepository.RequestedCityId);
        }

        [Fact]
        public async Task Handle_WhenSummaryExists_MapsOptionalSectionsAndRoundsMetrics()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            DateTimeOffset archivedAtUtc = new(
                year: 2048,
                month: 4,
                day: 29,
                hour: 1,
                minute: 2,
                second: 3,
                offset: TimeSpan.Zero);
            DateTimeOffset deletedAtUtc = new(
                year: 2048,
                month: 4,
                day: 30,
                hour: 4,
                minute: 5,
                second: 6,
                offset: TimeSpan.Zero);
            DateTimeOffset environmentUpdatedAtUtc = new(
                year: 2048,
                month: 5,
                day: 1,
                hour: 7,
                minute: 8,
                second: 9,
                offset: TimeSpan.Zero);
            DateTimeOffset simulationUpdatedAtUtc = new(
                year: 2048,
                month: 5,
                day: 2,
                hour: 10,
                minute: 11,
                second: 12,
                offset: TimeSpan.Zero);
            DateTimeOffset weatherEffectiveAtUtc = new(
                year: 2048,
                month: 5,
                day: 3,
                hour: 13,
                minute: 14,
                second: 15,
                offset: TimeSpan.Zero);
            DateTimeOffset lastWeatherOccurredOnUtc = new(
                year: 2048,
                month: 5,
                day: 3,
                hour: 14,
                minute: 15,
                second: 16,
                offset: TimeSpan.Zero);
            DateTimeOffset lastExposureProcessedAtUtc = new(
                year: 2048,
                month: 5,
                day: 3,
                hour: 15,
                minute: 16,
                second: 17,
                offset: TimeSpan.Zero);
            DateTimeOffset lastWeatherImpactAppliedAtUtc = new(
                year: 2048,
                month: 5,
                day: 3,
                hour: 16,
                minute: 17,
                second: 18,
                offset: TimeSpan.Zero);
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var summaryReadRepository = new FakeCityPopulationSummaryReadRepository
            {
                Summary = new CityPopulationSummaryReadModel(
                    CityId: cityId,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 3),
                    IsArchived: true,
                    ArchivedAtUtc: archivedAtUtc,
                    IsDeleted: true,
                    DeletedAtUtc: deletedAtUtc,
                    ClimateZone: PopulationClimateZone.Continental,
                    Hemisphere: PopulationHemisphere.Northern,
                    UtcOffsetMinutes: 180,
                    EnvironmentUpdatedAtUtc: environmentUpdatedAtUtc,
                    LastProcessedTickId: 42,
                    LastProcessedDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 2),
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
            var handler = new GetCityPopulationSummaryQueryHandler(
                summaryProjectionService: summaryProjectionService,
                summaryReadRepository: summaryReadRepository);

            CityPopulationSummaryDto result = Assert.IsType<CityPopulationSummaryDto>(
                await handler.Handle(
                    request: new GetCityPopulationSummaryQuery(cityId),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: cityId,
                actual: result.CityId);
            Assert.Equal(
                expected: "2048-05-03",
                actual: result.CurrentDate);
            Assert.Equal(
                expected: archivedAtUtc.ToString("O"),
                actual: result.Lifecycle.ArchivedAtUtc);
            Assert.Equal(
                expected: deletedAtUtc.ToString("O"),
                actual: result.Lifecycle.DeletedAtUtc);
            Assert.NotNull(result.Environment);
            Assert.Equal(
                expected: "Continental",
                actual: result.Environment!.ClimateZone);
            Assert.Equal(
                expected: "Northern",
                actual: result.Environment.Hemisphere);
            Assert.Equal(
                expected: 180,
                actual: result.Environment.UtcOffsetMinutes);
            Assert.Equal(
                expected: environmentUpdatedAtUtc.ToString("O"),
                actual: result.Environment.UpdatedAtUtc);
            Assert.NotNull(result.Simulation);
            Assert.Equal(
                expected: 42,
                actual: result.Simulation!.LastProcessedTickId);
            Assert.Equal(
                expected: "2048-05-02",
                actual: result.Simulation.LastProcessedDate);
            Assert.Equal(
                expected: simulationUpdatedAtUtc.ToString("O"),
                actual: result.Simulation.UpdatedAtUtc);
            Assert.NotNull(result.Weather);
            Assert.Equal(
                expected: "Storm",
                actual: result.Weather!.CurrentType);
            Assert.Equal(
                expected: "Severe",
                actual: result.Weather.CurrentSeverity);
            Assert.True(result.Weather.IsRecoveryActive);
            Assert.Equal(
                expected: weatherEffectiveAtUtc.ToString("O"),
                actual: result.Weather.CurrentWeatherEffectiveAtSimTimeUtc);
            Assert.Equal(
                expected: lastWeatherOccurredOnUtc.ToString("O"),
                actual: result.Weather.LastWeatherOccurredOnUtc);
            Assert.Equal(
                expected: lastExposureProcessedAtUtc.ToString("O"),
                actual: result.Weather.LastExposureProcessedAtSimTimeUtc);
            Assert.Equal(
                expected: lastWeatherImpactAppliedAtUtc.ToString("O"),
                actual: result.Weather.LastWeatherImpactAppliedAtSimTimeUtc);
            Assert.Equal(
                expected: 34,
                actual: result.Housing.HouseholdCount);
            Assert.Equal(
                expected: 30,
                actual: result.Housing.HousedHouseholdCount);
            Assert.Equal(
                expected: 4,
                actual: result.Housing.HomelessHouseholdCount);
            Assert.Equal(
                expected: 120,
                actual: result.Residents.ResidentCount);
            Assert.Equal(
                expected: 70.13m,
                actual: result.Residents.AverageHealth);
            Assert.Equal(
                expected: 64.99m,
                actual: result.Residents.AverageHappiness);
            Assert.Equal(
                expected: 58.24m,
                actual: result.Residents.AverageEnergy);
            Assert.Equal(
                expected: 31.01m,
                actual: result.Residents.AverageStress);
            Assert.Equal(
                expected: 22.44m,
                actual: result.Residents.AverageSocialNeed);
            Assert.Equal(
                expected: 0.34m,
                actual: result.Residents.MedicalLoadIndex);
            Assert.Equal(
                expected: 0.67m,
                actual: result.Residents.TriagePressureIndex);
            Assert.Equal(
                expected: 1.00m,
                actual: result.Residents.RecoverySupportIndex);
            Assert.Equal(
                expected: 0.78m,
                actual: result.Residents.WorkforceCommuteAccessibilityIndex);
            Assert.Equal(
                expected: 0.67m,
                actual: result.Residents.WorkforceAttendanceIndex);
            Assert.Equal(
                expected: 0.56m,
                actual: result.Residents.WorkforceProductivityIndex);
            Assert.Equal(
                expected: 0.50m,
                actual: result.Residents.StudentCommuteAccessibilityIndex);
            Assert.Null(result.Residents.StudentAttendanceIndex);
        }
    }
}
