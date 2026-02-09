using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.ArchiveCity
{
    public sealed record ArchiveCityCommand(Guid CityId) : IRequest<bool>;
}
