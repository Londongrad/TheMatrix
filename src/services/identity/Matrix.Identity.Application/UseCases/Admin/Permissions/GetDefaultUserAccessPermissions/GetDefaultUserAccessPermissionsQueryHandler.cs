using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Permissions.GetDefaultUserAccessPermissions
{
    public sealed class GetDefaultUserAccessPermissionsQueryHandler(
        IRoleReadRepository roleReadRepository,
        IRolePermissionsRepository rolePermissionsRepository,
        IDefaultUserAccessPolicyRepository defaultUserAccessPolicyRepository)
        : IRequestHandler<GetDefaultUserAccessPermissionsQuery, DefaultUserAccessPermissionsResult>
    {
        public async Task<DefaultUserAccessPermissionsResult> Handle(
            GetDefaultUserAccessPermissionsQuery request,
            CancellationToken cancellationToken)
        {
            Role? userRole = await roleReadRepository.GetByNameAsync(
                roleName: SystemRoleNames.User,
                cancellationToken: cancellationToken);

            if (userRole is null)
                throw ApplicationErrorsFactory.RequiredSystemRoleMissing(SystemRoleNames.User);

            var effective = (await rolePermissionsRepository.GetRolePermissionsAsync(
                    roleId: userRole.Id,
                    cancellationToken: cancellationToken))
               .ToHashSet(StringComparer.Ordinal);

            IReadOnlyDictionary<string, PermissionEffect> overrides =
                await defaultUserAccessPolicyRepository.GetOverridesAsync(cancellationToken);

            foreach ((string permissionKey, PermissionEffect effect) in overrides)
                if (effect == PermissionEffect.Deny)
                    effective.Remove(permissionKey);

            foreach ((string permissionKey, PermissionEffect effect) in overrides)
                if (effect == PermissionEffect.Allow)
                    effective.Add(permissionKey);

            string[] permissionKeys = effective
               .OrderBy(
                    keySelector: x => x,
                    comparer: StringComparer.Ordinal)
               .ToArray();

            int version = await defaultUserAccessPolicyRepository.GetVersionAsync(cancellationToken);

            return new DefaultUserAccessPermissionsResult(
                Version: version,
                PermissionKeys: permissionKeys);
        }
    }
}
