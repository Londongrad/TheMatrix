using FluentValidation.Results;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.DispatchCitySanitationMaintenance;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.Sanitation.
    DispatchCitySanitationMaintenance
{
    public sealed class DispatchCitySanitationMaintenanceCommandValidatorTests
    {
        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            var validator = new DispatchCitySanitationMaintenanceCommandValidator();

            ValidationResult? result = validator.Validate(
                new DispatchCitySanitationMaintenanceCommand(
                    CityId: Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"),
                    Focus: "OverflowControl",
                    Intensity: "Heavy",
                    EmergencyOverride: false));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithInvalidInputs_ReturnsErrors()
        {
            var validator = new DispatchCitySanitationMaintenanceCommandValidator();

            ValidationResult? result = validator.Validate(
                new DispatchCitySanitationMaintenanceCommand(
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
