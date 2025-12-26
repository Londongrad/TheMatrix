using System.Linq.Expressions;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Matrix.Identity.Domain.Entities;

namespace Matrix.Identity.Infrastructure.Persistence.Projections
{
    internal static class PermissionProjections
    {
        public static readonly Expression<Func<Permission, PermissionCatalogItemResult>> ToCatalogItem =
            p => new PermissionCatalogItemResult
            {
                Key = p.Key,
                Service = p.Service,
                Group = p.Group,
                Description = p.Description,
                IsDeprecated = p.IsDeprecated
            };
    }
}
