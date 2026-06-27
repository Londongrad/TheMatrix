using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Integration;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary;
using Matrix.Population.Contracts.Events;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Tests.TestSupport
{
    internal static class PopulationApplicationTestSupport
    {
        internal static readonly DateTimeOffset UtcNow = new(
            year: 2048,
            month: 5,
            day: 3,
            hour: 9,
            minute: 10,
            second: 11,
            offset: TimeSpan.Zero);

        internal static TimeProvider CreateTimeProvider(DateTimeOffset? utcNow = null)
        {
            return new FakeTimeProvider(utcNow ?? UtcNow);
        }

        internal static Person CreatePerson(
            Guid? personId = null,
            Guid? householdId = null,
            string firstName = "Neo",
            string lastName = "Anderson",
            Sex sex = Sex.Male,
            LifeStatus lifeStatus = LifeStatus.Alive,
            MaritalStatus maritalStatus = MaritalStatus.Single,
            PersonId? spouseId = null,
            DateOnly? birthDate = null,
            DateOnly? currentDate = null,
            EmploymentStatus employmentStatus = EmploymentStatus.Unemployed,
            int happiness = 50,
            int energy = 70,
            int stress = 25,
            int socialNeed = 35,
            int health = 80,
            Job? job = null)
        {
            DateOnly resolvedCurrentDate = currentDate ??
            new DateOnly(
                year: 2048,
                month: 5,
                day: 3);
            var person = Person.CreatePerson(
                id: PersonId.From(personId ?? Guid.Parse("11111111-1111-1111-1111-111111111111")),
                householdId: HouseholdId.From(householdId ?? Guid.Parse("22222222-2222-2222-2222-222222222222")),
                name: new PersonName(
                    firstName: firstName,
                    lastName: lastName),
                sex: sex,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: maritalStatus,
                spouseId: spouseId,
                educationLevel: EducationLevel.UpperSecondary,
                educationInstitutionId: null,
                educationInstitutionAnchorId: null,
                employmentStatus: employmentStatus,
                happinessLevel: HappinessLevel.From(happiness),
                energyLevel: EnergyLevel.From(energy),
                stressLevel: StressLevel.From(stress),
                socialNeedLevel: SocialNeedLevel.From(socialNeed),
                personality: Personality.Neutral(),
                birthDate: birthDate ??
                new DateOnly(
                    year: 2030,
                    month: 5,
                    day: 3),
                healthLevel: HealthLevel.From(health),
                weight: BodyWeight.FromKilograms(72m),
                job: job,
                currentDate: resolvedCurrentDate,
                illness: IllnessInfo.Healthy());

            if (lifeStatus == LifeStatus.Deceased)
                person.Die(resolvedCurrentDate);

            return person;
        }

        internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow()
            {
                return utcNow;
            }
        }

        internal sealed class FakeUnitOfWork : IUnitOfWork
        {
            public int SaveChangesCalls { get; private set; }
            public int ExecuteTransactionCalls { get; private set; }

            public Task SaveChangesAsync(CancellationToken cancellationToken)
            {
                SaveChangesCalls++;
                return Task.CompletedTask;
            }

            public Task ExecuteInTransactionAsync(
                Func<CancellationToken, Task> action,
                CancellationToken cancellationToken,
                IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            {
                ExecuteTransactionCalls++;
                return action(cancellationToken);
            }

            public Task<T> ExecuteInTransactionAsync<T>(
                Func<CancellationToken, Task<T>> action,
                CancellationToken cancellationToken,
                IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            {
                ExecuteTransactionCalls++;
                return action(cancellationToken);
            }
        }

        internal sealed class FakePersonReadRepository : IPersonReadRepository
        {
            public Person? PersonById { get; set; }
            public Dictionary<PersonId, Person> PersonsById { get; } = [];

            public (IReadOnlyCollection<Person> Items, int TotalCount) PageResult { get; set; } =
                (Array.Empty<Person>(), 0);

            public PersonId? RequestedPersonId { get; private set; }
            public Pagination? RequestedPagination { get; private set; }

            public Task<IReadOnlyCollection<Person>> GetAllAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<Person?> FindByIdAsync(
                PersonId id,
                CancellationToken cancellationToken = default)
            {
                RequestedPersonId = id;

                if (PersonsById.TryGetValue(
                        key: id,
                        value: out Person? person))
                    return Task.FromResult<Person?>(person);

                return Task.FromResult(PersonById);
            }

            public Task<(IReadOnlyCollection<Person> Items, int TotalCount)> GetPageAsync(
                Pagination pagination,
                CancellationToken cancellationToken = default)
            {
                RequestedPagination = pagination;
                return Task.FromResult(PageResult);
            }
        }

        internal sealed class FakePersonWriteRepository : IPersonWriteRepository
        {
            public List<Person> UpdatedPersons { get; } = [];
            public List<IReadOnlyCollection<Person>> AddedRanges { get; } = [];

            public Task DeleteAllAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task DeleteAsync(
                Person person,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task AddAsync(
                Person person,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task AddRangeAsync(
                IReadOnlyCollection<Person> persons,
                CancellationToken cancellationToken = default)
            {
                AddedRanges.Add(persons);
                return Task.CompletedTask;
            }

            public Task UpdateAsync(
                Person person,
                CancellationToken cancellationToken = default)
            {
                UpdatedPersons.Add(person);
                return Task.CompletedTask;
            }
        }

        internal sealed class FakeCityPopulationPersonReadRepository : ICityPopulationPersonReadRepository
        {
            public CityId? CityIdByPersonId { get; set; }
            public Dictionary<PersonId, CityId?> CityIdByPersonIds { get; } = [];
            public Dictionary<(CityId CityId, PersonId PersonId), Person> PersonsByCityAndId { get; } = [];
            public Dictionary<PersonId, IReadOnlyCollection<Person>> ChildrenByParentId { get; } = [];

            public IReadOnlyCollection<Person> ListByCityResult { get; set; } =
                Array.Empty<Person>();

            public IReadOnlyCollection<CityEmploymentWorkplaceSnapshot> EmploymentWorkplaces { get; set; } =
                Array.Empty<CityEmploymentWorkplaceSnapshot>();

            public IReadOnlyCollection<CityEducationInstitutionSnapshot> EducationInstitutions { get; set; } =
                Array.Empty<CityEducationInstitutionSnapshot>();

            public Dictionary<PersonId, CityResidentHousingSnapshot?> HousingSnapshotsByPersonId { get; } = [];

            public (IReadOnlyCollection<Person> Items, int TotalCount) PageByCityResult { get; set; } =
                (Array.Empty<Person>(), 0);

            public CityId? RequestedCityId { get; private set; }
            public Pagination? RequestedPagination { get; private set; }

            public Task<IReadOnlyCollection<Person>> ListByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(ListByCityResult);
            }

            public Task<(IReadOnlyCollection<Person> Items, int TotalCount)> GetPageByCityAsync(
                CityId cityId,
                Pagination pagination,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                RequestedPagination = pagination;
                return Task.FromResult(PageByCityResult);
            }

            public Task<Person?> FindByCityAndPersonIdAsync(
                CityId cityId,
                PersonId personId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;

                if (PersonsByCityAndId.TryGetValue(
                        key: (cityId, personId),
                        value: out Person? person))
                    return Task.FromResult<Person?>(person);

                foreach (Person candidate in ListByCityResult)
                    if (candidate.Id == personId)
                        return Task.FromResult<Person?>(candidate);

                return Task.FromResult<Person?>(null);
            }

            public Task<IReadOnlyCollection<Person>> ListChildrenByParentIdAsync(
                CityId cityId,
                PersonId parentId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;

                if (ChildrenByParentId.TryGetValue(
                        key: parentId,
                        value: out IReadOnlyCollection<Person>? children))
                    return Task.FromResult(children);

                return Task.FromResult<IReadOnlyCollection<Person>>(Array.Empty<Person>());
            }

            public Task<CityId?> FindCityIdByPersonIdAsync(
                PersonId personId,
                CancellationToken cancellationToken = default)
            {
                if (CityIdByPersonIds.TryGetValue(
                        key: personId,
                        value: out CityId? cityId))
                    return Task.FromResult(cityId);

                return Task.FromResult(CityIdByPersonId);
            }

            public Task<CityResidentHousingSnapshot?> FindHousingSnapshotByPersonIdAsync(
                CityId cityId,
                PersonId personId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(
                    HousingSnapshotsByPersonId.TryGetValue(
                        key: personId,
                        value: out CityResidentHousingSnapshot? snapshot)
                        ? snapshot
                        : null);
            }

            public Task<IReadOnlyDictionary<HouseholdId, HousingStatus>> ListHousingStatusesByHouseholdAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<IReadOnlyCollection<CityEmploymentWorkplaceSnapshot>> ListEmploymentWorkplacesAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(EmploymentWorkplaces);
            }

            public Task<CityEmploymentWorkplaceSnapshot?> FindEmploymentWorkplaceByIdAsync(
                CityId cityId,
                WorkplaceId workplaceId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;

                foreach (CityEmploymentWorkplaceSnapshot workplace in EmploymentWorkplaces)
                    if (workplace.WorkplaceId == workplaceId)
                        return Task.FromResult<CityEmploymentWorkplaceSnapshot?>(workplace);

                return Task.FromResult<CityEmploymentWorkplaceSnapshot?>(null);
            }

            public Task<IReadOnlyCollection<CityEducationInstitutionSnapshot>> ListEducationInstitutionsAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(EducationInstitutions);
            }

            public Task<CityEducationInstitutionSnapshot?> FindEducationInstitutionByIdAsync(
                CityId cityId,
                EducationInstitutionId institutionId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;

                foreach (CityEducationInstitutionSnapshot institution in EducationInstitutions)
                    if (institution.InstitutionId == institutionId)
                        return Task.FromResult<CityEducationInstitutionSnapshot?>(institution);

                return Task.FromResult<CityEducationInstitutionSnapshot?>(null);
            }
        }

        internal sealed class FakeCityPopulationProgressionStateRepository : ICityPopulationProgressionStateRepository
        {
            public CityPopulationProgressionState? State { get; set; }
            public CityId? RequestedCityId { get; private set; }
            public int DeleteByCityCalls { get; private set; }
            public List<CityPopulationProgressionState> AddedStates { get; } = [];

            public Task<CityPopulationProgressionState?> GetByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(State);
            }

            public Task AddAsync(
                CityPopulationProgressionState state,
                CancellationToken cancellationToken = default)
            {
                AddedStates.Add(state);
                State = state;
                return Task.CompletedTask;
            }

            public Task DeleteByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                DeleteByCityCalls++;
                State = null;
                return Task.CompletedTask;
            }
        }

        internal sealed class FakeCityPopulationActivityJournalService : ICityPopulationActivityJournalService
        {
            public List<CityPopulationActivityWriteModel> Entries { get; } = [];
            public int DeleteByCityCalls { get; private set; }

            public Task RecordAsync(
                CityPopulationActivityWriteModel entry,
                CancellationToken cancellationToken = default)
            {
                Entries.Add(entry);
                return Task.CompletedTask;
            }

            public Task DeleteByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                DeleteByCityCalls++;
                return Task.CompletedTask;
            }
        }

        internal sealed class FakeCityPopulationSummaryProjectionService : ICityPopulationSummaryProjectionService
        {
            public List<(CityId CityId, DateOnly CurrentDate)> RebuildCalls { get; } = [];
            public List<CityId> EnsuredCityIds { get; } = [];

            public List<(CityId CityId, DateOnly CurrentDate, int PersonCount, int PlacementCount, bool
                IncludeCommuteMetrics)> UpdateCalls
            { get; } = [];

            public List<CityId> DeletedCityIds { get; } = [];

            public Task UpdateAsync(
                CityId cityId,
                DateOnly currentDate,
                IReadOnlyCollection<Person> persons,
                IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements,
                bool includeCommuteMetrics = true,
                CancellationToken cancellationToken = default)
            {
                UpdateCalls.Add((cityId, currentDate, persons.Count, householdPlacements.Count, includeCommuteMetrics));
                return Task.CompletedTask;
            }

            public Task UpdateAsync(
                CityId cityId,
                DateOnly currentDate,
                IReadOnlyCollection<Person> persons,
                bool includeCommuteMetrics = true,
                CancellationToken cancellationToken = default)
            {
                UpdateCalls.Add((cityId, currentDate, persons.Count, 0, includeCommuteMetrics));
                return Task.CompletedTask;
            }

            public Task RebuildAsync(
                CityId cityId,
                DateOnly currentDate,
                bool includeCommuteMetrics = true,
                CancellationToken cancellationToken = default)
            {
                RebuildCalls.Add((cityId, currentDate));
                return Task.CompletedTask;
            }

            public Task EnsureExistsAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                EnsuredCityIds.Add(cityId);
                return Task.CompletedTask;
            }

            public Task DeleteAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                DeletedCityIds.Add(cityId);
                return Task.CompletedTask;
            }
        }

        internal sealed class FakeCityPopulationSummaryReadRepository : ICityPopulationSummaryReadRepository
        {
            public CityPopulationSummaryReadModel? Summary { get; set; }
            public CityId? RequestedCityId { get; private set; }

            public Task<CityPopulationSummaryReadModel?> GetByCityIdAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(Summary);
            }
        }

        internal sealed class FakeCityPopulationArchiveStateRepository : ICityPopulationArchiveStateRepository
        {
            public CityPopulationArchiveState? State { get; set; }
            public CityId? RequestedCityId { get; private set; }
            public List<CityPopulationArchiveState> AddedStates { get; } = [];
            public int DeleteByCityCalls { get; private set; }

            public Task<CityPopulationArchiveState?> GetByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(State);
            }

            public Task AddAsync(
                CityPopulationArchiveState state,
                CancellationToken cancellationToken = default)
            {
                AddedStates.Add(state);
                State = state;
                return Task.CompletedTask;
            }

            public Task DeleteByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                DeleteByCityCalls++;
                State = null;
                return Task.CompletedTask;
            }
        }

        internal sealed class FakeCityPopulationDeletionStateRepository : ICityPopulationDeletionStateRepository
        {
            public CityPopulationDeletionState? State { get; set; }
            public CityId? RequestedCityId { get; private set; }
            public List<CityPopulationDeletionState> AddedStates { get; } = [];

            public Task<CityPopulationDeletionState?> GetByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(State);
            }

            public Task AddAsync(
                CityPopulationDeletionState state,
                CancellationToken cancellationToken = default)
            {
                AddedStates.Add(state);
                State = state;
                return Task.CompletedTask;
            }
        }

        internal sealed class FakeCityPopulationEnvironmentRepository : ICityPopulationEnvironmentRepository
        {
            public CityPopulationEnvironment? State { get; set; }
            public CityId? RequestedCityId { get; private set; }
            public int DeleteByCityCalls { get; private set; }
            public List<CityPopulationEnvironment> UpsertedEnvironments { get; } = [];

            public Task<CityPopulationEnvironment?> GetByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(State);
            }

            public Task AddAsync(
                CityPopulationEnvironment environment,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task DeleteByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                DeleteByCityCalls++;
                return Task.CompletedTask;
            }

            public Task<bool> UpsertAsync(
                CityPopulationEnvironment environment,
                CancellationToken cancellationToken = default)
            {
                UpsertedEnvironments.Add(environment);
                State = environment;
                return Task.FromResult(true);
            }
        }

        internal sealed class FakeCityPopulationAnchorCatalogRepository : ICityPopulationAnchorCatalogRepository
        {
            public int DeleteByCityCalls { get; private set; }
            public List<IReadOnlyCollection<CityPopulationAnchorCatalogItem>> AddedRanges { get; } = [];

            public IReadOnlyList<CityPopulationAnchorCatalogItem> Items { get; set; } =
                Array.Empty<CityPopulationAnchorCatalogItem>();

            public Task<IReadOnlyList<CityPopulationAnchorCatalogItem>> ListByCityAsync(
                CityId cityId,
                CityAnchorType? type = null,
                CancellationToken cancellationToken = default)
            {
                IReadOnlyList<CityPopulationAnchorCatalogItem> items = Items
                   .Where(x => x.CityId == cityId && (!type.HasValue || x.Type == type.Value))
                   .ToArray();

                return Task.FromResult(items);
            }

            public Task AddRangeAsync(
                IReadOnlyCollection<CityPopulationAnchorCatalogItem> items,
                CancellationToken cancellationToken = default)
            {
                AddedRanges.Add(items);
                return Task.CompletedTask;
            }

            public Task DeleteByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                DeleteByCityCalls++;
                return Task.CompletedTask;
            }
        }

        internal sealed class FakeHouseholdWriteRepository : IHouseholdWriteRepository
        {
            public int DeleteByCityCalls { get; private set; }

            public List<(IReadOnlyCollection<Household> Households, IReadOnlyCollection<ClassicCityHouseholdPlacement>
                Placements)> AddedRanges
            { get; } = [];

            public IReadOnlyCollection<ClassicCityHouseholdPlacement> PlacementsByCityResult { get; set; } =
                Array.Empty<ClassicCityHouseholdPlacement>();

            public IReadOnlyCollection<Household> HouseholdsByCityResult { get; set; } = Array.Empty<Household>();
            public CityId? RequestedCityId { get; private set; }
            public Dictionary<HouseholdId, Household> HouseholdsById { get; } = [];
            public Dictionary<HouseholdId, ClassicCityHouseholdPlacement> PlacementsByHouseholdId { get; } = [];
            public Dictionary<HouseholdId, int> ResidentCountByHouseholdId { get; } = [];
            public List<Household> UpdatedHouseholds { get; } = [];
            public List<Household> DeletedHouseholds { get; } = [];
            public List<(Household Household, ClassicCityHouseholdPlacement Placement)> AddedHouseholds { get; } = [];

            public Task<Household?> FindByIdAsync(
                HouseholdId householdId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(
                    HouseholdsById.TryGetValue(
                        key: householdId,
                        value: out Household? household)
                        ? household
                        : null);
            }

            public Task<ClassicCityHouseholdPlacement?> FindPlacementByHouseholdIdAsync(
                HouseholdId householdId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(
                    PlacementsByHouseholdId.TryGetValue(
                        key: householdId,
                        value: out ClassicCityHouseholdPlacement? placement)
                        ? placement
                        : null);
            }

            public Task<IReadOnlyCollection<ClassicCityHouseholdPlacement>> ListPlacementsByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(PlacementsByCityResult);
            }

            public Task<IReadOnlyCollection<Household>> ListByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(HouseholdsByCityResult);
            }

            public Task<int> CountResidentsAsync(
                HouseholdId householdId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(
                    ResidentCountByHouseholdId.TryGetValue(
                        key: householdId,
                        value: out int count)
                        ? count
                        : 0);
            }

            public Task DeleteAllAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task DeleteAsync(
                Household household,
                CancellationToken cancellationToken = default)
            {
                DeletedHouseholds.Add(household);
                HouseholdsById.Remove(household.Id);
                PlacementsByHouseholdId.Remove(household.Id);
                ResidentCountByHouseholdId.Remove(household.Id);
                return Task.CompletedTask;
            }

            public Task AddAsync(
                Household household,
                ClassicCityHouseholdPlacement householdPlacement,
                CancellationToken cancellationToken = default)
            {
                AddedHouseholds.Add((household, householdPlacement));
                HouseholdsById[household.Id] = household;
                PlacementsByHouseholdId[household.Id] = householdPlacement;
                ResidentCountByHouseholdId[household.Id] = household.Size.Value;
                return Task.CompletedTask;
            }

            public Task UpdateAsync(
                Household household,
                CancellationToken cancellationToken = default)
            {
                UpdatedHouseholds.Add(household);
                HouseholdsById[household.Id] = household;
                ResidentCountByHouseholdId[household.Id] = household.Size.Value;
                return Task.CompletedTask;
            }

            public Task DeleteByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                DeleteByCityCalls++;
                return Task.CompletedTask;
            }

            public Task AddRangeAsync(
                IReadOnlyCollection<Household> households,
                IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements,
                CancellationToken cancellationToken = default)
            {
                AddedRanges.Add((households, householdPlacements));
                return Task.CompletedTask;
            }
        }

        internal sealed class FakeCityEconomySettlementOutboxWriter : ICityEconomySettlementOutboxWriter
        {
            public List<CityEconomyDailySettlementV1> DailySettlements { get; } = [];
            public List<ClassicCityHouseholdCashflowSettlementBatchV1> HouseholdCashflowBatches { get; } = [];
            public List<ClassicCityHouseholdAccountSyncBatchV1> HouseholdBatches { get; } = [];
            public List<ClassicCityWorkplacePayrollSettlementBatchV1> WorkplacePayrollBatches { get; } = [];
            public List<ClassicCityWorkplaceBusinessSyncBatchV1> WorkplaceBatches { get; } = [];

            public Task AddCityDailySettlementAsync(
                CityEconomyDailySettlementV1 settlement,
                CancellationToken cancellationToken = default)
            {
                DailySettlements.Add(settlement);
                return Task.CompletedTask;
            }

            public Task AddClassicCityWorkplacePayrollSettlementBatchAsync(
                ClassicCityWorkplacePayrollSettlementBatchV1 batch,
                CancellationToken cancellationToken = default)
            {
                WorkplacePayrollBatches.Add(batch);
                return Task.CompletedTask;
            }

            public Task AddClassicCityHouseholdCashflowSettlementBatchAsync(
                ClassicCityHouseholdCashflowSettlementBatchV1 batch,
                CancellationToken cancellationToken = default)
            {
                HouseholdCashflowBatches.Add(batch);
                return Task.CompletedTask;
            }

            public Task AddClassicCityHouseholdAccountSyncBatchAsync(
                ClassicCityHouseholdAccountSyncBatchV1 batch,
                CancellationToken cancellationToken = default)
            {
                HouseholdBatches.Add(batch);
                return Task.CompletedTask;
            }

            public Task AddClassicCityWorkplaceBusinessSyncBatchAsync(
                ClassicCityWorkplaceBusinessSyncBatchV1 batch,
                CancellationToken cancellationToken = default)
            {
                WorkplaceBatches.Add(batch);
                return Task.CompletedTask;
            }
        }

        internal sealed class FakePopulationResidentFactsOutboxWriter
            : IPopulationResidentFactsOutboxWriter
        {
            public List<PopulationResidentFactsBatchV1> Batches { get; } = [];

            public Task AddResidentFactsBatchAsync(
                PopulationResidentFactsBatchV1 batch,
                CancellationToken cancellationToken = default)
            {
                Batches.Add(batch);
                return Task.CompletedTask;
            }
        }

        internal sealed class FakePopulationResidentMedicalStateOutboxWriter
            : IPopulationResidentMedicalStateOutboxWriter
        {
            public List<PopulationResidentMedicalStateBatchV1> Batches { get; } = [];

            public Task AddResidentMedicalStateBatchAsync(
                PopulationResidentMedicalStateBatchV1 batch,
                CancellationToken cancellationToken = default)
            {
                Batches.Add(batch);
                return Task.CompletedTask;
            }
        }

        internal sealed class
            FakeCityPopulationWeatherImpactStateRepository : ICityPopulationWeatherImpactStateRepository
        {
            public CityPopulationWeatherImpactState? State { get; set; }
            public CityId? RequestedCityId { get; private set; }
            public List<CityPopulationWeatherImpactState> AddedStates { get; } = [];
            public int DeleteByCityCalls { get; private set; }

            public Task<CityPopulationWeatherImpactState?> GetByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(State);
            }

            public Task AddAsync(
                CityPopulationWeatherImpactState state,
                CancellationToken cancellationToken = default)
            {
                AddedStates.Add(state);
                State = state;
                return Task.CompletedTask;
            }

            public Task DeleteByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                DeleteByCityCalls++;
                State = null;
                return Task.CompletedTask;
            }
        }

        internal sealed class FakeProcessedIntegrationMessageRepository : IProcessedIntegrationMessageRepository
        {
            public bool TryMarkProcessedResult { get; set; } = true;
            public string? RequestedConsumer { get; private set; }
            public Guid? RequestedMessageId { get; private set; }
            public DateTimeOffset? RequestedProcessedAtUtc { get; private set; }

            public Task<bool> TryMarkProcessedAsync(
                string consumer,
                Guid messageId,
                DateTimeOffset processedAtUtc,
                CancellationToken cancellationToken = default)
            {
                RequestedConsumer = consumer;
                RequestedMessageId = messageId;
                RequestedProcessedAtUtc = processedAtUtc;
                return Task.FromResult(TryMarkProcessedResult);
            }
        }

        internal sealed class
            FakeCityPopulationWeatherExposureStateRepository : ICityPopulationWeatherExposureStateRepository
        {
            public CityPopulationWeatherExposureState? State { get; set; }
            public CityId? RequestedCityId { get; private set; }
            public List<CityPopulationWeatherExposureState> AddedStates { get; } = [];
            public int DeleteByCityCalls { get; private set; }

            public Task<CityPopulationWeatherExposureState?> GetByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(State);
            }

            public Task AddAsync(
                CityPopulationWeatherExposureState state,
                CancellationToken cancellationToken = default)
            {
                AddedStates.Add(state);
                State = state;
                return Task.CompletedTask;
            }

            public Task DeleteByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                DeleteByCityCalls++;
                State = null;
                return Task.CompletedTask;
            }
        }

        internal sealed class FakeCityPopulationCostOfLivingStateRepository : ICityPopulationCostOfLivingStateRepository
        {
            public CityPopulationCostOfLivingState? State { get; set; }
            public CityId? RequestedCityId { get; private set; }
            public List<CityPopulationCostOfLivingState> AddedStates { get; } = [];
            public int DeleteByCityCalls { get; private set; }

            public Task<CityPopulationCostOfLivingState?> GetByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(State);
            }

            public Task AddAsync(
                CityPopulationCostOfLivingState state,
                CancellationToken cancellationToken = default)
            {
                AddedStates.Add(state);
                State = state;
                return Task.CompletedTask;
            }

            public Task DeleteByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                DeleteByCityCalls++;
                State = null;
                return Task.CompletedTask;
            }
        }

        internal sealed class FakeCityPopulationEssentialsStateRepository : ICityPopulationEssentialsStateRepository
        {
            public CityPopulationEssentialsState? State { get; set; }
            public CityId? RequestedCityId { get; private set; }
            public List<CityPopulationEssentialsState> AddedStates { get; } = [];
            public int DeleteByCityCalls { get; private set; }

            public Task<CityPopulationEssentialsState?> GetByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(State);
            }

            public Task AddAsync(
                CityPopulationEssentialsState state,
                CancellationToken cancellationToken = default)
            {
                AddedStates.Add(state);
                State = state;
                return Task.CompletedTask;
            }

            public Task DeleteByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                DeleteByCityCalls++;
                State = null;
                return Task.CompletedTask;
            }
        }

        internal sealed class
            FakeCityPopulationLivingConditionsStateRepository : ICityPopulationLivingConditionsStateRepository
        {
            public CityPopulationLivingConditionsState? State { get; set; }
            public CityId? RequestedCityId { get; private set; }
            public List<CityPopulationLivingConditionsState> AddedStates { get; } = [];
            public int DeleteByCityCalls { get; private set; }

            public Task<CityPopulationLivingConditionsState?> GetByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(State);
            }

            public Task AddAsync(
                CityPopulationLivingConditionsState state,
                CancellationToken cancellationToken = default)
            {
                AddedStates.Add(state);
                State = state;
                return Task.CompletedTask;
            }

            public Task DeleteByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                DeleteByCityCalls++;
                State = null;
                return Task.CompletedTask;
            }
        }

        internal sealed class
            FakeCityPopulationServiceQualityStateRepository : ICityPopulationServiceQualityStateRepository
        {
            public CityPopulationServiceQualityState? State { get; set; }
            public CityId? RequestedCityId { get; private set; }
            public List<CityPopulationServiceQualityState> AddedStates { get; } = [];
            public int DeleteByCityCalls { get; private set; }

            public Task<CityPopulationServiceQualityState?> GetByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(State);
            }

            public Task AddAsync(
                CityPopulationServiceQualityState state,
                CancellationToken cancellationToken = default)
            {
                AddedStates.Add(state);
                State = state;
                return Task.CompletedTask;
            }

            public Task DeleteByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                DeleteByCityCalls++;
                State = null;
                return Task.CompletedTask;
            }
        }

        internal sealed class
            FakeCityPopulationEmployerFinancialStressStateRepository :
            ICityPopulationEmployerFinancialStressStateRepository
        {
            public List<CityPopulationEmployerFinancialStressState> States { get; } = [];
            public List<CityPopulationEmployerFinancialStressState> AddedStates { get; } = [];
            public CityId? RequestedCityId { get; private set; }
            public WorkplaceId? RequestedWorkplaceId { get; private set; }
            public int DeleteByCityCalls { get; private set; }

            public Task<CityPopulationEmployerFinancialStressState?> GetByCityAndWorkplaceAsync(
                CityId cityId,
                WorkplaceId workplaceId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                RequestedWorkplaceId = workplaceId;

                foreach (CityPopulationEmployerFinancialStressState state in States)
                    if (state.CityId == cityId && state.WorkplaceId == workplaceId)
                        return Task.FromResult<CityPopulationEmployerFinancialStressState?>(state);

                return Task.FromResult<CityPopulationEmployerFinancialStressState?>(null);
            }

            public Task<IReadOnlyList<CityPopulationEmployerFinancialStressState>> ListByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                List<CityPopulationEmployerFinancialStressState> states = [];
                foreach (CityPopulationEmployerFinancialStressState state in States)
                    if (state.CityId == cityId)
                        states.Add(state);

                return Task.FromResult<IReadOnlyList<CityPopulationEmployerFinancialStressState>>(states);
            }

            public Task AddAsync(
                CityPopulationEmployerFinancialStressState state,
                CancellationToken cancellationToken = default)
            {
                AddedStates.Add(state);
                States.Add(state);
                return Task.CompletedTask;
            }

            public Task DeleteByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                DeleteByCityCalls++;
                States.RemoveAll(state => state.CityId == cityId);
                return Task.CompletedTask;
            }
        }

        internal sealed class
            FakeCityPopulationHouseholdFinancialStressStateRepository :
            ICityPopulationHouseholdFinancialStressStateRepository
        {
            public List<CityPopulationHouseholdFinancialStressState> States { get; } = [];
            public List<CityPopulationHouseholdFinancialStressState> AddedStates { get; } = [];
            public CityId? RequestedCityId { get; private set; }
            public HouseholdId? RequestedHouseholdId { get; private set; }
            public int DeleteByCityCalls { get; private set; }

            public Task<CityPopulationHouseholdFinancialStressState?> GetByCityAndHouseholdAsync(
                CityId cityId,
                HouseholdId householdId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                RequestedHouseholdId = householdId;

                foreach (CityPopulationHouseholdFinancialStressState state in States)
                    if (state.CityId == cityId && state.HouseholdId == householdId)
                        return Task.FromResult<CityPopulationHouseholdFinancialStressState?>(state);

                return Task.FromResult<CityPopulationHouseholdFinancialStressState?>(null);
            }

            public Task<IReadOnlyList<CityPopulationHouseholdFinancialStressState>> ListByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                List<CityPopulationHouseholdFinancialStressState> states = [];
                foreach (CityPopulationHouseholdFinancialStressState state in States)
                    if (state.CityId == cityId)
                        states.Add(state);

                return Task.FromResult<IReadOnlyList<CityPopulationHouseholdFinancialStressState>>(states);
            }

            public Task AddAsync(
                CityPopulationHouseholdFinancialStressState state,
                CancellationToken cancellationToken = default)
            {
                AddedStates.Add(state);
                States.Add(state);
                return Task.CompletedTask;
            }

            public Task DeleteByCityAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                DeleteByCityCalls++;
                States.RemoveAll(state => state.CityId == cityId);
                return Task.CompletedTask;
            }
        }

        internal sealed class FakeCityPopulationDashboardReadRepository : ICityPopulationDashboardReadRepository
        {
            public CityPopulationDashboardSnapshotReadModel? CurrentSnapshot { get; set; }
            public Dictionary<DateOnly, CityPopulationDashboardSnapshotReadModel> SnapshotsByDate { get; } = [];

            public CityPopulationDashboardEconomyReadModel EconomySnapshot { get; set; } = new(
                StableHouseholdCount: 0,
                StrainedHouseholdCount: 0,
                DeficitHouseholdCount: 0,
                AverageCashReserveAmount: 0m,
                AverageDailyNetAmount: 0m);

            public IReadOnlyList<CityPopulationActivityEventReadModel> ActivityEvents { get; set; } =
                Array.Empty<CityPopulationActivityEventReadModel>();

            public CityId? RequestedCityId { get; private set; }
            public DateOnly? RequestedSnapshotDate { get; private set; }
            public DateOnly? RequestedEconomyDate { get; private set; }
            public int RequestedRecentTake { get; private set; }

            public Task<CityPopulationDashboardSnapshotReadModel?> GetCurrentSnapshotAsync(
                CityId cityId,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                return Task.FromResult(CurrentSnapshot);
            }

            public Task<CityPopulationDashboardSnapshotReadModel?> GetSnapshotOnOrBeforeAsync(
                CityId cityId,
                DateOnly snapshotDate,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                RequestedSnapshotDate = snapshotDate;

                CityPopulationDashboardSnapshotReadModel? match = null;
                foreach ((DateOnly date, CityPopulationDashboardSnapshotReadModel snapshot) in SnapshotsByDate)
                {
                    if (date > snapshotDate)
                        continue;

                    if (match is null || date > match.SnapshotDate)
                        match = snapshot;
                }

                return Task.FromResult(match);
            }

            public Task<CityPopulationDashboardEconomyReadModel> GetCurrentEconomySnapshotAsync(
                CityId cityId,
                DateOnly currentDate,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                RequestedEconomyDate = currentDate;
                return Task.FromResult(EconomySnapshot);
            }

            public Task<IReadOnlyList<CityPopulationActivityEventReadModel>> ListRecentActivityAsync(
                CityId cityId,
                int take,
                CancellationToken cancellationToken = default)
            {
                RequestedCityId = cityId;
                RequestedRecentTake = take;
                return Task.FromResult(ActivityEvents);
            }
        }

        internal sealed class FakeCityDistrictUtilityConditionsClient : ICityDistrictUtilityConditionsClient
        {
            public IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> SnapshotsByDistrictId
            {
                get;
                set;
            } =
                new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>();

            public Exception? ExceptionToThrow { get; set; }
            public Guid? RequestedCityId { get; private set; }

            public Task<IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>> GetByCityAsync(
                Guid cityId,
                CancellationToken cancellationToken)
            {
                RequestedCityId = cityId;

                if (ExceptionToThrow is not null)
                    throw ExceptionToThrow;

                return Task.FromResult(SnapshotsByDistrictId);
            }
        }

        internal sealed class FakeCityPopulationCommuteRoutingService : ICityPopulationCommuteRoutingService
        {
            public CityPopulationCommuteContext AnchorContext { get; set; } = CityPopulationCommuteContext.Neutral;
            public CityPopulationCommuteContext EmploymentContext { get; set; } = CityPopulationCommuteContext.Neutral;
            public CityPopulationCommuteContext EducationContext { get; set; } = CityPopulationCommuteContext.Neutral;
            public CityPopulationCommuteContext HealthcareContext { get; set; } = CityPopulationCommuteContext.Neutral;
            public List<IReadOnlyCollection<CityPopulationCommuteRouteRequest>> PreloadRequests { get; } = [];

            public Task PreloadAnchorCommutesAsync(
                Guid cityId,
                IReadOnlyCollection<CityPopulationCommuteRouteRequest> requests,
                CancellationToken cancellationToken)
            {
                PreloadRequests.Add(requests);
                return Task.CompletedTask;
            }

            public Task<CityPopulationCommuteContext> ResolveAnchorCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                CityAnchorId? destinationAnchorId,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(AnchorContext);
            }

            public Task<CityPopulationCommuteContext> ResolveEmploymentCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                Person resident,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(EmploymentContext);
            }

            public Task<CityPopulationCommuteContext> ResolveEducationCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                Person resident,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(EducationContext);
            }

            public Task<CityPopulationCommuteContext> ResolveHealthcareCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                CityAnchorId? healthcareAnchorId,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(HealthcareContext);
            }
        }

        internal sealed class FakeCityPopulationActiveTripClient : ICityPopulationActiveTripClient
        {
            public Dictionary<Guid, CityPopulationActiveTripSnapshot> ActiveTripsByTravellerId { get; } = [];

            public IReadOnlyCollection<CityPopulationActiveTripSnapshot> ActiveTripsByCity { get; set; } =
                Array.Empty<CityPopulationActiveTripSnapshot>();

            public Guid? RequestedCityId { get; private set; }
            public Guid? RequestedTravellerEntityId { get; private set; }
            public bool TryDispatchResult { get; set; } = true;
            public CityPopulationTripDispatchRequest? RequestedDispatch { get; private set; }

            public Task<IReadOnlyCollection<CityPopulationActiveTripSnapshot>> ListActiveByCityAsync(
                Guid cityId,
                CancellationToken cancellationToken)
            {
                RequestedCityId = cityId;
                return Task.FromResult(ActiveTripsByCity);
            }

            public Task<CityPopulationActiveTripSnapshot?> FindActiveByTravellerAsync(
                Guid cityId,
                Guid travellerEntityId,
                CancellationToken cancellationToken)
            {
                RequestedCityId = cityId;
                RequestedTravellerEntityId = travellerEntityId;

                return Task.FromResult(
                    ActiveTripsByTravellerId.TryGetValue(
                        key: travellerEntityId,
                        value: out CityPopulationActiveTripSnapshot? trip)
                        ? trip
                        : null);
            }

            public Task<bool> TryDispatchAsync(
                CityPopulationTripDispatchRequest request,
                CancellationToken cancellationToken)
            {
                RequestedDispatch = request;
                return Task.FromResult(TryDispatchResult);
            }
        }

        internal sealed class FakeCityPopulationCommuteTripSyncService : ICityPopulationCommuteTripSyncService
        {
            public int SyncCalls { get; private set; }
            public Exception? ExceptionToThrow { get; set; }

            public Task SyncAsync(
                Guid cityId,
                long tickId,
                DateOnly currentDate,
                DateTimeOffset currentSimTimeUtc,
                IReadOnlyCollection<Person> residents,
                IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements,
                IReadOnlyCollection<CityPopulationAnchorCatalogItem> hospitalAnchors,
                CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
                CancellationToken cancellationToken)
            {
                SyncCalls++;
                if (ExceptionToThrow is not null)
                    throw ExceptionToThrow;
                return Task.CompletedTask;
            }
        }
    }
}
