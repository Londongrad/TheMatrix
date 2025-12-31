using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
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
            User user = await userRepository.GetByIdAsync(
                            userId: request.UserId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            List<Guid> distinctRoleIds = request.RoleIds
               .Distinct()
               .ToList();

            IReadOnlyCollection<Guid> existingRoleIds = await roleReadRepository
               .GetExistingRoleIdsAsync(distinctRoleIds, cancellationToken);

            List<Guid> missingRoleIds = distinctRoleIds
               .Except(existingRoleIds)
               .ToList();

            if (missingRoleIds.Count > 0)
            {
                var errors = new Dictionary<string, string[]>
                {
                    ["roleIds"] = new[]
                    {
                        $"Roles not found: {string.Join(", ", missingRoleIds)}"
                    }
                };

                throw ApplicationErrorsFactory.ValidationFailed(errors);
            }

            await userRolesRepository.ReplaceUserRolesAsync(
                userId: request.UserId,
                roleIds: distinctRoleIds,
                ct: cancellationToken);

            user.BumpPermissionsVersion();

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
