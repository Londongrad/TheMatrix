using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.RecordCityHouseholdPurchase;
using Matrix.Economy.Contracts.Scenarios.ClassicCity.HouseholdAccounts.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Economy.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/economy/household-accounts")]
    public class HouseholdAccountOperationsController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpPost("{householdAccountId:guid}/purchases")]
        public async Task<IActionResult> RecordPurchase(
            [FromRoute] Guid householdAccountId,
            [FromBody] RecordHouseholdPurchaseRequest request,
            CancellationToken cancellationToken)
        {
            CityHouseholdAccountLedgerEntryDto result = await _sender.Send(
                request: new RecordCityHouseholdPurchaseCommand(
                    HouseholdAccountId: householdAccountId,
                    BusinessId: request.BusinessId,
                    GrossAmount: request.GrossAmount,
                    SalesTaxAmount: request.SalesTaxAmount,
                    Title: request.Title,
                    Description: request.Description),
                cancellationToken: cancellationToken);

            return Ok(result);
        }
    }
}
