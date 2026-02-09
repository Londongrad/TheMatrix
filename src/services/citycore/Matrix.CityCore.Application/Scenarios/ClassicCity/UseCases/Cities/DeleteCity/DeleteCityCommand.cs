using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.DeleteCity
{
    public sealed record DeleteCityCommand(Guid CityId)
        : IRequest<DeleteCityResult>;
}
