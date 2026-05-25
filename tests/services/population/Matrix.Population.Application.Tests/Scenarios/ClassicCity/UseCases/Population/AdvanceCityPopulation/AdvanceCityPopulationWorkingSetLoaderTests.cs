using Matrix.BuildingBlocks.Domain.ValueObjects;
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
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;

public sealed class AdvanceCityPopulationWorkingSetLoaderTests
{
    private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
    private static readonly DateOnly CurrentDate = new(2048, 5, 6);
    private static readonly DateTimeOffset CreatedAtUtc = new(2048, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EvaluatedAtUtc = new(2048, 5, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LoadAsync_BuildsResidentHouseholdPlacementAnchorAndStressMaps()
    {
        HouseholdId firstHouseholdId = HouseholdId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        HouseholdId secondHouseholdId = HouseholdId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        DistrictId districtId = DistrictId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        ResidentialBuildingId residentialBuildingId = ResidentialBuildingId.From(Guid.Parse("44444444-4444-4444-4444-444444444444"));
        WorkplaceId workplaceId = WorkplaceId.From(Guid.Parse("55555555-5555-5555-5555-555555555555"));
        CityAnchorId workplaceAnchorId = CityAnchorId.From(Guid.Parse("66666666-6666-6666-6666-666666666666"));
        CityAnchorId schoolAnchorId = CityAnchorId.From(Guid.Parse("77777777-7777-7777-7777-777777777777"));
        CityAnchorId hospitalAnchorId = CityAnchorId.From(Guid.Parse("88888888-8888-8888-8888-888888888888"));
        PersonEntity firstResident = CreatePerson(
            personId: Guid.Parse("99999999-9999-9999-9999-999999999991"),
            householdId: firstHouseholdId.Value);
        PersonEntity secondResident = CreatePerson(
            personId: Guid.Parse("99999999-9999-9999-9999-999999999992"),
            householdId: firstHouseholdId.Value);
        PersonEntity thirdResident = CreatePerson(
            personId: Guid.Parse("99999999-9999-9999-9999-999999999993"),
            householdId: secondHouseholdId.Value);
        ClassicCityHouseholdPlacement housedPlacement = CreateHousedPlacement(
            householdId: firstHouseholdId,
            districtId: districtId,
            residentialBuildingId: residentialBuildingId);
        ClassicCityHouseholdPlacement homelessPlacement = ClassicCityHouseholdPlacement.CreateHomeless(
            householdId: secondHouseholdId,
            cityId: TestCityId);
        HouseholdEntity firstHousehold = CreateHousehold(firstHouseholdId, size: 2);
        HouseholdEntity secondHousehold = CreateHousehold(secondHouseholdId, size: 1);
        CityPopulationHouseholdFinancialStressState householdStress =
            CreateHouseholdFinancialStressState(firstHouseholdId);
        CityPopulationEmployerFinancialStressState employerStress =
            CreateEmployerFinancialStressState(workplaceId);
        var personReadRepository = new FakeCityPopulationPersonReadRepository
        {
            ListByCityResult = [firstResident, secondResident, thirdResident]
        };
        var householdWriteRepository = new FakeHouseholdWriteRepository
        {
            PlacementsByCityResult = [housedPlacement, homelessPlacement],
            HouseholdsByCityResult = [firstHousehold, secondHousehold]
        };
        var householdStressRepository = new FakeCityPopulationHouseholdFinancialStressStateRepository();
        householdStressRepository.States.Add(householdStress);
        var employerStressRepository = new FakeCityPopulationEmployerFinancialStressStateRepository();
        employerStressRepository.States.Add(employerStress);
        var anchorRepository = new FakeCityPopulationAnchorCatalogRepository
        {
            Items =
            [
                CreateAnchor(workplaceAnchorId, CityAnchorType.Workplace, TestCityId),
                CreateAnchor(schoolAnchorId, CityAnchorType.School, TestCityId),
                CreateAnchor(hospitalAnchorId, CityAnchorType.Hospital, TestCityId),
                CreateAnchor(
                    CityAnchorId.From(Guid.Parse("99999999-9999-9999-9999-999999999999")),
                    CityAnchorType.Workplace,
                    CityId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")))
            ]
        };

        AdvanceCityPopulationWorkingSet workingSet = await LoadAsync(
            personReadRepository: personReadRepository,
            householdWriteRepository: householdWriteRepository,
            householdStressRepository: householdStressRepository,
            employerStressRepository: employerStressRepository,
            anchorRepository: anchorRepository);

        Assert.Equal(TestCityId, personReadRepository.RequestedCityId);
        Assert.Equal(TestCityId, householdWriteRepository.RequestedCityId);
        Assert.Equal(TestCityId, householdStressRepository.RequestedCityId);
        Assert.Equal(TestCityId, employerStressRepository.RequestedCityId);
        Assert.Equal([firstResident, secondResident, thirdResident], workingSet.Residents);
        Assert.Same(firstResident, workingSet.ResidentsById[firstResident.Id]);
        Assert.Same(secondResident, workingSet.ResidentsById[secondResident.Id]);
        Assert.Same(thirdResident, workingSet.ResidentsById[thirdResident.Id]);
        Assert.Equal(2, workingSet.ResidentsByHouseholdId[firstHouseholdId].Count);
        Assert.Single(workingSet.ResidentsByHouseholdId[secondHouseholdId]);
        Assert.Equal([firstHousehold, secondHousehold], workingSet.Households);
        Assert.Same(firstHousehold, workingSet.HouseholdsById[firstHouseholdId]);
        Assert.Same(secondHousehold, workingSet.HouseholdsById[secondHouseholdId]);
        Assert.Equal([housedPlacement, homelessPlacement], workingSet.Placements);
        Assert.Equal(HousingStatus.Housed, workingSet.HousingByHouseholdId[firstHouseholdId]);
        Assert.Equal(HousingStatus.Homeless, workingSet.HousingByHouseholdId[secondHouseholdId]);
        Assert.Equal(districtId, workingSet.DistrictByHouseholdId[firstHouseholdId]);
        Assert.Null(workingSet.DistrictByHouseholdId[secondHouseholdId]);
        Assert.Equal(residentialBuildingId, workingSet.ResidentialBuildingByHouseholdId[firstHouseholdId]);
        Assert.Null(workingSet.ResidentialBuildingByHouseholdId[secondHouseholdId]);
        Assert.Same(householdStress, workingSet.FinancialStressByHouseholdId[firstHouseholdId]);
        Assert.Same(employerStress, workingSet.EmployerStressByWorkplaceId[workplaceId]);
        Assert.Equal(workplaceAnchorId, Assert.Single(workingSet.WorkplaceAnchors).CityAnchorId);
        Assert.Equal(schoolAnchorId, Assert.Single(workingSet.SchoolAnchors).CityAnchorId);
        Assert.Equal(hospitalAnchorId, Assert.Single(workingSet.HospitalAnchors).CityAnchorId);
    }

    [Fact]
    public async Task LoadAsync_EvaluatesHealthcarePressureFromLoadedResidents()
    {
        PersonEntity healthyResident = CreatePerson(
            personId: Guid.Parse("11111111-2222-3333-4444-555555555551"),
            currentDate: CurrentDate);
        PersonEntity severeResident = CreatePerson(
            personId: Guid.Parse("11111111-2222-3333-4444-555555555552"),
            currentDate: CurrentDate);
        severeResident.DiagnoseIllness(
            kind: IllnessKind.Infection,
            severity: IllnessSeverity.Severe,
            currentDate: CurrentDate);
        var healthcarePressurePolicy = new CityPopulationHealthcarePressurePolicy();
        CityPopulationHealthcarePressureProfile expectedProfile = healthcarePressurePolicy.Evaluate(
            residents: [healthyResident, severeResident]);
        var personReadRepository = new FakeCityPopulationPersonReadRepository
        {
            ListByCityResult = [healthyResident, severeResident]
        };

        AdvanceCityPopulationWorkingSet workingSet = await LoadAsync(
            personReadRepository: personReadRepository,
            healthcarePressurePolicy: healthcarePressurePolicy);

        Assert.Equal(expectedProfile, workingSet.HealthcarePressureProfile);
    }

    [Fact]
    public async Task LoadAsync_BuildsEducationInstitutionAndWorkplacePoolsFromLoadedResidents()
    {
        EducationInstitutionId institutionId = EducationInstitutionId.From(Guid.Parse("22222222-3333-4444-5555-666666666661"));
        CityAnchorId institutionAnchorId = CityAnchorId.From(Guid.Parse("22222222-3333-4444-5555-666666666662"));
        WorkplaceId workplaceId = WorkplaceId.From(Guid.Parse("22222222-3333-4444-5555-666666666663"));
        CityAnchorId workplaceAnchorId = CityAnchorId.From(Guid.Parse("22222222-3333-4444-5555-666666666664"));
        PersonEntity student = CreateStudent(
            institutionId: institutionId,
            institutionAnchorId: institutionAnchorId);
        PersonEntity employedResident = CreatePerson(
            personId: Guid.Parse("22222222-3333-4444-5555-666666666665"),
            employmentStatus: EmploymentStatus.Employed,
            job: new Job(
                workplaceId: workplaceId,
                title: "Engineer",
                workplaceAnchorId: workplaceAnchorId));
        var personReadRepository = new FakeCityPopulationPersonReadRepository
        {
            ListByCityResult = [student, employedResident]
        };

        AdvanceCityPopulationWorkingSet workingSet = await LoadAsync(
            personReadRepository: personReadRepository);

        CityEducationInstitutionBinding institutionBinding =
            Assert.Single(workingSet.InstitutionPools[EducationLevel.UpperSecondary]);
        Assert.Equal(institutionId, institutionBinding.InstitutionId);
        Assert.Equal(institutionAnchorId, institutionBinding.InstitutionAnchorId);
        Job job = Assert.Single(workingSet.WorkplacePools["Engineer"]);
        Assert.Equal(workplaceId, job.WorkplaceId);
        Assert.Equal(workplaceAnchorId, job.WorkplaceAnchorId);
    }

    private static Task<AdvanceCityPopulationWorkingSet> LoadAsync(
        FakeCityPopulationPersonReadRepository? personReadRepository = null,
        FakeHouseholdWriteRepository? householdWriteRepository = null,
        FakeCityPopulationHouseholdFinancialStressStateRepository? householdStressRepository = null,
        FakeCityPopulationEmployerFinancialStressStateRepository? employerStressRepository = null,
        FakeCityPopulationAnchorCatalogRepository? anchorRepository = null,
        CityPopulationHealthcarePressurePolicy? healthcarePressurePolicy = null)
    {
        return AdvanceCityPopulationWorkingSetLoader.LoadAsync(
            cityId: TestCityId,
            personReadRepository: personReadRepository ?? new FakeCityPopulationPersonReadRepository(),
            householdWriteRepository: householdWriteRepository ?? new FakeHouseholdWriteRepository(),
            householdFinancialStressStateRepository: householdStressRepository ??
                                                     new FakeCityPopulationHouseholdFinancialStressStateRepository(),
            employerFinancialStressStateRepository: employerStressRepository ??
                                                    new FakeCityPopulationEmployerFinancialStressStateRepository(),
            cityPopulationAnchorCatalogRepository: anchorRepository ?? new FakeCityPopulationAnchorCatalogRepository(),
            healthcarePressurePolicy: healthcarePressurePolicy ?? new CityPopulationHealthcarePressurePolicy(),
            serviceQualityState: null,
            livingConditionsState: null,
            essentialsState: null,
            cancellationToken: CancellationToken.None);
    }

    private static HouseholdEntity CreateHousehold(
        HouseholdId householdId,
        int size)
    {
        return HouseholdEntity.Create(
            id: householdId,
            size: HouseholdSize.From(size),
            createdAtUtc: CreatedAtUtc,
            cashReserve: Money.FromDecimal(1_000m));
    }

    private static ClassicCityHouseholdPlacement CreateHousedPlacement(
        HouseholdId householdId,
        DistrictId districtId,
        ResidentialBuildingId residentialBuildingId)
    {
        return ClassicCityHouseholdPlacement.CreateHoused(
            householdId: householdId,
            cityId: TestCityId,
            districtId: districtId,
            residentialBuildingId: residentialBuildingId);
    }

    private static CityPopulationAnchorCatalogItem CreateAnchor(
        CityAnchorId anchorId,
        CityAnchorType type,
        CityId cityId)
    {
        return CityPopulationAnchorCatalogItem.Create(
            cityId: cityId,
            cityAnchorId: anchorId,
            districtId: DistrictId.From(Guid.NewGuid()),
            accessRoadNodeId: RoadNodeId.From(Guid.NewGuid()),
            name: type.ToString(),
            type: type,
            capacity: 100,
            positionX: 0m,
            positionY: 0m,
            createdAtUtc: CreatedAtUtc);
    }

    private static CityPopulationHouseholdFinancialStressState CreateHouseholdFinancialStressState(
        HouseholdId householdId)
    {
        return CityPopulationHouseholdFinancialStressState.Create(
            cityId: TestCityId,
            householdId: householdId,
            overdueObligationCount: 1,
            overdueRentCount: 1,
            overdueUtilityCount: 0,
            arrearsObligationCount: 1,
            serviceCutoffCount: 0,
            evictionNoticeCount: 0,
            evictionEligibleCount: 0,
            oldestOverdueAgeDays: 12,
            totalOverdueAmount: 250m,
            distressScore: 0.35m,
            lastEvaluatedAtUtc: EvaluatedAtUtc,
            updatedAtUtc: EvaluatedAtUtc);
    }

    private static CityPopulationEmployerFinancialStressState CreateEmployerFinancialStressState(
        WorkplaceId workplaceId)
    {
        return CityPopulationEmployerFinancialStressState.Create(
            cityId: TestCityId,
            workplaceId: workplaceId,
            requestedGrossPayrollAmount: 1_000m,
            paidGrossPayrollAmount: 700m,
            missedGrossPayrollAmount: 300m,
            payrollFulfillmentRatio: 0.7m,
            failedPayrollCount: 0,
            partialPayrollCount: 1,
            currentBalanceAmount: -100m,
            distressScore: 0.45m,
            hasHiringFreeze: true,
            hasLayoffPressure: false,
            lastEvaluatedAtUtc: EvaluatedAtUtc,
            updatedAtUtc: EvaluatedAtUtc);
    }

    private static PersonEntity CreateStudent(
        EducationInstitutionId institutionId,
        CityAnchorId institutionAnchorId)
    {
        PersonEntity student = CreatePerson(
            personId: Guid.Parse("22222222-3333-4444-5555-666666666660"),
            currentDate: CurrentDate);
        student.StartStudying(
            currentDate: CurrentDate,
            institutionId: institutionId,
            institutionAnchorId: institutionAnchorId);

        return student;
    }
}
