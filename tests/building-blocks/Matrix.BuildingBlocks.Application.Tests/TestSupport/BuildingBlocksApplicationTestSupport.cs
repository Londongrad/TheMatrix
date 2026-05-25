using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;

namespace Matrix.BuildingBlocks.Application.Tests.TestSupport
{
    internal sealed class TestCurrentUserContext : ICurrentUserContext
    {
        public bool IsAuthenticated { get; init; }
        public Guid? UserId { get; init; }
        public Guid? SessionId { get; init; }
    }

    internal sealed class TestValidationExceptionFactory : IValidationExceptionFactory
    {
        public Type? LastRequestType { get; private set; }
        public IReadOnlyDictionary<string, string[]>? LastErrors { get; private set; }

        public MatrixApplicationException Create(
            Type requestType,
            IReadOnlyDictionary<string, string[]> errors)
        {
            LastRequestType = requestType;
            LastErrors = errors;

            return new MatrixApplicationException(
                code: "Common.ValidationFailed",
                message: $"Validation failed for {requestType.Name}.",
                errorType: ApplicationErrorType.Validation,
                errors: errors);
        }
    }

    internal sealed class TestPermissionChecker : IPermissionChecker
    {
        public bool HasResult { get; init; }
        public bool HasAnyResult { get; init; }
        public bool HasAllResult { get; init; }

        public Guid? LastUserId { get; private set; }
        public IReadOnlyCollection<string>? LastPermissions { get; private set; }
        public string? LastPermission { get; private set; }
        public string? LastMethod { get; private set; }

        public Task<bool> HasAsync(
            Guid userId,
            string permission,
            CancellationToken cancellationToken)
        {
            LastUserId = userId;
            LastPermission = permission;
            LastMethod = nameof(HasAsync);
            return Task.FromResult(HasResult);
        }

        public Task<bool> HasAnyAsync(
            Guid userId,
            IReadOnlyCollection<string> permissions,
            CancellationToken cancellationToken)
        {
            LastUserId = userId;
            LastPermissions = permissions.ToArray();
            LastMethod = nameof(HasAnyAsync);
            return Task.FromResult(HasAnyResult);
        }

        public Task<bool> HasAllAsync(
            Guid userId,
            IReadOnlyCollection<string> permissions,
            CancellationToken cancellationToken)
        {
            LastUserId = userId;
            LastPermissions = permissions.ToArray();
            LastMethod = nameof(HasAllAsync);
            return Task.FromResult(HasAllResult);
        }
    }

    internal sealed record PlainRequest(string Name = "matrix");

    internal sealed record ProtectedSinglePermissionRequest(string PermissionKey) : IRequirePermission;

    internal sealed record ProtectedMultiPermissionRequest(
        IReadOnlyCollection<string> PermissionKeys,
        PermissionMatchMode PermissionMatchMode) : IRequirePermissions;
}
