using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
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

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.RegisterUser
{
    public sealed class RegisterUserHandlerTests
    {
        private static readonly DateTime UtcNow = new(
            year: 2047,
            month: 6,
            day: 7,
            hour: 8,
            minute: 9,
            second: 10,
            kind: DateTimeKind.Utc);

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
            RegisterUserHandler handler = CreateHandler(
                userRepository: userRepository,
                userRolesRepository: userRolesRepository,
                roleReadRepository: roleRepository,
                passwordHasher: passwordHasher,
                unitOfWork: unitOfWork);

            RegisterUserResult result = await handler.Handle(
                request: new RegisterUserCommand(
                    Email: "Neo@Matrix.Local",
                    Username: "Neo",
                    Password: "Pa$$w0rd"),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(userRepository.AddedUser);
            Assert.Equal(
                expected: "neo@matrix.local",
                actual: userRepository.AddedUser!.Email.Value);
            Assert.Equal(
                expected: UtcNow,
                actual: userRepository.AddedUser.CreatedAtUtc);
            Assert.Equal(
                expected: "Pa$$w0rd",
                actual: passwordHasher.HashedPasswordInput);
            Assert.Equal(
                expected: "hash::Pa$$w0rd",
                actual: userRepository.AddedUser.PasswordHash);
            Assert.Equal(
                expected: SystemRoleNames.SuperAdmin,
                actual: roleRepository.RequestedRoleNames.Single());
            Assert.Equal(
                expected: userRepository.AddedUser.Id,
                actual: result.UserId);
            Assert.Equal(
                expected: "neo@matrix.local",
                actual: result.Email);
            Assert.Equal(
                expected: "Neo",
                actual: result.Username);
            Assert.Equal(
                expected: userRepository.AddedUser.Id,
                actual: userRolesRepository.ReplacedUserId);
            Assert.Equal(
                expected: new[]
                {
                    assignedRole.Id
                },
                actual: userRolesRepository.ReplacedRoleIds);
            Assert.True(unitOfWork.WasExecuted);
            Assert.Equal(
                expected: IsolationLevel.Serializable,
                actual: unitOfWork.LastIsolationLevel);
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
            RegisterUserHandler handler = CreateHandler(
                userRepository: userRepository,
                userRolesRepository: userRolesRepository,
                roleReadRepository: roleRepository);

            await handler.Handle(
                request: new RegisterUserCommand(
                    Email: "neo@matrix.local",
                    Username: "neo",
                    Password: "Pa$$w0rd"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SystemRoleNames.User,
                actual: roleRepository.RequestedRoleNames.Single());
            Assert.Equal(
                expected: new[]
                {
                    assignedRole.Id
                },
                actual: userRolesRepository.ReplacedRoleIds);
        }

        [Fact]
        public async Task Handle_WhenEmailAlreadyTaken_ThrowsConflictAndDoesNotPersist()
        {
            var userRepository = new FakeUserRepository
            {
                IsEmailTakenAsyncResult = true
            };
            RegisterUserHandler handler = CreateHandler(userRepository: userRepository);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new RegisterUserCommand(
                        Email: "neo@matrix.local",
                        Username: "neo",
                        Password: "Pa$$w0rd"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.EmailAlreadyInUse",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Conflict,
                actual: exception.ErrorType);
            Assert.Null(userRepository.AddedUser);
        }

        [Fact]
        public async Task Handle_WhenUsernameAlreadyTaken_ThrowsConflictAndDoesNotPersist()
        {
            var userRepository = new FakeUserRepository
            {
                IsUsernameTakenAsyncResult = true
            };
            RegisterUserHandler handler = CreateHandler(userRepository: userRepository);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new RegisterUserCommand(
                        Email: "neo@matrix.local",
                        Username: "neo",
                        Password: "Pa$$w0rd"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.UsernameAlreadyInUse",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Conflict,
                actual: exception.ErrorType);
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
            RegisterUserHandler handler = CreateHandler(
                userRepository: userRepository,
                roleReadRepository: roleRepository);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new RegisterUserCommand(
                        Email: "neo@matrix.local",
                        Username: "neo",
                        Password: "Pa$$w0rd"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Role.System.Missing",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.BusinessRule,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: SystemRoleNames.User,
                actual: roleRepository.RequestedRoleNames.Single());
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
                userRepository: userRepository ?? new FakeUserRepository(),
                userRolesRepository: userRolesRepository ?? new FakeUserRolesRepository(),
                roleReadRepository: roleReadRepository ??
                                    new FakeRoleReadRepository
                                    {
                                        RolesByName =
                                        {
                                            [SystemRoleNames.User] = Role.Create(
                                                name: SystemRoleNames.User,
                                                isSystem: true,
                                                createdAtUtc: UtcNow),
                                            [SystemRoleNames.SuperAdmin] = Role.Create(
                                                name: SystemRoleNames.SuperAdmin,
                                                isSystem: true,
                                                createdAtUtc: UtcNow)
                                        }
                                    },
                passwordHasher: passwordHasher ?? new FakePasswordHasher(),
                timeProvider: timeProvider ?? CreateTimeProvider(),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
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

            public Task AddAsync(
                User user,
                CancellationToken cancellationToken = default)
            {
                AddedUser = user;
                return Task.CompletedTask;
            }

            public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(AnyAsyncResult);
            }

            public Task<bool> BumpPermissionsVersionAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<int> BumpPermissionsVersionByRoleAsync(
                Guid roleId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task DeleteAsync(
                User user,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<bool> ExistsAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<User?> GetByEmailAsync(
                string normalizedEmail,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<User?> GetByIdAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<User?> GetByIdWithRefreshTokensAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<User?> GetByPendingEmailAsync(
                string normalizedEmail,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<int?> GetPermissionsVersionAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<User?> GetByRefreshTokenHashAsync(
                string tokenHash,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<User?> GetByUsernameAsync(
                string login,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public IAsyncEnumerable<Guid> StreamUserIdsByRoleAsync(
                Guid roleId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<bool> IsEmailTakenAsync(
                string normalizedEmail,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(IsEmailTakenAsyncResult);
            }

            public Task<bool> IsUsernameTakenAsync(
                string normalizedUsername,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(IsUsernameTakenAsyncResult);
            }
        }

        private sealed class FakeUserRolesRepository : IUserRolesRepository
        {
            public Guid ReplacedUserId { get; private set; }
            public IReadOnlyCollection<Guid>? ReplacedRoleIds { get; private set; }

            public Task<IReadOnlyCollection<UserRoleResult>> GetUserRolesAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<bool> ReplaceUserRolesAsync(
                Guid userId,
                IReadOnlyCollection<Guid> roleIds,
                CancellationToken cancellationToken)
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

            public Task<bool> ExistsAsync(
                Guid roleId,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<bool> ExistsByNameAsync(
                string roleName,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<bool> ExistsByNameExceptAsync(
                string roleName,
                Guid excludedRoleId,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<IReadOnlyCollection<Guid>> GetExistingRoleIdsAsync(
                IReadOnlyCollection<Guid> roleIds,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<Role?> GetByIdAsync(
                Guid roleId,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<Role?> GetByNameAsync(
                string roleName,
                CancellationToken cancellationToken)
            {
                RequestedRoleNames.Add(roleName);
                RolesByName.TryGetValue(
                    key: roleName,
                    value: out Role? role);
                return Task.FromResult(role);
            }

            public Task<IReadOnlyCollection<RoleListItemResult>> GetRolesAsync(CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class FakePasswordHasher : IPasswordHasher
        {
            public string? HashedPasswordInput { get; private set; }

            public string Hash(string password)
            {
                HashedPasswordInput = password;
                return $"hash::{password}";
            }

            public PasswordVerificationOutcome Verify(
                User user,
                string passwordHash,
                string providedPassword)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class FrozenTimeProvider(DateTime utcNow) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow()
            {
                return new DateTimeOffset(
                    dateTime: DateTime.SpecifyKind(
                        value: utcNow,
                        kind: DateTimeKind.Utc),
                    offset: TimeSpan.Zero);
            }
        }

        private sealed class FakeUnitOfWork : IUnitOfWork
        {
            public bool WasExecuted { get; private set; }
            public IsolationLevel LastIsolationLevel { get; private set; }

            public Task ExecuteInTransactionAsync(
                Func<CancellationToken, Task> action,
                CancellationToken cancellationToken,
                IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            {
                WasExecuted = true;
                LastIsolationLevel = isolationLevel;
                return action(cancellationToken);
            }

            public Task<T> ExecuteInTransactionAsync<T>(
                Func<CancellationToken, Task<T>> action,
                CancellationToken cancellationToken,
                IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
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
}
