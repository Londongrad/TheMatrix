using System.Data;
using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolesList;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles;
using Matrix.Identity.Application.UseCases.Self.Auth.RegisterUser;
using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.RegisterUser;

public sealed class RegisterUserHandlerTests
{
    private static readonly DateTime UtcNow = new(2047, 6, 7, 8, 9, 10, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_WhenFirstUser_AssignsSuperAdminAndPersistsUser()
    {
        var userRepository = new FakeUserRepository
        {
            AnyAsyncResult = false
        };
        var assignedRole = Role.Create(
            name: SystemRoleNames.SuperAdmin,
            isSystem: true,
            createdAtUtc: UtcNow);
        var roleRepository = new FakeRoleReadRepository
        {
            RolesByName =
            {
                [SystemRoleNames.SuperAdmin] = assignedRole
            }
        };
        var userRolesRepository = new FakeUserRolesRepository();
        var passwordHasher = new FakePasswordHasher();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            userRepository: userRepository,
            userRolesRepository: userRolesRepository,
            roleReadRepository: roleRepository,
            passwordHasher: passwordHasher,
            unitOfWork: unitOfWork);

        var result = await handler.Handle(
            new RegisterUserCommand(
                Email: "Neo@Matrix.Local",
                Username: "Neo",
                Password: "Pa$$w0rd"),
            CancellationToken.None);

        Assert.NotNull(userRepository.AddedUser);
        Assert.Equal("neo@matrix.local", userRepository.AddedUser!.Email.Value);
        Assert.Equal(UtcNow, userRepository.AddedUser.CreatedAtUtc);
        Assert.Equal("Pa$$w0rd", passwordHasher.HashedPasswordInput);
        Assert.Equal("hash::Pa$$w0rd", userRepository.AddedUser.PasswordHash);
        Assert.Equal(SystemRoleNames.SuperAdmin, roleRepository.RequestedRoleNames.Single());
        Assert.Equal(userRepository.AddedUser.Id, result.UserId);
        Assert.Equal("neo@matrix.local", result.Email);
        Assert.Equal("Neo", result.Username);
        Assert.Equal(userRepository.AddedUser.Id, userRolesRepository.ReplacedUserId);
        Assert.Equal(new[] { assignedRole.Id }, userRolesRepository.ReplacedRoleIds);
        Assert.True(unitOfWork.WasExecuted);
        Assert.Equal(IsolationLevel.Serializable, unitOfWork.LastIsolationLevel);
    }

    [Fact]
    public async Task Handle_WhenUsersAlreadyExist_AssignsUserRole()
    {
        var userRepository = new FakeUserRepository
        {
            AnyAsyncResult = true
        };
        var assignedRole = Role.Create(
            name: SystemRoleNames.User,
            isSystem: true,
            createdAtUtc: UtcNow);
        var roleRepository = new FakeRoleReadRepository
        {
            RolesByName =
            {
                [SystemRoleNames.User] = assignedRole
            }
        };
        var userRolesRepository = new FakeUserRolesRepository();
        var handler = CreateHandler(
            userRepository: userRepository,
            userRolesRepository: userRolesRepository,
            roleReadRepository: roleRepository);

        await handler.Handle(
            new RegisterUserCommand(
                Email: "neo@matrix.local",
                Username: "neo",
                Password: "Pa$$w0rd"),
            CancellationToken.None);

        Assert.Equal(SystemRoleNames.User, roleRepository.RequestedRoleNames.Single());
        Assert.Equal(new[] { assignedRole.Id }, userRolesRepository.ReplacedRoleIds);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyTaken_ThrowsConflictAndDoesNotPersist()
    {
        var userRepository = new FakeUserRepository
        {
            IsEmailTakenAsyncResult = true
        };
        var handler = CreateHandler(userRepository: userRepository);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new RegisterUserCommand(
                Email: "neo@matrix.local",
                Username: "neo",
                Password: "Pa$$w0rd"),
            CancellationToken.None));

        Assert.Equal("Identity.EmailAlreadyInUse", exception.Code);
        Assert.Equal(ApplicationErrorType.Conflict, exception.ErrorType);
        Assert.Null(userRepository.AddedUser);
    }

    [Fact]
    public async Task Handle_WhenUsernameAlreadyTaken_ThrowsConflictAndDoesNotPersist()
    {
        var userRepository = new FakeUserRepository
        {
            IsUsernameTakenAsyncResult = true
        };
        var handler = CreateHandler(userRepository: userRepository);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new RegisterUserCommand(
                Email: "neo@matrix.local",
                Username: "neo",
                Password: "Pa$$w0rd"),
            CancellationToken.None));

        Assert.Equal("Identity.UsernameAlreadyInUse", exception.Code);
        Assert.Equal(ApplicationErrorType.Conflict, exception.ErrorType);
        Assert.Null(userRepository.AddedUser);
    }

    [Fact]
    public async Task Handle_WhenRequiredSystemRoleMissing_ThrowsBusinessRule()
    {
        var userRepository = new FakeUserRepository
        {
            AnyAsyncResult = true
        };
        var roleRepository = new FakeRoleReadRepository();
        var handler = CreateHandler(
            userRepository: userRepository,
            roleReadRepository: roleRepository);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new RegisterUserCommand(
                Email: "neo@matrix.local",
                Username: "neo",
                Password: "Pa$$w0rd"),
            CancellationToken.None));

        Assert.Equal("Identity.Role.System.Missing", exception.Code);
        Assert.Equal(ApplicationErrorType.BusinessRule, exception.ErrorType);
        Assert.Equal(SystemRoleNames.User, roleRepository.RequestedRoleNames.Single());
    }

    private static RegisterUserHandler CreateHandler(
        FakeUserRepository? userRepository = null,
        FakeUserRolesRepository? userRolesRepository = null,
        FakeRoleReadRepository? roleReadRepository = null,
        FakePasswordHasher? passwordHasher = null,
        FakeUnitOfWork? unitOfWork = null,
        TimeProvider? timeProvider = null)
    {
        return new RegisterUserHandler(
            userRepository ?? new FakeUserRepository(),
            userRolesRepository ?? new FakeUserRolesRepository(),
            roleReadRepository ?? new FakeRoleReadRepository
            {
                RolesByName =
                {
                    [SystemRoleNames.User] = Role.Create(SystemRoleNames.User, true, UtcNow),
                    [SystemRoleNames.SuperAdmin] = Role.Create(SystemRoleNames.SuperAdmin, true, UtcNow)
                }
            },
            passwordHasher ?? new FakePasswordHasher(),
            timeProvider ?? CreateTimeProvider(),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private static TimeProvider CreateTimeProvider(DateTime? utcNow = null)
    {
        return new FrozenTimeProvider(utcNow ?? UtcNow);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public bool AnyAsyncResult { get; init; }
        public bool IsEmailTakenAsyncResult { get; init; }
        public bool IsUsernameTakenAsyncResult { get; init; }
        public User? AddedUser { get; private set; }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            AddedUser = user;
            return Task.CompletedTask;
        }

        public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => Task.FromResult(AnyAsyncResult);
        public Task<bool> BumpPermissionsVersionAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> BumpPermissionsVersionByRoleAsync(Guid roleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdWithRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByPendingEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int?> GetPermissionsVersionAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUsernameAsync(string login, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<Guid> StreamUserIdsByRoleAsync(Guid roleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsEmailTakenAsync(string normalizedEmail, CancellationToken cancellationToken = default) => Task.FromResult(IsEmailTakenAsyncResult);
        public Task<bool> IsUsernameTakenAsync(string normalizedUsername, CancellationToken cancellationToken = default) => Task.FromResult(IsUsernameTakenAsyncResult);
    }

    private sealed class FakeUserRolesRepository : IUserRolesRepository
    {
        public Guid ReplacedUserId { get; private set; }
        public IReadOnlyCollection<Guid>? ReplacedRoleIds { get; private set; }

        public Task<IReadOnlyCollection<UserRoleResult>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken)
        {
            ReplacedUserId = userId;
            ReplacedRoleIds = roleIds.ToArray();
            return Task.FromResult(true);
        }
    }

    private sealed class FakeRoleReadRepository : IRoleReadRepository
    {
        public Dictionary<string, Role> RolesByName { get; } = new(StringComparer.Ordinal);
        public List<string> RequestedRoleNames { get; } = new();

        public Task<bool> ExistsAsync(Guid roleId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> ExistsByNameAsync(string roleName, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> ExistsByNameExceptAsync(string roleName, Guid excludedRoleId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Guid>> GetExistingRoleIdsAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken)
        {
            RequestedRoleNames.Add(roleName);
            RolesByName.TryGetValue(roleName, out var role);
            return Task.FromResult(role);
        }

        public Task<IReadOnlyCollection<RoleListItemResult>> GetRolesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string? HashedPasswordInput { get; private set; }

        public string Hash(string password)
        {
            HashedPasswordInput = password;
            return $"hash::{password}";
        }

        public PasswordVerificationOutcome Verify(User user, string passwordHash, string providedPassword)
        {
            throw new NotSupportedException();
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

    private sealed class FakeUnitOfWork : Matrix.BuildingBlocks.Application.Abstractions.IUnitOfWork
    {
        public bool WasExecuted { get; private set; }
        public IsolationLevel LastIsolationLevel { get; private set; }

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            WasExecuted = true;
            LastIsolationLevel = isolationLevel;
            return action(cancellationToken);
        }

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            WasExecuted = true;
            LastIsolationLevel = isolationLevel;
            return action(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
