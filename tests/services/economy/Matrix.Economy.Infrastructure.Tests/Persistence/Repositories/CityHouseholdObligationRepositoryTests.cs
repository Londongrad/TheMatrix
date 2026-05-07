using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Matrix.Economy.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Persistence.Repositories;

public sealed class CityHouseholdObligationRepositoryTests
{
    [Fact]
    public async Task ListDueByCityAsync_FiltersInactiveAndOrdersByDueDateThenName()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid householdA = Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid householdB = Guid.Parse("10000000-0000-0000-0000-000000000002");
        Guid provider = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var early = CreateHouseholdObligation(
            cityId,
            householdA,
            provider,
            "Electricity",
            new DateTimeOffset(2048, 5, 7, 8, 0, 0, TimeSpan.Zero));
        var later = CreateHouseholdObligation(
            cityId,
            householdB,
            provider,
            "Water",
            new DateTimeOffset(2048, 5, 8, 8, 0, 0, TimeSpan.Zero));
        var inactive = CreateHouseholdObligation(
            cityId,
            householdB,
            provider,
            "Inactive",
            new DateTimeOffset(2048, 5, 7, 7, 0, 0, TimeSpan.Zero));
        inactive.Deactivate();

        await using var dbContext = CreateDbContext();
        dbContext.CityHouseholdObligations.AddRange(early, later, inactive);
        await dbContext.SaveChangesAsync();

        CityHouseholdObligationRepository repository = new(dbContext);

        var due = await repository.ListDueByCityAsync(
            cityId,
            new DateTimeOffset(2048, 5, 8, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(2, due.Count);
        Assert.Collection(
            due,
            x => Assert.Equal("Electricity", x.Name),
            x => Assert.Equal("Water", x.Name));
    }

    [Fact]
    public async Task ListByHouseholdsAsync_ReturnsEmptyForEmptyInputAndOrdersByHouseholdThenName()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid householdA = Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid householdB = Guid.Parse("10000000-0000-0000-0000-000000000002");
        Guid provider = Guid.Parse("20000000-0000-0000-0000-000000000001");

        await using var dbContext = CreateDbContext();
        dbContext.CityHouseholdObligations.AddRange(
            CreateHouseholdObligation(cityId, householdB, provider, "Water"),
            CreateHouseholdObligation(cityId, householdA, provider, "Electricity"),
            CreateHouseholdObligation(cityId, householdA, provider, "Internet"));
        await dbContext.SaveChangesAsync();

        CityHouseholdObligationRepository repository = new(dbContext);

        var empty = await repository.ListByHouseholdsAsync([]);
        var obligations = await repository.ListByHouseholdsAsync([householdA, householdB]);

        Assert.Empty(empty);
        Assert.Collection(
            obligations,
            x => Assert.Equal((householdA, "Electricity"), (x.HouseholdAccountId, x.Name)),
            x => Assert.Equal((householdA, "Internet"), (x.HouseholdAccountId, x.Name)),
            x => Assert.Equal((householdB, "Water"), (x.HouseholdAccountId, x.Name)));
    }
}
