using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Application.Abstractions.Services.Validation;
using Matrix.Identity.Application.Errors;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserRoles
{
    public sealed class UpdateUserRolesCommandHandler(
        IUserRepository userRepository,
        IUserRolesRepository userRolesRepository,
        IRoleIdsValidator roleIdsValidator,
        IUnitOfWork unitOfWork,
        ISecurityStateChangeCollector securityStateChangeCollector)
        : IRequestHandler<UpdateUserRolesCommand>
    {
        public async Task Handle(
            UpdateUserRolesCommand request,
            CancellationToken cancellationToken)
        {
            var desiredRoleIds = request.RoleIds.ToHashSet();

            if (!await userRepository.ExistsAsync(
                    userId: request.UserId,
                    cancellationToken: cancellationToken))
                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            await roleIdsValidator.ValidateExistAsync(
                roleIds: desiredRoleIds,
                cancellationToken: cancellationToken);

            await unitOfWork.ExecuteInTransactionAsync(
                action: async token =>
                {
                    bool changed = await userRolesRepository.ReplaceUserRolesAsync(
                        userId: request.UserId,
                        roleIds: desiredRoleIds,
                        cancellationToken: token);

                    if (!changed)
                        return;

                    securityStateChangeCollector.MarkUserChanged(request.UserId);
                },
                cancellationToken: cancellationToken);
        }
    }
}
