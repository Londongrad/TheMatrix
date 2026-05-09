using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.AdvanceCityStockpiles;
using Xunit;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.AdvanceCityStockpiles;

public sealed class AdvanceCityStockpilesValidatorTests
{
    [Fact]
    public void Validator_RejectsEmptyCityIdAndNonUtcTimestamps()
    {
        var validator = new AdvanceCityStockpilesCommandValidator();

        var result = validator.Validate(new AdvanceCityStockpilesCommand(
            CityId: Guid.Empty,
            FromSimTimeUtc: new DateTimeOffset(2049, 1, 1, 18, 0, 0, TimeSpan.FromHours(9)),
            ToSimTimeUtc: new DateTimeOffset(2049, 1, 1, 19, 0, 0, TimeSpan.FromHours(9)),
            TickId: 4));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 3);
    }
}
