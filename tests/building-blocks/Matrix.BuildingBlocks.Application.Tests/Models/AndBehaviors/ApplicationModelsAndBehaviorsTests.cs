using FluentValidation;
using FluentValidation.Results;
using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Behaviors;
using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.BuildingBlocks.Application.Tests.TestSupport;
using Matrix.BuildingBlocks.Application.Validation;
using Matrix.BuildingBlocks.Domain.Exceptions;
using Xunit;

namespace Matrix.BuildingBlocks.Application.Tests.Models.AndBehaviors
{
    public sealed class ApplicationModelsAndBehaviorsTests
    {
        [Fact]
        public void Pagination_WhenConstructedWithValidValues_ComputesSkipAndDeconstructs()
        {
            Pagination pagination = new(
                pageNumber: 3,
                pageSize: 25);

            pagination.Deconstruct(
                pageNumber: out int pageNumber,
                pageSize: out int pageSize);

            Assert.Equal(
                expected: 50,
                actual: pagination.Skip);
            Assert.Equal(
                expected: 3,
                actual: pageNumber);
            Assert.Equal(
                expected: 25,
                actual: pageSize);
        }

        [Fact]
        public void Pagination_WhenConstructedWithInvalidValues_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() => new Pagination(
                pageNumber: 0,
                pageSize: 25));
            Assert.Throws<DomainException>(() => new Pagination(
                pageNumber: 1,
                pageSize: Pagination.MaxPageSize + 1));
        }

        [Fact]
        public void PaginationValidator_WhenValuesAreInvalid_ReturnsValidationFailures()
        {
            PaginationValidator validator = new();
            var pagination = new Pagination(
                pageNumber: 1,
                pageSize: 10)
            {
                PageNumber = 0,
                PageSize = Pagination.MaxPageSize + 1
            };

            ValidationResult? result = validator.Validate(pagination);

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == nameof(Pagination.PageNumber));
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == nameof(Pagination.PageSize));
        }

        [Fact]
        public async Task ValidationBehavior_WhenValidatorsPass_InvokesNext()
        {
            ValidationBehavior<PlainRequest, string> behavior = new(
                validators: [new PlainRequestValidator()],
                errorFactory: new TestValidationExceptionFactory());

            string response = await behavior.Handle(
                request: new PlainRequest(),
                next: _ => Task.FromResult("ok"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "ok",
                actual: response);
        }

        [Fact]
        public async Task ValidationBehavior_WhenValidatorsFail_GroupsErrorsAndThrowsFactoryException()
        {
            TestValidationExceptionFactory factory = new();
            ValidationBehavior<PlainRequest, string> behavior = new(
                validators:
                [
                    new PlainRequestValidator(),
                    new PlainRequestLengthValidator()
                ],
                errorFactory: factory);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => behavior.Handle(
                    request: new PlainRequest(string.Empty),
                    next: _ => Task.FromResult("ok"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: ApplicationErrorType.Validation,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: typeof(PlainRequest),
                actual: factory.LastRequestType);
            Assert.NotNull(factory.LastErrors);
            Assert.Contains(
                expected: "Name is required.",
                collection: factory.LastErrors![nameof(PlainRequest.Name)]);
            Assert.Contains(
                expected: "Name must be at least 2 characters.",
                collection: factory.LastErrors[nameof(PlainRequest.Name)]);
        }

        [Fact]
        public async Task PermissionBehavior_WhenRequestHasNoPermissions_InvokesNext()
        {
            PermissionBehavior<PlainRequest, string> behavior = new(
                currentUser: new TestCurrentUserContext(),
                permissionChecker: new TestPermissionChecker());

            string response = await behavior.Handle(
                request: new PlainRequest(),
                next: _ => Task.FromResult("ok"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "ok",
                actual: response);
        }

        [Fact]
        public async Task PermissionBehavior_WhenUserIsUnauthenticated_ThrowsUnauthorized()
        {
            PermissionBehavior<ProtectedSinglePermissionRequest, string> behavior = new(
                currentUser: new TestCurrentUserContext(),
                permissionChecker: new TestPermissionChecker());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => behavior.Handle(
                    request: new ProtectedSinglePermissionRequest("users.read"),
                    next: _ => Task.FromResult("ok"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: ApplicationErrorType.Unauthorized,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: "Common.Unauthorized",
                actual: exception.Code);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public async Task PermissionBehavior_WhenSinglePermissionKeyIsBlank_ThrowsInvalidOperationException(
            string permissionKey)
        {
            bool nextCalled = false;
            PermissionBehavior<ProtectedSinglePermissionRequest, string> behavior = new(
                currentUser: new TestCurrentUserContext(),
                permissionChecker: new TestPermissionChecker());

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => behavior.Handle(
                    request: new ProtectedSinglePermissionRequest(permissionKey),
                    next: _ =>
                    {
                        nextCalled = true;
                        return Task.FromResult("ok");
                    },
                    cancellationToken: CancellationToken.None));

            Assert.False(nextCalled);
            Assert.Contains(
                expectedSubstring: nameof(ProtectedSinglePermissionRequest),
                actualString: exception.Message,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "permission",
                actualString: exception.Message,
                comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PermissionBehavior_WhenSinglePermissionIsGranted_UsesHasAllAndInvokesNext()
        {
            var userId = Guid.NewGuid();
            TestPermissionChecker checker = new()
            {
                HasAllResult = true
            };
            PermissionBehavior<ProtectedSinglePermissionRequest, string> behavior = new(
                currentUser: new TestCurrentUserContext
                {
                    IsAuthenticated = true,
                    UserId = userId
                },
                permissionChecker: checker);

            string response = await behavior.Handle(
                request: new ProtectedSinglePermissionRequest("users.read"),
                next: _ => Task.FromResult("ok"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "ok",
                actual: response);
            Assert.Equal(
                expected: nameof(IPermissionChecker.HasAllAsync),
                actual: checker.LastMethod);
            Assert.Equal(
                expected: userId,
                actual: checker.LastUserId);
            Assert.Equal(
                expected: ["users.read"],
                actual: checker.LastPermissions);
        }

        [Fact]
        public async Task PermissionBehavior_WhenMultiPermissionKeysAreNull_ThrowsInvalidOperationException()
        {
            bool nextCalled = false;
            PermissionBehavior<ProtectedMultiPermissionRequest, string> behavior = new(
                currentUser: new TestCurrentUserContext(),
                permissionChecker: new TestPermissionChecker());

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => behavior.Handle(
                    request: new ProtectedMultiPermissionRequest(
                        PermissionKeys: null!,
                        PermissionMatchMode: PermissionMatchMode.Any),
                    next: _ =>
                    {
                        nextCalled = true;
                        return Task.FromResult("ok");
                    },
                    cancellationToken: CancellationToken.None));

            Assert.False(nextCalled);
            Assert.Contains(
                expectedSubstring: nameof(ProtectedMultiPermissionRequest),
                actualString: exception.Message,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "permission",
                actualString: exception.Message,
                comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PermissionBehavior_WhenMultiPermissionKeysAreEmpty_ThrowsInvalidOperationException()
        {
            bool nextCalled = false;
            PermissionBehavior<ProtectedMultiPermissionRequest, string> behavior = new(
                currentUser: new TestCurrentUserContext(),
                permissionChecker: new TestPermissionChecker());

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => behavior.Handle(
                    request: new ProtectedMultiPermissionRequest(
                        PermissionKeys: [],
                        PermissionMatchMode: PermissionMatchMode.Any),
                    next: _ =>
                    {
                        nextCalled = true;
                        return Task.FromResult("ok");
                    },
                    cancellationToken: CancellationToken.None));

            Assert.False(nextCalled);
            Assert.Contains(
                expectedSubstring: nameof(ProtectedMultiPermissionRequest),
                actualString: exception.Message,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "permission",
                actualString: exception.Message,
                comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public async Task PermissionBehavior_WhenMultiPermissionKeysContainBlank_ThrowsInvalidOperationException(
            string permissionKey)
        {
            bool nextCalled = false;
            PermissionBehavior<ProtectedMultiPermissionRequest, string> behavior = new(
                currentUser: new TestCurrentUserContext(),
                permissionChecker: new TestPermissionChecker());

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => behavior.Handle(
                    request: new ProtectedMultiPermissionRequest(
                        PermissionKeys:
                        [
                            "users.read",
                            permissionKey
                        ],
                        PermissionMatchMode: PermissionMatchMode.Any),
                    next: _ =>
                    {
                        nextCalled = true;
                        return Task.FromResult("ok");
                    },
                    cancellationToken: CancellationToken.None));

            Assert.False(nextCalled);
            Assert.Contains(
                expectedSubstring: nameof(ProtectedMultiPermissionRequest),
                actualString: exception.Message,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "permission",
                actualString: exception.Message,
                comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PermissionBehavior_WhenMultiPermissionKeysContainNull_ThrowsInvalidOperationException()
        {
            bool nextCalled = false;
            PermissionBehavior<ProtectedMultiPermissionRequest, string> behavior = new(
                currentUser: new TestCurrentUserContext(),
                permissionChecker: new TestPermissionChecker());

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => behavior.Handle(
                    request: new ProtectedMultiPermissionRequest(
                        PermissionKeys:
                        [
                            "users.read",
                            null!
                        ],
                        PermissionMatchMode: PermissionMatchMode.Any),
                    next: _ =>
                    {
                        nextCalled = true;
                        return Task.FromResult("ok");
                    },
                    cancellationToken: CancellationToken.None));

            Assert.False(nextCalled);
            Assert.Contains(
                expectedSubstring: nameof(ProtectedMultiPermissionRequest),
                actualString: exception.Message,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "permission",
                actualString: exception.Message,
                comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PermissionBehavior_WhenMultiPermissionKeysContainDuplicates_DeduplicatesAndChecksPermissions()
        {
            var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            TestPermissionChecker checker = new()
            {
                HasAnyResult = true
            };
            PermissionBehavior<ProtectedMultiPermissionRequest, string> behavior = new(
                currentUser: new TestCurrentUserContext
                {
                    IsAuthenticated = true,
                    UserId = userId
                },
                permissionChecker: checker);

            string response = await behavior.Handle(
                request: new ProtectedMultiPermissionRequest(
                    PermissionKeys:
                    [
                        "users.read",
                        "users.read"
                    ],
                    PermissionMatchMode: PermissionMatchMode.Any),
                next: _ => Task.FromResult("ok"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "ok",
                actual: response);
            Assert.Equal(
                expected: nameof(IPermissionChecker.HasAnyAsync),
                actual: checker.LastMethod);
            Assert.Equal(
                expected: userId,
                actual: checker.LastUserId);
            Assert.Equal(
                expected: ["users.read"],
                actual: checker.LastPermissions);
        }

        [Fact]
        public async Task PermissionBehavior_WhenAnyPermissionIsRequiredAndMissing_ThrowsForbidden()
        {
            PermissionBehavior<ProtectedMultiPermissionRequest, string> behavior = new(
                currentUser: new TestCurrentUserContext
                {
                    IsAuthenticated = true,
                    UserId = Guid.NewGuid()
                },
                permissionChecker: new TestPermissionChecker
                {
                    HasAnyResult = false
                });

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => behavior.Handle(
                    request: new ProtectedMultiPermissionRequest(
                        PermissionKeys:
                        [
                            "users.read",
                            "users.write"
                        ],
                        PermissionMatchMode: PermissionMatchMode.Any),
                    next: _ => Task.FromResult("ok"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: ApplicationErrorType.Forbidden,
                actual: exception.ErrorType);
            Assert.Contains(
                expectedSubstring: "Any of permissions",
                actualString: exception.Message,
                comparisonType: StringComparison.Ordinal);
        }

        private sealed class PlainRequestValidator : AbstractValidator<PlainRequest>
        {
            public PlainRequestValidator()
            {
                RuleFor(x => x.Name)
                   .NotEmpty()
                   .WithMessage("Name is required.");
            }
        }

        private sealed class PlainRequestLengthValidator : AbstractValidator<PlainRequest>
        {
            public PlainRequestLengthValidator()
            {
                RuleFor(x => x.Name)
                   .MinimumLength(2)
                   .WithMessage("Name must be at least 2 characters.");
            }
        }
    }
}
