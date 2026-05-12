using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.DispatchCityRoadAccessMaintenance;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.RoadAccess.DispatchCityRoadAccessMaintenance;

public sealed class DispatchCityRoadAccessMaintenanceCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var validator = new DispatchCityRoadAccessMaintenanceCommandValidator();

        var result = validator.Validate(new DispatchCityRoadAccessMaintenanceCommand(
            CityId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Focus: "CorridorClearance",
            Intensity: "Heavy",
            EmergencyOverride: false));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithInvalidInputs_ReturnsErrors()
    {
        var validator = new DispatchCityRoadAccessMaintenanceCommandValidator();

        var result = validator.Validate(new DispatchCityRoadAccessMaintenanceCommand(
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
