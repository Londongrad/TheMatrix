using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.BuildingBlocks.Application.Behaviors
{
    public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            string requestName = typeof(TRequest).Name;

            logger.LogInformation(
                message: "Handling request {RequestName}",
                requestName);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                TResponse response = await next(cancellationToken);

                stopwatch.Stop();

                logger.LogInformation(
                    message: "Handled request {RequestName} in {ElapsedMilliseconds} ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                logger.LogError(
                    exception: ex,
                    message: "Error handling request {RequestName} after {ElapsedMilliseconds} ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
        }
    }
}
