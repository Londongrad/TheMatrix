using FluentValidation.Results;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.DispatchCityHeatingMaintenance;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.Heating.
    DispatchCityHeatingMaintenance
{
    public sealed class DispatchCityHeatingMaintenanceCommandValidatorTests
    {
        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            var validator = new DispatchCityHeatingMaintenanceCommandValidator();

            ValidationResult? result = validator.Validate(
                new DispatchCityHeatingMaintenanceCommand(
                    CityId: Guid.Parse("ffffffff-aaaa-bbbb-cccc-dddddddddddd"),
                    Focus: "PlantRepairs",
                    Intensity: "Heavy",
                    EmergencyOverride: false));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithInvalidInputs_ReturnsErrors()
        {
            var validator = new DispatchCityHeatingMaintenanceCommandValidator();

            ValidationResult? result = validator.Validate(
                new DispatchCityHeatingMaintenanceCommand(
                    CityId: Guid.Empty,
                    Focus: "Unknown",
                    Intensity: "Ultra",
                    EmergencyOverride: true));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CityId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Focus");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Intensity");
        }
    }
}
