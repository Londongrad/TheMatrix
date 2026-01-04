using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.Validation;
using Matrix.Identity.Application.Errors;

namespace Matrix.Identity.Application.Services
{
    public sealed class RoleIdsValidator(IRoleReadRepository roleReadRepository) : IRoleIdsValidator
    {
        public async Task ValidateExistAsync(
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken cancellationToken)
        {
            if (roleIds.Count == 0)
                return;

            IReadOnlyCollection<Guid> existing =
                await roleReadRepository.GetExistingRoleIdsAsync(
                    roleIds: roleIds,
                    cancellationToken: cancellationToken);

            HashSet<Guid> existingSet = existing.Count == 0
                ? []
                : existing.ToHashSet();

            var missing = roleIds.Where(id => !existingSet.Contains(id))
               .ToList();

            if (missing.Count > 0)
                throw ApplicationErrorsFactory.ValidationFailed(
                    new Dictionary<string, string[]>
                    {
                        ["roleIds"] = [$"Roles not found: {string.Join(separator: ", ", values: missing)}"]
                    });
        }
    }
}
