using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.Common;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.GetCityStockpiles;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SetCityEmergencyRationing;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Requests;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views;
using DomainResupplyFocus = Matrix.Resources.Domain.Scenarios.ClassicCity.Enums.ResupplyFocus;
using DomainResupplyIntensity = Matrix.Resources.Domain.Scenarios.ClassicCity.Enums.ResupplyIntensity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Resources.Api.Controllers.Scenarios.ClassicCity
{
    [ApiController]
    [Authorize]
    [Route("api/classic-city/cities")]
    public sealed class StockpilesController(IMediator mediator) : ControllerBase
    {
        [HttpGet("{cityId:guid}/stockpiles")]
        public async Task<IResult> Get(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityStockpilesDto? stockpiles = await mediator.Send(
                request: new GetCityStockpilesQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return stockpiles is null
                ? Results.NotFound()
                : Results.Ok(MapToView(stockpiles));
        }

        [HttpPut("{cityId:guid}/stockpiles/emergency-rationing")]
        public async Task<IResult> SetEmergencyRationing(
            [FromRoute] Guid cityId,
            [FromBody] SetCityEmergencyRationingRequest request,
            CancellationToken cancellationToken)
        {
            SetCityEmergencyRationingResult result = await mediator.Send(
                request: new SetCityEmergencyRationingCommand(
                    CityId: cityId,
                    Enabled: request.Enabled),
                cancellationToken: cancellationToken);

            if (result.Status == SetCityEmergencyRationingStatus.NotInitialized)
                return Results.NotFound();

            return await LoadCurrentViewAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
        }

        [HttpPost("{cityId:guid}/stockpiles/resupply")]
        public async Task<IResult> DispatchResupply(
            [FromRoute] Guid cityId,
            [FromBody] DispatchCityResupplyRequest request,
            CancellationToken cancellationToken)
        {
            DispatchCityResupplyResult result = await mediator.Send(
                request: new DispatchCityResupplyCommand(
                    CityId: cityId,
                    Focus: MapFocus(request.Focus),
                    Intensity: MapIntensity(request.Intensity),
                    EmergencyOverride: request.EmergencyOverride),
                cancellationToken: cancellationToken);

            if (result.Status == DispatchCityResupplyStatus.NotInitialized)
                return Results.NotFound();

            DispatchCityResupplyView view = MapDispatchView(result);

            return result.Status is DispatchCityResupplyStatus.BudgetBlocked or DispatchCityResupplyStatus.AuthorizationDenied
                ? Results.Conflict(view)
                : Results.Ok(view);
        }

        private async Task<IResult> LoadCurrentViewAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            CityStockpilesDto? stockpiles = await mediator.Send(
                request: new GetCityStockpilesQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return stockpiles is null
                ? Results.NotFound()
                : Results.Ok(MapToView(stockpiles));
        }

        private static DomainResupplyFocus MapFocus(ResupplyFocus focus)
        {
            return focus switch
            {
                ResupplyFocus.All => DomainResupplyFocus.All,
                ResupplyFocus.Fuel => DomainResupplyFocus.Fuel,
                ResupplyFocus.Food => DomainResupplyFocus.Food,
                ResupplyFocus.Medicine => DomainResupplyFocus.Medicine,
                ResupplyFocus.SpareParts => DomainResupplyFocus.SpareParts,
                ResupplyFocus.Filters => DomainResupplyFocus.Filters,
                ResupplyFocus.EmergencyWater => DomainResupplyFocus.EmergencyWater,
                _ => DomainResupplyFocus.All
            };
        }

        private static DomainResupplyIntensity MapIntensity(ResupplyIntensity intensity)
        {
            return intensity switch
            {
                ResupplyIntensity.Low => DomainResupplyIntensity.Low,
                ResupplyIntensity.Medium => DomainResupplyIntensity.Medium,
                ResupplyIntensity.High => DomainResupplyIntensity.High,
                _ => DomainResupplyIntensity.Medium
            };
        }

        private static CityStockpilesView MapToView(CityStockpilesDto dto)
        {
            return new CityStockpilesView(
                CityId: dto.CityId,
                SupplyStressIndex: dto.SupplyStressIndex,
                EmergencyRationingEnabled: dto.EmergencyRationingEnabled,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                Fuel: MapLine(dto.Fuel),
                Food: MapLine(dto.Food),
                Medicine: MapLine(dto.Medicine),
                SpareParts: MapLine(dto.SpareParts),
                Filters: MapLine(dto.Filters),
                EmergencyWater: MapLine(dto.EmergencyWater));
        }

        private static CityStockpileLineView MapLine(CityStockpileLineDto dto)
        {
            return new CityStockpileLineView(
                Kind: dto.Kind,
                StockLevelIndex: dto.StockLevelIndex,
                DemandPressureIndex: dto.DemandPressureIndex,
                ResupplyReadinessIndex: dto.ResupplyReadinessIndex,
                ShortageRiskIndex: dto.ShortageRiskIndex);
        }

        private static DispatchCityResupplyView MapDispatchView(DispatchCityResupplyResult result)
        {
            return new DispatchCityResupplyView(
                Status: result.Status.ToString(),
                CityId: result.CityId,
                RequestedIntensity: result.RequestedIntensity,
                BudgetAuthorizedIntensity: result.BudgetAuthorizedIntensity,
                AppliedIntensity: result.AppliedIntensity,
                BudgetPressureIndex: result.BudgetPressureIndex,
                BudgetAuthorizationStatus: result.BudgetAuthorizationStatus,
                BudgetAuthorizationLevel: result.BudgetAuthorizationLevel,
                BudgetAvailableAmount: result.BudgetAvailableAmount,
                BudgetAuthorizedByEmergencyOverride: result.BudgetAuthorizedByEmergencyOverride,
                BudgetAuthorizationSummary: result.BudgetAuthorizationSummary,
                SupplyStressIndex: result.SupplyStressIndex,
                FuelStockLevelIndex: result.FuelStockLevelIndex,
                FoodStockLevelIndex: result.FoodStockLevelIndex,
                EmergencyWaterStockLevelIndex: result.EmergencyWaterStockLevelIndex);
        }
    }
}
