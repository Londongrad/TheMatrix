using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class CityHouseholdObligationRepositoryTests
    {
        [Fact]
        public async Task ListDueByCityAsync_FiltersInactiveAndOrdersByDueDateThenName()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var householdA = Guid.Parse("10000000-0000-0000-0000-000000000001");
            var householdB = Guid.Parse("10000000-0000-0000-0000-000000000002");
            var provider = Guid.Parse("20000000-0000-0000-0000-000000000001");
            CityHouseholdObligation early = CreateHouseholdObligation(
                cityId: cityId,
                householdAccountId: householdA,
                providerBusinessId: provider,
                name: "Electricity",
                firstChargeDueAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 7,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            CityHouseholdObligation later = CreateHouseholdObligation(
                cityId: cityId,
                householdAccountId: householdB,
                providerBusinessId: provider,
                name: "Water",
                firstChargeDueAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 8,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            CityHouseholdObligation inactive = CreateHouseholdObligation(
                cityId: cityId,
                householdAccountId: householdB,
                providerBusinessId: provider,
                name: "Inactive",
                firstChargeDueAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 7,
                    hour: 7,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            inactive.Deactivate();

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityHouseholdObligations.AddRange(
                early,
                later,
                inactive);
            await dbContext.SaveChangesAsync();

            CityHouseholdObligationRepository repository = new(dbContext);

            IReadOnlyList<CityHouseholdObligation> due = await repository.ListDueByCityAsync(
                cityId: cityId,
                asOfUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 8,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));

            Assert.Equal(
                expected: 2,
                actual: due.Count);
            Assert.Collection(
                collection: due,
                x => Assert.Equal(
                    expected: "Electricity",
                    actual: x.Name),
                x => Assert.Equal(
                    expected: "Water",
                    actual: x.Name));
        }

        [Fact]
        public async Task ListByHouseholdsAsync_ReturnsEmptyForEmptyInputAndOrdersByHouseholdThenName()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var householdA = Guid.Parse("10000000-0000-0000-0000-000000000001");
            var householdB = Guid.Parse("10000000-0000-0000-0000-000000000002");
            var provider = Guid.Parse("20000000-0000-0000-0000-000000000001");

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityHouseholdObligations.AddRange(
                CreateHouseholdObligation(
                    cityId: cityId,
                    householdAccountId: householdB,
                    providerBusinessId: provider,
                    name: "Water"),
                CreateHouseholdObligation(
                    cityId: cityId,
                    householdAccountId: householdA,
                    providerBusinessId: provider,
                    name: "Electricity"),
                CreateHouseholdObligation(
                    cityId: cityId,
                    householdAccountId: householdA,
                    providerBusinessId: provider,
                    name: "Internet"));
            await dbContext.SaveChangesAsync();

            CityHouseholdObligationRepository repository = new(dbContext);

            IReadOnlyList<CityHouseholdObligation> empty = await repository.ListByHouseholdsAsync([]);
            IReadOnlyList<CityHouseholdObligation> obligations = await repository.ListByHouseholdsAsync(
            [
                householdA,
                householdB
            ]);

            Assert.Empty(empty);
            Assert.Collection(
                collection: obligations,
                x => Assert.Equal(
                    expected: (householdA, "Electricity"),
                    actual: (x.HouseholdAccountId, x.Name)),
                x => Assert.Equal(
                    expected: (householdA, "Internet"),
                    actual: (x.HouseholdAccountId, x.Name)),
                x => Assert.Equal(
                    expected: (householdB, "Water"),
                    actual: (x.HouseholdAccountId, x.Name)));
        }
    }
}
