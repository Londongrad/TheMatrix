using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Authorization.Permissions;
using Matrix.Population.Contracts.Models;
using MediatR;

namespace Matrix.Population.Application.UseCases.Person.UpdatePerson
{
    public sealed record UpdatePersonCommand(
        Guid Id,
        string? FullName,
        string? EducationLevel,
        int? Health,
        int? Happiness,
        int? Energy,
        int? Stress,
        int? SocialNeed) : IRequest<PersonDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationPersonUpdate;
    }
}
