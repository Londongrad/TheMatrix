using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;

public sealed class AdvanceCityPopulationCommandValidatorTests
{
    private readonly AdvanceCityPopulationCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var result = _validator.Validate(new AdvanceCityPopulationCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            FromSimTimeUtc: new DateTimeOffset(2048, 5, 5, 9, 0, 0, TimeSpan.Zero),
            ToSimTimeUtc: new DateTimeOffset(2048, 5, 5, 10, 0, 0, TimeSpan.Zero),
            TickId: 15));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidInputs_ReturnsErrors()
    {
        var result = _validator.Validate(new AdvanceCityPopulationCommand(
            CityId: Guid.Empty,
            FromSimTimeUtc: new DateTimeOffset(2048, 5, 5, 9, 0, 0, TimeSpan.FromHours(3)),
            ToSimTimeUtc: new DateTimeOffset(2048, 5, 4, 10, 0, 0, TimeSpan.FromHours(3)),
            TickId: -1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CityId");
        Assert.Contains(result.Errors, x => x.PropertyName == "FromSimTimeUtc");
        Assert.Contains(result.Errors, x => x.PropertyName == "ToSimTimeUtc");
        Assert.Contains(result.Errors, x => x.PropertyName == "TickId");
        Assert.Contains(result.Errors, x => x.ErrorMessage.Contains("ToSimTimeUtc date cannot be earlier than FromSimTimeUtc date."));
    }
}
