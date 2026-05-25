using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentDetails;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
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

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.GetCityResidentDetails
{
    public sealed class GetCityResidentDetailsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenResidentIsEmployed_ReturnsEnrichedResidentDetails()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var spouseId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
            var childId = Guid.Parse("bbbbbbbb-1111-2222-3333-cccccccccccc");
            var districtId = Guid.Parse("dddddddd-1111-2222-3333-eeeeeeeeeeee");
            var buildingId = Guid.Parse("ffffffff-1111-2222-3333-000000000000");
            var hospitalAnchorId = Guid.Parse("12121212-3434-5656-7878-909090909090");
            var workplaceId = Guid.Parse("abababab-1111-2222-3333-cdcdcdcdcdcd");
            var workplaceAnchorId = Guid.Parse("dededede-1111-2222-3333-efefefefefef");

            Person spouse = CreatePerson(
                personId: spouseId,
                firstName: "Trinity",
                sex: Sex.Female);
            Person resident = CreatePerson(
                personId: residentId,
                maritalStatus: MaritalStatus.Married,
                spouseId: spouse.Id,
                employmentStatus: EmploymentStatus.Employed,
                job: new Job(
                    workplaceId: WorkplaceId.From(workplaceId),
                    title: "Engineer",
                    workplaceAnchorId: CityAnchorId.From(workplaceAnchorId)));
            Person child = CreatePerson(
                personId: childId,
                birthDate: new DateOnly(
                    year: 2042,
                    month: 5,
                    day: 5),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 5),
                firstName: "Kid");

            var personReadRepository = new FakePersonReadRepository();
            personReadRepository.PersonsById[resident.Id] = resident;
            personReadRepository.PersonsById[spouse.Id] = spouse;

            var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository();
            cityPopulationPersonReadRepository.PersonsByCityAndId[(CityId.From(cityId), resident.Id)] = resident;
            cityPopulationPersonReadRepository.ChildrenByParentId[resident.Id] = [child];
            cityPopulationPersonReadRepository.HousingSnapshotsByPersonId[resident.Id] =
                new CityResidentHousingSnapshot(
                    HouseholdId: resident.HouseholdId,
                    HousingStatus: HousingStatus.Housed,
                    DistrictId: DistrictId.From(districtId),
                    ResidentialBuildingId: ResidentialBuildingId.From(buildingId));

            var anchorCatalogRepository = new FakeCityPopulationAnchorCatalogRepository
            {
                Items =
                [
                    CityPopulationAnchorCatalogItem.Create(
                        cityId: CityId.From(cityId),
                        cityAnchorId: CityAnchorId.From(hospitalAnchorId),
                        districtId: DistrictId.From(districtId),
                        accessRoadNodeId: RoadNodeId.From(Guid.Parse("45454545-6666-7777-8888-999999999999")),
                        name: "Central Hospital",
                        type: CityAnchorType.Hospital,
                        capacity: 100,
                        positionX: 10m,
                        positionY: 15m,
                        createdAtUtc: UtcNow)
                ]
            };
            var commuteRoutingService = new FakeCityPopulationCommuteRoutingService
            {
                EmploymentContext = new CityPopulationCommuteContext(
                    HasRouteData: true,
                    IsAccessible: true,
                    AccessibilityIndex: 0.82m,
                    PassabilityIndex: 0.91m,
                    EstimatedTravelTimeMinutes: 18m),
                HealthcareContext = new CityPopulationCommuteContext(
                    HasRouteData: true,
                    IsAccessible: true,
                    AccessibilityIndex: 0.77m,
                    PassabilityIndex: 0.80m,
                    EstimatedTravelTimeMinutes: 12m)
            };
            var activeTripClient = new FakeCityPopulationActiveTripClient();
            activeTripClient.ActiveTripsByTravellerId[residentId] = new CityPopulationActiveTripSnapshot(
                TravellerEntityId: residentId,
                Subject: "Morning commute",
                Purpose: "Work",
                Status: "InProgress",
                CurrentProgressIndex: 0.45m,
                StartedAtSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 5,
                    hour: 7,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                ExpectedArrivalAtSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 5,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                FromName: "Home",
                FromEntityId: buildingId,
                ToName: "Workplace",
                ToEntityId: workplaceId);

            GetCityResidentDetailsQueryHandler handler = CreateHandler(
                cityPopulationActiveTripClient: activeTripClient,
                cityPopulationAnchorCatalogRepository: anchorCatalogRepository,
                cityPopulationCommuteRoutingService: commuteRoutingService,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                personReadRepository: personReadRepository);

            CityResidentDetailsDto result = await handler.Handle(
                request: new GetCityResidentDetailsQuery(
                    CityId: cityId,
                    PersonId: residentId,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 5)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: residentId,
                actual: result.Id);
            Assert.Equal(
                expected: "Married",
                actual: result.MaritalStatus);
            Assert.NotNull(result.CurrentSpouse);
            Assert.Equal(
                expected: spouseId,
                actual: result.CurrentSpouse!.Id);
            Assert.Single(result.Children);
            Assert.Equal(
                expected: childId,
                actual: result.Children.Single()
                   .Id);
            Assert.Equal(
                expected: "Employed",
                actual: result.EmploymentStatus);
            Assert.Equal(
                expected: "Engineer",
                actual: result.JobTitle);
            Assert.NotNull(result.CurrentHousing);
            Assert.Equal(
                expected: resident.HouseholdId.Value,
                actual: result.CurrentHousing.HouseholdId);
            Assert.NotNull(result.CurrentWorkplace);
            Assert.Equal(
                expected: workplaceId,
                actual: result.CurrentWorkplace!.WorkplaceId);
            Assert.Equal(
                expected: workplaceAnchorId,
                actual: result.CurrentWorkplace.WorkplaceAnchorId);
            Assert.NotNull(result.CurrentWorkplace.RouteAccess);
            Assert.True(result.CurrentWorkplace.RouteAccess!.HasRouteData);
            Assert.Equal(
                expected: 18m,
                actual: result.CurrentWorkplace.RouteAccess.EstimatedTravelTimeMinutes);
            Assert.NotNull(result.PrimaryHealthcareProvider);
            Assert.Equal(
                expected: hospitalAnchorId,
                actual: result.PrimaryHealthcareProvider!.PrimaryCareAnchorId);
            Assert.NotNull(result.PrimaryHealthcareProvider.RouteAccess);
            Assert.Equal(
                expected: 12m,
                actual: result.PrimaryHealthcareProvider.RouteAccess!.EstimatedTravelTimeMinutes);
            Assert.NotNull(result.CurrentActiveTrip);
            Assert.Equal(
                expected: "Morning commute",
                actual: result.CurrentActiveTrip!.Subject);
            Assert.Equal(
                expected: "Work",
                actual: result.CurrentActiveTrip.Purpose);
            Assert.Equal(
                expected: "InProgress",
                actual: result.CurrentActiveTrip.Status);
        }

        [Fact]
        public async Task Handle_WhenResidentIsStudent_ReturnsEducationInstitutionAndRoute()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var districtId = Guid.Parse("dddddddd-1111-2222-3333-eeeeeeeeeeee");
            var buildingId = Guid.Parse("ffffffff-1111-2222-3333-000000000000");
            var institutionId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
            var institutionAnchorId = Guid.Parse("bbbbbbbb-1111-2222-3333-cccccccccccc");

            Person resident = CreatePerson(
                personId: residentId,
                employmentStatus: EmploymentStatus.Unemployed);
            resident.StartStudying(
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 4),
                institutionId: EducationInstitutionId.From(institutionId),
                institutionAnchorId: CityAnchorId.From(institutionAnchorId));

            var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository();
            cityPopulationPersonReadRepository.PersonsByCityAndId[(CityId.From(cityId), resident.Id)] = resident;
            cityPopulationPersonReadRepository.HousingSnapshotsByPersonId[resident.Id] =
                new CityResidentHousingSnapshot(
                    HouseholdId: resident.HouseholdId,
                    HousingStatus: HousingStatus.Housed,
                    DistrictId: DistrictId.From(districtId),
                    ResidentialBuildingId: ResidentialBuildingId.From(buildingId));

            var commuteRoutingService = new FakeCityPopulationCommuteRoutingService
            {
                EducationContext = new CityPopulationCommuteContext(
                    HasRouteData: true,
                    IsAccessible: false,
                    AccessibilityIndex: 0.35m,
                    PassabilityIndex: 0.20m,
                    EstimatedTravelTimeMinutes: null)
            };

            GetCityResidentDetailsQueryHandler handler = CreateHandler(
                cityPopulationCommuteRoutingService: commuteRoutingService,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                personReadRepository: new FakePersonReadRepository());

            CityResidentDetailsDto result = await handler.Handle(
                request: new GetCityResidentDetailsQuery(
                    CityId: cityId,
                    PersonId: residentId,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 5)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "Student",
                actual: result.EmploymentStatus);
            Assert.NotNull(result.CurrentEducationInstitution);
            Assert.Equal(
                expected: institutionId,
                actual: result.CurrentEducationInstitution!.InstitutionId);
            Assert.Equal(
                expected: institutionAnchorId,
                actual: result.CurrentEducationInstitution.InstitutionAnchorId);
            Assert.Equal(
                expected: "UpperSecondary",
                actual: result.CurrentEducationInstitution.EducationLevel);
            Assert.NotNull(result.CurrentEducationInstitution.RouteAccess);
            Assert.True(result.CurrentEducationInstitution.RouteAccess!.HasRouteData);
            Assert.False(result.CurrentEducationInstitution.RouteAccess.IsAccessible);
            Assert.Equal(
                expected: 0.35m,
                actual: result.CurrentEducationInstitution.RouteAccess.AccessibilityIndex);
            Assert.Null(result.CurrentWorkplace);
        }

        [Fact]
        public async Task Handle_WhenResidentIsMissing_ThrowsNotFound()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            GetCityResidentDetailsQueryHandler handler = CreateHandler();

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(async ()
                => await handler.Handle(
                    request: new GetCityResidentDetailsQuery(
                        CityId: cityId,
                        PersonId: residentId,
                        CurrentDate: new DateOnly(
                            year: 2048,
                            month: 5,
                            day: 5)),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Population.Person.NotFound",
                actual: exception.Code);
        }

        private static GetCityResidentDetailsQueryHandler CreateHandler(
            ICityPopulationActiveTripClient? cityPopulationActiveTripClient = null,
            FakeCityPopulationAnchorCatalogRepository? cityPopulationAnchorCatalogRepository = null,
            ICityPopulationCommuteRoutingService? cityPopulationCommuteRoutingService = null,
            FakeCityPopulationPersonReadRepository? cityPopulationPersonReadRepository = null,
            FakePersonReadRepository? personReadRepository = null)
        {
            return new GetCityResidentDetailsQueryHandler(
                cityPopulationActiveTripClient: cityPopulationActiveTripClient ??
                                                new FakeCityPopulationActiveTripClient(),
                cityPopulationAnchorCatalogRepository: cityPopulationAnchorCatalogRepository ??
                                                       new FakeCityPopulationAnchorCatalogRepository(),
                cityPopulationCommuteRoutingService: cityPopulationCommuteRoutingService ??
                                                     new FakeCityPopulationCommuteRoutingService(),
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository ??
                                                    new FakeCityPopulationPersonReadRepository(),
                anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy(),
                personReadRepository: personReadRepository ?? new FakePersonReadRepository());
        }
    }
}
