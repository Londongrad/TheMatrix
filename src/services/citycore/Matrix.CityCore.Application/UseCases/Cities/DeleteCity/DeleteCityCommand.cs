using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.DeleteCity
{
    public sealed record DeleteCityCommand(Guid CityId)
        : IRequest<DeleteCityResult>;
}
