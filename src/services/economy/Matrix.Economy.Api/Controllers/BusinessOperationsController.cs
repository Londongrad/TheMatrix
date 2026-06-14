using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RecordCityBusinessExpense;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RecordCityBusinessPayroll;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RecordCityBusinessRetailSale;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RemitCityBusinessTax;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RunCityBusinessTaxCycle;
using Matrix.Economy.Contracts.Business.Requests;
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
                request: new RecordCityBusinessRetailSaleCommand(
                    BusinessId: businessId,
                    GrossAmount: request.GrossAmount,
                    SalesTaxAmount: request.SalesTaxAmount,
                    Title: request.Title,
                    Description: request.Description),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("{businessId:guid}/expenses")]
        public async Task<IActionResult> RecordExpense(
            [FromRoute] Guid businessId,
            [FromBody] RecordBusinessExpenseRequest request,
            CancellationToken cancellationToken)
        {
            CityBusinessLedgerEntryDto result = await _sender.Send(
                request: new RecordCityBusinessExpenseCommand(
                    BusinessId: businessId,
                    Amount: request.Amount,
                    Title: request.Title,
                    Description: request.Description),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("{businessId:guid}/payroll")]
        public async Task<IActionResult> RecordPayroll(
            [FromRoute] Guid businessId,
            [FromBody] RecordBusinessPayrollRequest request,
            CancellationToken cancellationToken)
        {
            CityBusinessLedgerEntryDto result = await _sender.Send(
                request: new RecordCityBusinessPayrollCommand(
                    BusinessId: businessId,
                    HouseholdAccountId: request.HouseholdAccountId,
                    GrossAmount: request.GrossAmount,
                    IncomeTaxAmount: request.IncomeTaxAmount,
                    Title: request.Title,
                    Description: request.Description),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("{businessId:guid}/tax-remittances")]
        public async Task<IActionResult> RemitTax(
            [FromRoute] Guid businessId,
            [FromBody] RemitBusinessTaxRequest request,
            CancellationToken cancellationToken)
        {
            CityBudgetCategory category = CityBudgetCategory.Taxation;

            if (!string.IsNullOrWhiteSpace(request.BudgetCategory) &&
                !Enum.TryParse(
                    value: request.BudgetCategory,
                    ignoreCase: true,
                    result: out category))
                return BadRequest(
                    new
                    {
                        error = $"Unsupported budget category '{request.BudgetCategory}'."
                    });

            CityBusinessLedgerEntryDto result = await _sender.Send(
                request: new RemitCityBusinessTaxCommand(
                    BusinessId: businessId,
                    Amount: request.Amount,
                    BudgetCategory: category,
                    Title: request.Title,
                    Description: request.Description),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("cities/{cityId:guid}/run-tax-cycle")]
        public async Task<IActionResult> RunTaxCycle(
            [FromRoute] Guid cityId,
            [FromBody] RunCityBusinessTaxCycleRequest? request,
            CancellationToken cancellationToken)
        {
            CityBudgetCategory budgetCategory = CityBudgetCategory.Taxation;

            if (!string.IsNullOrWhiteSpace(request?.BudgetCategory) &&
                !Enum.TryParse(
                    value: request.BudgetCategory,
                    ignoreCase: true,
                    result: out budgetCategory))
                return BadRequest(
                    new
                    {
                        error = $"Unsupported budget category '{request.BudgetCategory}'."
                    });

            RunCityBusinessTaxCycleResultDto result = await _sender.Send(
                request: new RunCityBusinessTaxCycleCommand(
                    CityId: cityId,
                    BudgetCategory: budgetCategory),
                cancellationToken: cancellationToken);

            return Ok(result);
        }
    }
}
