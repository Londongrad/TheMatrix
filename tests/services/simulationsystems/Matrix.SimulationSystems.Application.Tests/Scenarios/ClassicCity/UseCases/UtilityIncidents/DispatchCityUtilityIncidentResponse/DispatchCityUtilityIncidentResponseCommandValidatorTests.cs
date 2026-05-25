using FluentValidation.Results;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    DispatchCityUtilityIncidentResponse;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    DispatchCityUtilityIncidentResponse
{
    public sealed class DispatchCityUtilityIncidentResponseCommandValidatorTests
    {
        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            var validator = new DispatchCityUtilityIncidentResponseCommandValidator();

            ValidationResult? result = validator.Validate(
                new DispatchCityUtilityIncidentResponseCommand(
                    CityId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    Focus: "PowerOutages",
                    Intensity: "Heavy",
                    EmergencyOverride: false,
                    FocusDistrictId: Guid.Parse("74000000-0000-0000-0000-000000000001")));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithInvalidInputs_ReturnsErrors()
        {
            var validator = new DispatchCityUtilityIncidentResponseCommandValidator();

            ValidationResult? result = validator.Validate(
                new DispatchCityUtilityIncidentResponseCommand(
                    CityId: Guid.Empty,
                    Focus: "Unknown",
                    Intensity: "Ultra",
                    EmergencyOverride: true,
                    FocusDistrictId: null));

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
