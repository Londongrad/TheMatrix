namespace Matrix.ApiGateway.Configurations.Security
{
    public sealed class FrontendSecurityOptions
    {
        public const string SectionName = "FrontendSecurity";

        public bool EnforceCookieOriginProtection { get; set; } = true;

        public string[] AllowedOrigins { get; set; } = [];
    }
}
