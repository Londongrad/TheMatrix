using System.Data;
using System.Runtime.CompilerServices;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Application.Abstractions.Services.Validation;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolesList;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Matrix.Identity.Domain.Entities;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles;

internal static class AdminRolesTestSupport
{
    internal static readonly DateTime UtcNow = new(2047, 7, 1, 9, 10, 11, DateTimeKind.Utc);

    internal static TimeProvider CreateTimeProvider(DateTime? utcNow = null)
    {
        return new FrozenTimeProvider(utcNow ?? UtcNow);
    }

    internal static Role CreateRole(
        string name = "Operators",
        bool isSystem = false,
        DateTime? createdAtUtc = null)
    {
        return Role.Create(
            name: name,
            isSystem: isSystem,
            createdAtUtc: createdAtUtc ?? UtcNow);
    }

    internal sealed class FakeRoleReadRepository : IRoleReadRepository
    {
        public List<RoleListItemResult> Roles { get; } = new();
        public Dictionary<Guid, Role> RolesById { get; } = new();
        public HashSet<string> ExistingNames { get; } = new(StringComparer.Ordinal);
        public List<Guid> ExistsRequests { get; } = new();
        public List<string> ExistsByNameRequests { get; } = new();
        public List<(string RoleName, Guid ExcludedRoleId)> ExistsByNameExceptRequests { get; } = new();

        public Task<IReadOnlyCollection<RoleListItemResult>> GetRolesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((IReadOnlyCollection<RoleListItemResult>)Roles.ToArray());
        }

        public Task<IReadOnlyCollection<Guid>> GetExistingRoleIdsAsync(
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken cancellationToken)
        {
            Guid[] result = roleIds
                .Where(RolesById.ContainsKey)
                .ToArray();

            return Task.FromResult((IReadOnlyCollection<Guid>)result);
        }

        public Task<bool> ExistsAsync(
            Guid roleId,
            CancellationToken cancellationToken)
        {
            ExistsRequests.Add(roleId);
            return Task.FromResult(RolesById.ContainsKey(roleId));
        }

        public Task<bool> ExistsByNameAsync(
            string roleName,
            CancellationToken cancellationToken)
        {
            ExistsByNameRequests.Add(roleName);
            return Task.FromResult(ExistingNames.Contains(roleName));
        }

        public Task<bool> ExistsByNameExceptAsync(
            string roleName,
            Guid excludedRoleId,
            CancellationToken cancellationToken)
        {
            ExistsByNameExceptRequests.Add((roleName, excludedRoleId));
            bool exists = RolesById.Values.Any(x => x.Id != excludedRoleId && x.Name == roleName) || ExistingNames.Contains(roleName);
            return Task.FromResult(exists);
        }

        public Task<Role?> GetByIdAsync(
            Guid roleId,
            CancellationToken cancellationToken)
        {
            RolesById.TryGetValue(roleId, out Role? role);
            return Task.FromResult(role);
        }

        public Task<Role?> GetByNameAsync(
            string roleName,
            CancellationToken cancellationToken)
        {
            Role? role = RolesById.Values.SingleOrDefault(x => x.Name == roleName);
            return Task.FromResult(role);
        }
    }

    internal sealed class FakeRoleWriteRepository : IRoleWriteRepository
    {
        public Dictionary<Guid, Role> RolesById { get; } = new();
        public List<Role> AddedRoles { get; } = new();
        public List<Role> DeletedRoles { get; } = new();
        public Guid? RequestedRoleId { get; private set; }

        public Task AddAsync(
            Role role,
            CancellationToken cancellationToken)
        {
            AddedRoles.Add(role);
            RolesById[role.Id] = role;
            return Task.CompletedTask;
        }

        public Task<Role?> GetByIdForUpdateAsync(
            Guid roleId,
            CancellationToken cancellationToken)
        {
            RequestedRoleId = roleId;
            RolesById.TryGetValue(roleId, out Role? role);
            return Task.FromResult(role);
        }

        public Task DeleteAsync(
            Role role,
            CancellationToken cancellationToken)
        {
            DeletedRoles.Add(role);
            RolesById.Remove(role.Id);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeRolePermissionsRepository : IRolePermissionsRepository
    {
        public bool ReplaceResult { get; set; }
        public IReadOnlyCollection<string> GetRolePermissionsResult { get; set; } = Array.Empty<string>();
        public Guid? RequestedRoleId { get; private set; }
        public IReadOnlyCollection<string>? RequestedPermissionKeys { get; private set; }

        public Task<IReadOnlyCollection<string>> GetRolePermissionsAsync(
            Guid roleId,
            CancellationToken cancellationToken)
        {
            RequestedRoleId = roleId;
            return Task.FromResult(GetRolePermissionsResult);
        }

        public Task<bool> ReplaceRolePermissionsAsync(
            Guid roleId,
            IReadOnlyCollection<string> permissionKeys,
            CancellationToken cancellationToken)
        {
            RequestedRoleId = roleId;
            RequestedPermissionKeys = permissionKeys.ToArray();
            return Task.FromResult(ReplaceResult);
        }
    }

    internal sealed class FakeRoleMembersReadRepository : IRoleMembersReadRepository
    {
        public Guid? RequestedRoleId { get; private set; }
        public Pagination? RequestedPagination { get; private set; }
        public PagedResult<UserListItemResult> Result { get; set; } = new(
            items: Array.Empty<UserListItemResult>(),
            totalCount: 0,
            pageNumber: 1,
            pageSize: 10);

        public Task<PagedResult<UserListItemResult>> GetRoleMembersPageAsync(
            Guid roleId,
            Pagination pagination,
            CancellationToken cancellationToken)
        {
            RequestedRoleId = roleId;
            RequestedPagination = pagination;
            return Task.FromResult(Result);
        }
    }

    internal sealed class FakeUserRepository : IUserRepository
    {
        public Dictionary<Guid, IReadOnlyCollection<Guid>> UserIdsByRoleId { get; } = new();
        public Guid? RequestedRoleId { get; private set; }

        public Task<IReadOnlyCollection<Guid>> GetUserIdsByRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            RequestedRoleId = roleId;
            UserIdsByRoleId.TryGetValue(roleId, out IReadOnlyCollection<Guid>? userIds);
            return Task.FromResult(userIds ?? (IReadOnlyCollection<Guid>)Array.Empty<Guid>());
        }

        public async IAsyncEnumerable<Guid> StreamUserIdsByRoleAsync(
            Guid roleId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            RequestedRoleId = roleId;
            UserIdsByRoleId.TryGetValue(roleId, out IReadOnlyCollection<Guid>? userIds);

            foreach (Guid userId in userIds ?? Array.Empty<Guid>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return userId;
                await Task.Yield();
            }
        }

        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdWithRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByPendingEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUsernameAsync(string login, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsEmailTakenAsync(string normalizedEmail, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsUsernameTakenAsync(string normalizedUsername, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int?> GetPermissionsVersionAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> BumpPermissionsVersionAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> BumpPermissionsVersionByRoleAsync(Guid roleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    internal sealed class FakePermissionKeysValidator : IPermissionKeysValidator
    {
        public IReadOnlyCollection<string>? ValidatedKeys { get; private set; }
        public Exception? ValidateException { get; set; }

        public Task ValidateAsync(
            IReadOnlyCollection<string> permissionKeys,
            CancellationToken cancellationToken)
        {
            ValidatedKeys = permissionKeys.ToArray();
            if (ValidateException is not null)
                throw ValidateException;
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeSecurityStateChangeCollector : ISecurityStateChangeCollector
    {
        public List<Guid> ChangedUsers { get; } = new();
        public bool DefaultUserAccessChanged { get; private set; }

        public void MarkUserChanged(Guid userId)
        {
            ChangedUsers.Add(userId);
        }

        public void MarkDefaultUserAccessChanged()
        {
            DefaultUserAccessChanged = true;
        }

        public IReadOnlyCollection<Guid> DrainUsers()
        {
            Guid[] users = ChangedUsers.ToArray();
            ChangedUsers.Clear();
            return users;
        }

        public bool DrainDefaultUserAccessChanged()
        {
            bool changed = DefaultUserAccessChanged;
            DefaultUserAccessChanged = false;
            return changed;
        }
    }

    internal sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }
        public int TransactionCalls { get; private set; }
        public IsolationLevel? LastIsolationLevel { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            TransactionCalls++;
            LastIsolationLevel = isolationLevel;
            return action(cancellationToken);
        }

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            TransactionCalls++;
            LastIsolationLevel = isolationLevel;
            return action(cancellationToken);
        }
    }

    private sealed class FrozenTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(
                DateTime.SpecifyKind(utcNow, DateTimeKind.Utc),
                TimeSpan.Zero);
        }
    }
}
