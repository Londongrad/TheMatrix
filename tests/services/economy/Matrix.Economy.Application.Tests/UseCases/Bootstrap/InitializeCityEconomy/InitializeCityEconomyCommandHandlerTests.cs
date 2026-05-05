using Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy;
using Matrix.Economy.Application.Tests.TestSupport;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.Bootstrap.InitializeCityEconomy;

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
            CreatedAtUtc: new DateTimeOffset(2048, 5, 6, 11, 0, 0, TimeSpan.Zero));

        CityEconomyBootstrapResultDto result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(bootstrapService.Result, result);
        Assert.Equal(
            (command.CityId, command.SimulationKind, command.EconomyProfile, command.CreatedAtUtc),
            bootstrapService.Request);
    }
}
