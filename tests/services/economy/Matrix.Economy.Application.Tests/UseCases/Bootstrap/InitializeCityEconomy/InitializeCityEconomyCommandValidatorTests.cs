using Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy;
using Xunit;

namespace Matrix.Economy.Application.Tests.UseCases.Bootstrap.InitializeCityEconomy;

public sealed class InitializeCityEconomyCommandValidatorTests
{
    private readonly InitializeCityEconomyCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var result = _validator.Validate(CreateCommand());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidFields_ReturnsErrors()
    {
        var result = _validator.Validate(CreateCommand() with
        {
            CityId = Guid.Empty,
            SimulationKind = new string('x', 65),
            CreatedAtUtc = new DateTimeOffset(2048, 5, 6, 11, 0, 0, TimeSpan.FromHours(3))
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CityId");
        Assert.Contains(result.Errors, x => x.PropertyName == "SimulationKind");
        Assert.Contains(result.Errors, x => x.PropertyName == "CreatedAtUtc");
    }

    private static InitializeCityEconomyCommand CreateCommand()
    {
        return new InitializeCityEconomyCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            SimulationKind: "ClassicCity",
            EconomyProfile: "baseline",
            CreatedAtUtc: new DateTimeOffset(2048, 5, 6, 11, 0, 0, TimeSpan.Zero));
    }
}
