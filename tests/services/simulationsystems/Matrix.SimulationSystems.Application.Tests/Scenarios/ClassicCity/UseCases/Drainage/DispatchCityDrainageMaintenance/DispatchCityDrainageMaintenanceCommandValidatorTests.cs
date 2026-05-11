using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.DispatchCityDrainageMaintenance;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.Drainage.DispatchCityDrainageMaintenance;

public sealed class DispatchCityDrainageMaintenanceCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var validator = new DispatchCityDrainageMaintenanceCommandValidator();

        var result = validator.Validate(new DispatchCityDrainageMaintenanceCommand(
            CityId: Guid.Parse("eeeeeeee-ffff-aaaa-bbbb-cccccccccccc"),
            Focus: "PumpRepairs",
            Intensity: "Heavy",
            EmergencyOverride: false));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithInvalidInputs_ReturnsErrors()
    {
        var validator = new DispatchCityDrainageMaintenanceCommandValidator();

        var result = validator.Validate(new DispatchCityDrainageMaintenanceCommand(
            CityId: Guid.Empty,
            Focus: "Unknown",
            Intensity: "Ultra",
            EmergencyOverride: true));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CityId");
        Assert.Contains(result.Errors, x => x.PropertyName == "Focus");
        Assert.Contains(result.Errors, x => x.PropertyName == "Intensity");
    }
}
