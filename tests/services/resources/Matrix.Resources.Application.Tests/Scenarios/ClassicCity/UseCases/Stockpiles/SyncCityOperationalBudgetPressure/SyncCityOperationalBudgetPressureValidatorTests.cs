using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCityOperationalBudgetPressure;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCityOperationalBudgetPressure;

public sealed class SyncCityOperationalBudgetPressureValidatorTests
{
    [Fact]
    public void Validator_RejectsInvalidIdentifiersAndSnapshotValues()
    {
        var validator = new SyncCityOperationalBudgetPressureCommandValidator();

        var result = validator.Validate(new SyncCityOperationalBudgetPressureCommand(
            CityId: Guid.Empty,
            Balance: 1_000m,
            MunicipalOperationsExpenses: 200m,
            GeneralAvailableAmount: 900m,
            OperationsAvailableAmount: 800m,
            InfrastructureAvailableAmount: 700m,
            HealthcareAvailableAmount: 600m,
            GeneralAuthorizationLevel: "High",
            OperationsAuthorizationLevel: "Medium",
            InfrastructureAuthorizationLevel: "Medium",
            HealthcareAuthorizationLevel: "Low",
            PressureIndex: 1.2m,
            EffectiveTickId: -1,
            EffectiveAtUtc: new DateTimeOffset(2049, 1, 1, 18, 0, 0, TimeSpan.FromHours(9))));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 4);
    }
}
