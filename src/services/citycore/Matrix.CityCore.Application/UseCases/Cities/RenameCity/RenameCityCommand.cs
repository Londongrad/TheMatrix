using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.RenameCity
{
    public sealed record RenameCityCommand(
        Guid CityId,
        string Name) : IRequest<bool>;
}
