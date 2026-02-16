namespace Matrix.Identity.Infrastructure.Integration.Links
{
    public sealed class FrontendLinksOptions
    {
        public const string SectionName = "FrontendLinks";

        public string BaseUrl { get; init; } = "http://localhost:5173";
        public string ConfirmEmailPath { get; init; } = "/confirm-email";
        public string ResetPasswordPath { get; init; } = "/reset-password";
    }
}
