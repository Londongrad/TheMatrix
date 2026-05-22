using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Application.Abstractions.Services.Validation;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Permissions.UpdateDefaultUserAccessPermissions
{
    public sealed class UpdateDefaultUserAccessPermissionsCommandHandler(
        IRoleReadRepository roleReadRepository,
        IRolePermissionsRepository rolePermissionsRepository,
        IDefaultUserAccessPolicyRepository defaultUserAccessPolicyRepository,
        IPermissionKeysValidator permissionKeysValidator,
        TimeProvider timeProvider,
        ISecurityStateChangeCollector securityStateChangeCollector,
        IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateDefaultUserAccessPermissionsCommand>
    {
        public async Task Handle(
            UpdateDefaultUserAccessPermissionsCommand request,
            CancellationToken cancellationToken)
        {
            Role? userRole = await roleReadRepository.GetByNameAsync(
                roleName: SystemRoleNames.User,
                cancellationToken: cancellationToken);

            if (userRole is null)
                throw ApplicationErrorsFactory.RequiredSystemRoleMissing(SystemRoleNames.User);

            var desiredKeys = request.PermissionKeys
               .Where(key => !string.IsNullOrWhiteSpace(key))
               .Select(key => key.Trim())
               .ToHashSet(StringComparer.Ordinal);

            await permissionKeysValidator.ValidateAsync(
                permissionKeys: desiredKeys,
                cancellationToken: cancellationToken);

            HashSet<string> basePermissionKeys = (await rolePermissionsRepository.GetRolePermissionsAsync(
                    roleId: userRole.Id,
                    cancellationToken: cancellationToken))
               .ToHashSet(StringComparer.Ordinal);

            var desiredOverrides = new Dictionary<string, PermissionEffect>(StringComparer.Ordinal);

            foreach (string permissionKey in desiredKeys)
                if (!basePermissionKeys.Contains(permissionKey))
                    desiredOverrides[permissionKey] = PermissionEffect.Allow;

            foreach (string permissionKey in basePermissionKeys)
                if (!desiredKeys.Contains(permissionKey))
                    desiredOverrides[permissionKey] = PermissionEffect.Deny;

            await unitOfWork.ExecuteInTransactionAsync(
                action: async token =>
                {
                    bool changed = await defaultUserAccessPolicyRepository.ReplaceOverridesAsync(
                        overrides: desiredOverrides,
                        cancellationToken: token);

                    if (!changed)
                        return;

                    DefaultUserAccessPolicy policy = await defaultUserAccessPolicyRepository.GetForUpdateAsync(token);
                    policy.Touch(timeProvider.GetUtcNow().UtcDateTime);
                    securityStateChangeCollector.MarkDefaultUserAccessChanged();
                },
                cancellationToken: cancellationToken);
        }
    }
}
