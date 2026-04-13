using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.GetCityDistrictPowerDistributionConditions
{
    public sealed record GetCityDistrictPowerDistributionConditionsQuery(Guid CityId)
        : IRequest<CityDistrictPowerDistributionConditionsDto?>;
}
