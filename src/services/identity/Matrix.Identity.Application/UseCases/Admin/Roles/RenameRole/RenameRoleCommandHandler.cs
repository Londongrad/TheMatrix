using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.RenameRole
{
    public sealed class RenameRoleCommandHandler(
        IRoleReadRepository roleReadRepository,
        IRoleWriteRepository roleWriteRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<RenameRoleCommand, RoleRenamedResult>
    {
        public async Task<RoleRenamedResult> Handle(
            RenameRoleCommand request,
            CancellationToken cancellationToken)
        {
            Role? role = await roleWriteRepository.GetByIdForUpdateAsync(
                roleId: request.RoleId,
                cancellationToken: cancellationToken);

            if (role is null)
                throw ApplicationErrorsFactory.RoleNotFound(request.RoleId);

            bool exists = await roleReadRepository.ExistsByNameExceptAsync(
                roleName: request.Name,
                excludedRoleId: role.Id,
                cancellationToken: cancellationToken);

            if (exists)
                throw ApplicationErrorsFactory.RoleNameAlreadyInUse(request.Name);

            role.Rename(request.Name);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RoleRenamedResult
            {
                Id = role.Id,
                Name = role.Name,
                IsSystem = role.IsSystem,
                CreatedAtUtc = role.CreatedAtUtc
            };
        }
    }
}
