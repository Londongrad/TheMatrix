using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Api.Controllers;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.GetCityHouseholdAccountLedgerFeed;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.GetCityHouseholdAccounts;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.RecordCityHouseholdPurchase;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.RegisterCityHouseholdAccount;
using Matrix.Economy.Application.UseCases.HouseholdObligations;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.GetCityHouseholdObligations;
using Matrix.Economy.Application.UseCases.HouseholdObligations.GetHouseholdObligations;
using Matrix.Economy.Application.UseCases.HouseholdObligations.IssueHouseholdObligationCharge;
using Matrix.Economy.Application.UseCases.HouseholdObligations.RegisterCityHouseholdObligation;
using Matrix.Economy.Application.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle;
using Matrix.Economy.Contracts.HouseholdAccounts.Requests;
using Matrix.Economy.Contracts.HouseholdObligations.Requests;
using Matrix.Economy.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Economy.Api.Tests.TestSupport.EconomyApiTestSupport;

namespace Matrix.Economy.Api.Tests.Controllers
{
    public sealed class HouseholdControllersTests
    {
        [Fact]
        public async Task HouseholdAccountEndpoints_ReturnPayloadsAndForwardCommands()
        {
            var cityId = Guid.Parse("b0e5a970-bb04-40f8-af70-56fe8fc6722d");
            var householdAccountId = Guid.Parse("ed4c0dfd-1a16-44d0-aa53-6477b959a974");
            var businessId = Guid.Parse("b4c6c4b3-a7a0-4329-a58f-2a8c4f3d2dd6");
            var sender = new FakeSender();
            sender.Handle<GetCityHouseholdAccountsQuery, IReadOnlyList<CityHouseholdAccountDto>>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                return [CreateHouseholdAccountDto(cityId)];
            });
            sender.Handle<RegisterCityHouseholdAccountCommand, CityHouseholdAccountDto>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: "Anderson Household",
                    actual: command.Name);
                return CreateHouseholdAccountDto(cityId);
            });
            sender
               .Handle<GetCityHouseholdAccountLedgerFeedQuery,
                    CursorPagedResult<CityHouseholdAccountLedgerEntryDto>>(query =>
                {
                    Assert.Equal(
                        expected: householdAccountId,
                        actual: query.HouseholdAccountId);
                    Assert.Equal(
                        expected: "cursor-hh",
                        actual: query.Cursor);
                    Assert.Equal(
                        expected: 20,
                        actual: query.PageSize);
                    return CreateHouseholdAccountLedgerFeed();
                });
            sender.Handle<RecordCityHouseholdPurchaseCommand, CityHouseholdAccountLedgerEntryDto>(command =>
            {
                Assert.Equal(
                    expected: householdAccountId,
                    actual: command.HouseholdAccountId);
                Assert.Equal(
                    expected: businessId,
                    actual: command.BusinessId);
                return CreateHouseholdAccountLedgerEntryDto();
            });
            var accountsController = new HouseholdAccountsController(sender);
            var operationsController = new HouseholdAccountOperationsController(sender);

            IActionResult list = await accountsController.ListCityHouseholdAccounts(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IActionResult register = await accountsController.RegisterHouseholdAccount(
                cityId: cityId,
                request: new RegisterCityHouseholdAccountRequest
                {
                    Name = "Anderson Household",
                    ExternalReferenceCode = "HH-01",
                    OpeningBalance = 900m,
                    UnitKind = "Currency",
                    UnitCode = "CRD",
                    UnitDisplayName = "Credits",
                    UnitSymbol = "C"
                },
                cancellationToken: CancellationToken.None);
            IActionResult feed = await accountsController.GetHouseholdAccountLedgerFeed(
                householdAccountId: householdAccountId,
                cursor: "cursor-hh",
                pageSize: 20,
                cancellationToken: CancellationToken.None);
            IActionResult purchase = await operationsController.RecordPurchase(
                householdAccountId: householdAccountId,
                request: new RecordHouseholdPurchaseRequest
                {
                    BusinessId = businessId,
                    GrossAmount = 75m,
                    SalesTaxAmount = 4m,
                    Title = "Household purchase",
                    Description = "desc"
                },
                cancellationToken: CancellationToken.None);

            Assert.Single(
                Assert.IsAssignableFrom<IReadOnlyList<CityHouseholdAccountDto>>(
                    Assert.IsType<OkObjectResult>(list)
                       .Value));
            Assert.Equal(
                expected: "Anderson Household",
                actual: Assert.IsType<CityHouseholdAccountDto>(
                        Assert.IsType<OkObjectResult>(register)
                           .Value)
                   .Name);
            Assert.True(
                Assert.IsType<CursorPagedResult<CityHouseholdAccountLedgerEntryDto>>(
                        Assert.IsType<OkObjectResult>(feed)
                           .Value)
                   .HasNext);
            Assert.Equal(
                expected: "Household purchase",
                actual: Assert.IsType<CityHouseholdAccountLedgerEntryDto>(
                        Assert.IsType<OkObjectResult>(purchase)
                           .Value)
                   .Title);
        }

        [Fact]
        public async Task HouseholdObligationEndpoints_ReturnPayloadsAndForwardCommands()
        {
            var cityId = Guid.Parse("b0e5a970-bb04-40f8-af70-56fe8fc6722d");
            var householdAccountId = Guid.Parse("ed4c0dfd-1a16-44d0-aa53-6477b959a974");
            var obligationId = Guid.Parse("0d18f845-d6d4-41af-a4b8-c6dc2f9495dc");
            var providerBusinessId = Guid.Parse("b4c6c4b3-a7a0-4329-a58f-2a8c4f3d2dd6");
            var sender = new FakeSender();
            sender.Handle<GetCityHouseholdObligationsQuery, IReadOnlyList<CityHouseholdObligationDto>>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                return [CreateHouseholdObligationDto(cityId)];
            });
            sender.Handle<GetHouseholdObligationsQuery, IReadOnlyList<CityHouseholdObligationDto>>(query =>
            {
                Assert.Equal(
                    expected: householdAccountId,
                    actual: query.HouseholdAccountId);
                return [CreateHouseholdObligationDto(cityId)];
            });
            sender.Handle<RegisterCityHouseholdObligationCommand, CityHouseholdObligationDto>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: CityHouseholdObligationKind.Utilities,
                    actual: command.Kind);
                Assert.Equal(
                    expected: CityHouseholdObligationBillingCadence.Monthly,
                    actual: command.BillingCadence);
                return CreateHouseholdObligationDto(cityId);
            });
            sender.Handle<IssueHouseholdObligationChargeCommand, CityHouseholdAccountLedgerEntryDto>(command =>
            {
                Assert.Equal(
                    expected: obligationId,
                    actual: command.ObligationId);
                return CreateHouseholdAccountLedgerEntryDto("Charge");
            });
            sender.Handle<RunCityHouseholdBillingCycleCommand, RunCityHouseholdBillingCycleResultDto>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: new DateTimeOffset(
                        year: 2048,
                        month: 6,
                        day: 1,
                        hour: 10,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    actual: command.AsOfUtc);
                return CreateBillingCycleResultDto(cityId);
            });
            var controller = new HouseholdObligationsController(sender);

            IActionResult listCity = await controller.ListCityObligations(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IActionResult listHousehold = await controller.ListHouseholdObligations(
                householdAccountId: householdAccountId,
                cancellationToken: CancellationToken.None);
            IActionResult register = await controller.RegisterObligation(
                cityId: cityId,
                request: new RegisterCityHouseholdObligationRequest
                {
                    HouseholdAccountId = householdAccountId,
                    ProviderBusinessId = providerBusinessId,
                    Name = "Water Bill",
                    Kind = "Utilities",
                    BillingCadence = "Monthly",
                    ChargeAmount = 40m,
                    TaxAmount = 3m,
                    FirstChargeDueAtUtc = new DateTimeOffset(
                        year: 2048,
                        month: 7,
                        day: 1,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)
                },
                cancellationToken: CancellationToken.None);
            IActionResult issueCharge = await controller.IssueCharge(
                obligationId: obligationId,
                request: new IssueHouseholdObligationChargeRequest
                {
                    Description = "Monthly charge"
                },
                cancellationToken: CancellationToken.None);
            IActionResult runBilling = await controller.RunBillingCycle(
                cityId: cityId,
                request: new RunCityHouseholdBillingCycleRequest
                {
                    AsOfUtc = new DateTimeOffset(
                        year: 2048,
                        month: 6,
                        day: 1,
                        hour: 10,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)
                },
                cancellationToken: CancellationToken.None);

            Assert.Single(
                Assert.IsAssignableFrom<IReadOnlyList<CityHouseholdObligationDto>>(
                    Assert.IsType<OkObjectResult>(listCity)
                       .Value));
            Assert.Single(
                Assert.IsAssignableFrom<IReadOnlyList<CityHouseholdObligationDto>>(
                    Assert.IsType<OkObjectResult>(listHousehold)
                       .Value));
            Assert.Equal(
                expected: "Utilities",
                actual: Assert.IsType<CityHouseholdObligationDto>(
                        Assert.IsType<OkObjectResult>(register)
                           .Value)
                   .Kind);
            Assert.Equal(
                expected: "Charge",
                actual: Assert.IsType<CityHouseholdAccountLedgerEntryDto>(
                        Assert.IsType<OkObjectResult>(issueCharge)
                           .Value)
                   .Title);
            Assert.Equal(
                expected: 6,
                actual: Assert.IsType<RunCityHouseholdBillingCycleResultDto>(
                        Assert.IsType<OkObjectResult>(runBilling)
                           .Value)
                   .ChargedObligations);
        }

        [Fact]
        public async Task HouseholdObligationParsingGuards_ReturnBadRequest()
        {
            var controller = new HouseholdObligationsController(new FakeSender());

            IActionResult invalidKind = await controller.RegisterObligation(
                cityId: Guid.NewGuid(),
                request: new RegisterCityHouseholdObligationRequest
                {
                    HouseholdAccountId = Guid.NewGuid(),
                    ProviderBusinessId = Guid.NewGuid(),
                    Name = "Water Bill",
                    Kind = "Unsupported",
                    ChargeAmount = 40m,
                    TaxAmount = 3m
                },
                cancellationToken: CancellationToken.None);
            IActionResult invalidCadence = await controller.RegisterObligation(
                cityId: Guid.NewGuid(),
                request: new RegisterCityHouseholdObligationRequest
                {
                    HouseholdAccountId = Guid.NewGuid(),
                    ProviderBusinessId = Guid.NewGuid(),
                    Name = "Water Bill",
                    Kind = "Utilities",
                    BillingCadence = "UnsupportedCadence",
                    ChargeAmount = 40m,
                    TaxAmount = 3m
                },
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: StatusCodes.Status400BadRequest,
                actual: Assert.IsType<BadRequestObjectResult>(invalidKind)
                   .StatusCode);
            Assert.Equal(
                expected: StatusCodes.Status400BadRequest,
                actual: Assert.IsType<BadRequestObjectResult>(invalidCadence)
                   .StatusCode);
        }
    }
}
