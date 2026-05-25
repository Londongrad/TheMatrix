using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using Xunit.Sdk;
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class BirthAutonomyStepTests
    {
        private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

        private static readonly DateOnly PreviousDate = new(
            year: 2047,
            month: 11,
            day: 1);

        private static readonly DateOnly CurrentDate = new(
            year: 2048,
            month: 5,
            day: 1);

        private static readonly DateTimeOffset CreatedAtUtc = new(
            year: 2048,
            month: 1,
            day: 1,
            hour: 0,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        private static readonly DateTimeOffset OccurredAtUtc = new(
            year: 2048,
            month: 5,
            day: 1,
            hour: 12,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        [Fact]
        public async Task ApplyAsync_WhenPolicyPlansNoBirths_ReturnsZeroAndDoesNotMutateCollections()
        {
            CityBirthAutonomyPolicy policy = CreatePolicy();
            (PersonEntity mother, PersonEntity father, HouseholdId householdId) = FindStableBirthCouple(policy);
            Dictionary<PersonId, PersonEntity> residentsById = CreateResidentsById(
                mother,
                father);
            var residents = new List<PersonEntity>
            {
                mother,
                father
            };
            var personWriteRepository = new RecordingPersonWriteRepository();
            var householdWriteRepository = new RecordingHouseholdWriteRepository();
            List<CityPopulationActivityWriteModel> activityEntries = [];

            int affected = await BirthAutonomyStep.ApplyAsync(
                cityId: TestCityId,
                residentsById: residentsById,
                housingStatusesByHouseholdId: CreateHousingMap(householdId),
                previousDate: CurrentDate,
                currentDate: CurrentDate,
                birthAutonomyPolicy: policy,
                populationBirthDomainService: new PopulationBirthDomainService(),
                personWriteRepository: personWriteRepository,
                householdWriteRepository: householdWriteRepository,
                activityEntries: activityEntries,
                residents: residents,
                occurredAtUtc: OccurredAtUtc,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 0,
                actual: affected);
            Assert.Empty(personWriteRepository.AddedPeople);
            Assert.Empty(householdWriteRepository.FindRequests);
            Assert.Empty(householdWriteRepository.UpdatedHouseholds);
            Assert.Empty(activityEntries);
            Assert.Equal(
                expected: 2,
                actual: residents.Count);
            Assert.Equal(
                expected: 2,
                actual: residentsById.Count);
        }

        [Fact]
        public async Task ApplyAsync_WhenBirthDecisionIsPlanned_RegistersNewbornPersistsHouseholdAndWritesActivity()
        {
            CityBirthAutonomyPolicy policy = CreatePolicy();
            (PersonEntity mother, PersonEntity father, HouseholdId householdId) = FindStableBirthCouple(policy);
            HouseholdEntity household = CreateHousehold(householdId);
            Dictionary<PersonId, PersonEntity> residentsById = CreateResidentsById(
                mother,
                father);
            var residents = new List<PersonEntity>
            {
                mother,
                father
            };
            var callOrder = new List<string>();
            var personWriteRepository = new RecordingPersonWriteRepository(callOrder);
            var householdWriteRepository = new RecordingHouseholdWriteRepository(callOrder);
            householdWriteRepository.AddHousehold(household);
            List<CityPopulationActivityWriteModel> activityEntries = [];

            int affected = await BirthAutonomyStep.ApplyAsync(
                cityId: TestCityId,
                residentsById: residentsById,
                housingStatusesByHouseholdId: CreateHousingMap(householdId),
                previousDate: PreviousDate,
                currentDate: CurrentDate,
                birthAutonomyPolicy: policy,
                populationBirthDomainService: new PopulationBirthDomainService(),
                personWriteRepository: personWriteRepository,
                householdWriteRepository: householdWriteRepository,
                activityEntries: activityEntries,
                residents: residents,
                occurredAtUtc: OccurredAtUtc,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: affected);
            PersonEntity newborn = Assert.Single(personWriteRepository.AddedPeople);
            Assert.Equal(
                expected: householdId,
                actual: newborn.HouseholdId);
            Assert.Equal(
                expected: mother.Id,
                actual: newborn.MotherId);
            Assert.Equal(
                expected: father.Id,
                actual: newborn.FatherId);
            Assert.Equal(
                expected: CurrentDate,
                actual: newborn.BirthDate);
            Assert.Equal(
                expected: 3,
                actual: household.Size.Value);
            Assert.Same(
                expected: household,
                actual: Assert.Single(householdWriteRepository.UpdatedHouseholds));
            Assert.Contains(
                expected: newborn,
                collection: residents);
            Assert.Same(
                expected: newborn,
                actual: residentsById[newborn.Id]);
            Assert.Equal(
                expected: CurrentDate,
                actual: mother.LastChildbirthDate);
            Assert.Equal(
                expected:
                [
                    "person:add",
                    "household:update"
                ],
                actual: callOrder);

            CityPopulationActivityWriteModel activity = Assert.Single(activityEntries);
            Assert.Equal(
                expected: CityPopulationActivityEventType.ResidentBorn,
                actual: activity.EventType);
            Assert.Equal(
                expected: CityPopulationActivitySource.Autonomy,
                actual: activity.Source);
            Assert.Equal(
                expected: CityPopulationActivitySeverity.Success,
                actual: activity.Severity);
            Assert.Equal(
                expected: TestCityId.Value,
                actual: activity.CityId);
            Assert.Equal(
                expected: CurrentDate,
                actual: activity.CurrentDate);
            Assert.Equal(
                expected: OccurredAtUtc,
                actual: activity.OccurredAtUtc);
            Assert.Equal(
                expected: newborn.Id.Value,
                actual: activity.PrimaryResidentId);
            Assert.Equal(
                expected: mother.Id.Value,
                actual: activity.SecondaryResidentId);
        }

        [Fact]
        public async Task ApplyAsync_WhenMotherHouseholdIsMissing_SkipsBirth()
        {
            CityBirthAutonomyPolicy policy = CreatePolicy();
            (PersonEntity mother, PersonEntity father, HouseholdId householdId) = FindStableBirthCouple(policy);
            Dictionary<PersonId, PersonEntity> residentsById = CreateResidentsById(
                mother,
                father);
            var residents = new List<PersonEntity>
            {
                mother,
                father
            };
            var personWriteRepository = new RecordingPersonWriteRepository();
            var householdWriteRepository = new RecordingHouseholdWriteRepository();
            List<CityPopulationActivityWriteModel> activityEntries = [];

            int affected = await BirthAutonomyStep.ApplyAsync(
                cityId: TestCityId,
                residentsById: residentsById,
                housingStatusesByHouseholdId: CreateHousingMap(householdId),
                previousDate: PreviousDate,
                currentDate: CurrentDate,
                birthAutonomyPolicy: policy,
                populationBirthDomainService: new PopulationBirthDomainService(),
                personWriteRepository: personWriteRepository,
                householdWriteRepository: householdWriteRepository,
                activityEntries: activityEntries,
                residents: residents,
                occurredAtUtc: OccurredAtUtc,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 0,
                actual: affected);
            Assert.Equal(
                expected: [householdId],
                actual: householdWriteRepository.FindRequests);
            Assert.Empty(personWriteRepository.AddedPeople);
            Assert.Empty(householdWriteRepository.UpdatedHouseholds);
            Assert.Empty(activityEntries);
            Assert.Equal(
                expected: 2,
                actual: residents.Count);
            Assert.Equal(
                expected: 2,
                actual: residentsById.Count);
            Assert.Null(mother.LastChildbirthDate);
        }

        private static CityBirthAutonomyPolicy CreatePolicy()
        {
            return new CityBirthAutonomyPolicy(
                contentCatalog: new TestPopulationGenerationContentCatalog(),
                householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy());
        }

        private static (PersonEntity Mother, PersonEntity Father, HouseholdId HouseholdId) FindStableBirthCouple(
            CityBirthAutonomyPolicy policy)
        {
            for (int seed = 1; seed <= 2_000; seed++)
            {
                var motherId = PersonId.From(CreateGuid(70_000 + seed));
                var fatherId = PersonId.From(CreateGuid(80_000 + seed));
                var householdId = HouseholdId.From(CreateGuid(90_000 + seed));
                PersonEntity mother = CreateParent(
                    personId: motherId,
                    spouseId: fatherId,
                    householdId: householdId,
                    sex: Sex.Female,
                    birthDate: new DateOnly(
                        year: 2020,
                        month: 5,
                        day: 1));
                PersonEntity father = CreateParent(
                    personId: fatherId,
                    spouseId: motherId,
                    householdId: householdId,
                    sex: Sex.Male,
                    birthDate: new DateOnly(
                        year: 2018,
                        month: 5,
                        day: 1));

                IReadOnlyList<CityBirthAutonomyDecision> decisions = policy.Plan(
                    residents:
                    [
                        mother,
                        father
                    ],
                    housingStatuses: CreateHousingMap(householdId),
                    previousDate: PreviousDate,
                    currentDate: CurrentDate);

                if (decisions.Count == 1)
                    return (mother, father, householdId);
            }

            throw new XunitException("Expected deterministic stable couple to schedule childbirth.");
        }

        private static Dictionary<PersonId, PersonEntity> CreateResidentsById(params PersonEntity[] residents)
        {
            return residents.ToDictionary(x => x.Id);
        }

        private static IReadOnlyDictionary<HouseholdId, HousingStatus> CreateHousingMap(HouseholdId householdId)
        {
            return new Dictionary<HouseholdId, HousingStatus>
            {
                [householdId] = HousingStatus.Housed
            };
        }

        private static HouseholdEntity CreateHousehold(HouseholdId householdId)
        {
            return HouseholdEntity.Create(
                id: householdId,
                size: HouseholdSize.From(2),
                createdAtUtc: CreatedAtUtc);
        }

        private static PersonEntity CreateParent(
            PersonId personId,
            PersonId spouseId,
            HouseholdId householdId,
            Sex sex,
            DateOnly birthDate)
        {
            return PersonEntity.CreatePerson(
                id: personId,
                householdId: householdId,
                name: new PersonName(
                    firstName: sex == Sex.Female
                        ? "Anna"
                        : "Mikhail",
                    lastName: "Petrov"),
                sex: sex,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: MaritalStatus.Married,
                spouseId: spouseId,
                educationLevel: EducationLevel.Postgraduate,
                educationInstitutionId: null,
                educationInstitutionAnchorId: null,
                employmentStatus: EmploymentStatus.Employed,
                happinessLevel: HappinessLevel.From(100),
                energyLevel: EnergyLevel.From(90),
                stressLevel: StressLevel.From(0),
                socialNeedLevel: SocialNeedLevel.From(100),
                personality: Personality.Create(
                    optimism: 100,
                    discipline: 100,
                    riskTolerance: 50,
                    sociability: 100),
                birthDate: birthDate,
                healthLevel: HealthLevel.From(100),
                weight: BodyWeight.FromKilograms(70m),
                job: new Job(
                    workplaceId: WorkplaceId.From(Guid.Parse("77777777-aaaa-bbbb-cccc-777777777777")),
                    title: "Engineer",
                    workplaceAnchorId: CityAnchorId.From(Guid.Parse("88888888-aaaa-bbbb-cccc-888888888888"))),
                currentDate: CurrentDate,
                illness: IllnessInfo.Healthy());
        }

        private static Guid CreateGuid(int seed)
        {
            return Guid.Parse($"00000000-0000-0000-0000-{seed:000000000000}");
        }

        private sealed class RecordingPersonWriteRepository(List<string>? callOrder = null) : IPersonWriteRepository
        {
            private readonly List<string>? _callOrder = callOrder;

            public List<PersonEntity> AddedPeople { get; } = [];

            public Task DeleteAllAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task DeleteAsync(
                PersonEntity person,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task AddRangeAsync(
                IReadOnlyCollection<PersonEntity> persons,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task AddAsync(
                PersonEntity person,
                CancellationToken cancellationToken = default)
            {
                _callOrder?.Add("person:add");
                AddedPeople.Add(person);
                return Task.CompletedTask;
            }

            public Task UpdateAsync(
                PersonEntity person,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class RecordingHouseholdWriteRepository(List<string>? callOrder = null)
            : IHouseholdWriteRepository
        {
            private readonly List<string>? _callOrder = callOrder;
            private readonly Dictionary<HouseholdId, HouseholdEntity> _households = [];

            public List<HouseholdId> FindRequests { get; } = [];
            public List<HouseholdEntity> UpdatedHouseholds { get; } = [];

            public Task<HouseholdEntity?> FindByIdAsync(
                HouseholdId householdId,
                CancellationToken cancellationToken = default)
            {
                FindRequests.Add(householdId);
                _households.TryGetValue(
                    key: householdId,
                    value: out HouseholdEntity? household);
                return Task.FromResult(household);
            }

            public Task<ClassicCityHouseholdPlacement?> FindPlacementByHouseholdIdAsync(
                HouseholdId householdId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<IReadOnlyCollection<ClassicCityHouseholdPlacement>> ListPlacementsByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<IReadOnlyCollection<HouseholdEntity>> ListByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<int> CountResidentsAsync(
                HouseholdId householdId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task DeleteAllAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task DeleteByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task DeleteAsync(
                HouseholdEntity household,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task AddRangeAsync(
                IReadOnlyCollection<HouseholdEntity> households,
                IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task AddAsync(
                HouseholdEntity household,
                ClassicCityHouseholdPlacement householdPlacement,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task UpdateAsync(
                HouseholdEntity household,
                CancellationToken cancellationToken = default)
            {
                _callOrder?.Add("household:update");
                UpdatedHouseholds.Add(household);
                return Task.CompletedTask;
            }

            public void AddHousehold(HouseholdEntity household)
            {
                _households[household.Id] = household;
            }
        }

        private sealed class TestPopulationGenerationContentCatalog : IPopulationGenerationContentCatalog
        {
            public IReadOnlyList<string> MaleFirstNames => ["Mikhail"];
            public IReadOnlyList<string> FemaleFirstNames => ["Anna"];
            public IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnames => [];
            public IReadOnlyList<PopulationProfessionCatalogItem> Professions => [];
        }
    }
}
