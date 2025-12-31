using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.GetRolePermissions
{
    public sealed class GetRolePermissionsQueryHandler(
        IRoleReadRepository roleReadRepository,
        IRolePermissionsRepository rolePermissionsRepository)
        : IRequestHandler<GetRolePermissionsQuery, IReadOnlyCollection<string>>
    {
        public async Task<IReadOnlyCollection<string>> Handle(
            GetRolePermissionsQuery request,
            CancellationToken cancellationToken)
        {
            bool isExist = await roleReadRepository.ExistsAsync(
                request.RoleId,
                cancellationToken);

            if (!isExist)
                throw ApplicationErrorsFactory.RoleNotFound(request.RoleId);

            return await rolePermissionsRepository.GetRolePermissionsAsync(
                roleId: request.RoleId,
                cancellationToken: cancellationToken);
        }
    }
}
