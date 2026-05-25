using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Self.Account.ChangeAvatarFromFile
{
    public sealed class ChangeAvatarFromFileCommandValidator : AbstractValidator<ChangeAvatarFromFileCommand>
    {
        public ChangeAvatarFromFileCommandValidator()
        {
            RuleFor(x => x.FileStream)
               .NotNull();

            RuleFor(x => x.FileName)
               .NotEmpty()
               .Must(fileName =>
                {
                    string ext = Path.GetExtension(fileName);
                    return !string.IsNullOrWhiteSpace(ext) &&
                           AvatarUploadConstraints.AllowedExtensions.Contains(ext);
                })
               .WithMessage("Avatar must use one of the supported extensions: .jpg, .jpeg, .png, .webp.");

            RuleFor(x => x.ContentType)
               .NotEmpty()
               .Must(contentType => AvatarUploadConstraints.AllowedContentTypes.Contains(contentType))
               .WithMessage("Avatar content type must be image/jpeg, image/png, or image/webp.");

            RuleFor(x => x.FileSize)
               .GreaterThan(0)
               .LessThanOrEqualTo(AvatarUploadConstraints.MaxFileBytes)
               .WithMessage($"Avatar must not exceed {AvatarUploadConstraints.MaxFileBytes / (1024 * 1024)} MB.");
        }
    }
}
