using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard
{
    public sealed record GetCityDashboardQuery(Guid CityId) : IRequest<CityPopulationDashboardDto?>;
}
