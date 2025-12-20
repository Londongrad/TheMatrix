using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Account.ChangeAvatarFromFile
{
    public sealed class ChangeAvatarFromFileCommandHandler(
        IUserRepository userRepository,
        IAvatarStorage avatarStorage,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser)
        : IRequestHandler<ChangeAvatarFromFileCommand, string>
    {
        public async Task<string> Handle(
            ChangeAvatarFromFileCommand request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            User user = await userRepository.GetByIdAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            // если была старая аватарка — опционально удалить
            if (!string.IsNullOrEmpty(user.AvatarUrl))
                await avatarStorage.DeleteAsync(
                    path: user.AvatarUrl!,
                    cancellationToken: cancellationToken);

            // сохраняем новый файл
            string newAvatarPath = await avatarStorage.SaveAsync(
                content: request.FileStream,
                fileName: request.FileName,
                contentType: request.ContentType,
                cancellationToken: cancellationToken);

            user.ChangeAvatar(newAvatarPath);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // возвращаем путь/URL
            return newAvatarPath;
        }
    }
}
