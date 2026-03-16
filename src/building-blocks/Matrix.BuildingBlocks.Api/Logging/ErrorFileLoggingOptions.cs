namespace Matrix.BuildingBlocks.Api.Logging
{
    public sealed class ErrorFileLoggingOptions
    {
        public const string SectionName = "Diagnostics:ErrorFileLogging";

        public bool Enabled { get; set; } = true;
        public string RootDirectory { get; set; } = "logs";
        public int RetentionDays { get; set; } = 14;
        public int? RetainedFileCountLimit { get; set; }
        public string FileNamePrefix { get; set; } = "errors";
        public string RestrictedToMinimumLevel { get; set; } = "Error";
        public bool Shared { get; set; } = true;
        public bool Buffered { get; set; }
        public bool RollOnFileSizeLimit { get; set; } = true;
        public long FileSizeLimitBytes { get; set; } = 104857600;
        public int FlushToDiskIntervalSeconds { get; set; } = 1;

        public string OutputTemplate { get; set; } =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}";
    }
}
