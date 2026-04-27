using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.DispatchCityTrip;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.World.DispatchCityTrip;

public sealed class DispatchCityTripCommandValidatorTests
{
    private readonly DispatchCityTripCommandValidator _validator = new();

    [Fact]
    public void Validate_WithNormalizedSupportedValues_ReturnsNoErrors()
    {
        var result = _validator.Validate(
            new DispatchCityTripCommand(
                CityId: Guid.NewGuid(),
                FromKind: "residential_building",
                FromId: Guid.NewGuid(),
                ToKind: "city-anchor",
                ToId: Guid.NewGuid(),
                Purpose: "service_response",
                Profile: "emergency-response",
                MovementCapabilityIndex: 1.2m,
                TravellerEntityId: Guid.NewGuid(),
                Subject: "Urgent route"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithUnsupportedAndInvalidValues_ReturnsErrors()
    {
        var result = _validator.Validate(
            new DispatchCityTripCommand(
                CityId: Guid.Empty,
                FromKind: "mystery",
                FromId: Guid.Empty,
                ToKind: "unknown",
                ToId: Guid.Empty,
                Purpose: "teleport",
                Profile: "hovercraft",
                MovementCapabilityIndex: 10m,
                TravellerEntityId: null,
                Subject: new string('x', 161)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "CityId");
        Assert.Contains(result.Errors, error => error.PropertyName == "FromKind");
        Assert.Contains(result.Errors, error => error.PropertyName == "FromId");
        Assert.Contains(result.Errors, error => error.PropertyName == "ToKind");
        Assert.Contains(result.Errors, error => error.PropertyName == "ToId");
        Assert.Contains(result.Errors, error => error.PropertyName == "Purpose");
        Assert.Contains(result.Errors, error => error.PropertyName == "Profile");
        Assert.Contains(result.Errors, error => error.PropertyName == "MovementCapabilityIndex");
        Assert.Contains(result.Errors, error => error.PropertyName == "Subject");
    }
}
