using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using MediatR;

namespace Matrix.BuildingBlocks.Application.Behaviors
{
    public sealed class PermissionBehavior<TRequest, TResponse>(ICurrentUserContext currentUser)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is not IRequirePermission secured)
                return await next();

            if (!currentUser.IsAuthenticated)
                throw new MatrixApplicationException(
                    code: "Common.Unauthorized",
                    message: "Authentication is required.",
                    errorType: ApplicationErrorType.Unauthorized,
                    errors: null);

            string required = secured.PermissionKey;

            if (!currentUser.Permissions.Contains(required))
                throw new MatrixApplicationException(
                    code: "Common.Forbidden",
                    message: $"Permission '{required}' is required.",
                    errorType: ApplicationErrorType.Forbidden,
                    errors: null);

            return await next();
        }
    }
}
