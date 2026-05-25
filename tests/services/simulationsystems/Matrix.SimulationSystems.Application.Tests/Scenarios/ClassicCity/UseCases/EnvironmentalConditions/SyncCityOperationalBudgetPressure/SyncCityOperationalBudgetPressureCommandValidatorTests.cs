using FluentValidation.Results;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SyncCityOperationalBudgetPressure;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SyncCityOperationalBudgetPressure
{
    public sealed class SyncCityOperationalBudgetPressureCommandValidatorTests
    {
        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            var validator = new SyncCityOperationalBudgetPressureCommandValidator();

            ValidationResult? result = validator.Validate(CreateCommand());

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithInvalidInputs_ReturnsErrors()
        {
            var validator = new SyncCityOperationalBudgetPressureCommandValidator();

            ValidationResult? result = validator.Validate(
                new SyncCityOperationalBudgetPressureCommand(
                    CityId: Guid.Empty,
                    Balance: 1m,
                    MunicipalOperationsExpenses: 2m,
                    GeneralAvailableAmount: 3m,
                    OperationsAvailableAmount: 4m,
                    InfrastructureAvailableAmount: 5m,
                    HealthcareAvailableAmount: 6m,
                    GeneralAuthorizationLevel: "High",
                    OperationsAuthorizationLevel: "High",
                    InfrastructureAuthorizationLevel: "High",
                    HealthcareAuthorizationLevel: "High",
                    PressureIndex: 1.2m,
                    EffectiveTickId: -1,
                    EffectiveAtUtc: new DateTimeOffset(
                        year: 2052,
                        month: 3,
                        day: 4,
                        hour: 8,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.FromHours(3))));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CityId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "PressureIndex");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "EffectiveTickId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "EffectiveAtUtc");
        }

        private static SyncCityOperationalBudgetPressureCommand CreateCommand()
        {
            return new SyncCityOperationalBudgetPressureCommand(
                CityId: SimulationSystemsApplicationTestSupport.CityId,
                Balance: -50_000m,
                MunicipalOperationsExpenses: 300_000m,
                GeneralAvailableAmount: 80_000m,
                OperationsAvailableAmount: 70_000m,
                InfrastructureAvailableAmount: 60_000m,
                HealthcareAvailableAmount: 50_000m,
                GeneralAuthorizationLevel: "Restricted",
                OperationsAuthorizationLevel: "Emergency",
                InfrastructureAuthorizationLevel: "Restricted",
                HealthcareAuthorizationLevel: "Constrained",
                PressureIndex: 0.72m,
                EffectiveTickId: 5,
                EffectiveAtUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc);
        }
    }
}
