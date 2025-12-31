using Matrix.Identity.Application.UseCases.Admin.Roles.CreateRole;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolesList;
using Matrix.Identity.Contracts.Admin.Roles.Responses;

namespace Matrix.Identity.Api.Mappers
{
    public static class RoleResponseMapper
    {
        public static RoleResponse ToResponse(this RoleListItemResult r) => new()
        {
            Id = r.Id,
            Name = r.Name,
            IsSystem = r.IsSystem,
            CreatedAtUtc = r.CreatedAtUtc
        };

        public static RoleResponse ToResponse(this RoleCreatedResult r) => new()
        {
            Id = r.Id,
            Name = r.Name,
            IsSystem = r.IsSystem,
            CreatedAtUtc = r.CreatedAtUtc
        };
    }
}
