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
            // лучше сразу HashSet (и дубликаты убрали, и Contains быстрый)
            var desiredRoleIds = request.RoleIds.ToHashSet();

            // 1) user exists
            bool exists = await userRepository.ExistsAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            if (!exists)
                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            // 2) validate roles (только если что-то передали)
            if (desiredRoleIds.Count > 0)
            {
                IReadOnlyCollection<Guid> existingRoleIds =
                    await roleReadRepository.GetExistingRoleIdsAsync(
                        roleIds: desiredRoleIds,
                        cancellationToken: cancellationToken);

                // missing = desired - existing
                var missingRoleIds = desiredRoleIds
                   .Except(existingRoleIds)
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

            // 3) replace roles (repo сам сравнивает и возвращает changed)
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
