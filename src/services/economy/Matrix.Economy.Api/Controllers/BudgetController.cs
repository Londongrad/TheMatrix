using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Api.Contracts.Budget;
using Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy;
using Matrix.Economy.Application.UseCases.BudgetAllocations;
using Matrix.Economy.Application.UseCases.BudgetAllocations.GetCityBudgetAllocations;
using Matrix.Economy.Application.UseCases.BudgetAllocations.SetCityBudgetAllocation;
using Matrix.Economy.Application.UseCases.BudgetLedger;
using Matrix.Economy.Application.UseCases.BudgetLedger.GetCityBudgetLedger;
using Matrix.Economy.Application.UseCases.BudgetOperations.DisburseCityBudgetToBusiness;
using Matrix.Economy.Application.UseCases.BudgetOperations.RecordCityBudgetExpense;
using Matrix.Economy.Application.UseCases.BudgetOperations.RecordCityBudgetRevenue;
using Matrix.Economy.Application.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle;
using Matrix.Economy.Application.UseCases.GetBudgetSummary;
using Matrix.Economy.Application.UseCases.GetCityBudgetSummary;
using Matrix.Economy.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Economy.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/economy/[controller]")]
    public class BudgetController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        {
            BudgetSummaryDto result = await _sender.Send(
                request: new GetBudgetSummaryQuery(),
                cancellationToken: cancellationToken);

            return Ok(MapSummary(result));
        }

        [HttpGet("cities/{cityId:guid}/summary")]
        public async Task<IActionResult> GetCitySummary(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            BudgetSummaryDto result = await _sender.Send(
                request: new GetCityBudgetSummaryQuery(cityId),
                cancellationToken: cancellationToken);

            return Ok(MapSummary(result));
        }

        [HttpPost("cities/{cityId:guid}/bootstrap")]
        public async Task<IActionResult> InitializeCityEconomy(
            [FromRoute] Guid cityId,
            [FromBody] InitializeCityEconomyRequest request,
            CancellationToken cancellationToken)
        {
            CityEconomyBootstrapResultDto result = await _sender.Send(
                request: new InitializeCityEconomyCommand(
                    CityId: cityId,
                    SimulationKind: request.SimulationKind,
                    EconomyProfile: request.EconomyProfile,
                    CreatedAtUtc: request.CreatedAtUtc),
                cancellationToken: cancellationToken);

            return Ok(
                new
                {
                    cityId = result.CityId,
                    budgetCreated = result.BudgetCreated,
                    createdAllocations = result.CreatedAllocations,
                    createdBusinesses = result.CreatedBusinesses,
                    unitKind = result.UnitKind,
                    unitCode = result.UnitCode,
                    unitDisplayName = result.UnitDisplayName,
                    unitSymbol = result.UnitSymbol
                });
        }

        [HttpGet("cities/{cityId:guid}/ledger")]
        public async Task<IActionResult> GetCityLedger(
            [FromRoute] Guid cityId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            PagedResult<BudgetLedgerEntryDto> result = await _sender.Send(
                request: new GetCityBudgetLedgerQuery(
                    CityId: cityId,
                    PageNumber: pageNumber,
                    PageSize: pageSize),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpGet("cities/{cityId:guid}/allocations")]
        public async Task<IActionResult> GetCityAllocations(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityBudgetAllocationDto> result = await _sender.Send(
                request: new GetCityBudgetAllocationsQuery(cityId),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("cities/{cityId:guid}/revenue")]
        public async Task<IActionResult> RecordRevenue(
            [FromRoute] Guid cityId,
            [FromBody] RecordBudgetEntryRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryParseCategory(
                    rawCategory: request.Category,
                    category: out CityBudgetCategory category))
                return BadRequest(
                    new
                    {
                        error = $"Unsupported budget category '{request.Category}'."
                    });

            BudgetLedgerEntryDto result = await _sender.Send(
                request: new RecordCityBudgetRevenueCommand(
                    CityId: cityId,
                    Category: category,
                    Amount: request.Amount,
                    Title: request.Title,
                    Description: request.Description,
                    UnitKind: request.UnitKind,
                    UnitCode: request.UnitCode,
                    UnitDisplayName: request.UnitDisplayName,
                    UnitSymbol: request.UnitSymbol),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("cities/{cityId:guid}/expense")]
        public async Task<IActionResult> RecordExpense(
            [FromRoute] Guid cityId,
            [FromBody] RecordBudgetEntryRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryParseCategory(
                    rawCategory: request.Category,
                    category: out CityBudgetCategory category))
                return BadRequest(
                    new
                    {
                        error = $"Unsupported budget category '{request.Category}'."
                    });

            BudgetLedgerEntryDto result = await _sender.Send(
                request: new RecordCityBudgetExpenseCommand(
                    CityId: cityId,
                    Category: category,
                    Amount: request.Amount,
                    Title: request.Title,
                    Description: request.Description,
                    UnitKind: request.UnitKind,
                    UnitCode: request.UnitCode,
                    UnitDisplayName: request.UnitDisplayName,
                    UnitSymbol: request.UnitSymbol),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("cities/{cityId:guid}/disbursements")]
        public async Task<IActionResult> DisburseToBusiness(
            [FromRoute] Guid cityId,
            [FromBody] DisburseBudgetToBusinessRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryParseCategory(
                    rawCategory: request.Category,
                    category: out CityBudgetCategory category))
                return BadRequest(
                    new
                    {
                        error = $"Unsupported budget category '{request.Category}'."
                    });

            BudgetLedgerEntryDto result = await _sender.Send(
                request: new DisburseCityBudgetToBusinessCommand(
                    CityId: cityId,
                    BusinessId: request.BusinessId,
                    Category: category,
                    Amount: request.Amount,
                    Title: request.Title,
                    Description: request.Description),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPut("cities/{cityId:guid}/allocations/{category}")]
        public async Task<IActionResult> SetAllocation(
            [FromRoute] Guid cityId,
            [FromRoute] string category,
            [FromBody] SetBudgetAllocationRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryParseCategory(
                    rawCategory: category,
                    category: out CityBudgetCategory parsedCategory))
                return BadRequest(
                    new
                    {
                        error = $"Unsupported budget category '{category}'."
                    });

            CityBudgetAllocationDto result = await _sender.Send(
                request: new SetCityBudgetAllocationCommand(
                    CityId: cityId,
                    Category: parsedCategory,
                    TargetAmount: request.TargetAmount,
                    UnitKind: request.UnitKind,
                    UnitCode: request.UnitCode,
                    UnitDisplayName: request.UnitDisplayName,
                    UnitSymbol: request.UnitSymbol),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("cities/{cityId:guid}/run-operating-cycle")]
        public async Task<IActionResult> RunOperatingCycle(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            RunCityMunicipalOperatingCycleResultDto result = await _sender.Send(
                request: new RunCityMunicipalOperatingCycleCommand(cityId),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(
                new
                {
                    status = "ok"
                });
        }

        private static bool TryParseCategory(
            string rawCategory,
            out CityBudgetCategory category)
        {
            return Enum.TryParse(
                value: rawCategory,
                ignoreCase: true,
                result: out category);
        }

        private static object MapSummary(BudgetSummaryDto result)
        {
            return new
            {
                unitKind = result.UnitKind,
                unitCode = result.UnitCode,
                unitDisplayName = result.UnitDisplayName,
                unitSymbol = result.UnitSymbol,
                balance = result.Balance.Amount,
                totalTaxIncome = result.TotalTaxIncome.Amount,
                totalIncomeTaxIncome = result.TotalIncomeTaxIncome.Amount,
                totalSalesTaxIncome = result.TotalSalesTaxIncome.Amount,
                totalDirectRevenue = result.TotalDirectRevenue.Amount,
                totalCityExpenses = result.TotalCityExpenses.Amount,
                totalRetailTurnover = result.TotalRetailTurnover.Amount,
                totalGrossPayroll = result.TotalGrossPayroll.Amount,
                totalNetPayroll = result.TotalNetPayroll.Amount
            };
        }
    }
}
