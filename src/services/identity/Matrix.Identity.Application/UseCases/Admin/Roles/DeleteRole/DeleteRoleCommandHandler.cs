using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.DeleteRole
{
    public sealed class DeleteRoleCommandHandler(
        IRoleWriteRepository roleWriteRepository,
        IUserRepository userRepository,
        ISecurityStateChangeCollector securityStateChangeCollector,
        IUnitOfWork unitOfWork)
        : IRequestHandler<DeleteRoleCommand>
    {
        public async Task Handle(
            DeleteRoleCommand request,
            CancellationToken cancellationToken)
        {
            Role? role = await roleWriteRepository.GetByIdForUpdateAsync(
                roleId: request.RoleId,
                cancellationToken: cancellationToken);

            if (role is null)
                throw ApplicationErrorsFactory.RoleNotFound(request.RoleId);

            await unitOfWork.ExecuteInTransactionAsync(
                action: async token =>
                {
                    IReadOnlyCollection<Guid> affectedUsers = await userRepository.GetUserIdsByRoleAsync(
                        roleId: role.Id,
                        cancellationToken: token);

                    foreach (Guid userId in affectedUsers)
                        securityStateChangeCollector.MarkUserChanged(userId);

                    await roleWriteRepository.DeleteAsync(
                        role: role,
                        cancellationToken: token);
                },
                cancellationToken: cancellationToken);
        }
    }
}
