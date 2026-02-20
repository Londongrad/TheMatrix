using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using MediatR;

namespace Matrix.BuildingBlocks.Application.Behaviors
{
    public sealed class PermissionBehavior<TRequest, TResponse>(
        ICurrentUserContext currentUser,
        IPermissionChecker permissionChecker)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            PermissionRequirement? requirement = CreateRequirement(request);

            if (requirement is null)
                return await next();

            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                throw new MatrixApplicationException(
                    code: "Common.Unauthorized",
                    message: "Authentication is required.",
                    errorType: ApplicationErrorType.Unauthorized,
                    errors: null);

            bool allowed = requirement.MatchMode switch
            {
                PermissionMatchMode.All => await permissionChecker.HasAllAsync(
                    userId: currentUser.UserId.Value,
                    permissions: requirement.PermissionKeys,
                    cancellationToken: cancellationToken),
                _ => await permissionChecker.HasAnyAsync(
                    userId: currentUser.UserId.Value,
                    permissions: requirement.PermissionKeys,
                    cancellationToken: cancellationToken)
            };

            if (!allowed)
                throw new MatrixApplicationException(
                    code: "Common.Forbidden",
                    message: BuildForbiddenMessage(requirement),
                    errorType: ApplicationErrorType.Forbidden,
                    errors: null);

            return await next();
        }

        private static PermissionRequirement? CreateRequirement(TRequest request)
        {
            if (request is IRequirePermissions multi)
            {
                string[] keys = multi.PermissionKeys
                   .Where(key => !string.IsNullOrWhiteSpace(key))
                   .Distinct(StringComparer.Ordinal)
                   .ToArray();

                return keys.Length == 0
                    ? null
                    : new PermissionRequirement(
                        PermissionKeys: keys,
                        MatchMode: multi.PermissionMatchMode);
            }

            if (request is IRequirePermission single && !string.IsNullOrWhiteSpace(single.PermissionKey))
                return new PermissionRequirement(
                    PermissionKeys: [single.PermissionKey],
                    MatchMode: PermissionMatchMode.All);

            return null;
        }

        private static string BuildForbiddenMessage(PermissionRequirement requirement)
        {
            if (requirement.PermissionKeys.Count == 1)
                return $"Permission '{requirement.PermissionKeys[0]}' is required.";

            string joined = string.Join(
                separator: "', '",
                values: requirement.PermissionKeys);

            return requirement.MatchMode == PermissionMatchMode.All
                ? $"All permissions '{joined}' are required."
                : $"Any of permissions '{joined}' is required.";
        }

        private sealed record PermissionRequirement(
            IReadOnlyList<string> PermissionKeys,
            PermissionMatchMode MatchMode);
    }
}
