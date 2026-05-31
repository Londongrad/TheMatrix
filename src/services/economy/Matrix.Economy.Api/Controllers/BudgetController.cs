using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.UseCases.AuthorizeCityBudgetOperation;
using Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy;
using Matrix.Economy.Application.UseCases.BudgetAllocations;
using Matrix.Economy.Application.UseCases.BudgetAllocations.GetCityBudgetAllocations;
using Matrix.Economy.Application.UseCases.BudgetAllocations.SetCityBudgetAllocation;
using Matrix.Economy.Application.UseCases.BudgetLedger;
using Matrix.Economy.Application.UseCases.BudgetLedger.GetCityBudgetLedgerFeed;
using Matrix.Economy.Application.UseCases.BudgetOperations.DisburseCityBudgetToBusiness;
using Matrix.Economy.Application.UseCases.BudgetOperations.RecordCityBudgetExpense;
using Matrix.Economy.Application.UseCases.BudgetOperations.RecordCityBudgetRevenue;
using Matrix.Economy.Application.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle;
using Matrix.Economy.Application.UseCases.GetBudgetSummary;
using Matrix.Economy.Application.UseCases.GetCityBudgetSummary;
using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Contracts.Budget.Requests;
using Matrix.Economy.Contracts.Budget.Views;
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

        [HttpGet("cities/{cityId:guid}/operational-pressure")]
        public async Task<IActionResult> GetCityOperationalPressure(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityOperationalBudgetPressureDto result = await _sender.Send(
                request: new GetCityOperationalBudgetPressureQuery(cityId),
                cancellationToken: cancellationToken);

            return Ok(
                new CityOperationalBudgetPressureView(
                    CityId: result.CityId,
                    EffectiveTickId: result.EffectiveTickId,
                    EffectivePhase: "BudgetSettlement",
                    EffectiveAtUtc: result.EffectiveAtUtc,
                    UnitKind: result.UnitKind,
                    UnitCode: result.UnitCode,
                    UnitDisplayName: result.UnitDisplayName,
                    UnitSymbol: result.UnitSymbol,
                    Balance: result.Balance,
                    TotalCityExpenses: result.TotalCityExpenses,
                    MunicipalOperationsExpenses: result.MunicipalOperationsExpenses,
                    InfrastructureOperationsExpenses: result.InfrastructureOperationsExpenses,
                    EmergencyOperationsExpenses: result.EmergencyOperationsExpenses,
                    GeneralAvailableAmount: result.GeneralAvailableAmount,
                    OperationsAvailableAmount: result.OperationsAvailableAmount,
                    InfrastructureAvailableAmount: result.InfrastructureAvailableAmount,
                    HealthcareAvailableAmount: result.HealthcareAvailableAmount,
                    GeneralAuthorizationLevel: result.GeneralAuthorizationLevel,
                    OperationsAuthorizationLevel: result.OperationsAuthorizationLevel,
                    InfrastructureAuthorizationLevel: result.InfrastructureAuthorizationLevel,
                    HealthcareAuthorizationLevel: result.HealthcareAuthorizationLevel,
                    LastMunicipalExpenseAtUtc: result.LastMunicipalExpenseAtUtc,
                    PressureIndex: result.PressureIndex));
        }

        [HttpPost("cities/{cityId:guid}/operation-authorizations")]
        public async Task<IActionResult> AuthorizeBudgetOperation(
            [FromRoute] Guid cityId,
            [FromBody] AuthorizeBudgetOperationRequest request,
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

            CityBudgetOperationAuthorizationDto result = await _sender.Send(
                request: new AuthorizeCityBudgetOperationCommand(
                    CityId: cityId,
                    Category: category,
                    OperationKind: request.OperationKind,
                    RequestedIntensity: request.RequestedIntensity,
                    EstimatedAmount: request.EstimatedAmount,
                    EmergencyOverrideRequested: request.EmergencyOverride),
                cancellationToken: cancellationToken);

            return Ok(
                new BudgetOperationAuthorizationView(
                    CityId: result.CityId,
                    Category: result.Category,
                    OperationKind: result.OperationKind,
                    RequestedIntensity: result.RequestedIntensity,
                    ApprovedIntensity: result.ApprovedIntensity,
                    Status: result.Status,
                    AuthorizationLevel: result.AuthorizationLevel,
                    AvailableAmount: result.AvailableAmount,
                    EstimatedAmount: result.EstimatedAmount,
                    PressureIndex: result.PressureIndex,
                    EmergencyOverrideRequested: result.EmergencyOverrideRequested,
                    AuthorizedByEmergencyOverride: result.AuthorizedByEmergencyOverride,
                    Summary: result.Summary));
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
                    ScenarioKey: request.ScenarioKey,
                    EconomyProfile: request.EconomyProfile,
                    CreatedAtUtc: request.CreatedAtUtc),
                cancellationToken: cancellationToken);

            return Ok(
                new CityEconomyBootstrapResultView(
                    CityId: result.CityId,
                    BudgetCreated: result.BudgetCreated,
                    CreatedAllocations: result.CreatedAllocations,
                    CreatedBusinesses: result.CreatedBusinesses,
                    UnitKind: result.UnitKind,
                    UnitCode: result.UnitCode,
                    UnitDisplayName: result.UnitDisplayName,
                    UnitSymbol: result.UnitSymbol));
        }

        [HttpGet("cities/{cityId:guid}/ledger-feed")]
        public async Task<IActionResult> GetCityLedgerFeed(
            [FromRoute] Guid cityId,
            [FromQuery] string? cursor = null,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            CursorPagedResult<BudgetLedgerEntryDto> result = await _sender.Send(
                request: new GetCityBudgetLedgerFeedQuery(
                    CityId: cityId,
                    Cursor: cursor,
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

        private static bool TryParseCategory(
            string rawCategory,
            out CityBudgetCategory category)
        {
            return Enum.TryParse(
                value: rawCategory,
                ignoreCase: true,
                result: out category);
        }

        private static EconomySummaryView MapSummary(BudgetSummaryDto result)
        {
            return new EconomySummaryView(
                UnitKind: result.UnitKind,
                UnitCode: result.UnitCode,
                UnitDisplayName: result.UnitDisplayName,
                UnitSymbol: result.UnitSymbol,
                Balance: result.Balance.Amount,
                TotalTaxIncome: result.TotalTaxIncome.Amount,
                TotalIncomeTaxIncome: result.TotalIncomeTaxIncome.Amount,
                TotalSalesTaxIncome: result.TotalSalesTaxIncome.Amount,
                TotalDirectRevenue: result.TotalDirectRevenue.Amount,
                TotalCityExpenses: result.TotalCityExpenses.Amount,
                TotalRetailTurnover: result.TotalRetailTurnover.Amount,
                TotalGrossPayroll: result.TotalGrossPayroll.Amount,
                TotalNetPayroll: result.TotalNetPayroll.Amount);
        }
    }
}
