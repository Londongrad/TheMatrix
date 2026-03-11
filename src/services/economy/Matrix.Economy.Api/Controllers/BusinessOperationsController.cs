using Matrix.Economy.Api.Contracts.Business;
using Matrix.Economy.Application.UseCases.Businesses;
using Matrix.Economy.Application.UseCases.Businesses.RecordCityBusinessExpense;
using Matrix.Economy.Application.UseCases.Businesses.RecordCityBusinessPayroll;
using Matrix.Economy.Application.UseCases.Businesses.RecordCityBusinessRetailSale;
using Matrix.Economy.Application.UseCases.Businesses.RemitCityBusinessTax;
using Matrix.Economy.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Economy.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/economy/business")]
    public class BusinessOperationsController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpPost("{businessId:guid}/retail-sales")]
        public async Task<IActionResult> RecordRetailSale(
            [FromRoute] Guid businessId,
            [FromBody] RecordBusinessRetailSaleRequest request,
            CancellationToken cancellationToken)
        {
            CityBusinessLedgerEntryDto result = await _sender.Send(
                new RecordCityBusinessRetailSaleCommand(
                    businessId,
                    request.GrossAmount,
                    request.SalesTaxAmount,
                    request.Title,
                    request.Description),
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("{businessId:guid}/expenses")]
        public async Task<IActionResult> RecordExpense(
            [FromRoute] Guid businessId,
            [FromBody] RecordBusinessExpenseRequest request,
            CancellationToken cancellationToken)
        {
            CityBusinessLedgerEntryDto result = await _sender.Send(
                new RecordCityBusinessExpenseCommand(
                    businessId,
                    request.Amount,
                    request.Title,
                    request.Description),
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("{businessId:guid}/payroll")]
        public async Task<IActionResult> RecordPayroll(
            [FromRoute] Guid businessId,
            [FromBody] RecordBusinessPayrollRequest request,
            CancellationToken cancellationToken)
        {
            CityBusinessLedgerEntryDto result = await _sender.Send(
                new RecordCityBusinessPayrollCommand(
                    BusinessId: businessId,
                    HouseholdAccountId: request.HouseholdAccountId,
                    GrossAmount: request.GrossAmount,
                    IncomeTaxAmount: request.IncomeTaxAmount,
                    Title: request.Title,
                    Description: request.Description),
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("{businessId:guid}/tax-remittances")]
        public async Task<IActionResult> RemitTax(
            [FromRoute] Guid businessId,
            [FromBody] RemitBusinessTaxRequest request,
            CancellationToken cancellationToken)
        {
            CityBudgetCategory category = CityBudgetCategory.Taxation;

            if (!string.IsNullOrWhiteSpace(request.BudgetCategory)
                && !Enum.TryParse(request.BudgetCategory, ignoreCase: true, out category))
            {
                return BadRequest(new { error = $"Unsupported budget category '{request.BudgetCategory}'." });
            }

            CityBusinessLedgerEntryDto result = await _sender.Send(
                new RemitCityBusinessTaxCommand(
                    BusinessId: businessId,
                    Amount: request.Amount,
                    BudgetCategory: category,
                    Title: request.Title,
                    Description: request.Description),
                cancellationToken);

            return Ok(result);
        }
    }
}
