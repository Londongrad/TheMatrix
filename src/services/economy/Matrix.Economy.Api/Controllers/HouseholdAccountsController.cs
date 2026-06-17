using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.GetCityHouseholdAccountLedgerFeed;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.GetCityHouseholdAccounts;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.RegisterCityHouseholdAccount;
using Matrix.Economy.Contracts.Scenarios.ClassicCity.HouseholdAccounts.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Economy.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/economy/[controller]")]
    public class HouseholdAccountsController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpGet("cities/{cityId:guid}")]
        public async Task<IActionResult> ListCityHouseholdAccounts(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityHouseholdAccountDto> result = await _sender.Send(
                request: new GetCityHouseholdAccountsQuery(cityId),
                cancellationToken: cancellationToken);
            return Ok(result);
        }

        [HttpPost("cities/{cityId:guid}")]
        public async Task<IActionResult> RegisterHouseholdAccount(
            [FromRoute] Guid cityId,
            [FromBody] RegisterCityHouseholdAccountRequest request,
            CancellationToken cancellationToken)
        {
            CityHouseholdAccountDto result = await _sender.Send(
                request: new RegisterCityHouseholdAccountCommand(
                    CityId: cityId,
                    Name: request.Name,
                    ExternalReferenceCode: request.ExternalReferenceCode,
                    OpeningBalance: request.OpeningBalance,
                    UnitKind: request.UnitKind,
                    UnitCode: request.UnitCode,
                    UnitDisplayName: request.UnitDisplayName,
                    UnitSymbol: request.UnitSymbol),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpGet("{householdAccountId:guid}/ledger-feed")]
        public async Task<IActionResult> GetHouseholdAccountLedgerFeed(
            [FromRoute] Guid householdAccountId,
            [FromQuery] string? cursor = null,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            CursorPagedResult<CityHouseholdAccountLedgerEntryDto> result = await _sender.Send(
                request: new GetCityHouseholdAccountLedgerFeedQuery(
                    HouseholdAccountId: householdAccountId,
                    Cursor: cursor,
                    PageSize: pageSize),
                cancellationToken: cancellationToken);

            return Ok(result);
        }
    }
}
