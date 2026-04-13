using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.GetCityDistrictWaterDistributionConditions
{
    public sealed record GetCityDistrictWaterDistributionConditionsQuery(Guid CityId)
        : IRequest<CityDistrictWaterDistributionConditionsDto?>;
}
