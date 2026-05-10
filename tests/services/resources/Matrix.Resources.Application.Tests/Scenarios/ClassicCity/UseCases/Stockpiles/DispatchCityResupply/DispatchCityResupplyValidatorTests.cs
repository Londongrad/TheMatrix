using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Xunit;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply;

public sealed class DispatchCityResupplyValidatorTests
{
    [Fact]
    public void Validator_RejectsEmptyCityIdAndInvalidEnums()
    {
        var validator = new DispatchCityResupplyCommandValidator();

        var result = validator.Validate(new DispatchCityResupplyCommand(
            CityId: Guid.Empty,
            Focus: (ResupplyFocus)99,
            Intensity: (ResupplyIntensity)99,
            EmergencyOverride: false));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 3);
    }
}
