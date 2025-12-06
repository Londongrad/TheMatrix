using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Account.ChangeAvatar
{
    public sealed class ChangeAvatarCommandValidator
        : AbstractValidator<ChangeAvatarCommand>
    {
        public ChangeAvatarCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User id is required.");

            RuleFor(x => x.AvatarUrl)
                .MaximumLength(512)
                .WithMessage("Avatar URL must be at most 512 characters long.")
                .Must(BeValidUrl)
                .WithMessage("Avatar URL must be a valid http or https URL.")
                .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));
        }

        private static bool BeValidUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return true; // null/пустая строка допустима (сброс аватара)

            if (!Uri.TryCreate(uriString: url, uriKind: UriKind.Absolute, result: out Uri? uri)) return false;

            // вариант 1: через Uri.* (обычные сравнения)
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

            // вариант 2: pattern matching, можно так:
            //return uri.Scheme is "http" or "https";
        }
    }
}
