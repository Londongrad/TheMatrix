using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.Common;

public sealed class CityPopulationBootstrapTuningInputValidatorTests
{
    private readonly CityPopulationBootstrapTuningInputValidator _validator = new();

    [Fact]
    public void Validate_WithValidInput_ReturnsNoErrors()
    {
        var result = _validator.Validate(new CityPopulationBootstrapTuningInput(
            HousingPressurePercent: 35,
            EconomicStabilityPercent: 60,
            SocialVolatilityPercent: 25,
            FamilyFormationPercent: 40));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithOutOfRangeValues_ReturnsErrors()
    {
        var result = _validator.Validate(new CityPopulationBootstrapTuningInput(
            HousingPressurePercent: -1,
            EconomicStabilityPercent: 101,
            SocialVolatilityPercent: 150,
            FamilyFormationPercent: -20));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "HousingPressurePercent");
        Assert.Contains(result.Errors, x => x.PropertyName == "EconomicStabilityPercent");
        Assert.Contains(result.Errors, x => x.PropertyName == "SocialVolatilityPercent");
        Assert.Contains(result.Errors, x => x.PropertyName == "FamilyFormationPercent");
    }
}
