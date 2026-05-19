using FluentValidation;
using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Behaviors;
using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.BuildingBlocks.Application.Tests.TestSupport;
using Matrix.BuildingBlocks.Application.Validation;
using Matrix.BuildingBlocks.Domain.Exceptions;
using Xunit;

namespace Matrix.BuildingBlocks.Application.Tests.Models.AndBehaviors;

public sealed class ApplicationModelsAndBehaviorsTests
{
    [Fact]
    public void Pagination_WhenConstructedWithValidValues_ComputesSkipAndDeconstructs()
    {
        Pagination pagination = new(pageNumber: 3, pageSize: 25);

        pagination.Deconstruct(out int pageNumber, out int pageSize);

        Assert.Equal(50, pagination.Skip);
        Assert.Equal(3, pageNumber);
        Assert.Equal(25, pageSize);
    }

    [Fact]
    public void Pagination_WhenConstructedWithInvalidValues_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new Pagination(pageNumber: 0, pageSize: 25));
        Assert.Throws<DomainException>(() => new Pagination(pageNumber: 1, pageSize: Pagination.MaxPageSize + 1));
    }

    [Fact]
    public void PaginationValidator_WhenValuesAreInvalid_ReturnsValidationFailures()
    {
        PaginationValidator validator = new();
        Pagination pagination = new Pagination(pageNumber: 1, pageSize: 10)
        {
            PageNumber = 0,
            PageSize = Pagination.MaxPageSize + 1
        };

        var result = validator.Validate(pagination);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(Pagination.PageNumber));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(Pagination.PageSize));
    }

    [Fact]
    public async Task ValidationBehavior_WhenValidatorsPass_InvokesNext()
    {
        ValidationBehavior<PlainRequest, string> behavior = new(
            validators: [new PlainRequestValidator()],
            errorFactory: new TestValidationExceptionFactory());

        string response = await behavior.Handle(
            request: new PlainRequest("matrix"),
            next: _ => Task.FromResult("ok"),
            cancellationToken: CancellationToken.None);

        Assert.Equal("ok", response);
    }

    [Fact]
    public async Task ValidationBehavior_WhenValidatorsFail_GroupsErrorsAndThrowsFactoryException()
    {
        TestValidationExceptionFactory factory = new();
        ValidationBehavior<PlainRequest, string> behavior = new(
            validators: [new PlainRequestValidator(), new PlainRequestLengthValidator()],
            errorFactory: factory);

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(
            () => behavior.Handle(
                request: new PlainRequest(string.Empty),
                next: _ => Task.FromResult("ok"),
                cancellationToken: CancellationToken.None));

        Assert.Equal(ApplicationErrorType.Validation, exception.ErrorType);
        Assert.Equal(typeof(PlainRequest), factory.LastRequestType);
        Assert.NotNull(factory.LastErrors);
        Assert.Contains("Name is required.", factory.LastErrors![nameof(PlainRequest.Name)]);
        Assert.Contains("Name must be at least 2 characters.", factory.LastErrors[nameof(PlainRequest.Name)]);
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

        Assert.Equal("ok", response);
    }

    [Fact]
    public async Task PermissionBehavior_WhenUserIsUnauthenticated_ThrowsUnauthorized()
    {
        PermissionBehavior<ProtectedSinglePermissionRequest, string> behavior = new(
            currentUser: new TestCurrentUserContext(),
            permissionChecker: new TestPermissionChecker());

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(
            () => behavior.Handle(
                request: new ProtectedSinglePermissionRequest("users.read"),
                next: _ => Task.FromResult("ok"),
                cancellationToken: CancellationToken.None));

        Assert.Equal(ApplicationErrorType.Unauthorized, exception.ErrorType);
        Assert.Equal("Common.Unauthorized", exception.Code);
    }

    [Fact]
    public async Task PermissionBehavior_WhenSinglePermissionIsGranted_UsesHasAllAndInvokesNext()
    {
        Guid userId = Guid.NewGuid();
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

        Assert.Equal("ok", response);
        Assert.Equal(nameof(IPermissionChecker.HasAllAsync), checker.LastMethod);
        Assert.Equal(userId, checker.LastUserId);
        Assert.Equal(["users.read"], checker.LastPermissions);
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

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(
            () => behavior.Handle(
                request: new ProtectedMultiPermissionRequest(
                    PermissionKeys: ["users.read", "users.write"],
                    PermissionMatchMode: PermissionMatchMode.Any),
                next: _ => Task.FromResult("ok"),
                cancellationToken: CancellationToken.None));

        Assert.Equal(ApplicationErrorType.Forbidden, exception.ErrorType);
        Assert.Contains("Any of permissions", exception.Message, StringComparison.Ordinal);
    }

    private sealed class PlainRequestValidator : AbstractValidator<PlainRequest>
    {
        public PlainRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        }
    }

    private sealed class PlainRequestLengthValidator : AbstractValidator<PlainRequest>
    {
        public PlainRequestLengthValidator()
        {
            RuleFor(x => x.Name).MinimumLength(2).WithMessage("Name must be at least 2 characters.");
        }
    }
}
