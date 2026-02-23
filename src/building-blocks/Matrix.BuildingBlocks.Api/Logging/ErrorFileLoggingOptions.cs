namespace Matrix.BuildingBlocks.Api.Logging
{
    public sealed class ErrorFileLoggingOptions
    {
        public const string SectionName = "Diagnostics:ErrorFileLogging";

        public bool Enabled { get; set; } = true;
        public string RootDirectory { get; set; } = "logs";
        public int RetentionDays { get; set; } = 14;
        public string FileNamePrefix { get; set; } = "errors";
    }
}
