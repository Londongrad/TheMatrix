using Matrix.BuildingBlocks.Application.Authorization;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog
{
    public sealed record GetPermissionsCatalogQuery : IRequest<IReadOnlyCollection<PermissionCatalogItemResult>>,
        IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityPermissionsCatalogRead;
    }
}
