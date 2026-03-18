using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IDefaultUserAccessPolicyRepository
    {
        Task<DefaultUserAccessPolicy> GetForUpdateAsync(CancellationToken cancellationToken);

        Task<int> GetVersionAsync(CancellationToken cancellationToken);

        Task<IReadOnlyDictionary<string, PermissionEffect>> GetOverridesAsync(CancellationToken cancellationToken);

        Task<bool> ReplaceOverridesAsync(
            IReadOnlyDictionary<string, PermissionEffect> overrides,
            CancellationToken cancellationToken);
    }
}
