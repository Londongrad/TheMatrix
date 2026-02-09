using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.RenameCity
{
    public sealed record RenameCityCommand(
        Guid CityId,
        string Name) : IRequest<bool>;
}
