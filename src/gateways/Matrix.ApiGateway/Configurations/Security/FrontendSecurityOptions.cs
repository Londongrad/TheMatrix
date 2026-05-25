namespace Matrix.ApiGateway.Configurations.Security
{
    public sealed class FrontendSecurityOptions
    {
        public const string SectionName = "FrontendSecurity";

        public static readonly string[] DevelopmentLocalAllowedOrigins =
        [
            "https://localhost:5173",
            "http://localhost:5173"
        ];

        public bool EnforceCookieOriginProtection { get; set; } = true;

        public string[] AllowedOrigins { get; set; } = [];
    }
}
