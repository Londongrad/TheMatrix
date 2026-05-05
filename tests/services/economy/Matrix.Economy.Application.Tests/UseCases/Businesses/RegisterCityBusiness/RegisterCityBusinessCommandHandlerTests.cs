using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.Businesses;
using Matrix.Economy.Application.UseCases.Businesses.RegisterCityBusiness;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.Businesses.RegisterCityBusiness;

public sealed class RegisterCityBusinessCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesBusinessWithFrozenTimestamp()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var repository = new FakeCityBusinessRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 7, 12, 34, 56, TimeSpan.Zero));
        var handler = new RegisterCityBusinessCommandHandler(repository, unitOfWork, timeProvider);
        var command = new RegisterCityBusinessCommand(
            CityId: cityId,
            Name: "Central Utility",
            Kind: CityBusinessKind.Utility,
            StartingCapital: 350m,
            UnitKind: null,
            UnitCode: null,
            UnitDisplayName: null,
            UnitSymbol: null);

        CityBusinessDto result = await handler.Handle(command, CancellationToken.None);

        var business = Assert.Single(repository.AddedBusinesses);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(timeProvider.UtcNow, business.CreatedAtUtc);
        Assert.Equal(result.BusinessId, business.Id);
        Assert.Equal(cityId, result.CityId);
        Assert.Equal(timeProvider.UtcNow.ToString("O"), result.CreatedAtUtc);
        Assert.Equal("Central Utility", result.Name);
        Assert.Equal("Utility", result.Kind);
        Assert.Equal("Currency", result.UnitKind);
        Assert.Equal("MNY", result.UnitCode);
        Assert.Equal("Money", result.UnitDisplayName);
        Assert.Equal("¤", result.UnitSymbol);
        Assert.Equal(350m, result.Balance);
        Assert.Equal(350m, result.TotalCapitalInjections);
    }

    [Fact]
    public async Task Handle_UsesExplicitUnitProfile()
    {
        Guid cityId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var repository = new FakeCityBusinessRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 6, 1, 8, 0, 0, TimeSpan.Zero));
        var handler = new RegisterCityBusinessCommandHandler(repository, unitOfWork, timeProvider);
        var command = new RegisterCityBusinessCommand(
            CityId: cityId,
            Name: "Ore Exchange",
            Kind: CityBusinessKind.Manufacturer,
            StartingCapital: 90m,
            UnitKind: "Resource",
            UnitCode: "ore",
            UnitDisplayName: "Ore Credits",
            UnitSymbol: "OC");

        CityBusinessDto result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Resource", result.UnitKind);
        Assert.Equal("ORE", result.UnitCode);
        Assert.Equal("Ore Credits", result.UnitDisplayName);
        Assert.Equal("OC", result.UnitSymbol);
        Assert.Equal(90m, result.Balance);
    }
}
