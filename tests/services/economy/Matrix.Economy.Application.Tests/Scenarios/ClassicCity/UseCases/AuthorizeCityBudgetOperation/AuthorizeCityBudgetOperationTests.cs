using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.AuthorizeCityBudgetOperation;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.AuthorizeCityBudgetOperation
{
    public sealed class AuthorizeCityBudgetOperationTests
    {
        [Fact]
        public async Task Handle_ForwardsPressureToAuthorizationPolicy()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var projectionService = new FakeCityOperationalBudgetPressureProjectionService();
            var handler = new AuthorizeCityBudgetOperationCommandHandler(projectionService);

            CityBudgetOperationAuthorizationDto result = await handler.Handle(
                request: new AuthorizeCityBudgetOperationCommand(
                    CityId: cityId,
                    Category: CityBudgetCategory.Infrastructure,
                    OperationKind: "RoadRepair",
                    RequestedIntensity: "High",
                    EstimatedAmount: 120m,
                    EmergencyOverrideRequested: false),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: cityId,
                actual: projectionService.RequestedCityId);
            Assert.Equal(
                expected: cityId,
                actual: result.CityId);
            Assert.Equal(
                expected: "Infrastructure",
                actual: result.Category);
            Assert.Equal(
                expected: "RoadRepair",
                actual: result.OperationKind);
        }

        [Fact]
        public void Authorize_WhenAmountExceedsBudgetEnvelope_ReturnsDenied()
        {
            CityBudgetOperationAuthorizationDto result = CityBudgetOperationAuthorizationPolicy.Authorize(
                cityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                category: CityBudgetCategory.General,
                operationKind: "EmergencyProcurement",
                requestedIntensity: "High",
                estimatedAmount: 200m,
                emergencyOverrideRequested: false,
                pressure: CreatePressure(
                    generalLevel: "Medium",
                    generalAvailable: 100m,
                    pressureIndex: 0.65m,
                    balance: 800m));

            Assert.Equal(
                expected: "Denied",
                actual: result.Status);
            Assert.Null(result.ApprovedIntensity);
            Assert.Equal(
                expected: 100m,
                actual: result.AvailableAmount);
        }

        [Fact]
        public void Authorize_WhenAmountRequiresCeilingReduction_ReturnsApprovedReduced()
        {
            CityBudgetOperationAuthorizationDto result = CityBudgetOperationAuthorizationPolicy.Authorize(
                cityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                category: CityBudgetCategory.General,
                operationKind: "Maintenance",
                requestedIntensity: "High",
                estimatedAmount: 80m,
                emergencyOverrideRequested: false,
                pressure: CreatePressure(
                    generalLevel: "High",
                    generalAvailable: 100m,
                    pressureIndex: 0.30m,
                    balance: 1200m));

            Assert.Equal(
                expected: "ApprovedReduced",
                actual: result.Status);
            Assert.Equal(
                expected: "Medium",
                actual: result.ApprovedIntensity);
        }

        [Fact]
        public void Authorize_WhenEmergencyOverrideIsAllowed_UpgradesApproval()
        {
            CityBudgetOperationAuthorizationDto result = CityBudgetOperationAuthorizationPolicy.Authorize(
                cityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                category: CityBudgetCategory.General,
                operationKind: "EmergencyShelter",
                requestedIntensity: "High",
                estimatedAmount: 40m,
                emergencyOverrideRequested: true,
                pressure: CreatePressure(
                    generalLevel: "Low",
                    generalAvailable: 100m,
                    pressureIndex: 0.40m,
                    balance: 900m));

            Assert.Equal(
                expected: "ApprovedByEmergencyOverride",
                actual: result.Status);
            Assert.True(result.AuthorizedByEmergencyOverride);
            Assert.Equal(
                expected: "Medium",
                actual: result.ApprovedIntensity);
        }

        private static CityOperationalBudgetPressureDto CreatePressure(
            string generalLevel,
            decimal generalAvailable,
            decimal pressureIndex,
            decimal balance)
        {
            return new CityOperationalBudgetPressureDto(
                CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                EffectiveTickId: 42,
                EffectiveAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 11,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                UnitKind: "Currency",
                UnitCode: "MNY",
                UnitDisplayName: "Money",
                UnitSymbol: "В¤",
                Balance: balance,
                TotalCityExpenses: 420m,
                MunicipalOperationsExpenses: 120m,
                InfrastructureOperationsExpenses: 80m,
                EmergencyOperationsExpenses: 30m,
                GeneralAvailableAmount: generalAvailable,
                OperationsAvailableAmount: 100m,
                InfrastructureAvailableAmount: 100m,
                HealthcareAvailableAmount: 100m,
                GeneralAuthorizationLevel: generalLevel,
                OperationsAuthorizationLevel: "High",
                InfrastructureAuthorizationLevel: "High",
                HealthcareAuthorizationLevel: "High",
                LastMunicipalExpenseAtUtc: "2048-05-06T10:30:00Z",
                PressureIndex: pressureIndex);
        }
    }
}
