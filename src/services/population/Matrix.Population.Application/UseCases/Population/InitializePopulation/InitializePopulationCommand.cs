using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Population.Application.UseCases.Population.InitializePopulation
{
    public sealed record InitializePopulationCommand(
        int PeopleCount,
        int? RandomSeed) : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationPeopleInitialize;
    }
}
