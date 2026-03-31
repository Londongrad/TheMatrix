using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand
{
    public sealed record SyncCitySystemsDemandCommand(
        Guid CityId,
        decimal FuelDemandPressureIndex,
        decimal SparePartsDemandPressureIndex,
        decimal FiltersDemandPressureIndex,
        decimal EmergencyWaterDemandPressureIndex,
        decimal OverallDemandPressureIndex,
        long EffectiveTickId,
        DateTimeOffset EffectiveAtUtc) : IRequest<SyncCitySystemsDemandResult>;
}
