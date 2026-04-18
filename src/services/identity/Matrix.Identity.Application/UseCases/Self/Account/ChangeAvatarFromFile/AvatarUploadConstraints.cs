namespace Matrix.Identity.Application.UseCases.Self.Account.ChangeAvatarFromFile
{
    public static class AvatarUploadConstraints
    {
        public const long MaxFileBytes = 2 * 1024 * 1024;

        public static readonly IReadOnlySet<string> AllowedExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

        public static readonly IReadOnlySet<string> AllowedContentTypes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };
    }
}
