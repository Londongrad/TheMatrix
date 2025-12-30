using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.Validation;
using Matrix.Identity.Application.Errors;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserRoles
{
    public sealed class UpdateUserRolesCommandHandler(
        IUserRepository userRepository,
        IUserRolesRepository userRolesRepository,
        IRoleIdsValidator roleIdsValidator,
        IUnitOfWork unitOfWork)
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

            bool changed = await userRolesRepository.ReplaceUserRolesAsync(
                userId: request.UserId,
                roleIds: desiredRoleIds,
                cancellationToken: cancellationToken);

            if (!changed)
                return;

            await userRepository.BumpPermissionsVersionAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
