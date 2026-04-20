using System.Net;
using Matrix.BuildingBlocks.Api.Errors;
using Matrix.BuildingBlocks.Api.Exceptions;
using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Matrix.BuildingBlocks.Api.Middleware
{
    public sealed class ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (DomainException ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Handled domain exception with code {Code}",
                    ex.Code);

                IReadOnlyDictionary<string, string[]>? errors = null;

                if (ex.PropertyName is not null)
                    errors = new Dictionary<string, string[]>
                    {
                        [ex.PropertyName] = [ex.Message]
                    };

                await ApiProblemDetailsFactory.WriteAsync(
                    context: context,
                    statusCode: (int)HttpStatusCode.BadRequest,
                    code: ex.Code,
                    message: ex.Message,
                    errors: errors,
                    cancellationToken: context.RequestAborted);
            }
            catch (MatrixApplicationException ex)
            {
                if (ex.ErrorType is ApplicationErrorType.Forbidden or
                    ApplicationErrorType.Unauthorized or
                    ApplicationErrorType.NotFound)
                    logger.LogInformation(
                        message: "Handled expected application exception with code {Code}",
                        ex.Code);
                else
                    logger.LogWarning(
                        exception: ex,
                        message: "Handled application exception with code {Code}",
                        ex.Code);

                HttpStatusCode statusCode = MapToHttpStatusCode(ex.ErrorType);

                await ApiProblemDetailsFactory.WriteAsync(
                    context: context,
                    statusCode: (int)statusCode,
                    code: ex.Code,
                    message: ex.Message,
                    errors: ex.Errors,
                    cancellationToken: context.RequestAborted);
            }
            catch (MatrixInfrastructureException ex)
            {
                logger.LogError(
                    exception: ex,
                    message: "Handled infrastructure exception with code {Code}",
                    ex.Code);

                HttpStatusCode statusCode = MapToHttpStatusCode(ex.ErrorType);

                await ApiProblemDetailsFactory.WriteAsync(
                    context: context,
                    statusCode: (int)statusCode,
                    code: ex.Code,
                    message: ex.Message,
                    errors: ex.Details,
                    cancellationToken: context.RequestAborted);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Invalid argument");

                await ApiProblemDetailsFactory.WriteAsync(
                    context: context,
                    statusCode: (int)HttpStatusCode.BadRequest,
                    code: "Common.InvalidArgument",
                    message: ex.Message,
                    cancellationToken: context.RequestAborted);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Invalid operation");

                await ApiProblemDetailsFactory.WriteAsync(
                    context: context,
                    statusCode: (int)HttpStatusCode.BadRequest,
                    code: "Common.InvalidOperation",
                    message: ex.Message,
                    cancellationToken: context.RequestAborted);
            }
            catch (TaskCanceledException ex) when (!context.RequestAborted.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Gateway timeout while calling downstream service");

                await ApiProblemDetailsFactory.WriteAsync(
                    context: context,
                    statusCode: (int)HttpStatusCode.GatewayTimeout,
                    code: "Common.GatewayTimeout",
                    message: "Downstream service did not respond in time.",
                    cancellationToken: context.RequestAborted);
            }
            catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested)
            {
                logger.LogInformation(
                    exception: ex,
                    message: "Request aborted by client");
            }
            catch (Exception ex) when (ex is IHttpResponseException httpEx)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Downstream error from {Service}. Status {Status}. Url {Url}",
                    httpEx.ServiceName,
                    (int)httpEx.StatusCode,
                    httpEx.RequestUrl);

                context.Response.StatusCode = (int)httpEx.StatusCode;
                context.Response.ContentType = httpEx.ContentType ?? ApiProblemDetailsFactory.ProblemContentType;

                if (!string.IsNullOrWhiteSpace(httpEx.Body))
                {
                    await context.Response.WriteAsync(httpEx.Body);
                    return;
                }

                await ApiProblemDetailsFactory.WriteAsync(
                    context: context,
                    statusCode: (int)httpEx.StatusCode,
                    code: "Common.DownstreamError",
                    message: "Downstream service error.",
                    cancellationToken: context.RequestAborted);
            }

            catch (HttpRequestException ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Bad gateway while calling downstream service");

                await ApiProblemDetailsFactory.WriteAsync(
                    context: context,
                    statusCode: (int)HttpStatusCode.BadGateway,
                    code: "Common.BadGateway",
                    message: "Downstream service error.",
                    cancellationToken: context.RequestAborted);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    exception: ex,
                    message: "Unhandled exception");

                await ApiProblemDetailsFactory.WriteAsync(
                    context: context,
                    statusCode: (int)HttpStatusCode.InternalServerError,
                    code: "Common.UnexpectedError",
                    message: "Internal server error",
                    cancellationToken: context.RequestAborted);
            }
        }

        private static HttpStatusCode MapToHttpStatusCode(ApplicationErrorType errorType)
        {
            return errorType switch
            {
                ApplicationErrorType.Validation => HttpStatusCode.BadRequest,
                ApplicationErrorType.NotFound => HttpStatusCode.NotFound,
                ApplicationErrorType.Unauthorized => HttpStatusCode.Unauthorized,
                ApplicationErrorType.Forbidden => HttpStatusCode.Forbidden,
                ApplicationErrorType.Conflict => HttpStatusCode.Conflict,
                ApplicationErrorType.BusinessRule => HttpStatusCode.BadRequest,
                ApplicationErrorType.TooManyRequests => HttpStatusCode.TooManyRequests,
                _ => HttpStatusCode.InternalServerError
            };
        }
    }
}
