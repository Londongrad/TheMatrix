using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Api.Controllers;
using Matrix.Economy.Application.UseCases.AuthorizeCityBudgetOperation;
using Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy;
using Matrix.Economy.Application.UseCases.BudgetAllocations;
using Matrix.Economy.Application.UseCases.BudgetAllocations.GetCityBudgetAllocations;
using Matrix.Economy.Application.UseCases.BudgetAllocations.SetCityBudgetAllocation;
using Matrix.Economy.Application.UseCases.BudgetLedger;
using Matrix.Economy.Application.UseCases.BudgetLedger.GetCityBudgetLedgerFeed;
using Matrix.Economy.Application.UseCases.BudgetOperations.DisburseCityBudgetToBusiness;
using Matrix.Economy.Application.UseCases.BudgetOperations.RecordCityBudgetExpense;
using Matrix.Economy.Application.UseCases.BudgetOperations.RecordCityBudgetRevenue;
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

namespace Matrix.Economy.Api.Tests.Controllers;

public sealed class BudgetControllerTests
{
    [Fact]
    public async Task SummaryAndPressureEndpoints_MapViews()
    {
        Guid cityId = Guid.Parse("b0e5a970-bb04-40f8-af70-56fe8fc6722d");
        var sender = new FakeSender();
        sender.Handle<GetBudgetSummaryQuery, BudgetSummaryDto>(_ => CreateBudgetSummaryDto());
        sender.Handle<GetCityBudgetSummaryQuery, BudgetSummaryDto>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            return CreateBudgetSummaryDto();
        });
        sender.Handle<GetCityOperationalBudgetPressureQuery, CityOperationalBudgetPressureDto>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            return CreateOperationalPressureDto(cityId);
        });
        var controller = new BudgetController(sender);

        IActionResult summary = await controller.GetSummary(CancellationToken.None);
        IActionResult citySummary = await controller.GetCitySummary(cityId, CancellationToken.None);
        IActionResult pressure = await controller.GetCityOperationalPressure(cityId, CancellationToken.None);

        EconomySummaryView summaryView = Assert.IsType<EconomySummaryView>(Assert.IsType<OkObjectResult>(summary).Value);
        EconomySummaryView citySummaryView = Assert.IsType<EconomySummaryView>(Assert.IsType<OkObjectResult>(citySummary).Value);
        CityOperationalBudgetPressureView pressureView = Assert.IsType<CityOperationalBudgetPressureView>(Assert.IsType<OkObjectResult>(pressure).Value);

        Assert.Equal("CRD", summaryView.UnitCode);
        Assert.Equal(10250.55m, citySummaryView.Balance);
        Assert.Equal(cityId, pressureView.CityId);
        Assert.Equal("BudgetSettlement", pressureView.EffectivePhase);
    }

    [Fact]
    public async Task AuthorizationBootstrapAndMutationEndpoints_ForwardCommandsAndReturnPayloads()
    {
        Guid cityId = Guid.Parse("b0e5a970-bb04-40f8-af70-56fe8fc6722d");
        Guid businessId = Guid.Parse("b4c6c4b3-a7a0-4329-a58f-2a8c4f3d2dd6");
        var sender = new FakeSender();
        sender.Handle<AuthorizeCityBudgetOperationCommand, CityBudgetOperationAuthorizationDto>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.Equal(CityBudgetCategory.Operations, command.Category);
            Assert.Equal("DrainageMaintenance", command.OperationKind);
            return CreateBudgetOperationAuthorizationDto(cityId);
        });
        sender.Handle<InitializeCityEconomyCommand, CityEconomyBootstrapResultDto>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.Equal("ClassicCity", command.SimulationKind);
            return CreateBootstrapResultDto(cityId);
        });
        sender.Handle<GetCityBudgetLedgerFeedQuery, CursorPagedResult<BudgetLedgerEntryDto>>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            Assert.Equal("cursor-1", query.Cursor);
            Assert.Equal(25, query.PageSize);
            return CreateBudgetLedgerFeed();
        });
        sender.Handle<GetCityBudgetAllocationsQuery, IReadOnlyList<CityBudgetAllocationDto>>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            return [CreateBudgetAllocationDto(cityId)];
        });
        sender.Handle<RecordCityBudgetRevenueCommand, BudgetLedgerEntryDto>(command =>
        {
            Assert.Equal(CityBudgetCategory.Taxation, command.Category);
            Assert.Equal(125.50m, command.Amount);
            return CreateBudgetLedgerEntryDto("Revenue");
        });
        sender.Handle<RecordCityBudgetExpenseCommand, BudgetLedgerEntryDto>(command =>
        {
            Assert.Equal(CityBudgetCategory.Operations, command.Category);
            Assert.Equal(90.00m, command.Amount);
            return CreateBudgetLedgerEntryDto("Expense");
        });
        sender.Handle<DisburseCityBudgetToBusinessCommand, BudgetLedgerEntryDto>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.Equal(businessId, command.BusinessId);
            Assert.Equal(CityBudgetCategory.Infrastructure, command.Category);
            return CreateBudgetLedgerEntryDto("Disbursement");
        });
        sender.Handle<SetCityBudgetAllocationCommand, CityBudgetAllocationDto>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.Equal(CityBudgetCategory.Healthcare, command.Category);
            Assert.Equal(2400.00m, command.TargetAmount);
            return CreateBudgetAllocationDto(cityId, "Healthcare");
        });
        sender.Handle<RunCityMunicipalOperatingCycleCommand, RunCityMunicipalOperatingCycleResultDto>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            return CreateOperatingCycleResultDto(cityId);
        });
        var controller = new BudgetController(sender);

        IActionResult authorization = await controller.AuthorizeBudgetOperation(
            cityId,
            new AuthorizeBudgetOperationRequest("Operations", "DrainageMaintenance", "Elevated", 180.00m),
            CancellationToken.None);
        IActionResult bootstrap = await controller.InitializeCityEconomy(
            cityId,
            new InitializeCityEconomyRequest("ClassicCity", "Balanced", new DateTimeOffset(2048, 6, 1, 9, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);
        IActionResult ledgerFeed = await controller.GetCityLedgerFeed(cityId, "cursor-1", 25, CancellationToken.None);
        IActionResult allocations = await controller.GetCityAllocations(cityId, CancellationToken.None);
        IActionResult revenue = await controller.RecordRevenue(
            cityId,
            new RecordBudgetEntryRequest("Taxation", 125.50m, "Revenue", "desc", "Currency", "CRD", "Credits", "C"),
            CancellationToken.None);
        IActionResult expense = await controller.RecordExpense(
            cityId,
            new RecordBudgetEntryRequest("Operations", 90.00m, "Expense", "desc", "Currency", "CRD", "Credits", "C"),
            CancellationToken.None);
        IActionResult disbursement = await controller.DisburseToBusiness(
            cityId,
            new DisburseBudgetToBusinessRequest(businessId, "Infrastructure", 250.00m, "Disburse", "desc"),
            CancellationToken.None);
        IActionResult setAllocation = await controller.SetAllocation(
            cityId,
            "Healthcare",
            new SetBudgetAllocationRequest(2400.00m, "Currency", "CRD", "Credits", "C"),
            CancellationToken.None);
        IActionResult operatingCycle = await controller.RunOperatingCycle(cityId, CancellationToken.None);

        BudgetOperationAuthorizationView authorizationView =
            Assert.IsType<BudgetOperationAuthorizationView>(Assert.IsType<OkObjectResult>(authorization).Value);
        CityEconomyBootstrapResultView bootstrapView =
            Assert.IsType<CityEconomyBootstrapResultView>(Assert.IsType<OkObjectResult>(bootstrap).Value);
        CursorPagedResult<BudgetLedgerEntryDto> feed =
            Assert.IsType<CursorPagedResult<BudgetLedgerEntryDto>>(Assert.IsType<OkObjectResult>(ledgerFeed).Value);
        IReadOnlyList<CityBudgetAllocationDto> allocationList =
            Assert.IsAssignableFrom<IReadOnlyList<CityBudgetAllocationDto>>(Assert.IsType<OkObjectResult>(allocations).Value);
        BudgetLedgerEntryDto revenueEntry = Assert.IsType<BudgetLedgerEntryDto>(Assert.IsType<OkObjectResult>(revenue).Value);
        BudgetLedgerEntryDto expenseEntry = Assert.IsType<BudgetLedgerEntryDto>(Assert.IsType<OkObjectResult>(expense).Value);
        BudgetLedgerEntryDto disbursementEntry = Assert.IsType<BudgetLedgerEntryDto>(Assert.IsType<OkObjectResult>(disbursement).Value);
        CityBudgetAllocationDto allocation = Assert.IsType<CityBudgetAllocationDto>(Assert.IsType<OkObjectResult>(setAllocation).Value);
        RunCityMunicipalOperatingCycleResultDto cycleResult =
            Assert.IsType<RunCityMunicipalOperatingCycleResultDto>(Assert.IsType<OkObjectResult>(operatingCycle).Value);

        Assert.Equal("Approved", authorizationView.Status);
        Assert.True(bootstrapView.BudgetCreated);
        Assert.True(feed.HasNext);
        Assert.Single(allocationList);
        Assert.Equal("Revenue", revenueEntry.Title);
        Assert.Equal("Expense", expenseEntry.Title);
        Assert.Equal("Disbursement", disbursementEntry.Title);
        Assert.Equal("Healthcare", allocation.Category);
        Assert.Equal(5, cycleResult.ProviderPayments);
    }

    [Fact]
    public async Task CategoryParsingGuards_ReturnBadRequest()
    {
        Guid cityId = Guid.Parse("b0e5a970-bb04-40f8-af70-56fe8fc6722d");
        var controller = new BudgetController(new FakeSender());

        IActionResult authorization = await controller.AuthorizeBudgetOperation(
            cityId,
            new AuthorizeBudgetOperationRequest("InvalidCategory", "DrainageMaintenance", "Elevated", 180.00m),
            CancellationToken.None);
        IActionResult revenue = await controller.RecordRevenue(
            cityId,
            new RecordBudgetEntryRequest("InvalidCategory", 125.50m, "Revenue", null, null, null, null, null),
            CancellationToken.None);
        IActionResult expense = await controller.RecordExpense(
            cityId,
            new RecordBudgetEntryRequest("InvalidCategory", 90.00m, "Expense", null, null, null, null, null),
            CancellationToken.None);
        IActionResult disbursement = await controller.DisburseToBusiness(
            cityId,
            new DisburseBudgetToBusinessRequest(Guid.NewGuid(), "InvalidCategory", 250.00m, "Disburse", null),
            CancellationToken.None);
        IActionResult allocation = await controller.SetAllocation(
            cityId,
            "InvalidCategory",
            new SetBudgetAllocationRequest(100m, null, null, null, null),
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<BadRequestObjectResult>(authorization).StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<BadRequestObjectResult>(revenue).StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<BadRequestObjectResult>(expense).StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<BadRequestObjectResult>(disbursement).StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<BadRequestObjectResult>(allocation).StatusCode);
    }
}
