using Matrix.BuildingBlocks.Application.Authorization;
using Matrix.Population.Contracts.Models;
using MediatR;

namespace Matrix.Population.Application.UseCases.Person.KillPerson
{
    public record KillPersonCommand(Guid Id) : IRequest<PersonDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationPeopleKill;
    }
}
