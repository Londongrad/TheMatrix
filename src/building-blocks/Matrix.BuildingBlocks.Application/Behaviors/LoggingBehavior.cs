using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.BuildingBlocks.Application.Behaviors
{
    public sealed class LoggingBehavior<TRequest, TResponse>(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger = logger;

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            string requestName = typeof(TRequest).Name;

            _logger.LogInformation(message: "Handling request {RequestName}", requestName);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                TResponse response = await next(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    message: "Handled request {RequestName} in {ElapsedMilliseconds} ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    exception: ex,
                    message: "Error handling request {RequestName} after {ElapsedMilliseconds} ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
        }
    }
}
