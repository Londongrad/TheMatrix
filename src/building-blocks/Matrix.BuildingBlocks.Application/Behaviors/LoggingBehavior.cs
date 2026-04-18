using System.Diagnostics;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Domain.Exceptions;
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

            logger.LogDebug(
                message: "Handling request {RequestName}",
                requestName);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                TResponse response = await next(cancellationToken);

                stopwatch.Stop();

                logger.LogDebug(
                    message: "Handled request {RequestName} in {ElapsedMilliseconds} ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                logger.LogWarning(
                    exception: ex,
                    message: "Request {RequestName} was canceled after {ElapsedMilliseconds} ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (MatrixApplicationException ex)
            {
                stopwatch.Stop();

                if (ex.ErrorType is ApplicationErrorType.Forbidden or
                    ApplicationErrorType.Unauthorized or
                    ApplicationErrorType.NotFound)
                    logger.LogInformation(
                        message: "Request {RequestName} failed with expected application error {ErrorCode} after {ElapsedMilliseconds} ms",
                        requestName,
                        ex.Code,
                        stopwatch.ElapsedMilliseconds);
                else
                    logger.LogWarning(
                        exception: ex,
                        message: "Request {RequestName} failed with application error {ErrorCode} after {ElapsedMilliseconds} ms",
                        requestName,
                        ex.Code,
                        stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (DomainException ex)
            {
                stopwatch.Stop();

                logger.LogWarning(
                    exception: ex,
                    message: "Request {RequestName} failed with domain error {ErrorCode} after {ElapsedMilliseconds} ms",
                    requestName,
                    ex.Code,
                    stopwatch.ElapsedMilliseconds);

                throw;
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
