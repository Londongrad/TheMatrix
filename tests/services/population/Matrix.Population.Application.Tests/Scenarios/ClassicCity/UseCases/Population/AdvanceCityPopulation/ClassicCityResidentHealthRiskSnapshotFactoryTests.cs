using Matrix.Population.Application.Integration;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class ClassicCityResidentHealthRiskSnapshotFactoryTests
    {
        private static readonly CityId CityId = CityId.From(Guid.NewGuid());
        private static readonly DistrictId DistrictId = DistrictId.From(Guid.NewGuid());
        private static readonly ResidentialBuildingId BuildingId = ResidentialBuildingId.From(Guid.NewGuid());
        private static readonly DateOnly CurrentDate = new(2048, 5, 6);

        [Fact]
        public async Task BuildAsync_GroupsHouseholdRiskAndPreloadsDistinctHealthcareRoutes()
        {
            Guid householdId = Guid.NewGuid();
            Person infectedResident = CreatePerson(
                personId: Guid.NewGuid(),
                householdId: householdId,
                birthDate: new DateOnly(1990, 1, 1),
                currentDate: CurrentDate);
            infectedResident.DiagnoseIllness(
                IllnessKind.Infection,
                IllnessSeverity.Mild,
                CurrentDate.AddDays(-1));
            Person familyMember = CreatePerson(
                personId: Guid.NewGuid(),
                householdId: householdId,
                birthDate: new DateOnly(1992, 1, 1),
                currentDate: CurrentDate);
            Person[] residents = [infectedResident, familyMember];
            var household = HouseholdId.From(householdId);
            CityPopulationAnchorCatalogItem hospital = CreateHospitalAnchor();
            var routingService = new RecordingCommuteRoutingService();

            IReadOnlyCollection<PopulationResidentHealthRiskSnapshot> snapshots =
                await ClassicCityResidentHealthRiskSnapshotFactory.BuildAsync(
                    cityId: CityId,
                    residents: residents,
                    residentsByHouseholdId: new Dictionary<HouseholdId, IReadOnlyCollection<Person>>
                    {
                        [household] = residents
                    },
                    currentDate: CurrentDate,
                    housingByHouseholdId: new Dictionary<HouseholdId, HousingStatus>
                    {
                        [household] = HousingStatus.Housed
                    },
                    districtByHouseholdId: new Dictionary<HouseholdId, DistrictId?>
                    {
                        [household] = DistrictId
                    },
                    residentialBuildingByHouseholdId: new Dictionary<HouseholdId, ResidentialBuildingId?>
                    {
                        [household] = BuildingId
                    },
                    hadAdverseWeatherExposure: true,
                    livingConditionsState: null,
                    districtUtilityConditionsByDistrictId:
                    new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>(),
                    essentialsState: null,
                    serviceQualityState: null,
                    healthcarePressureProfile: new CityPopulationHealthcarePressureProfile(
                        ActiveIllnessCount: 1,
                        SevereIllnessCount: 0,
                        MedicalLoadIndex: 0.2m,
                        TriagePressureIndex: 0m,
                        RecoverySupportIndex: 1m),
                    healthcareAutonomyPolicy: new CityHealthcareAutonomyPolicy(
                        new CityHouseholdLivelihoodPolicy()),
                    anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy(),
                    hospitalAnchors: [hospital],
                    districtImpactPolicy: new CityPopulationDistrictImpactPolicy(),
                    livingConditionsPressurePolicy: new CityPopulationLivingConditionsPressurePolicy(),
                    commuteRoutingService: routingService,
                    cancellationToken: CancellationToken.None);

            Assert.Equal(2, snapshots.Count);
            Assert.Single(routingService.PreloadedRequests);
            Assert.Equal(2, routingService.HealthcareResolveCallCount);
            PopulationResidentHealthRiskSnapshot exposed = snapshots.Single(x =>
                x.ResidentId == familyMember.Id.Value);
            Assert.Equal(1, exposed.InfectiousHouseholdContacts);
            Assert.Equal(2, exposed.HouseholdSize);
            Assert.Equal("Housed", exposed.HousingStability);
            Assert.True(exposed.HadAdverseWeatherExposure);
        }

        private static CityPopulationAnchorCatalogItem CreateHospitalAnchor() =>
            CityPopulationAnchorCatalogItem.Create(
                cityId: CityId,
                cityAnchorId: CityAnchorId.From(Guid.NewGuid()),
                districtId: DistrictId,
                accessRoadNodeId: RoadNodeId.From(Guid.NewGuid()),
                name: "Primary Care",
                type: CityAnchorType.Hospital,
                capacity: 100,
                positionX: 0m,
                positionY: 0m,
                createdAtUtc: new DateTimeOffset(2048, 5, 6, 10, 0, 0, TimeSpan.Zero));

        private sealed class RecordingCommuteRoutingService : ICityPopulationCommuteRoutingService
        {
            public IReadOnlyCollection<CityPopulationCommuteRouteRequest> PreloadedRequests { get; private set; } = [];
            public int HealthcareResolveCallCount { get; private set; }

            public Task PreloadAnchorCommutesAsync(
                Guid cityId,
                IReadOnlyCollection<CityPopulationCommuteRouteRequest> requests,
                CancellationToken cancellationToken)
            {
                PreloadedRequests = requests;
                return Task.CompletedTask;
            }

            public Task<CityPopulationCommuteContext> ResolveHealthcareCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                CityAnchorId? healthcareAnchorId,
                CancellationToken cancellationToken)
            {
                HealthcareResolveCallCount++;
                return Task.FromResult(CityPopulationCommuteContext.Neutral);
            }

            public Task<CityPopulationCommuteContext> ResolveAnchorCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                CityAnchorId? destinationAnchorId,
                CancellationToken cancellationToken) => throw new NotSupportedException();

            public Task<CityPopulationCommuteContext> ResolveEmploymentCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                Person resident,
                CancellationToken cancellationToken) => throw new NotSupportedException();

            public Task<CityPopulationCommuteContext> ResolveEducationCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                Person resident,
                CancellationToken cancellationToken) => throw new NotSupportedException();
        }
    }
}
