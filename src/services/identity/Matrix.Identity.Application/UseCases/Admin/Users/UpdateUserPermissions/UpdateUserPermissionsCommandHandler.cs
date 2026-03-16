using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.Administration;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Application.Abstractions.Services.Validation;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Enums;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserPermissions
{
    public sealed class UpdateUserPermissionsCommandHandler(
        IUserRepository userRepository,
        IUserPermissionsRepository permissionsRepository,
        IPermissionKeysValidator permissionKeysValidator,
        IAdminUserGuard adminUserGuard,
        ISecurityStateChangeCollector securityStateChangeCollector,
        IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateUserPermissionsCommand>
    {
        public async Task Handle(
            UpdateUserPermissionsCommand request,
            CancellationToken cancellationToken)
        {
            bool exists = await userRepository.ExistsAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            if (!exists)
                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            await adminUserGuard.EnsureUserCanBeManagedAsync(
                targetUserId: request.UserId,
                cancellationToken: cancellationToken);

            var desiredOverrides = request.Overrides
               .Where(x => !string.IsNullOrWhiteSpace(x.PermissionKey))
               .ToDictionary(
                    keySelector: x => x.PermissionKey.Trim(),
                    elementSelector: x => ParseEffect(x.Effect),
                    comparer: StringComparer.Ordinal);

            await permissionKeysValidator.ValidateAsync(
                permissionKeys: desiredOverrides.Keys.ToArray(),
                cancellationToken: cancellationToken);

            await unitOfWork.ExecuteInTransactionAsync(
                action: async token =>
                {
                    bool changed = await permissionsRepository.ReplaceUserPermissionsAsync(
                        userId: request.UserId,
                        permissionEffects: desiredOverrides,
                        cancellationToken: token);

                    if (!changed)
                        return;

                    securityStateChangeCollector.MarkUserChanged(request.UserId);
                },
                cancellationToken: cancellationToken);
        }

        private static PermissionEffect ParseEffect(string effect)
        {
            return effect.Trim()
               .Equals(
                    value: "Allow",
                    comparisonType: StringComparison.OrdinalIgnoreCase)
                ? PermissionEffect.Allow
                : PermissionEffect.Deny;
        }
    }
}
