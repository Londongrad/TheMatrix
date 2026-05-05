using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentDetails;
using Matrix.Population.Application.Tests.TestSupport;
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

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.GetCityResidentDetails;

public sealed class GetCityResidentDetailsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenResidentIsEmployed_ReturnsEnrichedResidentDetails()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Guid spouseId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
        Guid childId = Guid.Parse("bbbbbbbb-1111-2222-3333-cccccccccccc");
        Guid districtId = Guid.Parse("dddddddd-1111-2222-3333-eeeeeeeeeeee");
        Guid buildingId = Guid.Parse("ffffffff-1111-2222-3333-000000000000");
        Guid hospitalAnchorId = Guid.Parse("12121212-3434-5656-7878-909090909090");
        Guid workplaceId = Guid.Parse("abababab-1111-2222-3333-cdcdcdcdcdcd");
        Guid workplaceAnchorId = Guid.Parse("dededede-1111-2222-3333-efefefefefef");

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
            birthDate: new DateOnly(2042, 5, 5),
            currentDate: new DateOnly(2048, 5, 5),
            firstName: "Kid");

        var personReadRepository = new FakePersonReadRepository();
        personReadRepository.PersonsById[resident.Id] = resident;
        personReadRepository.PersonsById[spouse.Id] = spouse;

        var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository();
        cityPopulationPersonReadRepository.PersonsByCityAndId[(CityId.From(cityId), resident.Id)] = resident;
        cityPopulationPersonReadRepository.ChildrenByParentId[resident.Id] = [child];
        cityPopulationPersonReadRepository.HousingSnapshotsByPersonId[resident.Id] = new CityResidentHousingSnapshot(
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
            StartedAtSimTimeUtc: new DateTimeOffset(2048, 5, 5, 7, 30, 0, TimeSpan.Zero),
            ExpectedArrivalAtSimTimeUtc: new DateTimeOffset(2048, 5, 5, 8, 0, 0, TimeSpan.Zero),
            FromName: "Home",
            FromEntityId: buildingId,
            ToName: "Workplace",
            ToEntityId: workplaceId);

        var handler = CreateHandler(
            cityPopulationActiveTripClient: activeTripClient,
            cityPopulationAnchorCatalogRepository: anchorCatalogRepository,
            cityPopulationCommuteRoutingService: commuteRoutingService,
            cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
            personReadRepository: personReadRepository);

        var result = await handler.Handle(
            new GetCityResidentDetailsQuery(
                CityId: cityId,
                PersonId: residentId,
                CurrentDate: new DateOnly(2048, 5, 5)),
            CancellationToken.None);

        Assert.Equal(residentId, result.Id);
        Assert.Equal("Married", result.MaritalStatus);
        Assert.NotNull(result.CurrentSpouse);
        Assert.Equal(spouseId, result.CurrentSpouse!.Id);
        Assert.Single(result.Children);
        Assert.Equal(childId, result.Children.Single().Id);
        Assert.Equal("Employed", result.EmploymentStatus);
        Assert.Equal("Engineer", result.JobTitle);
        Assert.NotNull(result.CurrentHousing);
        Assert.Equal(resident.HouseholdId.Value, result.CurrentHousing.HouseholdId);
        Assert.NotNull(result.CurrentWorkplace);
        Assert.Equal(workplaceId, result.CurrentWorkplace!.WorkplaceId);
        Assert.Equal(workplaceAnchorId, result.CurrentWorkplace.WorkplaceAnchorId);
        Assert.NotNull(result.CurrentWorkplace.RouteAccess);
        Assert.True(result.CurrentWorkplace.RouteAccess!.HasRouteData);
        Assert.Equal(18m, result.CurrentWorkplace.RouteAccess.EstimatedTravelTimeMinutes);
        Assert.NotNull(result.PrimaryHealthcareProvider);
        Assert.Equal(hospitalAnchorId, result.PrimaryHealthcareProvider!.PrimaryCareAnchorId);
        Assert.NotNull(result.PrimaryHealthcareProvider.RouteAccess);
        Assert.Equal(12m, result.PrimaryHealthcareProvider.RouteAccess!.EstimatedTravelTimeMinutes);
        Assert.NotNull(result.CurrentActiveTrip);
        Assert.Equal("Morning commute", result.CurrentActiveTrip!.Subject);
        Assert.Equal("Work", result.CurrentActiveTrip.Purpose);
        Assert.Equal("InProgress", result.CurrentActiveTrip.Status);
    }

    [Fact]
    public async Task Handle_WhenResidentIsStudent_ReturnsEducationInstitutionAndRoute()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Guid districtId = Guid.Parse("dddddddd-1111-2222-3333-eeeeeeeeeeee");
        Guid buildingId = Guid.Parse("ffffffff-1111-2222-3333-000000000000");
        Guid institutionId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
        Guid institutionAnchorId = Guid.Parse("bbbbbbbb-1111-2222-3333-cccccccccccc");

        Person resident = CreatePerson(
            personId: residentId,
            employmentStatus: EmploymentStatus.Unemployed);
        resident.StartStudying(
            currentDate: new DateOnly(2048, 5, 4),
            institutionId: EducationInstitutionId.From(institutionId),
            institutionAnchorId: CityAnchorId.From(institutionAnchorId));

        var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository();
        cityPopulationPersonReadRepository.PersonsByCityAndId[(CityId.From(cityId), resident.Id)] = resident;
        cityPopulationPersonReadRepository.HousingSnapshotsByPersonId[resident.Id] = new CityResidentHousingSnapshot(
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

        var handler = CreateHandler(
            cityPopulationCommuteRoutingService: commuteRoutingService,
            cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
            personReadRepository: new FakePersonReadRepository());

        var result = await handler.Handle(
            new GetCityResidentDetailsQuery(
                CityId: cityId,
                PersonId: residentId,
                CurrentDate: new DateOnly(2048, 5, 5)),
            CancellationToken.None);

        Assert.Equal("Student", result.EmploymentStatus);
        Assert.NotNull(result.CurrentEducationInstitution);
        Assert.Equal(institutionId, result.CurrentEducationInstitution!.InstitutionId);
        Assert.Equal(institutionAnchorId, result.CurrentEducationInstitution.InstitutionAnchorId);
        Assert.Equal("UpperSecondary", result.CurrentEducationInstitution.EducationLevel);
        Assert.NotNull(result.CurrentEducationInstitution.RouteAccess);
        Assert.True(result.CurrentEducationInstitution.RouteAccess!.HasRouteData);
        Assert.False(result.CurrentEducationInstitution.RouteAccess.IsAccessible);
        Assert.Equal(0.35m, result.CurrentEducationInstitution.RouteAccess.AccessibilityIndex);
        Assert.Null(result.CurrentWorkplace);
    }

    [Fact]
    public async Task Handle_WhenResidentIsMissing_ThrowsNotFound()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var handler = CreateHandler();

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(
            async () => await handler.Handle(
                new GetCityResidentDetailsQuery(
                    CityId: cityId,
                    PersonId: residentId,
                    CurrentDate: new DateOnly(2048, 5, 5)),
                CancellationToken.None));

        Assert.Equal("Population.Person.NotFound", exception.Code);
    }

    private static GetCityResidentDetailsQueryHandler CreateHandler(
        ICityPopulationActiveTripClient? cityPopulationActiveTripClient = null,
        FakeCityPopulationAnchorCatalogRepository? cityPopulationAnchorCatalogRepository = null,
        ICityPopulationCommuteRoutingService? cityPopulationCommuteRoutingService = null,
        FakeCityPopulationPersonReadRepository? cityPopulationPersonReadRepository = null,
        FakePersonReadRepository? personReadRepository = null)
    {
        return new GetCityResidentDetailsQueryHandler(
            cityPopulationActiveTripClient ?? new FakeCityPopulationActiveTripClient(),
            cityPopulationAnchorCatalogRepository ?? new FakeCityPopulationAnchorCatalogRepository(),
            cityPopulationCommuteRoutingService ?? new FakeCityPopulationCommuteRoutingService(),
            cityPopulationPersonReadRepository ?? new FakeCityPopulationPersonReadRepository(),
            new CityPopulationAnchorSelectionPolicy(),
            personReadRepository ?? new FakePersonReadRepository());
    }
}
