using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDistrictPressure;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.GetCityDistrictPressure;

public sealed class GetCityDistrictPressureQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenResidentsOrPlacementsAreMissing_ReturnsNull()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var personReadRepository = new FakeCityPopulationPersonReadRepository();
        var householdWriteRepository = new FakeHouseholdWriteRepository();
        var utilityClient = new FakeCityDistrictUtilityConditionsClient();
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            householdWriteRepository: householdWriteRepository,
            utilityClient: utilityClient);

        CityPopulationDistrictPressureDto? result = await handler.Handle(
            new GetCityDistrictPressureQuery(cityId),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenUtilityClientThrows_FallsBackToResidentOnlyPressure()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        DistrictId districtId = DistrictId.From(Guid.Parse("11111111-aaaa-bbbb-cccc-222222222222"));
        HouseholdId householdId = HouseholdId.From(Guid.Parse("33333333-aaaa-bbbb-cccc-444444444444"));
        var person = CreatePerson(
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
                    residentialBuildingId: ResidentialBuildingId.From(Guid.Parse("77777777-aaaa-bbbb-cccc-888888888888")))
            ]
        };
        var utilityClient = new FakeCityDistrictUtilityConditionsClient
        {
            ExceptionToThrow = new InvalidOperationException("district utility service unavailable")
        };
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            householdWriteRepository: householdWriteRepository,
            utilityClient: utilityClient);

        CityPopulationDistrictPressureDto? result = await handler.Handle(
            new GetCityDistrictPressureQuery(cityId),
            CancellationToken.None);

        Assert.NotNull(result);
        CityPopulationDistrictPressureDto dto = result!;
        CityPopulationDistrictPressureItemDto district = Assert.Single(dto.Districts);
        Assert.Equal(districtId.Value, district.DistrictId);
        Assert.Equal(1m, district.UtilityContinuityIndex);
        Assert.Equal(0m, district.UtilityIncidentPressureIndex);
        Assert.Equal(0m, district.HousingFragilityIndex);
        Assert.True(DateTimeOffset.TryParse(dto.GeneratedAtUtc, out _));
    }

    [Fact]
    public async Task Handle_WhenUtilityConditionsExist_ReturnsDistrictsOrderedByPressure()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        DistrictId higherPressureDistrictId = DistrictId.From(Guid.Parse("11111111-aaaa-bbbb-cccc-222222222222"));
        DistrictId lowerPressureDistrictId = DistrictId.From(Guid.Parse("99999999-aaaa-bbbb-cccc-000000000000"));
        HouseholdId firstHouseholdId = HouseholdId.From(Guid.Parse("33333333-aaaa-bbbb-cccc-444444444444"));
        HouseholdId secondHouseholdId = HouseholdId.From(Guid.Parse("55555555-aaaa-bbbb-cccc-666666666666"));
        var higherPressureResident = CreatePerson(
            personId: Guid.Parse("77777777-aaaa-bbbb-cccc-888888888888"),
            householdId: firstHouseholdId.Value,
            happiness: 42,
            stress: 73,
            health: 58);
        higherPressureResident.DiagnoseIllness(
            kind: IllnessKind.Infection,
            severity: IllnessSeverity.Severe,
            currentDate: new DateOnly(2048, 5, 4));
        var lowerPressureResident = CreatePerson(
            personId: Guid.Parse("11111111-9999-8888-7777-666666666666"),
            householdId: secondHouseholdId.Value,
            happiness: 78,
            stress: 18,
            health: 89);
        var personReadRepository = new FakeCityPopulationPersonReadRepository
        {
            ListByCityResult = [higherPressureResident, lowerPressureResident]
        };
        var householdWriteRepository = new FakeHouseholdWriteRepository
        {
            PlacementsByCityResult =
            [
                ClassicCityHouseholdPlacement.CreateHoused(
                    householdId: firstHouseholdId,
                    cityId: CityId.From(cityId),
                    districtId: higherPressureDistrictId,
                    residentialBuildingId: ResidentialBuildingId.From(Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"))),
                ClassicCityHouseholdPlacement.CreateHoused(
                    householdId: secondHouseholdId,
                    cityId: CityId.From(cityId),
                    districtId: lowerPressureDistrictId,
                    residentialBuildingId: ResidentialBuildingId.From(Guid.Parse("cccccccc-1111-2222-3333-dddddddddddd")))
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
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            householdWriteRepository: householdWriteRepository,
            utilityClient: utilityClient);

        CityPopulationDistrictPressureDto? result = await handler.Handle(
            new GetCityDistrictPressureQuery(cityId),
            CancellationToken.None);

        Assert.NotNull(result);
        CityPopulationDistrictPressureDto dto = result!;
        Assert.Equal(2, dto.Districts.Count);
        CityPopulationDistrictPressureItemDto first = dto.Districts[0];
        CityPopulationDistrictPressureItemDto second = dto.Districts[1];
        Assert.Equal(higherPressureDistrictId.Value, first.DistrictId);
        Assert.Equal(lowerPressureDistrictId.Value, second.DistrictId);
        Assert.Equal(1, first.ActiveIllnessCount);
        Assert.Equal(1, first.SevereIllnessCount);
        Assert.True(first.PopulationPressureIndex > second.PopulationPressureIndex);
        Assert.True(first.UtilityContinuityIndex < second.UtilityContinuityIndex);
        Assert.Equal(cityId, utilityClient.RequestedCityId);
    }

    private static GetCityDistrictPressureQueryHandler CreateHandler(
        FakeCityPopulationPersonReadRepository? personReadRepository = null,
        FakeHouseholdWriteRepository? householdWriteRepository = null,
        FakeCityDistrictUtilityConditionsClient? utilityClient = null)
    {
        return new GetCityDistrictPressureQueryHandler(
            personReadRepository ?? new FakeCityPopulationPersonReadRepository(),
            householdWriteRepository ?? new FakeHouseholdWriteRepository(),
            utilityClient ?? new FakeCityDistrictUtilityConditionsClient(),
            NullLogger<GetCityDistrictPressureQueryHandler>.Instance);
    }
}
