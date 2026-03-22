using Matrix.Economy.Contracts.HouseholdObligations.Requests;
using Matrix.Economy.Application.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.UseCases.HouseholdObligations;
using Matrix.Economy.Application.UseCases.HouseholdObligations.GetCityHouseholdObligations;
using Matrix.Economy.Application.UseCases.HouseholdObligations.GetHouseholdObligations;
using Matrix.Economy.Application.UseCases.HouseholdObligations.IssueHouseholdObligationCharge;
using Matrix.Economy.Application.UseCases.HouseholdObligations.RegisterCityHouseholdObligation;
using Matrix.Economy.Application.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle;
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
        public async Task<IActionResult> ListCityObligations(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityHouseholdObligationDto> result = await _sender.Send(
                request: new GetCityHouseholdObligationsQuery(cityId),
                cancellationToken: cancellationToken);
            return Ok(result);
        }

        [HttpGet("households/{householdAccountId:guid}")]
        public async Task<IActionResult> ListHouseholdObligations(
            [FromRoute] Guid householdAccountId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityHouseholdObligationDto> result = await _sender.Send(
                request: new GetHouseholdObligationsQuery(householdAccountId),
                cancellationToken: cancellationToken);
            return Ok(result);
        }

        [HttpPost("cities/{cityId:guid}")]
        public async Task<IActionResult> RegisterObligation(
            [FromRoute] Guid cityId,
            [FromBody] RegisterCityHouseholdObligationRequest request,
            CancellationToken cancellationToken)
        {
            if (!Enum.TryParse(
                    value: request.Kind,
                    ignoreCase: true,
                    result: out CityHouseholdObligationKind kind))
                return BadRequest(
                    new
                    {
                        error = $"Unsupported obligation kind '{request.Kind}'."
                    });

            CityHouseholdObligationBillingCadence billingCadence = CityHouseholdObligationBillingCadence.Monthly;
            if (!string.IsNullOrWhiteSpace(request.BillingCadence) &&
                !Enum.TryParse(
                    value: request.BillingCadence,
                    ignoreCase: true,
                    result: out billingCadence))
                return BadRequest(
                    new
                    {
                        error = $"Unsupported billing cadence '{request.BillingCadence}'."
                    });

            CityHouseholdObligationDto result = await _sender.Send(
                request: new RegisterCityHouseholdObligationCommand(
                    CityId: cityId,
                    HouseholdAccountId: request.HouseholdAccountId,
                    ProviderBusinessId: request.ProviderBusinessId,
                    Name: request.Name,
                    Kind: kind,
                    BillingCadence: billingCadence,
                    ChargeAmount: request.ChargeAmount,
                    TaxAmount: request.TaxAmount,
                    FirstChargeDueAtUtc: request.FirstChargeDueAtUtc),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("{obligationId:guid}/charges")]
        public async Task<IActionResult> IssueCharge(
            [FromRoute] Guid obligationId,
            [FromBody] IssueHouseholdObligationChargeRequest request,
            CancellationToken cancellationToken)
        {
            CityHouseholdAccountLedgerEntryDto result = await _sender.Send(
                request: new IssueHouseholdObligationChargeCommand(
                    ObligationId: obligationId,
                    Description: request.Description),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("cities/{cityId:guid}/run-billing")]
        public async Task<IActionResult> RunBillingCycle(
            [FromRoute] Guid cityId,
            [FromBody] RunCityHouseholdBillingCycleRequest? request,
            CancellationToken cancellationToken)
        {
            RunCityHouseholdBillingCycleResultDto result = await _sender.Send(
                request: new RunCityHouseholdBillingCycleCommand(
                    CityId: cityId,
                    AsOfUtc: request?.AsOfUtc),
                cancellationToken: cancellationToken);

            return Ok(result);
        }
    }
}
