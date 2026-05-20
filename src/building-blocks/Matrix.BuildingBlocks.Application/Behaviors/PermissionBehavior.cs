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
                return await next(cancellationToken);

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

            return await next(cancellationToken);
        }

        private static PermissionRequirement? CreateRequirement(TRequest request)
        {
            if (request is IRequirePermissions multi)
            {
                if (multi.PermissionKeys is null)
                    throw CreateInvalidPermissionConfigurationException(
                        message: "PermissionKeys collection must not be null.");

                string[] rawKeys = multi.PermissionKeys.ToArray();

                if (rawKeys.Length == 0)
                    throw CreateInvalidPermissionConfigurationException(
                        message: "PermissionKeys collection must not be empty.");

                if (rawKeys.Any(string.IsNullOrWhiteSpace))
                    throw CreateInvalidPermissionConfigurationException(
                        message: "PermissionKeys collection must not contain null, empty, or whitespace keys.");

                string[] keys = rawKeys
                   .Distinct(StringComparer.Ordinal)
                   .ToArray();

                return new PermissionRequirement(
                    PermissionKeys: keys,
                    MatchMode: multi.PermissionMatchMode);
            }

            if (request is IRequirePermission single)
            {
                if (string.IsNullOrWhiteSpace(single.PermissionKey))
                    throw CreateInvalidPermissionConfigurationException(
                        message: "PermissionKey must not be null, empty, or whitespace.");

                return new PermissionRequirement(
                    PermissionKeys: [single.PermissionKey],
                    MatchMode: PermissionMatchMode.All);
            }

            return null;
        }

        private static InvalidOperationException CreateInvalidPermissionConfigurationException(string message)
        {
            return new InvalidOperationException(
                $"Request '{typeof(TRequest).FullName}' declares a permission requirement, but it is invalid: {message}");
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
