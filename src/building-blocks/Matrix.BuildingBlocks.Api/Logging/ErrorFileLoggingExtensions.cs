using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Matrix.BuildingBlocks.Api.Logging
{
    public static class ErrorFileLoggingExtensions
    {
        public static WebApplicationBuilder AddErrorFileLogging(this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            var options = new ErrorFileLoggingOptions();
            builder.Configuration
               .GetSection(ErrorFileLoggingOptions.SectionName)
               .Bind(options);

            if (!options.Enabled)
                return builder;

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

            builder.Logging.AddProvider(
                new ErrorFileLoggerProvider(
                    applicationName: builder.Environment.ApplicationName,
                    logDirectory: logDirectory,
                    fileNamePrefix: fileNamePrefix,
                    retentionDays: options.RetentionDays));

            return builder;
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
