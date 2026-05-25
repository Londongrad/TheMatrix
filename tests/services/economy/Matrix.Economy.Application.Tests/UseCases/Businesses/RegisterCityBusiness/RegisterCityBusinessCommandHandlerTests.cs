using Matrix.Economy.Application.UseCases.Businesses;
using Matrix.Economy.Application.UseCases.Businesses.RegisterCityBusiness;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.Businesses.RegisterCityBusiness
{
    public sealed class RegisterCityBusinessCommandHandlerTests
    {
        [Fact]
        public async Task Handle_CreatesBusinessWithFrozenTimestamp()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var repository = new FakeCityBusinessRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 7,
                    hour: 12,
                    minute: 34,
                    second: 56,
                    offset: TimeSpan.Zero));
            var handler = new RegisterCityBusinessCommandHandler(
                businessRepository: repository,
                unitOfWork: unitOfWork,
                timeProvider: timeProvider);
            var command = new RegisterCityBusinessCommand(
                CityId: cityId,
                Name: "Central Utility",
                Kind: CityBusinessKind.Utility,
                StartingCapital: 350m,
                UnitKind: null,
                UnitCode: null,
                UnitDisplayName: null,
                UnitSymbol: null);

            CityBusinessDto result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            CityBusiness business = Assert.Single(repository.AddedBusinesses);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: business.CreatedAtUtc);
            Assert.Equal(
                expected: result.BusinessId,
                actual: business.Id);
            Assert.Equal(
                expected: cityId,
                actual: result.CityId);
            Assert.Equal(
                expected: timeProvider.UtcNow.ToString("O"),
                actual: result.CreatedAtUtc);
            Assert.Equal(
                expected: "Central Utility",
                actual: result.Name);
            Assert.Equal(
                expected: "Utility",
                actual: result.Kind);
            Assert.Equal(
                expected: "Currency",
                actual: result.UnitKind);
            Assert.Equal(
                expected: "MNY",
                actual: result.UnitCode);
            Assert.Equal(
                expected: "Money",
                actual: result.UnitDisplayName);
            Assert.Equal(
                expected: "¤",
                actual: result.UnitSymbol);
            Assert.Equal(
                expected: 350m,
                actual: result.Balance);
            Assert.Equal(
                expected: 350m,
                actual: result.TotalCapitalInjections);
        }

        [Fact]
        public async Task Handle_UsesExplicitUnitProfile()
        {
            var cityId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var repository = new FakeCityBusinessRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            var handler = new RegisterCityBusinessCommandHandler(
                businessRepository: repository,
                unitOfWork: unitOfWork,
                timeProvider: timeProvider);
            var command = new RegisterCityBusinessCommand(
                CityId: cityId,
                Name: "Ore Exchange",
                Kind: CityBusinessKind.Manufacturer,
                StartingCapital: 90m,
                UnitKind: "Resource",
                UnitCode: "ore",
                UnitDisplayName: "Ore Credits",
                UnitSymbol: "OC");

            CityBusinessDto result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "Resource",
                actual: result.UnitKind);
            Assert.Equal(
                expected: "ORE",
                actual: result.UnitCode);
            Assert.Equal(
                expected: "Ore Credits",
                actual: result.UnitDisplayName);
            Assert.Equal(
                expected: "OC",
                actual: result.UnitSymbol);
            Assert.Equal(
                expected: 90m,
                actual: result.Balance);
        }
    }
}
