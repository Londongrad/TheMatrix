using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.ChangeAvatarFromFile
{
    public sealed record ChangeAvatarFromFileCommand(
        Stream FileStream,
        string FileName,
        string ContentType) : IRequest<string>;
}
