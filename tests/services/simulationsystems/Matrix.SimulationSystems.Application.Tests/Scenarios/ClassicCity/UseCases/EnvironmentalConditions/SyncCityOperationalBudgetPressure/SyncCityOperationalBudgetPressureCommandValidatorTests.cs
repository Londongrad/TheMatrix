using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityOperationalBudgetPressure;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityOperationalBudgetPressure;

public sealed class SyncCityOperationalBudgetPressureCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var validator = new SyncCityOperationalBudgetPressureCommandValidator();

        var result = validator.Validate(CreateCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithInvalidInputs_ReturnsErrors()
    {
        var validator = new SyncCityOperationalBudgetPressureCommandValidator();

        var result = validator.Validate(new SyncCityOperationalBudgetPressureCommand(
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
            EffectiveAtUtc: new DateTimeOffset(2052, 3, 4, 8, 0, 0, TimeSpan.FromHours(3))));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CityId");
        Assert.Contains(result.Errors, x => x.PropertyName == "PressureIndex");
        Assert.Contains(result.Errors, x => x.PropertyName == "EffectiveTickId");
        Assert.Contains(result.Errors, x => x.PropertyName == "EffectiveAtUtc");
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
