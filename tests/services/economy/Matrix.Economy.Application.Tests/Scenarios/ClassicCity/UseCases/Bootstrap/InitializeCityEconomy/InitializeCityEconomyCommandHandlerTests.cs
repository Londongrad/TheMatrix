using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Bootstrap.InitializeCityEconomy;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.Bootstrap.InitializeCityEconomy
{
    public sealed class InitializeCityEconomyCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ForwardsCommandToBootstrapService()
        {
            var bootstrapService = new FakeCityEconomyBootstrapService();
            var deletionRepository = new FakeCityEconomyDeletionRepository();
            var handler = new InitializeCityEconomyCommandHandler(
                bootstrapService,
                deletionRepository);
            var command = new InitializeCityEconomyCommand(
                CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                ScenarioKey: "classic-city",
                EconomyProfile: "baseline",
                CreatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 11,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));

            CityEconomyBootstrapResultDto result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: bootstrapService.Result,
                actual: result);
            Assert.Equal(
                expected: (command.CityId, command.ScenarioKey, command.EconomyProfile, command.CreatedAtUtc),
                actual: bootstrapService.Request);
        }

        [Fact]
        public async Task Handle_WhenCityWasDeleted_RejectsReinitialization()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var bootstrapService = new FakeCityEconomyBootstrapService();
            var deletionRepository = new FakeCityEconomyDeletionRepository
            {
                DeletedAtUtc = new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 7,
                    hour: 11,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero)
            };
            var handler = new InitializeCityEconomyCommandHandler(
                bootstrapService,
                deletionRepository);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(() =>
                handler.Handle(
                    request: new InitializeCityEconomyCommand(
                        CityId: cityId,
                        ScenarioKey: "classic-city",
                        EconomyProfile: "baseline",
                        CreatedAtUtc: deletionRepository.DeletedAtUtc.Value.AddDays(-1)),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Economy.City.Deleted",
                actual: exception.Code);
            Assert.Null(bootstrapService.Request);
        }
    }
}
