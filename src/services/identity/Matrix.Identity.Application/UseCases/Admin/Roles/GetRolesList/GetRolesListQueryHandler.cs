using Matrix.Identity.Application.Abstractions.Persistence;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.GetRolesList
{
    public sealed class GetRolesListQueryHandler(IRoleReadRepository roleReadRepository)
        : IRequestHandler<GetRolesListQuery, IReadOnlyCollection<RoleListItemResult>>
    {
        public async Task<IReadOnlyCollection<RoleListItemResult>> Handle(
            GetRolesListQuery request,
            CancellationToken cancellationToken)
        {
            return await roleReadRepository.GetRolesAsync(cancellationToken);
        }
    }
}
