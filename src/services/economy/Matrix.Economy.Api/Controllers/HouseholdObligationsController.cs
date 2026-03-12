using Matrix.Economy.Api.Contracts.HouseholdObligations;
using Matrix.Economy.Application.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.UseCases.HouseholdObligations;
using Matrix.Economy.Application.UseCases.HouseholdObligations.GetCityHouseholdObligations;
using Matrix.Economy.Application.UseCases.HouseholdObligations.GetHouseholdObligations;
using Matrix.Economy.Application.UseCases.HouseholdObligations.IssueHouseholdObligationCharge;
using Matrix.Economy.Application.UseCases.HouseholdObligations.RegisterCityHouseholdObligation;
using Matrix.Economy.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Economy.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/economy/[controller]")]
    public class HouseholdObligationsController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpGet("cities/{cityId:guid}")]
        public async Task<IActionResult> ListCityObligations([FromRoute] Guid cityId, CancellationToken cancellationToken)
        {
            IReadOnlyList<CityHouseholdObligationDto> result = await _sender.Send(new GetCityHouseholdObligationsQuery(cityId), cancellationToken);
            return Ok(result);
        }

        [HttpGet("households/{householdAccountId:guid}")]
        public async Task<IActionResult> ListHouseholdObligations([FromRoute] Guid householdAccountId, CancellationToken cancellationToken)
        {
            IReadOnlyList<CityHouseholdObligationDto> result = await _sender.Send(new GetHouseholdObligationsQuery(householdAccountId), cancellationToken);
            return Ok(result);
        }

        [HttpPost("cities/{cityId:guid}")]
        public async Task<IActionResult> RegisterObligation(
            [FromRoute] Guid cityId,
            [FromBody] RegisterCityHouseholdObligationRequest request,
            CancellationToken cancellationToken)
        {
            if (!Enum.TryParse(request.Kind, ignoreCase: true, out CityHouseholdObligationKind kind))
            {
                return BadRequest(new { error = $"Unsupported obligation kind '{request.Kind}'." });
            }

            CityHouseholdObligationDto result = await _sender.Send(
                new RegisterCityHouseholdObligationCommand(
                    CityId: cityId,
                    HouseholdAccountId: request.HouseholdAccountId,
                    ProviderBusinessId: request.ProviderBusinessId,
                    Name: request.Name,
                    Kind: kind,
                    ChargeAmount: request.ChargeAmount,
                    TaxAmount: request.TaxAmount),
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("{obligationId:guid}/charges")]
        public async Task<IActionResult> IssueCharge(
            [FromRoute] Guid obligationId,
            [FromBody] IssueHouseholdObligationChargeRequest request,
            CancellationToken cancellationToken)
        {
            CityHouseholdAccountLedgerEntryDto result = await _sender.Send(
                new IssueHouseholdObligationChargeCommand(obligationId, request.Description),
                cancellationToken);

            return Ok(result);
        }
    }
}
