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

            IConfiguration loggingConfiguration = BuildLoggingConfiguration(builder);

            var options = new ErrorFileLoggingOptions();
            loggingConfiguration
               .GetSection(ErrorFileLoggingOptions.SectionName)
               .Bind(options);

            builder.Host.UseSerilog((
                _,
                services,
                loggerConfiguration) =>
            {
                loggerConfiguration
                   .ReadFrom.Configuration(loggingConfiguration)
                   .ReadFrom.Services(services)
                   .Enrich.FromLogContext()
                   .Enrich.WithProperty(
                        name: "Application",
                        value: builder.Environment.ApplicationName)
                   .Enrich.WithProperty(
                        name: "EnvironmentName",
                        value: builder.Environment.EnvironmentName);

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
                    path1: logsRootDirectory,
                    path2: applicationDirectoryName);

                Directory.CreateDirectory(logDirectory);

                loggerConfiguration.WriteTo.File(
                    path: Path.Combine(
                        path1: logDirectory,
                        path2: $"{fileNamePrefix}-.log"),
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

        private static IConfiguration BuildLoggingConfiguration(WebApplicationBuilder builder)
        {
            string? repositoryRoot = FindRepositoryRoot(builder.Environment.ContentRootPath);

            var sharedConfigurationBuilder = new ConfigurationBuilder();

            if (!string.IsNullOrWhiteSpace(repositoryRoot) &&
                !string.Equals(
                    a: builder.Environment.ContentRootPath,
                    b: repositoryRoot,
                    comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                sharedConfigurationBuilder.AddJsonFile(
                    path: Path.Combine(
                        path1: repositoryRoot,
                        path2: "appsettings.logging.json"),
                    optional: true,
                    reloadOnChange: true);

                sharedConfigurationBuilder.AddJsonFile(
                    path: Path.Combine(
                        path1: repositoryRoot,
                        path2: $"appsettings.logging.{builder.Environment.EnvironmentName}.json"),
                    optional: true,
                    reloadOnChange: true);
            }

            sharedConfigurationBuilder.SetBasePath(builder.Environment.ContentRootPath);

            sharedConfigurationBuilder.AddJsonFile(
                path: "appsettings.logging.json",
                optional: true,
                reloadOnChange: true);

            sharedConfigurationBuilder.AddJsonFile(
                path: $"appsettings.logging.{builder.Environment.EnvironmentName}.json",
                optional: true,
                reloadOnChange: true);

            IConfiguration sharedConfiguration = sharedConfigurationBuilder.Build();

            return new ConfigurationBuilder()
               .AddConfiguration(sharedConfiguration)
               .AddConfiguration(builder.Configuration)
               .Build();
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
            return Enum.TryParse(
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
                    path1: baseDirectory,
                    path2: relativeRootDirectory));
        }

        private static string? FindRepositoryRoot(string startDirectory)
        {
            var current = new DirectoryInfo(startDirectory);

            while (current is not null)
            {
                if (Directory.Exists(
                        Path.Combine(
                            path1: current.FullName,
                            path2: ".git")) ||
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
            string sanitized = new(
                value
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
