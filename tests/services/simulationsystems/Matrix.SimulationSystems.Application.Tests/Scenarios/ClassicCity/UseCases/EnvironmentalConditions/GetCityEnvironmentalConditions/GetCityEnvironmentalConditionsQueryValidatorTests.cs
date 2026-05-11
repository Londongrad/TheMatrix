using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.GetCityEnvironmentalConditions;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.GetCityEnvironmentalConditions;

public sealed class GetCityEnvironmentalConditionsQueryValidatorTests
{
    [Fact]
    public void Validate_WhenCityIdIsPresent_ReturnsNoErrors()
    {
        var validator = new GetCityEnvironmentalConditionsQueryValidator();

        var result = validator.Validate(new GetCityEnvironmentalConditionsQuery(
            CityId: Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff")));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenCityIdIsEmpty_ReturnsError()
    {
        var validator = new GetCityEnvironmentalConditionsQueryValidator();

        var result = validator.Validate(new GetCityEnvironmentalConditionsQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CityId");
    }
}
