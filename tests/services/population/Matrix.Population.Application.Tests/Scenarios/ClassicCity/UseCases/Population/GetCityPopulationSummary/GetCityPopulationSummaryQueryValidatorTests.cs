using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary;

public sealed class GetCityPopulationSummaryQueryValidatorTests
{
    private readonly GetCityPopulationSummaryQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsNoErrors()
    {
        var result = _validator.Validate(new GetCityPopulationSummaryQuery(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyCityId_ReturnsError()
    {
        var result = _validator.Validate(new GetCityPopulationSummaryQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CityId");
    }
}
