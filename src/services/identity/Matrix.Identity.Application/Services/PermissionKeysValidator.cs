using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.Validation;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;

namespace Matrix.Identity.Application.Services
{
    public sealed class PermissionKeysValidator(IPermissionReadRepository permissionReadRepository)
        : IPermissionKeysValidator
    {
        public async Task ValidateAsync(
            IReadOnlyCollection<string> permissionKeys,
            CancellationToken cancellationToken)
        {
            if (permissionKeys.Count == 0)
                return;

            IReadOnlyCollection<PermissionCatalogItemResult> catalog =
                await permissionReadRepository.GetPermissionsAsync(cancellationToken);

            var known = new HashSet<string>(StringComparer.Ordinal);
            var deprecated = new HashSet<string>(StringComparer.Ordinal);

            foreach (PermissionCatalogItemResult p in catalog)
            {
                known.Add(p.Key);
                if (p.IsDeprecated)
                    deprecated.Add(p.Key);
            }

            var missing = permissionKeys.Where(k => !known.Contains(k))
               .OrderBy(x => x)
               .ToList();

            if (missing.Count > 0)
                throw ApplicationErrorsFactory.ValidationFailed(
                    new Dictionary<string, string[]>
                    {
                        ["permissionKeys"] = new[]
                        {
                            $"Permissions not found: {string.Join(separator: ", ", values: missing)}"
                        }
                    });

            var deprecatedKeys = permissionKeys.Where(k => deprecated.Contains(k))
               .OrderBy(x => x)
               .ToList();

            if (deprecatedKeys.Count > 0)
                throw ApplicationErrorsFactory.ValidationFailed(
                    new Dictionary<string, string[]>
                    {
                        ["permissionKeys"] = new[]
                        {
                            $"Deprecated permissions: {string.Join(separator: ", ", values: deprecatedKeys)}"
                        }
                    });
        }
    }
}
