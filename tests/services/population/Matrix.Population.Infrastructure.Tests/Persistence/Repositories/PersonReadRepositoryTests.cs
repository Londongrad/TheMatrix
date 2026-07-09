using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Persistence.Repositories;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Population.Infrastructure.Tests.TestSupport.PopulationInfrastructureTestSupport;

namespace Matrix.Population.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class PersonReadRepositoryTests
    {
        [Fact]
        public async Task GetByIdsAsync_ReturnsOnlyDistinctRequestedPersons()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            Person first = CreatePerson(personId: Guid.Parse("11111111-1111-1111-1111-111111111111"));
            Person second = CreatePerson(personId: Guid.Parse("22222222-2222-2222-2222-222222222222"));
            Person ignored = CreatePerson(personId: Guid.Parse("33333333-3333-3333-3333-333333333333"));
            dbContext.Households.AddRange(
                CreateHousehold(householdId: first.HouseholdId.Value),
                CreateHousehold(householdId: second.HouseholdId.Value),
                CreateHousehold(householdId: ignored.HouseholdId.Value));
            dbContext.Persons.AddRange(first, second, ignored);
            await dbContext.SaveChangesAsync();
            var repository = new PersonReadRepository(dbContext);

            IReadOnlyCollection<Person> loaded = await repository.GetByIdsAsync(
                [second.Id, first.Id, first.Id]);

            Assert.Equal(2, loaded.Count);
            Assert.Equal(
                new[] { first.Id, second.Id }.OrderBy(id => id.Value),
                loaded.Select(person => person.Id).OrderBy(id => id.Value));
            Assert.All(loaded, person => Assert.Equal(-1, person.LastVitalStateRevision));
        }

        [Fact]
        public async Task GetAllAsync_ReturnsStoredPersons()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            Person first = CreatePerson(
                personId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                firstName: "Anna");
            Person second = CreatePerson(
                personId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                firstName: "Boris");
            dbContext.Households.AddRange(
                CreateHousehold(householdId: first.HouseholdId.Value),
                CreateHousehold(householdId: second.HouseholdId.Value));
            dbContext.Persons.AddRange(
                first,
                second);
            await dbContext.SaveChangesAsync();
            var repository = new PersonReadRepository(dbContext);

            IReadOnlyCollection<Person> persons = await repository.GetAllAsync();

            Assert.Equal(
                expected: 2,
                actual: persons.Count);
            Assert.Contains(
                collection: persons,
                filter: x => x.Id == first.Id);
            Assert.Contains(
                collection: persons,
                filter: x => x.Id == second.Id);
        }

        [Fact]
        public async Task FindByIdAsync_ReturnsMatchingPerson()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            Person person = CreatePerson(
                personId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                firstName: "Vera");
            dbContext.Households.Add(CreateHousehold(householdId: person.HouseholdId.Value));
            dbContext.Persons.Add(person);
            await dbContext.SaveChangesAsync();
            var repository = new PersonReadRepository(dbContext);

            Person? found =
                await repository.FindByIdAsync(PersonId.From(Guid.Parse("33333333-3333-3333-3333-333333333333")));

            Assert.NotNull(found);
            Assert.Equal(
                expected: person.Id,
                actual: found.Id);
            Assert.Equal(
                expected: "Vera",
                actual: found.Name.FirstName);
        }

        [Fact]
        public async Task GetPageAsync_ReturnsOrderedSliceAndTotalCount()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            Person first = CreatePerson(
                personId: Guid.Parse("00000000-0000-0000-0000-000000000003"),
                firstName: "Third");
            Person second = CreatePerson(
                personId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
                firstName: "First");
            Person third = CreatePerson(
                personId: Guid.Parse("00000000-0000-0000-0000-000000000002"),
                firstName: "Second");
            dbContext.Households.AddRange(
                CreateHousehold(householdId: first.HouseholdId.Value),
                CreateHousehold(householdId: second.HouseholdId.Value),
                CreateHousehold(householdId: third.HouseholdId.Value));
            dbContext.Persons.AddRange(
                first,
                second,
                third);
            await dbContext.SaveChangesAsync();
            var repository = new PersonReadRepository(dbContext);

            (IReadOnlyCollection<Person> Items, int TotalCount) page = await repository.GetPageAsync(
                new Pagination(
                    pageNumber: 2,
                    pageSize: 1));

            Person item = Assert.Single(page.Items);
            Assert.Equal(
                expected: 3,
                actual: page.TotalCount);
            Assert.Equal(
                expected: "Second",
                actual: item.Name.FirstName);
        }
    }
}
