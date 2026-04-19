namespace Matrix.BuildingBlocks.Infrastructure.DatabaseStartup
{
    public sealed class DatabaseStartupOptions
    {
        public const string SectionName = "DatabaseStartup";

        public bool? ApplyMigrationsOnStartup { get; init; }

        public bool? RunSeedOnStartup { get; init; }
    }
}
