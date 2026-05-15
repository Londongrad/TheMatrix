using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Api.Controllers;
using Matrix.Economy.Application.UseCases.Businesses;
using Matrix.Economy.Application.UseCases.Businesses.GetCityBusinesses;
using Matrix.Economy.Application.UseCases.Businesses.GetCityBusinessLedgerFeed;
using Matrix.Economy.Application.UseCases.Businesses.RecordCityBusinessExpense;
using Matrix.Economy.Application.UseCases.Businesses.RecordCityBusinessPayroll;
using Matrix.Economy.Application.UseCases.Businesses.RecordCityBusinessRetailSale;
using Matrix.Economy.Application.UseCases.Businesses.RegisterCityBusiness;
using Matrix.Economy.Application.UseCases.Businesses.RemitCityBusinessTax;
using Matrix.Economy.Application.UseCases.Businesses.RunCityBusinessTaxCycle;
using Matrix.Economy.Contracts.Business.Requests;
using Matrix.Economy.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Economy.Api.Tests.TestSupport.EconomyApiTestSupport;

namespace Matrix.Economy.Api.Tests.Controllers;

public sealed class BusinessControllerTests
{
    [Fact]
    public async Task BusinessQueriesAndRegistration_ReturnPayloadsAndForwardCommands()
    {
        Guid cityId = Guid.Parse("b0e5a970-bb04-40f8-af70-56fe8fc6722d");
        Guid businessId = Guid.Parse("b4c6c4b3-a7a0-4329-a58f-2a8c4f3d2dd6");
        var sender = new FakeSender();
        sender.Handle<GetCityBusinessesQuery, IReadOnlyList<CityBusinessDto>>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            return [CreateBusinessDto(cityId)];
        });
        sender.Handle<RegisterCityBusinessCommand, CityBusinessDto>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.Equal("North Works", command.Name);
            Assert.Equal(CityBusinessKind.Manufacturer, command.Kind);
            return CreateBusinessDto(cityId);
        });
        sender.Handle<GetCityBusinessLedgerFeedQuery, CursorPagedResult<CityBusinessLedgerEntryDto>>(query =>
        {
            Assert.Equal(businessId, query.BusinessId);
            Assert.Equal("cur-1", query.Cursor);
            Assert.Equal(25, query.PageSize);
            return CreateBusinessLedgerFeed();
        });
        var controller = new BusinessController(sender);

        IActionResult list = await controller.ListCityBusinesses(cityId, CancellationToken.None);
        IActionResult register = await controller.RegisterBusiness(
            cityId,
            new RegisterCityBusinessRequest
            {
                Name = "North Works",
                Kind = "Manufacturer",
                StartingCapital = 2500m,
                UnitKind = "Currency",
                UnitCode = "CRD",
                UnitDisplayName = "Credits",
                UnitSymbol = "C"
            },
            CancellationToken.None);
        IActionResult feed = await controller.GetBusinessLedgerFeed(businessId, "cur-1", 25, CancellationToken.None);

        IReadOnlyList<CityBusinessDto> businesses =
            Assert.IsAssignableFrom<IReadOnlyList<CityBusinessDto>>(Assert.IsType<OkObjectResult>(list).Value);
        CityBusinessDto business = Assert.IsType<CityBusinessDto>(Assert.IsType<OkObjectResult>(register).Value);
        CursorPagedResult<CityBusinessLedgerEntryDto> ledger =
            Assert.IsType<CursorPagedResult<CityBusinessLedgerEntryDto>>(Assert.IsType<OkObjectResult>(feed).Value);

        Assert.Single(businesses);
        Assert.Equal("North Works", business.Name);
        Assert.True(ledger.HasNext);
    }

    [Fact]
    public async Task RegisterBusiness_WithUnsupportedKind_ReturnsBadRequest()
    {
        var controller = new BusinessController(new FakeSender());

        IActionResult result = await controller.RegisterBusiness(
            Guid.NewGuid(),
            new RegisterCityBusinessRequest
            {
                Name = "North Works",
                Kind = "UnsupportedKind",
                StartingCapital = 2500m
            },
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<BadRequestObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task BusinessOperations_ReturnPayloadsAndGuardInvalidBudgetCategory()
    {
        Guid cityId = Guid.Parse("b0e5a970-bb04-40f8-af70-56fe8fc6722d");
        Guid businessId = Guid.Parse("b4c6c4b3-a7a0-4329-a58f-2a8c4f3d2dd6");
        Guid householdAccountId = Guid.Parse("ed4c0dfd-1a16-44d0-aa53-6477b959a974");
        var sender = new FakeSender();
        sender.Handle<RecordCityBusinessRetailSaleCommand, CityBusinessLedgerEntryDto>(command =>
        {
            Assert.Equal(businessId, command.BusinessId);
            Assert.Equal(210m, command.GrossAmount);
            return CreateBusinessLedgerEntryDto("Retail sale");
        });
        sender.Handle<RecordCityBusinessExpenseCommand, CityBusinessLedgerEntryDto>(command =>
        {
            Assert.Equal(businessId, command.BusinessId);
            Assert.Equal(90m, command.Amount);
            return CreateBusinessLedgerEntryDto("Expense");
        });
        sender.Handle<RecordCityBusinessPayrollCommand, CityBusinessLedgerEntryDto>(command =>
        {
            Assert.Equal(householdAccountId, command.HouseholdAccountId);
            Assert.Equal(180m, command.GrossAmount);
            return CreateBusinessLedgerEntryDto("Payroll");
        });
        sender.Handle<RemitCityBusinessTaxCommand, CityBusinessLedgerEntryDto>(command =>
        {
            Assert.Equal(CityBudgetCategory.Taxation, command.BudgetCategory);
            return CreateBusinessLedgerEntryDto("Tax remittance");
        });
        sender.Handle<RunCityBusinessTaxCycleCommand, RunCityBusinessTaxCycleResultDto>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.Equal(CityBudgetCategory.Taxation, command.BudgetCategory);
            return CreateTaxCycleResultDto(cityId);
        });
        var controller = new BusinessOperationsController(sender);

        IActionResult retailSale = await controller.RecordRetailSale(
            businessId,
            new RecordBusinessRetailSaleRequest
            {
                GrossAmount = 210m,
                SalesTaxAmount = 12.5m,
                Title = "Retail sale",
                Description = "desc"
            },
            CancellationToken.None);
        IActionResult expense = await controller.RecordExpense(
            businessId,
            new RecordBusinessExpenseRequest
            {
                Amount = 90m,
                Title = "Expense",
                Description = "desc"
            },
            CancellationToken.None);
        IActionResult payroll = await controller.RecordPayroll(
            businessId,
            new RecordBusinessPayrollRequest
            {
                HouseholdAccountId = householdAccountId,
                GrossAmount = 180m,
                IncomeTaxAmount = 18m,
                Title = "Payroll",
                Description = "desc"
            },
            CancellationToken.None);
        IActionResult remit = await controller.RemitTax(
            businessId,
            new RemitBusinessTaxRequest
            {
                Amount = 40m,
                BudgetCategory = "Taxation",
                Title = "Tax remittance",
                Description = "desc"
            },
            CancellationToken.None);
        IActionResult runTaxCycle = await controller.RunTaxCycle(
            cityId,
            new RunCityBusinessTaxCycleRequest
            {
                BudgetCategory = "Taxation"
            },
            CancellationToken.None);
        IActionResult invalidRemit = await controller.RemitTax(
            businessId,
            new RemitBusinessTaxRequest
            {
                Amount = 40m,
                BudgetCategory = "Unsupported",
                Title = "Tax remittance"
            },
            CancellationToken.None);
        IActionResult invalidRunTaxCycle = await controller.RunTaxCycle(
            cityId,
            new RunCityBusinessTaxCycleRequest
            {
                BudgetCategory = "Unsupported"
            },
            CancellationToken.None);

        Assert.Equal("Retail sale", Assert.IsType<CityBusinessLedgerEntryDto>(Assert.IsType<OkObjectResult>(retailSale).Value).Title);
        Assert.Equal("Expense", Assert.IsType<CityBusinessLedgerEntryDto>(Assert.IsType<OkObjectResult>(expense).Value).Title);
        Assert.Equal("Payroll", Assert.IsType<CityBusinessLedgerEntryDto>(Assert.IsType<OkObjectResult>(payroll).Value).Title);
        Assert.Equal("Tax remittance", Assert.IsType<CityBusinessLedgerEntryDto>(Assert.IsType<OkObjectResult>(remit).Value).Title);
        Assert.Equal(4, Assert.IsType<RunCityBusinessTaxCycleResultDto>(Assert.IsType<OkObjectResult>(runTaxCycle).Value).RemittedBusinesses);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<BadRequestObjectResult>(invalidRemit).StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<BadRequestObjectResult>(invalidRunTaxCycle).StatusCode);
    }
}
