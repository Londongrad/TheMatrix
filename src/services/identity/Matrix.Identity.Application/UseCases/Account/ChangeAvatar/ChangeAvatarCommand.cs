using MediatR;

namespace Matrix.Identity.Application.UseCases.Account.ChangeAvatar
{
    public sealed record ChangeAvatarCommand(
        Guid UserId,
        string? AvatarUrl
    ) : IRequest;
}
