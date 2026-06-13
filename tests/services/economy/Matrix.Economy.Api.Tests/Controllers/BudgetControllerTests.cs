using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Api.Controllers;
using Matrix.Economy.Application.UseCases.AuthorizeCityBudgetOperation;
using Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.GetCityBudgetAllocations;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.SetCityBudgetAllocation;
using Matrix.Economy.Application.UseCases.BudgetLedger;
using Matrix.Economy.Application.UseCases.BudgetLedger.GetCityBudgetLedgerFeed;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.DisburseCityBudgetToBusiness;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.RecordCityBudgetExpense;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.RecordCityBudgetRevenue;
using Matrix.Economy.Application.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle;
using Matrix.Economy.Application.UseCases.GetBudgetSummary;
using Matrix.Economy.Application.UseCases.GetCityBudgetSummary;
using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Contracts.Budget.Requests;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.Economy.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Economy.Api.Tests.TestSupport.EconomyApiTestSupport;

namespace Matrix.Economy.Api.Tests.Controllers
{
    public sealed class BudgetControllerTests
    {
        [Fact]
        public async Task SummaryAndPressureEndpoints_MapViews()
        {
            var cityId = Guid.Parse("b0e5a970-bb04-40f8-af70-56fe8fc6722d");
            var sender = new FakeSender();
            sender.Handle<GetBudgetSummaryQuery, BudgetSummaryDto>(_ => CreateBudgetSummaryDto());
            sender.Handle<GetCityBudgetSummaryQuery, BudgetSummaryDto>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                return CreateBudgetSummaryDto();
            });
            sender.Handle<GetCityOperationalBudgetPressureQuery, CityOperationalBudgetPressureDto>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                return CreateOperationalPressureDto(cityId);
            });
            var controller = new BudgetController(sender);

            IActionResult summary = await controller.GetSummary(CancellationToken.None);
            IActionResult citySummary = await controller.GetCitySummary(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IActionResult pressure = await controller.GetCityOperationalPressure(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            EconomySummaryView summaryView = Assert.IsType<EconomySummaryView>(
                Assert.IsType<OkObjectResult>(summary)
                   .Value);
            EconomySummaryView citySummaryView = Assert.IsType<EconomySummaryView>(
                Assert.IsType<OkObjectResult>(citySummary)
                   .Value);
            CityOperationalBudgetPressureView pressureView = Assert.IsType<CityOperationalBudgetPressureView>(
                Assert.IsType<OkObjectResult>(pressure)
                   .Value);

            Assert.Equal(
                expected: "CRD",
                actual: summaryView.UnitCode);
            Assert.Equal(
                expected: 10250.55m,
                actual: citySummaryView.Balance);
            Assert.Equal(
                expected: cityId,
                actual: pressureView.CityId);
            Assert.Equal(
                expected: "BudgetSettlement",
                actual: pressureView.EffectivePhase);
        }

        [Fact]
        public async Task AuthorizationBootstrapAndMutationEndpoints_ForwardCommandsAndReturnPayloads()
        {
            var cityId = Guid.Parse("b0e5a970-bb04-40f8-af70-56fe8fc6722d");
            var businessId = Guid.Parse("b4c6c4b3-a7a0-4329-a58f-2a8c4f3d2dd6");
            var sender = new FakeSender();
            sender.Handle<AuthorizeCityBudgetOperationCommand, CityBudgetOperationAuthorizationDto>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: CityBudgetCategory.Operations,
                    actual: command.Category);
                Assert.Equal(
                    expected: "DrainageMaintenance",
                    actual: command.OperationKind);
                return CreateBudgetOperationAuthorizationDto(cityId);
            });
            sender.Handle<InitializeCityEconomyCommand, CityEconomyBootstrapResultDto>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: "classic-city",
                    actual: command.ScenarioKey);
                return CreateBootstrapResultDto(cityId);
            });
            sender.Handle<GetCityBudgetLedgerFeedQuery, CursorPagedResult<BudgetLedgerEntryDto>>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                Assert.Equal(
                    expected: "cursor-1",
                    actual: query.Cursor);
                Assert.Equal(
                    expected: 25,
                    actual: query.PageSize);
                return CreateBudgetLedgerFeed();
            });
            sender.Handle<GetCityBudgetAllocationsQuery, IReadOnlyList<CityBudgetAllocationDto>>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                return [CreateBudgetAllocationDto(cityId)];
            });
            sender.Handle<RecordCityBudgetRevenueCommand, BudgetLedgerEntryDto>(command =>
            {
                Assert.Equal(
                    expected: CityBudgetCategory.Taxation,
                    actual: command.Category);
                Assert.Equal(
                    expected: 125.50m,
                    actual: command.Amount);
                return CreateBudgetLedgerEntryDto("Revenue");
            });
            sender.Handle<RecordCityBudgetExpenseCommand, BudgetLedgerEntryDto>(command =>
            {
                Assert.Equal(
                    expected: CityBudgetCategory.Operations,
                    actual: command.Category);
                Assert.Equal(
                    expected: 90.00m,
                    actual: command.Amount);
                return CreateBudgetLedgerEntryDto("Expense");
            });
            sender.Handle<DisburseCityBudgetToBusinessCommand, BudgetLedgerEntryDto>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: businessId,
                    actual: command.BusinessId);
                Assert.Equal(
                    expected: CityBudgetCategory.Infrastructure,
                    actual: command.Category);
                return CreateBudgetLedgerEntryDto("Disbursement");
            });
            sender.Handle<SetCityBudgetAllocationCommand, CityBudgetAllocationDto>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: CityBudgetCategory.Healthcare,
                    actual: command.Category);
                Assert.Equal(
                    expected: 2400.00m,
                    actual: command.TargetAmount);
                return CreateBudgetAllocationDto(
                    cityId: cityId,
                    category: "Healthcare");
            });
            sender.Handle<RunCityMunicipalOperatingCycleCommand, RunCityMunicipalOperatingCycleResultDto>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                return CreateOperatingCycleResultDto(cityId);
            });
            var controller = new BudgetController(sender);

            IActionResult authorization = await controller.AuthorizeBudgetOperation(
                cityId: cityId,
                request: new AuthorizeBudgetOperationRequest(
                    Category: "Operations",
                    OperationKind: "DrainageMaintenance",
                    RequestedIntensity: "Elevated",
                    EstimatedAmount: 180.00m),
                cancellationToken: CancellationToken.None);
            IActionResult bootstrap = await controller.InitializeCityEconomy(
                cityId: cityId,
                request: new InitializeCityEconomyRequest(
                    ScenarioKey: "classic-city",
                    EconomyProfile: "Balanced",
                    CreatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 6,
                        day: 1,
                        hour: 9,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                cancellationToken: CancellationToken.None);
            IActionResult ledgerFeed = await controller.GetCityLedgerFeed(
                cityId: cityId,
                cursor: "cursor-1",
                pageSize: 25,
                cancellationToken: CancellationToken.None);
            IActionResult allocations = await controller.GetCityAllocations(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IActionResult revenue = await controller.RecordRevenue(
                cityId: cityId,
                request: new RecordBudgetEntryRequest(
                    Category: "Taxation",
                    Amount: 125.50m,
                    Title: "Revenue",
                    Description: "desc",
                    UnitKind: "Currency",
                    UnitCode: "CRD",
                    UnitDisplayName: "Credits",
                    UnitSymbol: "C"),
                cancellationToken: CancellationToken.None);
            IActionResult expense = await controller.RecordExpense(
                cityId: cityId,
                request: new RecordBudgetEntryRequest(
                    Category: "Operations",
                    Amount: 90.00m,
                    Title: "Expense",
                    Description: "desc",
                    UnitKind: "Currency",
                    UnitCode: "CRD",
                    UnitDisplayName: "Credits",
                    UnitSymbol: "C"),
                cancellationToken: CancellationToken.None);
            IActionResult disbursement = await controller.DisburseToBusiness(
                cityId: cityId,
                request: new DisburseBudgetToBusinessRequest(
                    BusinessId: businessId,
                    Category: "Infrastructure",
                    Amount: 250.00m,
                    Title: "Disburse",
                    Description: "desc"),
                cancellationToken: CancellationToken.None);
            IActionResult setAllocation = await controller.SetAllocation(
                cityId: cityId,
                category: "Healthcare",
                request: new SetBudgetAllocationRequest(
                    TargetAmount: 2400.00m,
                    UnitKind: "Currency",
                    UnitCode: "CRD",
                    UnitDisplayName: "Credits",
                    UnitSymbol: "C"),
                cancellationToken: CancellationToken.None);
            IActionResult operatingCycle = await controller.RunOperatingCycle(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            BudgetOperationAuthorizationView authorizationView =
                Assert.IsType<BudgetOperationAuthorizationView>(
                    Assert.IsType<OkObjectResult>(authorization)
                       .Value);
            CityEconomyBootstrapResultView bootstrapView =
                Assert.IsType<CityEconomyBootstrapResultView>(
                    Assert.IsType<OkObjectResult>(bootstrap)
                       .Value);
            CursorPagedResult<BudgetLedgerEntryDto> feed =
                Assert.IsType<CursorPagedResult<BudgetLedgerEntryDto>>(
                    Assert.IsType<OkObjectResult>(ledgerFeed)
                       .Value);
            IReadOnlyList<CityBudgetAllocationDto> allocationList =
                Assert.IsAssignableFrom<IReadOnlyList<CityBudgetAllocationDto>>(
                    Assert.IsType<OkObjectResult>(allocations)
                       .Value);
            BudgetLedgerEntryDto revenueEntry = Assert.IsType<BudgetLedgerEntryDto>(
                Assert.IsType<OkObjectResult>(revenue)
                   .Value);
            BudgetLedgerEntryDto expenseEntry = Assert.IsType<BudgetLedgerEntryDto>(
                Assert.IsType<OkObjectResult>(expense)
                   .Value);
            BudgetLedgerEntryDto disbursementEntry = Assert.IsType<BudgetLedgerEntryDto>(
                Assert.IsType<OkObjectResult>(disbursement)
                   .Value);
            CityBudgetAllocationDto allocation = Assert.IsType<CityBudgetAllocationDto>(
                Assert.IsType<OkObjectResult>(setAllocation)
                   .Value);
            RunCityMunicipalOperatingCycleResultDto cycleResult =
                Assert.IsType<RunCityMunicipalOperatingCycleResultDto>(
                    Assert.IsType<OkObjectResult>(operatingCycle)
                       .Value);

            Assert.Equal(
                expected: "Approved",
                actual: authorizationView.Status);
            Assert.True(bootstrapView.BudgetCreated);
            Assert.True(feed.HasNext);
            Assert.Single(allocationList);
            Assert.Equal(
                expected: "Revenue",
                actual: revenueEntry.Title);
            Assert.Equal(
                expected: "Expense",
                actual: expenseEntry.Title);
            Assert.Equal(
                expected: "Disbursement",
                actual: disbursementEntry.Title);
            Assert.Equal(
                expected: "Healthcare",
                actual: allocation.Category);
            Assert.Equal(
                expected: 5,
                actual: cycleResult.ProviderPayments);
        }

        [Fact]
        public async Task CategoryParsingGuards_ReturnBadRequest()
        {
            var cityId = Guid.Parse("b0e5a970-bb04-40f8-af70-56fe8fc6722d");
            var controller = new BudgetController(new FakeSender());

            IActionResult authorization = await controller.AuthorizeBudgetOperation(
                cityId: cityId,
                request: new AuthorizeBudgetOperationRequest(
                    Category: "InvalidCategory",
                    OperationKind: "DrainageMaintenance",
                    RequestedIntensity: "Elevated",
                    EstimatedAmount: 180.00m),
                cancellationToken: CancellationToken.None);
            IActionResult revenue = await controller.RecordRevenue(
                cityId: cityId,
                request: new RecordBudgetEntryRequest(
                    Category: "InvalidCategory",
                    Amount: 125.50m,
                    Title: "Revenue",
                    Description: null,
                    UnitKind: null,
                    UnitCode: null,
                    UnitDisplayName: null,
                    UnitSymbol: null),
                cancellationToken: CancellationToken.None);
            IActionResult expense = await controller.RecordExpense(
                cityId: cityId,
                request: new RecordBudgetEntryRequest(
                    Category: "InvalidCategory",
                    Amount: 90.00m,
                    Title: "Expense",
                    Description: null,
                    UnitKind: null,
                    UnitCode: null,
                    UnitDisplayName: null,
                    UnitSymbol: null),
                cancellationToken: CancellationToken.None);
            IActionResult disbursement = await controller.DisburseToBusiness(
                cityId: cityId,
                request: new DisburseBudgetToBusinessRequest(
                    BusinessId: Guid.NewGuid(),
                    Category: "InvalidCategory",
                    Amount: 250.00m,
                    Title: "Disburse",
                    Description: null),
                cancellationToken: CancellationToken.None);
            IActionResult allocation = await controller.SetAllocation(
                cityId: cityId,
                category: "InvalidCategory",
                request: new SetBudgetAllocationRequest(
                    TargetAmount: 100m,
                    UnitKind: null,
                    UnitCode: null,
                    UnitDisplayName: null,
                    UnitSymbol: null),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: StatusCodes.Status400BadRequest,
                actual: Assert.IsType<BadRequestObjectResult>(authorization)
                   .StatusCode);
            Assert.Equal(
                expected: StatusCodes.Status400BadRequest,
                actual: Assert.IsType<BadRequestObjectResult>(revenue)
                   .StatusCode);
            Assert.Equal(
                expected: StatusCodes.Status400BadRequest,
                actual: Assert.IsType<BadRequestObjectResult>(expense)
                   .StatusCode);
            Assert.Equal(
                expected: StatusCodes.Status400BadRequest,
                actual: Assert.IsType<BadRequestObjectResult>(disbursement)
                   .StatusCode);
            Assert.Equal(
                expected: StatusCodes.Status400BadRequest,
                actual: Assert.IsType<BadRequestObjectResult>(allocation)
                   .StatusCode);
        }
    }
}
