using System.Text;
using Microsoft.Extensions.Logging;

namespace Matrix.BuildingBlocks.Api.Logging
{
    internal sealed class ErrorFileLoggerProvider(
        string applicationName,
        string logDirectory,
        string fileNamePrefix,
        int retentionDays) : ILoggerProvider
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false);

        private readonly string _applicationName = applicationName;
        private readonly string _fileNamePrefix = fileNamePrefix;
        private readonly string _logDirectory = logDirectory;
        private readonly int _retentionDays = retentionDays;
        private readonly object _syncRoot = new();
        private bool _disposed;

        public ILogger CreateLogger(string categoryName)
        {
            return new ErrorFileLogger(
                categoryName: categoryName,
                writeEntry: WriteEntry);
        }

        public void Dispose()
        {
            _disposed = true;
        }

        private void WriteEntry(ErrorFileLogEntry entry)
        {
            if (_disposed)
                return;

            try
            {
                Directory.CreateDirectory(_logDirectory);
                CleanupOldFiles(entry.TimestampUtc.UtcDateTime);

                string filePath = Path.Combine(
                    _logDirectory,
                    $"{_fileNamePrefix}-{entry.TimestampUtc:yyyy-MM-dd}.log");
                string payload = FormatEntry(entry);

                lock (_syncRoot)
                {
                    using var stream = new FileStream(
                        path: filePath,
                        mode: FileMode.Append,
                        access: FileAccess.Write,
                        share: FileShare.ReadWrite);
                    using var writer = new StreamWriter(
                        stream: stream,
                        encoding: Utf8NoBom);

                    writer.Write(payload);
                }
            }
            catch (IOException)
            {
                // Logging must never fail the request pipeline.
            }
            catch (UnauthorizedAccessException)
            {
                // Logging must never fail the request pipeline.
            }
        }

        private void CleanupOldFiles(DateTime utcNow)
        {
            if (_retentionDays <= 0)
                return;

            DateTime thresholdUtc = utcNow.Date.AddDays(-_retentionDays);

            foreach (string filePath in Directory.EnumerateFiles(
                         _logDirectory,
                         $"{_fileNamePrefix}-*.log"))
            {
                try
                {
                    DateTime lastWriteUtc = File.GetLastWriteTimeUtc(filePath);
                    if (lastWriteUtc < thresholdUtc)
                        File.Delete(filePath);
                }
                catch (IOException)
                {
                    // Ignore cleanup races; logging should never fail because of stale files.
                }
                catch (UnauthorizedAccessException)
                {
                    // Ignore cleanup races; logging should never fail because of stale files.
                }
            }
        }

        private string FormatEntry(ErrorFileLogEntry entry)
        {
            var builder = new StringBuilder(capacity: 512);

            builder.AppendLine("============================================================");
            builder.Append("Timestamp: ")
               .AppendLine(entry.TimestampUtc.ToString("O"));
            builder.Append("Application: ")
               .AppendLine(_applicationName);
            builder.Append("Level: ")
               .AppendLine(entry.LogLevel.ToString());
            builder.Append("Category: ")
               .AppendLine(entry.CategoryName);

            if (entry.EventId.Id != 0 || !string.IsNullOrWhiteSpace(entry.EventId.Name))
            {
                builder.Append("EventId: ")
                   .Append(entry.EventId.Id);

                if (!string.IsNullOrWhiteSpace(entry.EventId.Name))
                    builder.Append(" (")
                       .Append(entry.EventId.Name)
                       .Append(')');

                builder.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(entry.Message))
            {
                builder.AppendLine("Message:")
                   .AppendLine(entry.Message.Trim());
            }

            if (entry.Exception is not null)
            {
                builder.AppendLine("Exception:")
                   .AppendLine(entry.Exception.ToString());
            }

            builder.AppendLine();

            return builder.ToString();
        }

        private sealed record ErrorFileLogEntry(
            DateTimeOffset TimestampUtc,
            LogLevel LogLevel,
            string CategoryName,
            EventId EventId,
            string Message,
            Exception? Exception);

        private sealed class ErrorFileLogger(
            string categoryName,
            Action<ErrorFileLogEntry> writeEntry) : ILogger
        {
            private readonly string _categoryName = categoryName;
            private readonly Action<ErrorFileLogEntry> _writeEntry = writeEntry;

            public IDisposable BeginScope<TState>(TState state)
                where TState : notnull
            {
                return NoopScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel != LogLevel.None;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                ArgumentNullException.ThrowIfNull(formatter);

                if (!IsEnabled(logLevel))
                    return;

                if (exception is null && logLevel < LogLevel.Error)
                    return;

                string message = formatter(state, exception);
                if (string.IsNullOrWhiteSpace(message) && exception is null)
                    return;

                _writeEntry(
                    new ErrorFileLogEntry(
                        TimestampUtc: DateTimeOffset.UtcNow,
                        LogLevel: logLevel,
                        CategoryName: _categoryName,
                        EventId: eventId,
                        Message: message,
                        Exception: exception));
            }
        }

        private sealed class NoopScope : IDisposable
        {
            public static NoopScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
