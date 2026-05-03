using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Tests.TestSupport;

internal static class PopulationApplicationTestSupport
{
    internal static readonly DateTimeOffset UtcNow = new(2048, 5, 3, 9, 10, 11, TimeSpan.Zero);

    internal static Matrix.Population.Domain.Entities.Person CreatePerson(
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
        DateOnly resolvedCurrentDate = currentDate ?? new DateOnly(2048, 5, 3);
        Matrix.Population.Domain.Entities.Person person = Person.CreatePerson(
            id: PersonId.From(personId ?? Guid.Parse("11111111-1111-1111-1111-111111111111")),
            householdId: HouseholdId.From(householdId ?? Guid.Parse("22222222-2222-2222-2222-222222222222")),
            name: new PersonName(firstName, lastName),
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
            birthDate: birthDate ?? new DateOnly(2030, 5, 3),
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
        public override DateTimeOffset GetUtcNow() => utcNow;
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
        public Matrix.Population.Domain.Entities.Person? PersonById { get; set; }
        public Dictionary<PersonId, Matrix.Population.Domain.Entities.Person> PersonsById { get; } = [];
        public (IReadOnlyCollection<Matrix.Population.Domain.Entities.Person> Items, int TotalCount) PageResult { get; set; } =
            (Array.Empty<Matrix.Population.Domain.Entities.Person>(), 0);
        public PersonId? RequestedPersonId { get; private set; }
        public Pagination? RequestedPagination { get; private set; }

        public Task<IReadOnlyCollection<Matrix.Population.Domain.Entities.Person>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Matrix.Population.Domain.Entities.Person?> FindByIdAsync(
            PersonId id,
            CancellationToken cancellationToken = default)
        {
            RequestedPersonId = id;

             if (PersonsById.TryGetValue(
                    key: id,
                    value: out Matrix.Population.Domain.Entities.Person? person))
                return Task.FromResult<Matrix.Population.Domain.Entities.Person?>(person);

            return Task.FromResult(PersonById);
        }

        public Task<(IReadOnlyCollection<Matrix.Population.Domain.Entities.Person> Items, int TotalCount)> GetPageAsync(
            Pagination pagination,
            CancellationToken cancellationToken = default)
        {
            RequestedPagination = pagination;
            return Task.FromResult(PageResult);
        }
    }

    internal sealed class FakePersonWriteRepository : IPersonWriteRepository
    {
        public List<Matrix.Population.Domain.Entities.Person> UpdatedPersons { get; } = [];
        public List<IReadOnlyCollection<Matrix.Population.Domain.Entities.Person>> AddedRanges { get; } = [];

        public Task DeleteAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Matrix.Population.Domain.Entities.Person person, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Matrix.Population.Domain.Entities.Person person, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task AddRangeAsync(
            IReadOnlyCollection<Matrix.Population.Domain.Entities.Person> persons,
            CancellationToken cancellationToken = default)
        {
            AddedRanges.Add(persons);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Matrix.Population.Domain.Entities.Person person, CancellationToken cancellationToken = default)
        {
            UpdatedPersons.Add(person);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeCityPopulationPersonReadRepository : ICityPopulationPersonReadRepository
    {
        public CityId? CityIdByPersonId { get; set; }
        public IReadOnlyCollection<Matrix.Population.Domain.Entities.Person> ListByCityResult { get; set; } =
            Array.Empty<Matrix.Population.Domain.Entities.Person>();
        public IReadOnlyCollection<CityEmploymentWorkplaceSnapshot> EmploymentWorkplaces { get; set; } =
            Array.Empty<CityEmploymentWorkplaceSnapshot>();
        public IReadOnlyCollection<CityEducationInstitutionSnapshot> EducationInstitutions { get; set; } =
            Array.Empty<CityEducationInstitutionSnapshot>();
        public (IReadOnlyCollection<Matrix.Population.Domain.Entities.Person> Items, int TotalCount) PageByCityResult { get; set; } =
            (Array.Empty<Matrix.Population.Domain.Entities.Person>(), 0);
        public CityId? RequestedCityId { get; private set; }
        public Pagination? RequestedPagination { get; private set; }

        public Task<IReadOnlyCollection<Matrix.Population.Domain.Entities.Person>> ListByCityAsync(CityId cityId, CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult(ListByCityResult);
        }

        public Task<(IReadOnlyCollection<Matrix.Population.Domain.Entities.Person> Items, int TotalCount)> GetPageByCityAsync(
            CityId cityId,
            Pagination pagination,
            CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            RequestedPagination = pagination;
            return Task.FromResult(PageByCityResult);
        }

        public Task<Matrix.Population.Domain.Entities.Person?> FindByCityAndPersonIdAsync(CityId cityId, PersonId personId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Matrix.Population.Domain.Entities.Person>> ListChildrenByParentIdAsync(CityId cityId, PersonId parentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<CityId?> FindCityIdByPersonIdAsync(PersonId personId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CityIdByPersonId);
        }

        public Task<CityResidentHousingSnapshot?> FindHousingSnapshotByPersonIdAsync(CityId cityId, PersonId personId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<HouseholdId, HousingStatus>> ListHousingStatusesByHouseholdAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyCollection<CityEmploymentWorkplaceSnapshot>> ListEmploymentWorkplacesAsync(CityId cityId, CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult(EmploymentWorkplaces);
        }

        public Task<CityEmploymentWorkplaceSnapshot?> FindEmploymentWorkplaceByIdAsync(CityId cityId, WorkplaceId workplaceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyCollection<CityEducationInstitutionSnapshot>> ListEducationInstitutionsAsync(CityId cityId, CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult(EducationInstitutions);
        }

        public Task<CityEducationInstitutionSnapshot?> FindEducationInstitutionByIdAsync(CityId cityId, EducationInstitutionId institutionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    internal sealed class FakeCityPopulationProgressionStateRepository : ICityPopulationProgressionStateRepository
    {
        public CityPopulationProgressionState? State { get; set; }
        public CityId? RequestedCityId { get; private set; }

        public Task<CityPopulationProgressionState?> GetByCityAsync(CityId cityId, CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult(State);
        }

        public Task AddAsync(CityPopulationProgressionState state, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteByCityAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    internal sealed class FakeCityPopulationActivityJournalService : ICityPopulationActivityJournalService
    {
        public List<CityPopulationActivityWriteModel> Entries { get; } = [];

        public Task RecordAsync(CityPopulationActivityWriteModel entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task DeleteByCityAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    internal sealed class FakeCityPopulationSummaryProjectionService : ICityPopulationSummaryProjectionService
    {
        public List<(CityId CityId, DateOnly CurrentDate)> RebuildCalls { get; } = [];
        public List<CityId> EnsuredCityIds { get; } = [];
        public List<(CityId CityId, DateOnly CurrentDate, int PersonCount, int PlacementCount, bool IncludeCommuteMetrics)> UpdateCalls { get; } = [];

        public Task UpdateAsync(CityId cityId, DateOnly currentDate, IReadOnlyCollection<Matrix.Population.Domain.Entities.Person> persons, IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements, bool includeCommuteMetrics = true, CancellationToken cancellationToken = default)
        {
            UpdateCalls.Add((cityId, currentDate, persons.Count, householdPlacements.Count, includeCommuteMetrics));
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CityId cityId, DateOnly currentDate, IReadOnlyCollection<Matrix.Population.Domain.Entities.Person> persons, bool includeCommuteMetrics = true, CancellationToken cancellationToken = default)
        {
            UpdateCalls.Add((cityId, currentDate, persons.Count, 0, includeCommuteMetrics));
            return Task.CompletedTask;
        }

        public Task RebuildAsync(CityId cityId, DateOnly currentDate, bool includeCommuteMetrics = true, CancellationToken cancellationToken = default)
        {
            RebuildCalls.Add((cityId, currentDate));
            return Task.CompletedTask;
        }

        public Task EnsureExistsAsync(CityId cityId, CancellationToken cancellationToken = default)
        {
            EnsuredCityIds.Add(cityId);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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

        public Task<CityPopulationArchiveState?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult(State);
        }

        public Task AddAsync(CityPopulationArchiveState state, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteByCityAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    internal sealed class FakeCityPopulationDeletionStateRepository : ICityPopulationDeletionStateRepository
    {
        public CityPopulationDeletionState? State { get; set; }
        public CityId? RequestedCityId { get; private set; }

        public Task<CityPopulationDeletionState?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult(State);
        }

        public Task AddAsync(CityPopulationDeletionState state, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    internal sealed class FakeCityPopulationEnvironmentRepository : ICityPopulationEnvironmentRepository
    {
        public CityPopulationEnvironment? State { get; set; }
        public CityId? RequestedCityId { get; private set; }
        public int DeleteByCityCalls { get; private set; }
        public List<CityPopulationEnvironment> UpsertedEnvironments { get; } = [];

        public Task<CityPopulationEnvironment?> GetByCityAsync(CityId cityId, CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult(State);
        }

        public Task AddAsync(CityPopulationEnvironment environment, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteByCityAsync(CityId cityId, CancellationToken cancellationToken = default)
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

        public Task<IReadOnlyList<CityPopulationAnchorCatalogItem>> ListByCityAsync(CityId cityId, CityAnchorType? type = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

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
        public List<(IReadOnlyCollection<Household> Households, IReadOnlyCollection<ClassicCityHouseholdPlacement> Placements)> AddedRanges { get; } = [];

        public Task<Household?> FindByIdAsync(HouseholdId householdId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ClassicCityHouseholdPlacement?> FindPlacementByHouseholdIdAsync(HouseholdId householdId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ClassicCityHouseholdPlacement>> ListPlacementsByCityAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Household>> ListByCityAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> CountResidentsAsync(HouseholdId householdId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Household household, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Household household, ClassicCityHouseholdPlacement householdPlacement, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Household household, CancellationToken cancellationToken = default) => throw new NotSupportedException();

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
        public List<Matrix.BuildingBlocks.Application.IntegrationEvents.Economy.ClassicCityHouseholdAccountSyncBatchV1> HouseholdBatches { get; } = [];
        public List<Matrix.BuildingBlocks.Application.IntegrationEvents.Economy.ClassicCityWorkplaceBusinessSyncBatchV1> WorkplaceBatches { get; } = [];

        public Task AddCityDailySettlementAsync(Matrix.BuildingBlocks.Application.IntegrationEvents.Economy.CityEconomyDailySettlementV1 settlement, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddClassicCityWorkplacePayrollSettlementBatchAsync(Matrix.BuildingBlocks.Application.IntegrationEvents.Economy.ClassicCityWorkplacePayrollSettlementBatchV1 batch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddClassicCityHouseholdCashflowSettlementBatchAsync(Matrix.BuildingBlocks.Application.IntegrationEvents.Economy.ClassicCityHouseholdCashflowSettlementBatchV1 batch, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task AddClassicCityHouseholdAccountSyncBatchAsync(
            Matrix.BuildingBlocks.Application.IntegrationEvents.Economy.ClassicCityHouseholdAccountSyncBatchV1 batch,
            CancellationToken cancellationToken = default)
        {
            HouseholdBatches.Add(batch);
            return Task.CompletedTask;
        }

        public Task AddClassicCityWorkplaceBusinessSyncBatchAsync(
            Matrix.BuildingBlocks.Application.IntegrationEvents.Economy.ClassicCityWorkplaceBusinessSyncBatchV1 batch,
            CancellationToken cancellationToken = default)
        {
            WorkplaceBatches.Add(batch);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeCityPopulationWeatherImpactStateRepository : ICityPopulationWeatherImpactStateRepository
    {
        public CityPopulationWeatherImpactState? State { get; set; }
        public CityId? RequestedCityId { get; private set; }
        public List<CityPopulationWeatherImpactState> AddedStates { get; } = [];

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

        public Task DeleteByCityAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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

    internal sealed class FakeCityPopulationWeatherExposureStateRepository : ICityPopulationWeatherExposureStateRepository
    {
        public CityPopulationWeatherExposureState? State { get; set; }
        public CityId? RequestedCityId { get; private set; }
        public List<CityPopulationWeatherExposureState> AddedStates { get; } = [];

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

        public Task DeleteByCityAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    internal sealed class FakeCityPopulationCostOfLivingStateRepository : ICityPopulationCostOfLivingStateRepository
    {
        public CityPopulationCostOfLivingState? State { get; set; }
        public CityId? RequestedCityId { get; private set; }
        public List<CityPopulationCostOfLivingState> AddedStates { get; } = [];

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

        public Task DeleteByCityAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    internal sealed class FakeCityPopulationEssentialsStateRepository : ICityPopulationEssentialsStateRepository
    {
        public CityPopulationEssentialsState? State { get; set; }
        public CityId? RequestedCityId { get; private set; }
        public List<CityPopulationEssentialsState> AddedStates { get; } = [];

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

        public Task DeleteByCityAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    internal sealed class FakeCityPopulationLivingConditionsStateRepository : ICityPopulationLivingConditionsStateRepository
    {
        public CityPopulationLivingConditionsState? State { get; set; }
        public CityId? RequestedCityId { get; private set; }
        public List<CityPopulationLivingConditionsState> AddedStates { get; } = [];

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

        public Task DeleteByCityAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
