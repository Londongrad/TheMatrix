using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Api.Controllers;
using Matrix.Economy.Application.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.UseCases.HouseholdAccounts.GetCityHouseholdAccountLedgerFeed;
using Matrix.Economy.Application.UseCases.HouseholdAccounts.GetCityHouseholdAccounts;
using Matrix.Economy.Application.UseCases.HouseholdAccounts.RecordCityHouseholdPurchase;
using Matrix.Economy.Application.UseCases.HouseholdAccounts.RegisterCityHouseholdAccount;
using Matrix.Economy.Application.UseCases.HouseholdObligations;
using Matrix.Economy.Application.UseCases.HouseholdObligations.GetCityHouseholdObligations;
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

namespace Matrix.Economy.Api.Tests.Controllers;

public sealed class HouseholdControllersTests
{
    [Fact]
    public async Task HouseholdAccountEndpoints_ReturnPayloadsAndForwardCommands()
    {
        Guid cityId = Guid.Parse("b0e5a970-bb04-40f8-af70-56fe8fc6722d");
        Guid householdAccountId = Guid.Parse("ed4c0dfd-1a16-44d0-aa53-6477b959a974");
        Guid businessId = Guid.Parse("b4c6c4b3-a7a0-4329-a58f-2a8c4f3d2dd6");
        var sender = new FakeSender();
        sender.Handle<GetCityHouseholdAccountsQuery, IReadOnlyList<CityHouseholdAccountDto>>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            return [CreateHouseholdAccountDto(cityId)];
        });
        sender.Handle<RegisterCityHouseholdAccountCommand, CityHouseholdAccountDto>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.Equal("Anderson Household", command.Name);
            return CreateHouseholdAccountDto(cityId);
        });
        sender.Handle<GetCityHouseholdAccountLedgerFeedQuery, CursorPagedResult<CityHouseholdAccountLedgerEntryDto>>(query =>
        {
            Assert.Equal(householdAccountId, query.HouseholdAccountId);
            Assert.Equal("cursor-hh", query.Cursor);
            Assert.Equal(20, query.PageSize);
            return CreateHouseholdAccountLedgerFeed();
        });
        sender.Handle<RecordCityHouseholdPurchaseCommand, CityHouseholdAccountLedgerEntryDto>(command =>
        {
            Assert.Equal(householdAccountId, command.HouseholdAccountId);
            Assert.Equal(businessId, command.BusinessId);
            return CreateHouseholdAccountLedgerEntryDto();
        });
        var accountsController = new HouseholdAccountsController(sender);
        var operationsController = new HouseholdAccountOperationsController(sender);

        IActionResult list = await accountsController.ListCityHouseholdAccounts(cityId, CancellationToken.None);
        IActionResult register = await accountsController.RegisterHouseholdAccount(
            cityId,
            new RegisterCityHouseholdAccountRequest
            {
                Name = "Anderson Household",
                ExternalReferenceCode = "HH-01",
                OpeningBalance = 900m,
                UnitKind = "Currency",
                UnitCode = "CRD",
                UnitDisplayName = "Credits",
                UnitSymbol = "C"
            },
            CancellationToken.None);
        IActionResult feed = await accountsController.GetHouseholdAccountLedgerFeed(householdAccountId, "cursor-hh", 20, CancellationToken.None);
        IActionResult purchase = await operationsController.RecordPurchase(
            householdAccountId,
            new RecordHouseholdPurchaseRequest
            {
                BusinessId = businessId,
                GrossAmount = 75m,
                SalesTaxAmount = 4m,
                Title = "Household purchase",
                Description = "desc"
            },
            CancellationToken.None);

        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<CityHouseholdAccountDto>>(Assert.IsType<OkObjectResult>(list).Value));
        Assert.Equal("Anderson Household", Assert.IsType<CityHouseholdAccountDto>(Assert.IsType<OkObjectResult>(register).Value).Name);
        Assert.True(Assert.IsType<CursorPagedResult<CityHouseholdAccountLedgerEntryDto>>(Assert.IsType<OkObjectResult>(feed).Value).HasNext);
        Assert.Equal("Household purchase", Assert.IsType<CityHouseholdAccountLedgerEntryDto>(Assert.IsType<OkObjectResult>(purchase).Value).Title);
    }

    [Fact]
    public async Task HouseholdObligationEndpoints_ReturnPayloadsAndForwardCommands()
    {
        Guid cityId = Guid.Parse("b0e5a970-bb04-40f8-af70-56fe8fc6722d");
        Guid householdAccountId = Guid.Parse("ed4c0dfd-1a16-44d0-aa53-6477b959a974");
        Guid obligationId = Guid.Parse("0d18f845-d6d4-41af-a4b8-c6dc2f9495dc");
        Guid providerBusinessId = Guid.Parse("b4c6c4b3-a7a0-4329-a58f-2a8c4f3d2dd6");
        var sender = new FakeSender();
        sender.Handle<GetCityHouseholdObligationsQuery, IReadOnlyList<CityHouseholdObligationDto>>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            return [CreateHouseholdObligationDto(cityId)];
        });
        sender.Handle<GetHouseholdObligationsQuery, IReadOnlyList<CityHouseholdObligationDto>>(query =>
        {
            Assert.Equal(householdAccountId, query.HouseholdAccountId);
            return [CreateHouseholdObligationDto(cityId)];
        });
        sender.Handle<RegisterCityHouseholdObligationCommand, CityHouseholdObligationDto>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.Equal(CityHouseholdObligationKind.Utilities, command.Kind);
            Assert.Equal(CityHouseholdObligationBillingCadence.Monthly, command.BillingCadence);
            return CreateHouseholdObligationDto(cityId);
        });
        sender.Handle<IssueHouseholdObligationChargeCommand, CityHouseholdAccountLedgerEntryDto>(command =>
        {
            Assert.Equal(obligationId, command.ObligationId);
            return CreateHouseholdAccountLedgerEntryDto("Charge");
        });
        sender.Handle<RunCityHouseholdBillingCycleCommand, RunCityHouseholdBillingCycleResultDto>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.Equal(new DateTimeOffset(2048, 6, 1, 10, 0, 0, TimeSpan.Zero), command.AsOfUtc);
            return CreateBillingCycleResultDto(cityId);
        });
        var controller = new HouseholdObligationsController(sender);

        IActionResult listCity = await controller.ListCityObligations(cityId, CancellationToken.None);
        IActionResult listHousehold = await controller.ListHouseholdObligations(householdAccountId, CancellationToken.None);
        IActionResult register = await controller.RegisterObligation(
            cityId,
            new RegisterCityHouseholdObligationRequest
            {
                HouseholdAccountId = householdAccountId,
                ProviderBusinessId = providerBusinessId,
                Name = "Water Bill",
                Kind = "Utilities",
                BillingCadence = "Monthly",
                ChargeAmount = 40m,
                TaxAmount = 3m,
                FirstChargeDueAtUtc = new DateTimeOffset(2048, 7, 1, 0, 0, 0, TimeSpan.Zero)
            },
            CancellationToken.None);
        IActionResult issueCharge = await controller.IssueCharge(
            obligationId,
            new IssueHouseholdObligationChargeRequest
            {
                Description = "Monthly charge"
            },
            CancellationToken.None);
        IActionResult runBilling = await controller.RunBillingCycle(
            cityId,
            new RunCityHouseholdBillingCycleRequest
            {
                AsOfUtc = new DateTimeOffset(2048, 6, 1, 10, 0, 0, TimeSpan.Zero)
            },
            CancellationToken.None);

        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<CityHouseholdObligationDto>>(Assert.IsType<OkObjectResult>(listCity).Value));
        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<CityHouseholdObligationDto>>(Assert.IsType<OkObjectResult>(listHousehold).Value));
        Assert.Equal("Utilities", Assert.IsType<CityHouseholdObligationDto>(Assert.IsType<OkObjectResult>(register).Value).Kind);
        Assert.Equal("Charge", Assert.IsType<CityHouseholdAccountLedgerEntryDto>(Assert.IsType<OkObjectResult>(issueCharge).Value).Title);
        Assert.Equal(6, Assert.IsType<RunCityHouseholdBillingCycleResultDto>(Assert.IsType<OkObjectResult>(runBilling).Value).ChargedObligations);
    }

    [Fact]
    public async Task HouseholdObligationParsingGuards_ReturnBadRequest()
    {
        var controller = new HouseholdObligationsController(new FakeSender());

        IActionResult invalidKind = await controller.RegisterObligation(
            Guid.NewGuid(),
            new RegisterCityHouseholdObligationRequest
            {
                HouseholdAccountId = Guid.NewGuid(),
                ProviderBusinessId = Guid.NewGuid(),
                Name = "Water Bill",
                Kind = "Unsupported",
                ChargeAmount = 40m,
                TaxAmount = 3m
            },
            CancellationToken.None);
        IActionResult invalidCadence = await controller.RegisterObligation(
            Guid.NewGuid(),
            new RegisterCityHouseholdObligationRequest
            {
                HouseholdAccountId = Guid.NewGuid(),
                ProviderBusinessId = Guid.NewGuid(),
                Name = "Water Bill",
                Kind = "Utilities",
                BillingCadence = "UnsupportedCadence",
                ChargeAmount = 40m,
                TaxAmount = 3m
            },
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<BadRequestObjectResult>(invalidKind).StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<BadRequestObjectResult>(invalidCadence).StatusCode);
    }
}
