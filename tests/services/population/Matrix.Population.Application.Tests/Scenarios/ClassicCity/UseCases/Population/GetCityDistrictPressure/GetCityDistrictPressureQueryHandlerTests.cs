using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDistrictPressure;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.GetCityDistrictPressure
{
    public sealed class GetCityDistrictPressureQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenResidentsOrPlacementsAreMissing_ReturnsNull()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var personReadRepository = new FakeCityPopulationPersonReadRepository();
            var householdWriteRepository = new FakeHouseholdWriteRepository();
            var utilityClient = new FakeCityDistrictUtilityConditionsClient();
            GetCityDistrictPressureQueryHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                householdWriteRepository: householdWriteRepository,
                utilityClient: utilityClient);

            CityPopulationDistrictPressureDto? result = await handler.Handle(
                request: new GetCityDistrictPressureQuery(cityId),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_WhenUtilityClientThrows_FallsBackToResidentOnlyPressure()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var districtId = DistrictId.From(Guid.Parse("11111111-aaaa-bbbb-cccc-222222222222"));
            var householdId = HouseholdId.From(Guid.Parse("33333333-aaaa-bbbb-cccc-444444444444"));
            Person person = CreatePerson(
                personId: Guid.Parse("55555555-aaaa-bbbb-cccc-666666666666"),
                householdId: householdId.Value,
                happiness: 61,
                stress: 35,
                health: 72);
            var personReadRepository = new FakeCityPopulationPersonReadRepository
            {
                ListByCityResult = [person]
            };
            var householdWriteRepository = new FakeHouseholdWriteRepository
            {
                PlacementsByCityResult =
                [
                    ClassicCityHouseholdPlacement.CreateHoused(
                        householdId: householdId,
                        cityId: CityId.From(cityId),
                        districtId: districtId,
                        residentialBuildingId: ResidentialBuildingId.From(
                            Guid.Parse("77777777-aaaa-bbbb-cccc-888888888888")))
                ]
            };
            var utilityClient = new FakeCityDistrictUtilityConditionsClient
            {
                ExceptionToThrow = new InvalidOperationException("district utility service unavailable")
            };
            GetCityDistrictPressureQueryHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                householdWriteRepository: householdWriteRepository,
                utilityClient: utilityClient);

            CityPopulationDistrictPressureDto? result = await handler.Handle(
                request: new GetCityDistrictPressureQuery(cityId),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            CityPopulationDistrictPressureDto dto = result!;
            CityPopulationDistrictPressureItemDto district = Assert.Single(dto.Districts);
            Assert.Equal(
                expected: districtId.Value,
                actual: district.DistrictId);
            Assert.Equal(
                expected: 1m,
                actual: district.UtilityContinuityIndex);
            Assert.Equal(
                expected: 0m,
                actual: district.UtilityIncidentPressureIndex);
            Assert.Equal(
                expected: 0m,
                actual: district.HousingFragilityIndex);
            Assert.True(
                DateTimeOffset.TryParse(
                    input: dto.GeneratedAtUtc,
                    result: out _));
        }

        [Fact]
        public async Task Handle_WhenUtilityConditionsExist_ReturnsDistrictsOrderedByPressure()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var higherPressureDistrictId = DistrictId.From(Guid.Parse("11111111-aaaa-bbbb-cccc-222222222222"));
            var lowerPressureDistrictId = DistrictId.From(Guid.Parse("99999999-aaaa-bbbb-cccc-000000000000"));
            var firstHouseholdId = HouseholdId.From(Guid.Parse("33333333-aaaa-bbbb-cccc-444444444444"));
            var secondHouseholdId = HouseholdId.From(Guid.Parse("55555555-aaaa-bbbb-cccc-666666666666"));
            Person higherPressureResident = CreatePerson(
                personId: Guid.Parse("77777777-aaaa-bbbb-cccc-888888888888"),
                householdId: firstHouseholdId.Value,
                happiness: 42,
                stress: 73,
                health: 58);
            Person lowerPressureResident = CreatePerson(
                personId: Guid.Parse("11111111-9999-8888-7777-666666666666"),
                householdId: secondHouseholdId.Value,
                happiness: 78,
                stress: 18,
                health: 89);
            var personReadRepository = new FakeCityPopulationPersonReadRepository
            {
                ListByCityResult =
                [
                    higherPressureResident,
                    lowerPressureResident
                ]
            };
            var householdWriteRepository = new FakeHouseholdWriteRepository
            {
                PlacementsByCityResult =
                [
                    ClassicCityHouseholdPlacement.CreateHoused(
                        householdId: firstHouseholdId,
                        cityId: CityId.From(cityId),
                        districtId: higherPressureDistrictId,
                        residentialBuildingId: ResidentialBuildingId.From(
                            Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"))),
                    ClassicCityHouseholdPlacement.CreateHoused(
                        householdId: secondHouseholdId,
                        cityId: CityId.From(cityId),
                        districtId: lowerPressureDistrictId,
                        residentialBuildingId: ResidentialBuildingId.From(
                            Guid.Parse("cccccccc-1111-2222-3333-dddddddddddd")))
                ]
            };
            var utilityClient = new FakeCityDistrictUtilityConditionsClient
            {
                SnapshotsByDistrictId = new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>
                {
                    [higherPressureDistrictId] = new(
                        DistrictId: higherPressureDistrictId,
                        HeatingCoverageIndex: 0.55m,
                        HeatingComfortStressIndex: 0.45m,
                        WaterCoverageIndex: 0.60m,
                        WaterDisruptionRiskIndex: 0.35m,
                        PowerCoverageIndex: 0.52m,
                        PowerOutageRiskIndex: 0.40m,
                        SanitationCoverageIndex: 0.58m,
                        SanitationContaminationRiskIndex: 0.28m,
                        UtilityIncidentDispatchReadinessIndex: 0.42m,
                        UtilityIncidentPressureIndex: 0.66m,
                        UtilityIncidentCoordinationDifficultyIndex: 0.48m,
                        UtilityIncidentRestorationPriorityIndex: 0.38m),
                    [lowerPressureDistrictId] = new(
                        DistrictId: lowerPressureDistrictId,
                        HeatingCoverageIndex: 0.95m,
                        HeatingComfortStressIndex: 0.05m,
                        WaterCoverageIndex: 0.96m,
                        WaterDisruptionRiskIndex: 0.04m,
                        PowerCoverageIndex: 0.97m,
                        PowerOutageRiskIndex: 0.03m,
                        SanitationCoverageIndex: 0.94m,
                        SanitationContaminationRiskIndex: 0.02m,
                        UtilityIncidentDispatchReadinessIndex: 0.91m,
                        UtilityIncidentPressureIndex: 0.08m,
                        UtilityIncidentCoordinationDifficultyIndex: 0.07m,
                        UtilityIncidentRestorationPriorityIndex: 0.04m)
                }
            };
            GetCityDistrictPressureQueryHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                householdWriteRepository: householdWriteRepository,
                utilityClient: utilityClient,
                healthcareRepository: new FakeCityHealthcarePressureSnapshotRepository
                {
                    Snapshot = new ClassicCityHealthcarePressureSnapshot(
                        CityId.From(cityId),
                        SourceRevision: 17,
                        CurrentDate: new DateOnly(2048, 5, 6),
                        PatientCount: 2,
                        Pressure: new CityPopulationHealthcarePressureProfile(
                            ActiveIllnessCount: 1,
                            SevereIllnessCount: 1,
                            MedicalLoadIndex: 0.82m,
                            TriagePressureIndex: 0.34m,
                            RecoverySupportIndex: 1.12m),
                        OccurredAtUtc: new DateTimeOffset(2048, 5, 6, 10, 0, 0, TimeSpan.Zero),
                        UpdatedAtUtc: new DateTimeOffset(2048, 5, 6, 10, 1, 0, TimeSpan.Zero),
                        Districts:
                        [
                            new ClassicCityHealthcareDistrictHealthSnapshot(
                                higherPressureDistrictId,
                                PatientCount: 1,
                                ActiveIllnessCount: 1,
                                SevereIllnessCount: 1),
                            new ClassicCityHealthcareDistrictHealthSnapshot(
                                lowerPressureDistrictId,
                                PatientCount: 1,
                                ActiveIllnessCount: 0,
                                SevereIllnessCount: 0)
                        ])
                });

            CityPopulationDistrictPressureDto? result = await handler.Handle(
                request: new GetCityDistrictPressureQuery(cityId),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            CityPopulationDistrictPressureDto dto = result!;
            Assert.Equal(
                expected: 2,
                actual: dto.Districts.Count);
            CityPopulationDistrictPressureItemDto first = dto.Districts[0];
            CityPopulationDistrictPressureItemDto second = dto.Districts[1];
            Assert.Equal(
                expected: higherPressureDistrictId.Value,
                actual: first.DistrictId);
            Assert.Equal(
                expected: lowerPressureDistrictId.Value,
                actual: second.DistrictId);
            Assert.Equal(
                expected: 1,
                actual: first.ActiveIllnessCount);
            Assert.Equal(
                expected: 1,
                actual: first.SevereIllnessCount);
            Assert.True(first.PopulationPressureIndex > second.PopulationPressureIndex);
            Assert.True(first.UtilityContinuityIndex < second.UtilityContinuityIndex);
            Assert.Equal(
                expected: cityId,
                actual: utilityClient.RequestedCityId);
        }

        private static GetCityDistrictPressureQueryHandler CreateHandler(
            FakeCityPopulationPersonReadRepository? personReadRepository = null,
            FakeHouseholdWriteRepository? householdWriteRepository = null,
            FakeCityDistrictUtilityConditionsClient? utilityClient = null,
            FakeCityHealthcarePressureSnapshotRepository? healthcareRepository = null)
        {
            return new GetCityDistrictPressureQueryHandler(
                personReadRepository: personReadRepository ?? new FakeCityPopulationPersonReadRepository(),
                householdWriteRepository: householdWriteRepository ?? new FakeHouseholdWriteRepository(),
                healthcarePressureSnapshotRepository: healthcareRepository
                                                      ?? new FakeCityHealthcarePressureSnapshotRepository(),
                districtUtilityConditionsClient: utilityClient ?? new FakeCityDistrictUtilityConditionsClient(),
                timeProvider: CreateTimeProvider(),
                logger: NullLogger<GetCityDistrictPressureQueryHandler>.Instance);
        }
    }
}
