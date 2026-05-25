using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard
{
    public sealed class GetCityDashboardQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenCurrentSnapshotIsMissing_ReturnsNullAndEnsuresProjection()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var dashboardReadRepository = new FakeCityPopulationDashboardReadRepository();
            GetCityDashboardQueryHandler handler = CreateHandler(
                summaryProjectionService: summaryProjectionService,
                dashboardReadRepository: dashboardReadRepository);

            CityPopulationDashboardDto? result = await handler.Handle(
                request: new GetCityDashboardQuery(cityId),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: Assert.Single(summaryProjectionService.EnsuredCityIds));
        }

        [Fact]
        public async Task Handle_WhenCurrentSnapshotExists_MapsMetricsAndActivity()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            DateOnly currentDate = new(
                year: 2048,
                month: 5,
                day: 4);
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var dashboardReadRepository = new FakeCityPopulationDashboardReadRepository
            {
                CurrentSnapshot = new CityPopulationDashboardSnapshotReadModel(
                    CityId: cityId,
                    SnapshotDate: currentDate,
                    HouseholdCount: 42,
                    HousedHouseholdCount: 39,
                    HomelessHouseholdCount: 3,
                    ResidentCount: 120,
                    DeceasedCount: 7,
                    HousedResidentCount: 111,
                    HomelessResidentCount: 9,
                    ChildCount: 20,
                    YouthCount: 18,
                    AdultCount: 64,
                    SeniorCount: 18,
                    EmployedCount: 70,
                    StudentCount: 22,
                    UnemployedCount: 18,
                    RetiredCount: 10,
                    AverageHealth: 78.126m,
                    AverageHappiness: 66.444m,
                    AverageEnergy: 59.400m,
                    AverageStress: 31.111m,
                    AverageSocialNeed: 22.222m,
                    ActiveIllnessCount: 14,
                    SevereIllnessCount: 3,
                    MedicalLoadIndex: 0.42m,
                    TriagePressureIndex: 0.17m,
                    RecoverySupportIndex: 0.83m,
                    WorkforceCommuteAccessibilityIndex: 0.74m,
                    WorkforceAttendanceIndex: 0.69m,
                    WorkforceProductivityIndex: 0.63m,
                    StudentCommuteAccessibilityIndex: 0.77m,
                    StudentAttendanceIndex: 0.71m),
                EconomySnapshot = new CityPopulationDashboardEconomyReadModel(
                    StableHouseholdCount: 27,
                    StrainedHouseholdCount: 10,
                    DeficitHouseholdCount: 5,
                    AverageCashReserveAmount: 1234.567m,
                    AverageDailyNetAmount: -45.678m),
                ActivityEvents =
                [
                    new CityPopulationActivityEventReadModel(
                        ActivityEventId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                        CurrentDate: currentDate,
                        OccurredAtUtc: new DateTimeOffset(
                            year: 2048,
                            month: 5,
                            day: 4,
                            hour: 9,
                            minute: 30,
                            second: 0,
                            offset: TimeSpan.Zero),
                        EventType: "resident.changed",
                        Source: "population",
                        Severity: "info",
                        Title: "Resident state changed",
                        Summary: "Resident attendance updated.",
                        PrimaryResidentId: Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                        SecondaryResidentId: null)
                ]
            };
            dashboardReadRepository.SnapshotsByDate[new DateOnly(
                year: 2048,
                month: 5,
                day: 3)] = new CityPopulationDashboardSnapshotReadModel(
                CityId: cityId,
                SnapshotDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3),
                HouseholdCount: 40,
                HousedHouseholdCount: 37,
                HomelessHouseholdCount: 3,
                ResidentCount: 118,
                DeceasedCount: 7,
                HousedResidentCount: 109,
                HomelessResidentCount: 9,
                ChildCount: 20,
                YouthCount: 17,
                AdultCount: 63,
                SeniorCount: 18,
                EmployedCount: 68,
                StudentCount: 21,
                UnemployedCount: 19,
                RetiredCount: 10,
                AverageHealth: 77.111m,
                AverageHappiness: 65.444m,
                AverageEnergy: 58.400m,
                AverageStress: 32.111m,
                AverageSocialNeed: 22.500m,
                ActiveIllnessCount: 12,
                SevereIllnessCount: 2,
                MedicalLoadIndex: 0.40m,
                TriagePressureIndex: 0.15m,
                RecoverySupportIndex: 0.80m,
                WorkforceCommuteAccessibilityIndex: 0.73m,
                WorkforceAttendanceIndex: 0.68m,
                WorkforceProductivityIndex: 0.61m,
                StudentCommuteAccessibilityIndex: 0.76m,
                StudentAttendanceIndex: 0.70m);
            dashboardReadRepository.SnapshotsByDate[new DateOnly(
                year: 2048,
                month: 4,
                day: 4)] = new CityPopulationDashboardSnapshotReadModel(
                CityId: cityId,
                SnapshotDate: new DateOnly(
                    year: 2048,
                    month: 4,
                    day: 4),
                HouseholdCount: 35,
                HousedHouseholdCount: 33,
                HomelessHouseholdCount: 2,
                ResidentCount: 100,
                DeceasedCount: 4,
                HousedResidentCount: 96,
                HomelessResidentCount: 4,
                ChildCount: 15,
                YouthCount: 15,
                AdultCount: 55,
                SeniorCount: 15,
                EmployedCount: 60,
                StudentCount: 15,
                UnemployedCount: 15,
                RetiredCount: 10,
                AverageHealth: 74m,
                AverageHappiness: 62m,
                AverageEnergy: 55m,
                AverageStress: 35m,
                AverageSocialNeed: 25m,
                ActiveIllnessCount: 10,
                SevereIllnessCount: 1,
                MedicalLoadIndex: 0.35m,
                TriagePressureIndex: 0.11m,
                RecoverySupportIndex: 0.78m,
                WorkforceCommuteAccessibilityIndex: 0.70m,
                WorkforceAttendanceIndex: 0.65m,
                WorkforceProductivityIndex: 0.58m,
                StudentCommuteAccessibilityIndex: 0.72m,
                StudentAttendanceIndex: 0.66m);
            dashboardReadRepository.SnapshotsByDate[new DateOnly(
                year: 2047,
                month: 5,
                day: 4)] = new CityPopulationDashboardSnapshotReadModel(
                CityId: cityId,
                SnapshotDate: new DateOnly(
                    year: 2047,
                    month: 5,
                    day: 4),
                HouseholdCount: 30,
                HousedHouseholdCount: 29,
                HomelessHouseholdCount: 1,
                ResidentCount: 90,
                DeceasedCount: 2,
                HousedResidentCount: 88,
                HomelessResidentCount: 2,
                ChildCount: 12,
                YouthCount: 12,
                AdultCount: 50,
                SeniorCount: 16,
                EmployedCount: 54,
                StudentCount: 13,
                UnemployedCount: 13,
                RetiredCount: 10,
                AverageHealth: 70m,
                AverageHappiness: 60m,
                AverageEnergy: 52m,
                AverageStress: 37m,
                AverageSocialNeed: 28m,
                ActiveIllnessCount: 8,
                SevereIllnessCount: 1,
                MedicalLoadIndex: 0.30m,
                TriagePressureIndex: 0.10m,
                RecoverySupportIndex: 0.75m,
                WorkforceCommuteAccessibilityIndex: 0.68m,
                WorkforceAttendanceIndex: 0.62m,
                WorkforceProductivityIndex: 0.55m,
                StudentCommuteAccessibilityIndex: 0.69m,
                StudentAttendanceIndex: 0.63m);
            GetCityDashboardQueryHandler handler = CreateHandler(
                summaryProjectionService: summaryProjectionService,
                dashboardReadRepository: dashboardReadRepository);

            CityPopulationDashboardDto? result = await handler.Handle(
                request: new GetCityDashboardQuery(cityId),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            CityPopulationDashboardDto dto = result!;
            Assert.Equal(
                expected: cityId,
                actual: dto.CityId);
            Assert.Equal(
                expected: "2048-05-04",
                actual: dto.CurrentDate);
            Assert.True(
                DateTimeOffset.TryParse(
                    input: dto.GeneratedAtUtc,
                    result: out _));
            Assert.Equal(
                expected: 23,
                actual: dto.Metrics.Count);
            CityPopulationDashboardMetricDto residents = Assert.Single(
                collection: dto.Metrics,
                predicate: x => x.Key == "residents");
            Assert.Equal(
                expected: 120m,
                actual: residents.CurrentValue);
            Assert.Equal(
                expected: 2m,
                actual: residents.DeltaYesterday);
            Assert.Equal(
                expected: 20m,
                actual: residents.DeltaMonth);
            Assert.Equal(
                expected: 30m,
                actual: residents.DeltaYear);
            CityPopulationDashboardMetricDto averageHealth = Assert.Single(
                collection: dto.Metrics,
                predicate: x => x.Key == "averageHealth");
            Assert.Equal(
                expected: 78.13m,
                actual: averageHealth.CurrentValue);
            Assert.Equal(
                expected: 1.02m,
                actual: averageHealth.DeltaYesterday);
            CityPopulationDashboardMetricDto averageDailyNet = Assert.Single(
                collection: dto.Metrics,
                predicate: x => x.Key == "averageDailyNet");
            Assert.Equal(
                expected: -45.68m,
                actual: averageDailyNet.CurrentValue);
            Assert.Null(averageDailyNet.DeltaYesterday);
            CityPopulationActivityEventDto activity = Assert.Single(dto.RecentEvents);
            Assert.Equal(
                expected: "2048-05-04",
                actual: activity.CurrentDate);
            Assert.Equal(
                expected: "Resident state changed",
                actual: activity.Title);
            Assert.Equal(
                expected: 12,
                actual: dashboardReadRepository.RequestedRecentTake);
            Assert.Equal(
                expected: currentDate,
                actual: dashboardReadRepository.RequestedEconomyDate);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: Assert.Single(summaryProjectionService.EnsuredCityIds));
        }

        private static GetCityDashboardQueryHandler CreateHandler(
            FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
            FakeCityPopulationDashboardReadRepository? dashboardReadRepository = null)
        {
            return new GetCityDashboardQueryHandler(
                summaryProjectionService: summaryProjectionService ?? new FakeCityPopulationSummaryProjectionService(),
                dashboardReadRepository: dashboardReadRepository ?? new FakeCityPopulationDashboardReadRepository(),
                timeProvider: CreateTimeProvider());
        }
    }
}
