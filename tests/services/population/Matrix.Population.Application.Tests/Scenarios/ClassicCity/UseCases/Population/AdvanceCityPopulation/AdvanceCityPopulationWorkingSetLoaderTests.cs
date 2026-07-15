using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
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

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class AdvanceCityPopulationWorkingSetLoaderTests
    {
        private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

        private static readonly DateOnly CurrentDate = new(
            year: 2048,
            month: 5,
            day: 6);

        private static readonly DateTimeOffset CreatedAtUtc = new(
            year: 2048,
            month: 1,
            day: 1,
            hour: 0,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        private static readonly DateTimeOffset EvaluatedAtUtc = new(
            year: 2048,
            month: 5,
            day: 6,
            hour: 12,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        [Fact]
        public async Task LoadAsync_BuildsResidentHouseholdPlacementAnchorAndStressMaps()
        {
            var firstHouseholdId = HouseholdId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            var secondHouseholdId = HouseholdId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
            var districtId = DistrictId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
            var residentialBuildingId = ResidentialBuildingId.From(Guid.Parse("44444444-4444-4444-4444-444444444444"));
            var workplaceId = WorkplaceId.From(Guid.Parse("55555555-5555-5555-5555-555555555555"));
            var workplaceAnchorId = CityAnchorId.From(Guid.Parse("66666666-6666-6666-6666-666666666666"));
            var hospitalAnchorId = CityAnchorId.From(Guid.Parse("88888888-8888-8888-8888-888888888888"));
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
            var homelessPlacement = ClassicCityHouseholdPlacement.CreateHomeless(
                householdId: secondHouseholdId,
                cityId: TestCityId);
            HouseholdEntity firstHousehold = CreateHousehold(
                householdId: firstHouseholdId,
                size: 2);
            HouseholdEntity secondHousehold = CreateHousehold(
                householdId: secondHouseholdId,
                size: 1);
            CityPopulationHouseholdFinancialStressState householdStress =
                CreateHouseholdFinancialStressState(firstHouseholdId);
            CityPopulationEmployerFinancialStressState employerStress =
                CreateEmployerFinancialStressState(workplaceId);
            var personReadRepository = new FakeCityPopulationPersonReadRepository
            {
                ListByCityResult =
                [
                    firstResident,
                    secondResident,
                    thirdResident
                ]
            };
            var householdWriteRepository = new FakeHouseholdWriteRepository
            {
                PlacementsByCityResult =
                [
                    housedPlacement,
                    homelessPlacement
                ],
                HouseholdsByCityResult =
                [
                    firstHousehold,
                    secondHousehold
                ]
            };
            var householdStressRepository = new FakeCityPopulationHouseholdFinancialStressStateRepository();
            householdStressRepository.States.Add(householdStress);
            var employerStressRepository = new FakeCityPopulationEmployerFinancialStressStateRepository();
            employerStressRepository.States.Add(employerStress);
            var anchorRepository = new FakeCityPopulationAnchorCatalogRepository
            {
                Items =
                [
                    CreateAnchor(
                        anchorId: workplaceAnchorId,
                        type: CityAnchorType.Workplace,
                        cityId: TestCityId),
                    CreateAnchor(
                        anchorId: hospitalAnchorId,
                        type: CityAnchorType.Hospital,
                        cityId: TestCityId),
                    CreateAnchor(
                        anchorId: CityAnchorId.From(Guid.Parse("99999999-9999-9999-9999-999999999999")),
                        type: CityAnchorType.Workplace,
                        cityId: CityId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")))
                ]
            };
            var educationProjectionRepository = new FakeEducationParticipationProjectionRepository();

            AdvanceCityPopulationWorkingSet workingSet = await LoadAsync(
                personReadRepository: personReadRepository,
                householdWriteRepository: householdWriteRepository,
                householdStressRepository: householdStressRepository,
                employerStressRepository: employerStressRepository,
                anchorRepository: anchorRepository,
                educationProjectionRepository: educationProjectionRepository);

            Assert.Equal(
                expected: TestCityId,
                actual: personReadRepository.RequestedCityId);
            Assert.Equal(
                expected: TestCityId,
                actual: householdWriteRepository.RequestedCityId);
            Assert.Equal(
                expected: TestCityId,
                actual: householdStressRepository.RequestedCityId);
            Assert.Equal(
                expected: TestCityId,
                actual: employerStressRepository.RequestedCityId);
            Assert.Equal(
                expected: 1,
                actual: educationProjectionRepository.GetByResidentIdsCallCount);
            Assert.Equal(
                expected: TestCityId.Value,
                actual: educationProjectionRepository.RequestedSimulationHostId);
            Assert.Equal(
                expected:
                [
                    firstResident.Id.Value,
                    secondResident.Id.Value,
                    thirdResident.Id.Value
                ],
                actual: educationProjectionRepository.RequestedResidentIds);
            Assert.Equal(
                expected:
                [
                    firstResident,
                    secondResident,
                    thirdResident
                ],
                actual: workingSet.Residents);
            Assert.Same(
                expected: firstResident,
                actual: workingSet.ResidentsById[firstResident.Id]);
            Assert.Same(
                expected: secondResident,
                actual: workingSet.ResidentsById[secondResident.Id]);
            Assert.Same(
                expected: thirdResident,
                actual: workingSet.ResidentsById[thirdResident.Id]);
            Assert.Equal(
                expected: 2,
                actual: workingSet.ResidentsByHouseholdId[firstHouseholdId].Count);
            Assert.Single(workingSet.ResidentsByHouseholdId[secondHouseholdId]);
            Assert.Null(workingSet.EducationParticipation.FindCurrent(firstResident));
            Assert.Equal(
                expected:
                [
                    firstHousehold,
                    secondHousehold
                ],
                actual: workingSet.Households);
            Assert.Same(
                expected: firstHousehold,
                actual: workingSet.HouseholdsById[firstHouseholdId]);
            Assert.Same(
                expected: secondHousehold,
                actual: workingSet.HouseholdsById[secondHouseholdId]);
            Assert.Equal(
                expected:
                [
                    housedPlacement,
                    homelessPlacement
                ],
                actual: workingSet.Placements);
            Assert.Equal(
                expected: HousingStatus.Housed,
                actual: workingSet.HousingByHouseholdId[firstHouseholdId]);
            Assert.Equal(
                expected: HousingStatus.Homeless,
                actual: workingSet.HousingByHouseholdId[secondHouseholdId]);
            Assert.Equal(
                expected: districtId,
                actual: workingSet.DistrictByHouseholdId[firstHouseholdId]);
            Assert.Null(workingSet.DistrictByHouseholdId[secondHouseholdId]);
            Assert.Equal(
                expected: residentialBuildingId,
                actual: workingSet.ResidentialBuildingByHouseholdId[firstHouseholdId]);
            Assert.Null(workingSet.ResidentialBuildingByHouseholdId[secondHouseholdId]);
            Assert.Same(
                expected: householdStress,
                actual: workingSet.FinancialStressByHouseholdId[firstHouseholdId]);
            Assert.Same(
                expected: employerStress,
                actual: workingSet.EmployerStressByWorkplaceId[workplaceId]);
            Assert.Equal(
                expected: workplaceAnchorId,
                actual: Assert.Single(workingSet.WorkplaceAnchors)
                   .CityAnchorId);
            Assert.Equal(
                expected: hospitalAnchorId,
                actual: Assert.Single(workingSet.HospitalAnchors)
                   .CityAnchorId);
            Assert.DoesNotContain(
                anchorRepository.ListRequests,
                request => request.Type == CityAnchorType.School);
        }

        [Fact]
        public async Task LoadAsync_UsesHealthcareOwnedPressureProjection()
        {
            var expectedProfile = new CityPopulationHealthcarePressureProfile(
                ActiveIllnessCount: 8,
                SevereIllnessCount: 2,
                MedicalLoadIndex: 0.82m,
                TriagePressureIndex: 0.34m,
                RecoverySupportIndex: 1.12m);
            var snapshotRepository = new FakeCityHealthcarePressureSnapshotRepository
            {
                Snapshot = new ClassicCityHealthcarePressureSnapshot(
                    CityId: TestCityId,
                    SourceRevision: 17,
                    CurrentDate: CurrentDate,
                    PatientCount: 100,
                    Pressure: expectedProfile,
                    OccurredAtUtc: EvaluatedAtUtc,
                    UpdatedAtUtc: EvaluatedAtUtc)
            };

            AdvanceCityPopulationWorkingSet workingSet = await LoadAsync(
                healthcarePressureSnapshotRepository: snapshotRepository);

            Assert.Equal(
                expected: expectedProfile,
                actual: workingSet.HealthcarePressureProfile);
        }

        [Fact]
        public async Task LoadAsync_WhenEducationParticipationIsNotRequired_SkipsProjectionRead()
        {
            PersonEntity resident = CreatePerson();
            var projectionRepository = new FakeEducationParticipationProjectionRepository();
            var personReadRepository = new FakeCityPopulationPersonReadRepository
            {
                ListByCityResult =
                [
                    resident
                ]
            };

            AdvanceCityPopulationWorkingSet workingSet = await LoadAsync(
                personReadRepository: personReadRepository,
                educationProjectionRepository: projectionRepository,
                includeEducationParticipation: false);

            Assert.Equal(
                expected: 0,
                actual: projectionRepository.GetByResidentIdsCallCount);
            Assert.Null(workingSet.EducationParticipation.FindCurrent(resident));
        }

        [Fact]
        public async Task LoadAsync_BuildsWorkplacePoolsFromLoadedResidents()
        {
            var workplaceId = WorkplaceId.From(Guid.Parse("22222222-3333-4444-5555-666666666663"));
            var workplaceAnchorId = CityAnchorId.From(Guid.Parse("22222222-3333-4444-5555-666666666664"));
            PersonEntity employedResident = CreatePerson(
                personId: Guid.Parse("22222222-3333-4444-5555-666666666665"),
                employmentStatus: EmploymentStatus.Employed,
                job: new Job(
                    workplaceId: workplaceId,
                    title: "Engineer",
                    workplaceAnchorId: workplaceAnchorId));
            var personReadRepository = new FakeCityPopulationPersonReadRepository
            {
                ListByCityResult =
                [
                    employedResident
                ]
            };

            AdvanceCityPopulationWorkingSet workingSet = await LoadAsync(personReadRepository: personReadRepository);

            Job job = Assert.Single(workingSet.WorkplacePools["Engineer"]);
            Assert.Equal(
                expected: workplaceId,
                actual: job.WorkplaceId);
            Assert.Equal(
                expected: workplaceAnchorId,
                actual: job.WorkplaceAnchorId);
        }

        private static Task<AdvanceCityPopulationWorkingSet> LoadAsync(
            FakeCityPopulationPersonReadRepository? personReadRepository = null,
            FakeHouseholdWriteRepository? householdWriteRepository = null,
            FakeCityPopulationHouseholdFinancialStressStateRepository? householdStressRepository = null,
            FakeCityPopulationEmployerFinancialStressStateRepository? employerStressRepository = null,
            FakeCityPopulationAnchorCatalogRepository? anchorRepository = null,
            FakeCityHealthcarePressureSnapshotRepository? healthcarePressureSnapshotRepository = null,
            FakeEducationParticipationProjectionRepository? educationProjectionRepository = null,
            bool includeEducationParticipation = true)
        {
            return AdvanceCityPopulationWorkingSetLoader.LoadAsync(
                cityId: TestCityId,
                personReadRepository: personReadRepository ?? new FakeCityPopulationPersonReadRepository(),
                householdWriteRepository: householdWriteRepository ?? new FakeHouseholdWriteRepository(),
                householdFinancialStressStateRepository: householdStressRepository ??
                                                         new FakeCityPopulationHouseholdFinancialStressStateRepository(),
                employerFinancialStressStateRepository: employerStressRepository ??
                                                        new FakeCityPopulationEmployerFinancialStressStateRepository(),
                cityPopulationAnchorCatalogRepository: anchorRepository ??
                                                       new FakeCityPopulationAnchorCatalogRepository(),
                healthcarePressureSnapshotRepository: healthcarePressureSnapshotRepository ??
                                                      new FakeCityHealthcarePressureSnapshotRepository(),
                educationParticipationProjectionRepository: educationProjectionRepository ??
                                                            new FakeEducationParticipationProjectionRepository(),
                includeEducationParticipation: includeEducationParticipation,
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

    }
}
