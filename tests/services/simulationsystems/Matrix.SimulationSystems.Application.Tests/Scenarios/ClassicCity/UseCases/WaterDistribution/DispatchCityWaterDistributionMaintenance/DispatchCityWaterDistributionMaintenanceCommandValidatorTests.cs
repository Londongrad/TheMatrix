using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.DispatchCityWaterDistributionMaintenance;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.WaterDistribution.DispatchCityWaterDistributionMaintenance;

public sealed class DispatchCityWaterDistributionMaintenanceCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var validator = new DispatchCityWaterDistributionMaintenanceCommandValidator();

        var result = validator.Validate(new DispatchCityWaterDistributionMaintenanceCommand(
            CityId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Focus: "PumpRecovery",
            Intensity: "Heavy",
            EmergencyOverride: false));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithInvalidInputs_ReturnsErrors()
    {
        var validator = new DispatchCityWaterDistributionMaintenanceCommandValidator();

        var result = validator.Validate(new DispatchCityWaterDistributionMaintenanceCommand(
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
