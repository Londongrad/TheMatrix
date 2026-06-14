using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Api.Controllers;
using Matrix.Economy.Application.UseCases.Businesses;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.GetCityBusinesses;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.GetCityBusinessLedgerFeed;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RecordCityBusinessExpense;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RecordCityBusinessPayroll;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RecordCityBusinessRetailSale;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RegisterCityBusiness;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RemitCityBusinessTax;
using Matrix.Economy.Application.UseCases.Businesses.RunCityBusinessTaxCycle;
using Matrix.Economy.Contracts.Business.Requests;
using Matrix.Economy.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Economy.Api.Tests.TestSupport.EconomyApiTestSupport;

namespace Matrix.Economy.Api.Tests.Controllers
{
    public sealed class BusinessControllerTests
    {
        [Fact]
        public async Task BusinessQueriesAndRegistration_ReturnPayloadsAndForwardCommands()
        {
            var cityId = Guid.Parse("b0e5a970-bb04-40f8-af70-56fe8fc6722d");
            var businessId = Guid.Parse("b4c6c4b3-a7a0-4329-a58f-2a8c4f3d2dd6");
            var sender = new FakeSender();
            sender.Handle<GetCityBusinessesQuery, IReadOnlyList<CityBusinessDto>>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                return [CreateBusinessDto(cityId)];
            });
            sender.Handle<RegisterCityBusinessCommand, CityBusinessDto>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: "North Works",
                    actual: command.Name);
                Assert.Equal(
                    expected: CityBusinessKind.Manufacturer,
                    actual: command.Kind);
                return CreateBusinessDto(cityId);
            });
            sender.Handle<GetCityBusinessLedgerFeedQuery, CursorPagedResult<CityBusinessLedgerEntryDto>>(query =>
            {
                Assert.Equal(
                    expected: businessId,
                    actual: query.BusinessId);
                Assert.Equal(
                    expected: "cur-1",
                    actual: query.Cursor);
                Assert.Equal(
                    expected: 25,
                    actual: query.PageSize);
                return CreateBusinessLedgerFeed();
            });
            var controller = new BusinessController(sender);

            IActionResult list = await controller.ListCityBusinesses(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IActionResult register = await controller.RegisterBusiness(
                cityId: cityId,
                request: new RegisterCityBusinessRequest
                {
                    Name = "North Works",
                    Kind = "Manufacturer",
                    StartingCapital = 2500m,
                    UnitKind = "Currency",
                    UnitCode = "CRD",
                    UnitDisplayName = "Credits",
                    UnitSymbol = "C"
                },
                cancellationToken: CancellationToken.None);
            IActionResult feed = await controller.GetBusinessLedgerFeed(
                businessId: businessId,
                cursor: "cur-1",
                pageSize: 25,
                cancellationToken: CancellationToken.None);

            IReadOnlyList<CityBusinessDto> businesses =
                Assert.IsAssignableFrom<IReadOnlyList<CityBusinessDto>>(
                    Assert.IsType<OkObjectResult>(list)
                       .Value);
            CityBusinessDto business = Assert.IsType<CityBusinessDto>(
                Assert.IsType<OkObjectResult>(register)
                   .Value);
            CursorPagedResult<CityBusinessLedgerEntryDto> ledger =
                Assert.IsType<CursorPagedResult<CityBusinessLedgerEntryDto>>(
                    Assert.IsType<OkObjectResult>(feed)
                       .Value);

            Assert.Single(businesses);
            Assert.Equal(
                expected: "North Works",
                actual: business.Name);
            Assert.True(ledger.HasNext);
        }

        [Fact]
        public async Task RegisterBusiness_WithUnsupportedKind_ReturnsBadRequest()
        {
            var controller = new BusinessController(new FakeSender());

            IActionResult result = await controller.RegisterBusiness(
                cityId: Guid.NewGuid(),
                request: new RegisterCityBusinessRequest
                {
                    Name = "North Works",
                    Kind = "UnsupportedKind",
                    StartingCapital = 2500m
                },
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: StatusCodes.Status400BadRequest,
                actual: Assert.IsType<BadRequestObjectResult>(result)
                   .StatusCode);
        }

        [Fact]
        public async Task BusinessOperations_ReturnPayloadsAndGuardInvalidBudgetCategory()
        {
            var cityId = Guid.Parse("b0e5a970-bb04-40f8-af70-56fe8fc6722d");
            var businessId = Guid.Parse("b4c6c4b3-a7a0-4329-a58f-2a8c4f3d2dd6");
            var householdAccountId = Guid.Parse("ed4c0dfd-1a16-44d0-aa53-6477b959a974");
            var sender = new FakeSender();
            sender.Handle<RecordCityBusinessRetailSaleCommand, CityBusinessLedgerEntryDto>(command =>
            {
                Assert.Equal(
                    expected: businessId,
                    actual: command.BusinessId);
                Assert.Equal(
                    expected: 210m,
                    actual: command.GrossAmount);
                return CreateBusinessLedgerEntryDto();
            });
            sender.Handle<RecordCityBusinessExpenseCommand, CityBusinessLedgerEntryDto>(command =>
            {
                Assert.Equal(
                    expected: businessId,
                    actual: command.BusinessId);
                Assert.Equal(
                    expected: 90m,
                    actual: command.Amount);
                return CreateBusinessLedgerEntryDto("Expense");
            });
            sender.Handle<RecordCityBusinessPayrollCommand, CityBusinessLedgerEntryDto>(command =>
            {
                Assert.Equal(
                    expected: householdAccountId,
                    actual: command.HouseholdAccountId);
                Assert.Equal(
                    expected: 180m,
                    actual: command.GrossAmount);
                return CreateBusinessLedgerEntryDto("Payroll");
            });
            sender.Handle<RemitCityBusinessTaxCommand, CityBusinessLedgerEntryDto>(command =>
            {
                Assert.Equal(
                    expected: CityBudgetCategory.Taxation,
                    actual: command.BudgetCategory);
                return CreateBusinessLedgerEntryDto("Tax remittance");
            });
            sender.Handle<RunCityBusinessTaxCycleCommand, RunCityBusinessTaxCycleResultDto>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: CityBudgetCategory.Taxation,
                    actual: command.BudgetCategory);
                return CreateTaxCycleResultDto(cityId);
            });
            var controller = new BusinessOperationsController(sender);

            IActionResult retailSale = await controller.RecordRetailSale(
                businessId: businessId,
                request: new RecordBusinessRetailSaleRequest
                {
                    GrossAmount = 210m,
                    SalesTaxAmount = 12.5m,
                    Title = "Retail sale",
                    Description = "desc"
                },
                cancellationToken: CancellationToken.None);
            IActionResult expense = await controller.RecordExpense(
                businessId: businessId,
                request: new RecordBusinessExpenseRequest
                {
                    Amount = 90m,
                    Title = "Expense",
                    Description = "desc"
                },
                cancellationToken: CancellationToken.None);
            IActionResult payroll = await controller.RecordPayroll(
                businessId: businessId,
                request: new RecordBusinessPayrollRequest
                {
                    HouseholdAccountId = householdAccountId,
                    GrossAmount = 180m,
                    IncomeTaxAmount = 18m,
                    Title = "Payroll",
                    Description = "desc"
                },
                cancellationToken: CancellationToken.None);
            IActionResult remit = await controller.RemitTax(
                businessId: businessId,
                request: new RemitBusinessTaxRequest
                {
                    Amount = 40m,
                    BudgetCategory = "Taxation",
                    Title = "Tax remittance",
                    Description = "desc"
                },
                cancellationToken: CancellationToken.None);
            IActionResult runTaxCycle = await controller.RunTaxCycle(
                cityId: cityId,
                request: new RunCityBusinessTaxCycleRequest
                {
                    BudgetCategory = "Taxation"
                },
                cancellationToken: CancellationToken.None);
            IActionResult invalidRemit = await controller.RemitTax(
                businessId: businessId,
                request: new RemitBusinessTaxRequest
                {
                    Amount = 40m,
                    BudgetCategory = "Unsupported",
                    Title = "Tax remittance"
                },
                cancellationToken: CancellationToken.None);
            IActionResult invalidRunTaxCycle = await controller.RunTaxCycle(
                cityId: cityId,
                request: new RunCityBusinessTaxCycleRequest
                {
                    BudgetCategory = "Unsupported"
                },
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "Retail sale",
                actual: Assert.IsType<CityBusinessLedgerEntryDto>(
                        Assert.IsType<OkObjectResult>(retailSale)
                           .Value)
                   .Title);
            Assert.Equal(
                expected: "Expense",
                actual: Assert.IsType<CityBusinessLedgerEntryDto>(
                        Assert.IsType<OkObjectResult>(expense)
                           .Value)
                   .Title);
            Assert.Equal(
                expected: "Payroll",
                actual: Assert.IsType<CityBusinessLedgerEntryDto>(
                        Assert.IsType<OkObjectResult>(payroll)
                           .Value)
                   .Title);
            Assert.Equal(
                expected: "Tax remittance",
                actual: Assert.IsType<CityBusinessLedgerEntryDto>(
                        Assert.IsType<OkObjectResult>(remit)
                           .Value)
                   .Title);
            Assert.Equal(
                expected: 4,
                actual: Assert.IsType<RunCityBusinessTaxCycleResultDto>(
                        Assert.IsType<OkObjectResult>(runTaxCycle)
                           .Value)
                   .RemittedBusinesses);
            Assert.Equal(
                expected: StatusCodes.Status400BadRequest,
                actual: Assert.IsType<BadRequestObjectResult>(invalidRemit)
                   .StatusCode);
            Assert.Equal(
                expected: StatusCodes.Status400BadRequest,
                actual: Assert.IsType<BadRequestObjectResult>(invalidRunTaxCycle)
                   .StatusCode);
        }
    }
}
