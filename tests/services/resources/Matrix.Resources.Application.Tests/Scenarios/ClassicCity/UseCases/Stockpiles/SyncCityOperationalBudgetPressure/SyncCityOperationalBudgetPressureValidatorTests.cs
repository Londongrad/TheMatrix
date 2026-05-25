using FluentValidation.Results;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCityOperationalBudgetPressure;
using Xunit;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCityOperationalBudgetPressure
{
    public sealed class SyncCityOperationalBudgetPressureValidatorTests
    {
        [Fact]
        public void Validator_RejectsInvalidIdentifiersAndSnapshotValues()
        {
            var validator = new SyncCityOperationalBudgetPressureCommandValidator();

            ValidationResult? result = validator.Validate(
                new SyncCityOperationalBudgetPressureCommand(
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
                    EffectiveAtUtc: new DateTimeOffset(
                        year: 2049,
                        month: 1,
                        day: 1,
                        hour: 18,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.FromHours(9))));

            Assert.False(result.IsValid);
            Assert.True(result.Errors.Count >= 4);
        }
    }
}
