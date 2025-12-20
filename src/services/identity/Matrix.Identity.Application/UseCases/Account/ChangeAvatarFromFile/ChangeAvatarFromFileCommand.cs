using MediatR;

namespace Matrix.Identity.Application.UseCases.Account.ChangeAvatarFromFile
{
    public sealed record ChangeAvatarFromFileCommand(
        Stream FileStream,
        string FileName,
        string ContentType) : IRequest<string>;
}
