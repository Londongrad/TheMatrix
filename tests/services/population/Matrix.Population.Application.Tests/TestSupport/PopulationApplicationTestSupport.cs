using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
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
            return action(cancellationToken);
        }

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
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

        public Task DeleteAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Matrix.Population.Domain.Entities.Person person, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddRangeAsync(IReadOnlyCollection<Matrix.Population.Domain.Entities.Person> persons, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Matrix.Population.Domain.Entities.Person person, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateAsync(Matrix.Population.Domain.Entities.Person person, CancellationToken cancellationToken = default)
        {
            UpdatedPersons.Add(person);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeCityPopulationPersonReadRepository : ICityPopulationPersonReadRepository
    {
        public CityId? CityIdByPersonId { get; set; }

        public Task<IReadOnlyCollection<Matrix.Population.Domain.Entities.Person>> ListByCityAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyCollection<Matrix.Population.Domain.Entities.Person> Items, int TotalCount)> GetPageByCityAsync(CityId cityId, Pagination pagination, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Matrix.Population.Domain.Entities.Person?> FindByCityAndPersonIdAsync(CityId cityId, PersonId personId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Matrix.Population.Domain.Entities.Person>> ListChildrenByParentIdAsync(CityId cityId, PersonId parentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<CityId?> FindCityIdByPersonIdAsync(PersonId personId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CityIdByPersonId);
        }

        public Task<CityResidentHousingSnapshot?> FindHousingSnapshotByPersonIdAsync(CityId cityId, PersonId personId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<HouseholdId, HousingStatus>> ListHousingStatusesByHouseholdAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<CityEmploymentWorkplaceSnapshot>> ListEmploymentWorkplacesAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CityEmploymentWorkplaceSnapshot?> FindEmploymentWorkplaceByIdAsync(CityId cityId, WorkplaceId workplaceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<CityEducationInstitutionSnapshot>> ListEducationInstitutionsAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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

        public Task UpdateAsync(CityId cityId, DateOnly currentDate, IReadOnlyCollection<Matrix.Population.Domain.Entities.Person> persons, IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements, bool includeCommuteMetrics = true, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(CityId cityId, DateOnly currentDate, IReadOnlyCollection<Matrix.Population.Domain.Entities.Person> persons, bool includeCommuteMetrics = true, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task RebuildAsync(CityId cityId, DateOnly currentDate, bool includeCommuteMetrics = true, CancellationToken cancellationToken = default)
        {
            RebuildCalls.Add((cityId, currentDate));
            return Task.CompletedTask;
        }

        public Task EnsureExistsAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(CityId cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
