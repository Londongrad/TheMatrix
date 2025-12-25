using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.AssignUserRoles
{
    public sealed class AssignUserRolesCommandHandler(
        IUserRepository userRepository,
        IUserRolesRepository userRolesRepository,
        IRoleReadRepository roleReadRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<AssignUserRolesCommand>
    {
        public async Task Handle(
            AssignUserRolesCommand request,
            CancellationToken cancellationToken)
        {
            var distinctRoleIds = request.RoleIds
               .Distinct()
               .ToList();

            Task<bool> existsTask = userRepository.ExistsAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            Task<IReadOnlyCollection<Guid>> existingRoleIdsTask =
                distinctRoleIds.Count == 0
                    ? Task.FromResult<IReadOnlyCollection<Guid>>(Array.Empty<Guid>())
                    : roleReadRepository.GetExistingRoleIdsAsync(
                        roleIds: distinctRoleIds,
                        cancellationToken: cancellationToken);

            await Task.WhenAll(
                existsTask,
                existingRoleIdsTask);

            bool exists = existsTask.Result; // после WhenAll безопасно

            if (!exists)
                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            if (distinctRoleIds.Count > 0)
            {
                IReadOnlyCollection<Guid> existingRoleIds = existingRoleIdsTask.Result;

                var missingRoleIds = distinctRoleIds.Except(existingRoleIds)
                   .ToList();

                if (missingRoleIds.Count > 0)
                {
                    var errors = new Dictionary<string, string[]>
                    {
                        ["roleIds"] = new[]
                        {
                            $"Roles not found: {string.Join(separator: ", ", values: missingRoleIds)}"
                        }
                    };

                    throw ApplicationErrorsFactory.ValidationFailed(errors);
                }
            }

            bool changed = await userRolesRepository.ReplaceUserRolesAsync(
                userId: request.UserId,
                roleIds: distinctRoleIds,
                cancellationToken: cancellationToken);

            if (!changed)
                return; // роли уже такие -> не bump'аем и не сохраняем

            await userRepository.BumpPermissionsVersionAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
