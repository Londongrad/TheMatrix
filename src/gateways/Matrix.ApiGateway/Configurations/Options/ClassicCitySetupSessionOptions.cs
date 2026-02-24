namespace Matrix.ApiGateway.Configurations.Options
{
    public sealed class ClassicCitySetupSessionOptions
    {
        public const string SectionName = "ClassicCitySetupSessions";

        public int CacheTtlHours { get; init; } = 168;
    }
}
