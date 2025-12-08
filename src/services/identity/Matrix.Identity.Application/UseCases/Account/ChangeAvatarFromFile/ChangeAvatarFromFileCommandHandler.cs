using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Account.ChangeAvatarFromFile
{
    public sealed class ChangeAvatarFromFileCommandHandler(
        IUserRepository userRepository,
        IAvatarStorage avatarStorage)
        : IRequestHandler<ChangeAvatarFromFileCommand, string>
    {
        private readonly IAvatarStorage _avatarStorage = avatarStorage;
        private readonly IUserRepository _userRepository = userRepository;

        public async Task<string> Handle(
            ChangeAvatarFromFileCommand request,
            CancellationToken cancellationToken)
        {
            User user = await _userRepository.GetByIdAsync(userId: request.UserId, cancellationToken: cancellationToken)
                        ?? throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            // если была старая аватарка — опционально удалить
            if (!string.IsNullOrEmpty(user.AvatarUrl))
                await _avatarStorage.DeleteAsync(path: user.AvatarUrl!, cancellationToken: cancellationToken);

            // сохраняем новый файл
            string newAvatarPath = await _avatarStorage.SaveAsync(
                content: request.FileStream,
                fileName: request.FileName,
                contentType: request.ContentType,
                cancellationToken: cancellationToken);

            user.ChangeAvatar(newAvatarPath);

            await _userRepository.SaveChangesAsync(cancellationToken);

            // возвращаем путь/URL
            return newAvatarPath;
        }
    }
}
