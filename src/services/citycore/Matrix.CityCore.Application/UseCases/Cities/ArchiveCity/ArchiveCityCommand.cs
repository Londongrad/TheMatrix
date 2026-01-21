using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.ArchiveCity
{
    public sealed record ArchiveCityCommand(Guid CityId) : IRequest<bool>;
}
