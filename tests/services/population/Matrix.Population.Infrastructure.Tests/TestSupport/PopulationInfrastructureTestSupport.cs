using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.ValueObjects;
using Matrix.Population.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Tests.TestSupport
{
    internal static class PopulationInfrastructureTestSupport
    {
        internal static PopulationTestDatabase CreateDbContext()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            DbContextOptions<PopulationDbContext> options = new DbContextOptionsBuilder<PopulationDbContext>()
               .UseSqlite(connection)
               .Options;

            var dbContext = new PopulationDbContext(options);
            dbContext.Database.EnsureCreated();
            return new PopulationTestDatabase(
                dbContext: dbContext,
                connection: connection);
        }

        internal static Person CreatePerson(
            Guid? personId = null,
            Guid? householdId = null,
            string firstName = "Ivan",
            string lastName = "Ivanov",
            DateOnly? birthDate = null,
            DateOnly? currentDate = null,
            int functionalCapacity = 100)
        {
            DateOnly resolvedCurrentDate = currentDate ??
            new DateOnly(
                year: 2048,
                month: 5,
                day: 1);

            return Person.CreatePerson(
                id: PersonId.From(personId ?? Guid.NewGuid()),
                householdId: HouseholdId.From(householdId ?? Guid.NewGuid()),
                name: new PersonName(
                    firstName: firstName,
                    lastName: lastName),
                sex: Sex.Male,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: MaritalStatus.Single,
                spouseId: null,
                educationLevel: EducationLevel.UpperSecondary,
                educationInstitutionId: null,
                educationInstitutionAnchorId: null,
                employmentStatus: EmploymentStatus.Unemployed,
                happinessLevel: HappinessLevel.From(50),
                energyLevel: EnergyLevel.From(60),
                stressLevel: StressLevel.From(20),
                socialNeedLevel: SocialNeedLevel.From(30),
                personality: Personality.Neutral(),
                birthDate: birthDate ??
                new DateOnly(
                    year: 2030,
                    month: 4,
                    day: 2),
                healthLevel: HealthLevel.From(90),
                weight: BodyWeight.FromKilograms(72m),
                job: null,
                currentDate: resolvedCurrentDate,
                illness: IllnessInfo.Healthy(),
                functionalCapacity: FunctionalCapacityLevel.From(functionalCapacity));
        }

        internal static Household CreateHousehold(
            Guid? householdId = null,
            int size = 3,
            decimal cashReserve = 100m)
        {
            return Household.Create(
                id: HouseholdId.From(householdId ?? Guid.NewGuid()),
                size: HouseholdSize.From(size),
                createdAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                cashReserve: Money.FromDecimal(cashReserve));
        }
    }

    internal sealed class PopulationTestDatabase(
        PopulationDbContext dbContext,
        SqliteConnection connection)
        : IAsyncDisposable
    {
        public PopulationDbContext DbContext { get; } = dbContext;

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
