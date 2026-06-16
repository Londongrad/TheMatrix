using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.GetCityBusinesses;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.GetCityBusinessLedgerFeed;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RegisterCityBusiness;
using Matrix.Economy.Contracts.Business.Requests;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Economy.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/economy/[controller]")]
    public class BusinessController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpGet("cities/{cityId:guid}")]
        public async Task<IActionResult> ListCityBusinesses(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityBusinessDto> result = await _sender.Send(
                request: new GetCityBusinessesQuery(cityId),
                cancellationToken: cancellationToken);
            return Ok(result);
        }

        [HttpPost("cities/{cityId:guid}")]
        public async Task<IActionResult> RegisterBusiness(
            [FromRoute] Guid cityId,
            [FromBody] RegisterCityBusinessRequest request,
            CancellationToken cancellationToken)
        {
            if (!Enum.TryParse(
                    value: request.Kind,
                    ignoreCase: true,
                    result: out CityBusinessKind kind))
                return BadRequest(
                    new
                    {
                        error = $"Unsupported business kind '{request.Kind}'."
                    });

            CityBusinessDto result = await _sender.Send(
                request: new RegisterCityBusinessCommand(
                    CityId: cityId,
                    Name: request.Name,
                    Kind: kind,
                    StartingCapital: request.StartingCapital,
                    UnitKind: request.UnitKind,
                    UnitCode: request.UnitCode,
                    UnitDisplayName: request.UnitDisplayName,
                    UnitSymbol: request.UnitSymbol),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpGet("{businessId:guid}/ledger-feed")]
        public async Task<IActionResult> GetBusinessLedgerFeed(
            [FromRoute] Guid businessId,
            [FromQuery] string? cursor = null,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            CursorPagedResult<CityBusinessLedgerEntryDto> result = await _sender.Send(
                request: new GetCityBusinessLedgerFeedQuery(
                    BusinessId: businessId,
                    Cursor: cursor,
                    PageSize: pageSize),
                cancellationToken: cancellationToken);

            return Ok(result);
        }
    }
}
