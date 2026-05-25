using Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.Bootstrap.InitializeCityEconomy
{
    public sealed class InitializeCityEconomyCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ForwardsCommandToBootstrapService()
        {
            var bootstrapService = new FakeCityEconomyBootstrapService();
            var handler = new InitializeCityEconomyCommandHandler(bootstrapService);
            var command = new InitializeCityEconomyCommand(
                CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                SimulationKind: "ClassicCity",
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
                expected: (command.CityId, command.SimulationKind, command.EconomyProfile, command.CreatedAtUtc),
                actual: bootstrapService.Request);
        }
    }
}
