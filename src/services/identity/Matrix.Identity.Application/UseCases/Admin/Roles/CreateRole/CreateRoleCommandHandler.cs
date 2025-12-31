using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.CreateRole
{
    public sealed class CreateRoleCommandHandler(
        IRoleReadRepository roleReadRepository,
        IRoleWriteRepository roleWriteRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<CreateRoleCommand, RoleCreatedResult>
    {
        public async Task<RoleCreatedResult> Handle(
            CreateRoleCommand request,
            CancellationToken cancellationToken)
        {
            var role = Role.Create(
                name: request.Name,
                isSystem: false);

            bool exists = await roleReadRepository.ExistsByNameAsync(
                roleName: role.Name,
                cancellationToken: cancellationToken);

            if (exists)
                throw ApplicationErrorsFactory.RoleNameAlreadyInUse(role.Name);

            await roleWriteRepository.AddAsync(
                role: role,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RoleCreatedResult
            {
                Id = role.Id,
                Name = role.Name,
                IsSystem = role.IsSystem,
                CreatedAtUtc = role.CreatedAtUtc
            };
        }
    }
}
