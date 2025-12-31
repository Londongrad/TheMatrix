using Matrix.Identity.Application.Abstractions.Persistence;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog
{
    public sealed class GetPermissionsCatalogQueryHandler(IPermissionReadRepository permissionReadRepository)
        : IRequestHandler<GetPermissionsCatalogQuery, IReadOnlyCollection<PermissionCatalogItemResult>>
    {
        public Task<IReadOnlyCollection<PermissionCatalogItemResult>> Handle(
            GetPermissionsCatalogQuery request,
            CancellationToken cancellationToken)
        {
            return permissionReadRepository.GetPermissionsAsync(cancellationToken);
        }
    }
}
