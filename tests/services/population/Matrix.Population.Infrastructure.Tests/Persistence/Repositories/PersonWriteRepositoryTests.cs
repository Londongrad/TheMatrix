using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Persistence.Repositories;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Population.Infrastructure.Tests.TestSupport.PopulationInfrastructureTestSupport;

namespace Matrix.Population.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class PersonWriteRepositoryTests
    {
        [Fact]
        public async Task AddAsync_PersistsPerson()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            var repository = new PersonWriteRepository(dbContext);
            Person person = CreatePerson(
                personId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                firstName: "Pavel");
            dbContext.Households.Add(CreateHousehold(householdId: person.HouseholdId.Value));

            await repository.AddAsync(person);
            await dbContext.SaveChangesAsync();

            Assert.Contains(
                collection: dbContext.Persons,
                filter: x => x.Id == person.Id);
        }

        [Fact]
        public async Task AddRangeAsync_PersistsAllPersons()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            var repository = new PersonWriteRepository(dbContext);
            Person first = CreatePerson(firstName: "Nika");
            Person second = CreatePerson(firstName: "Oleg");
            dbContext.Households.AddRange(
                CreateHousehold(householdId: first.HouseholdId.Value),
                CreateHousehold(householdId: second.HouseholdId.Value));

            await repository.AddRangeAsync(
            [
                first,
                second
            ]);
            await dbContext.SaveChangesAsync();

            Assert.Equal(
                expected: 2,
                actual: dbContext.Persons.Count());
        }

        [Fact]
        public async Task UpdateAsync_StoresMutatedPerson()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            Person person = CreatePerson(
                personId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
                firstName: "Old");
            dbContext.Households.Add(CreateHousehold(householdId: person.HouseholdId.Value));
            dbContext.Persons.Add(person);
            await dbContext.SaveChangesAsync();
            var repository = new PersonWriteRepository(dbContext);

            person.ChangeName(
                new PersonName(
                    firstName: "New",
                    lastName: "Ivanov"));
            await repository.UpdateAsync(person);
            await dbContext.SaveChangesAsync();

            Person stored = Assert.Single(dbContext.Persons);
            Assert.Equal(
                expected: "New",
                actual: stored.Name.FirstName);
        }

        [Fact]
        public async Task DeleteAsync_RemovesPerson()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            Person person = CreatePerson(
                personId: Guid.Parse("66666666-6666-6666-6666-666666666666"),
                firstName: "Delete");
            dbContext.Households.Add(CreateHousehold(householdId: person.HouseholdId.Value));
            dbContext.Persons.Add(person);
            await dbContext.SaveChangesAsync();
            var repository = new PersonWriteRepository(dbContext);

            await repository.DeleteAsync(person);
            await dbContext.SaveChangesAsync();

            Assert.Empty(dbContext.Persons);
        }

        [Fact]
        public async Task DeleteAllAsync_RemovesEveryPerson()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            Person first = CreatePerson(firstName: "A");
            Person second = CreatePerson(firstName: "B");
            dbContext.Households.AddRange(
                CreateHousehold(householdId: first.HouseholdId.Value),
                CreateHousehold(householdId: second.HouseholdId.Value));
            dbContext.Persons.AddRange(
                first,
                second);
            await dbContext.SaveChangesAsync();
            var repository = new PersonWriteRepository(dbContext);

            await repository.DeleteAllAsync();

            Assert.Empty(dbContext.Persons);
        }
    }
}
