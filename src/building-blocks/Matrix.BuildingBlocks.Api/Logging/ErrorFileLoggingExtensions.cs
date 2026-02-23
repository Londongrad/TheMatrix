using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace Matrix.BuildingBlocks.Api.Logging
{
    public static class SerilogLoggingExtensions
    {
        private const string DefaultOutputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}";

        public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            var options = new ErrorFileLoggingOptions();
            builder.Configuration
               .GetSection(ErrorFileLoggingOptions.SectionName)
               .Bind(options);

            builder.Services.AddSerilog((
                services,
                loggerConfiguration) =>
            {
                loggerConfiguration
                   .ReadFrom.Configuration(builder.Configuration)
                   .ReadFrom.Services(services)
                   .Enrich.FromLogContext()
                   .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                   .Enrich.WithProperty("EnvironmentName", builder.Environment.EnvironmentName);

                if (!options.Enabled)
                    return;

                string logsRootDirectory = ResolveLogsRootDirectory(
                    builder: builder,
                    options: options);
                string fileNamePrefix = string.IsNullOrWhiteSpace(options.FileNamePrefix)
                    ? "errors"
                    : SanitizePathSegment(options.FileNamePrefix);
                string applicationDirectoryName = SanitizePathSegment(builder.Environment.ApplicationName);
                string logDirectory = Path.Combine(
                    logsRootDirectory,
                    applicationDirectoryName);

                Directory.CreateDirectory(logDirectory);

                loggerConfiguration.WriteTo.File(
                    path: Path.Combine(logDirectory, $"{fileNamePrefix}-.log"),
                    restrictedToMinimumLevel: ParseLogEventLevel(options.RestrictedToMinimumLevel),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: GetRetainedFileCountLimit(options),
                    outputTemplate: string.IsNullOrWhiteSpace(options.OutputTemplate)
                        ? DefaultOutputTemplate
                        : options.OutputTemplate,
                    shared: options.Shared,
                    buffered: options.Buffered,
                    rollOnFileSizeLimit: options.RollOnFileSizeLimit,
                    fileSizeLimitBytes: options.FileSizeLimitBytes > 0
                        ? options.FileSizeLimitBytes
                        : null,
                    flushToDiskInterval: options.FlushToDiskIntervalSeconds > 0
                        ? TimeSpan.FromSeconds(options.FlushToDiskIntervalSeconds)
                        : null);
            });

            return builder;
        }

        private static int? GetRetainedFileCountLimit(ErrorFileLoggingOptions options)
        {
            if (options.RetainedFileCountLimit is > 0)
                return options.RetainedFileCountLimit.Value;

            return options.RetentionDays > 0
                ? options.RetentionDays
                : null;
        }

        private static LogEventLevel ParseLogEventLevel(string value)
        {
            return Enum.TryParse<LogEventLevel>(
                value: value,
                ignoreCase: true,
                result: out LogEventLevel parsed)
                ? parsed
                : LogEventLevel.Error;
        }

        private static string ResolveLogsRootDirectory(
            WebApplicationBuilder builder,
            ErrorFileLoggingOptions options)
        {
            if (Path.IsPathRooted(options.RootDirectory))
                return options.RootDirectory;

            string relativeRootDirectory = string.IsNullOrWhiteSpace(options.RootDirectory)
                ? "logs"
                : options.RootDirectory;

            string? repositoryRoot = FindRepositoryRoot(builder.Environment.ContentRootPath);
            string baseDirectory = repositoryRoot ?? builder.Environment.ContentRootPath;

            return Path.GetFullPath(
                path: Path.Combine(
                    baseDirectory,
                    relativeRootDirectory));
        }

        private static string? FindRepositoryRoot(string startDirectory)
        {
            DirectoryInfo? current = new DirectoryInfo(startDirectory);

            while (current is not null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, ".git")) ||
                    current.EnumerateFiles("*.sln")
                       .Any())
                    return current.FullName;

                current = current.Parent;
            }

            return null;
        }

        private static string SanitizePathSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unknown-service";

            char[] invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(value
               .Select(ch => invalidChars.Contains(ch)
                    ? '-'
                    : ch)
               .ToArray());

            return string.IsNullOrWhiteSpace(sanitized)
                ? "unknown-service"
                : sanitized;
        }
    }
}
