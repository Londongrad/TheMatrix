using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Api.Contracts.Business;
using Matrix.Economy.Application.UseCases.Businesses;
using Matrix.Economy.Application.UseCases.Businesses.GetCityBusinessLedger;
using Matrix.Economy.Application.UseCases.Businesses.GetCityBusinesses;
using Matrix.Economy.Application.UseCases.Businesses.RegisterCityBusiness;
using Matrix.Economy.Domain.Enums;
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
        public async Task<IActionResult> ListCityBusinesses([FromRoute] Guid cityId, CancellationToken cancellationToken)
        {
            IReadOnlyList<CityBusinessDto> result = await _sender.Send(new GetCityBusinessesQuery(cityId), cancellationToken);
            return Ok(result);
        }

        [HttpPost("cities/{cityId:guid}")]
        public async Task<IActionResult> RegisterBusiness(
            [FromRoute] Guid cityId,
            [FromBody] RegisterCityBusinessRequest request,
            CancellationToken cancellationToken)
        {
            if (!Enum.TryParse(request.Kind, ignoreCase: true, out CityBusinessKind kind))
            {
                return BadRequest(new { error = $"Unsupported business kind '{request.Kind}'." });
            }

            CityBusinessDto result = await _sender.Send(
                new RegisterCityBusinessCommand(
                    CityId: cityId,
                    Name: request.Name,
                    Kind: kind,
                    StartingCapital: request.StartingCapital,
                    UnitKind: request.UnitKind,
                    UnitCode: request.UnitCode,
                    UnitDisplayName: request.UnitDisplayName,
                    UnitSymbol: request.UnitSymbol),
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("{businessId:guid}/ledger")]
        public async Task<IActionResult> GetBusinessLedger(
            [FromRoute] Guid businessId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            PagedResult<CityBusinessLedgerEntryDto> result = await _sender.Send(
                new GetCityBusinessLedgerQuery(businessId, pageNumber, pageSize),
                cancellationToken);

            return Ok(result);
        }
    }
}
