using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSuggestedCityNames;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.GetSuggestedCityNames;

public sealed class GetSuggestedCityNamesQueryValidatorTests
{
    private readonly GetSuggestedCityNamesQueryValidator _validator = new();

    [Fact]
    public void Validate_WithCountWithinAllowedRange_ReturnsNoErrors()
    {
        var result = _validator.Validate(new GetSuggestedCityNamesQuery("alpha", 12));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(26)]
    public void Validate_WithCountOutsideAllowedRange_ReturnsError(int count)
    {
        var result = _validator.Validate(new GetSuggestedCityNamesQuery("alpha", count));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Count");
    }
}
