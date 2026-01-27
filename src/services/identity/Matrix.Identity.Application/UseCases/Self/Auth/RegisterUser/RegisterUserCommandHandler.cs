using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.RegisterUser
{
    public sealed class RegisterUserHandler(
        IUserRepository userRepository,
        IUserRolesRepository userRolesRepository,
        IRoleReadRepository roleReadRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
        : IRequestHandler<RegisterUserCommand, RegisterUserResult>
    {
        public async Task<RegisterUserResult> Handle(
            RegisterUserCommand request,
            CancellationToken cancellationToken)
        {
            // Create value objects first so normalization and validation run once.
            var email = Email.Create(request.Email);
            var username = Username.Create(request.Username);

            return await unitOfWork.ExecuteInTransactionAsync(
                action: async token =>
                {
                    bool emailTaken = await userRepository
                       .IsEmailTakenAsync(
                            normalizedEmail: email.Value,
                            cancellationToken: token);

                    if (emailTaken)
                        throw ApplicationErrorsFactory.EmailAlreadyInUse(email.Value);

                    bool usernameTaken = await userRepository
                       .IsUsernameTakenAsync(
                            normalizedUsername: username.Value,
                            cancellationToken: token);

                    if (usernameTaken)
                        throw ApplicationErrorsFactory.UsernameAlreadyInUse(username.Value);

                    bool hasUsers = await userRepository.AnyAsync(token);
                    string assignedRoleName = hasUsers
                        ? SystemRoleNames.User
                        : SystemRoleNames.SuperAdmin;

                    Role assignedRole = await roleReadRepository.GetByNameAsync(
                                            roleName: assignedRoleName,
                                            cancellationToken: token) ??
                                        throw ApplicationErrorsFactory.RequiredSystemRoleMissing(
                                            assignedRoleName);

                    string passwordHash = passwordHasher.Hash(request.Password);

                    var user = User.CreateNew(
                        email: email,
                        username: username,
                        passwordHash: passwordHash);

                    await userRepository.AddAsync(
                        user: user,
                        cancellationToken: token);

                    await userRolesRepository.ReplaceUserRolesAsync(
                        userId: user.Id,
                        roleIds: new[] { assignedRole.Id },
                        cancellationToken: token);

                    return new RegisterUserResult
                    {
                        UserId = user.Id,
                        Username = user.Username.Value,
                        Email = user.Email.Value
                    };
                },
                cancellationToken: cancellationToken,
                isolationLevel: IsolationLevel.Serializable);
        }
    }
}