using System.Net;
using Matrix.BuildingBlocks.Api.Errors;
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

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/json";

                IReadOnlyDictionary<string, string[]>? errors = null;

                if (ex.PropertyName is not null)
                    errors = new Dictionary<string, string[]>
                    {
                        [ex.PropertyName] = [ex.Message]
                    };

                var response = new ErrorResponse(
                    Code: ex.Code,
                    Message: ex.Message,
                    Errors: errors,
                    TraceId: context.TraceIdentifier);

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (MatrixApplicationException ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Handled application exception with code {Code}",
                    ex.Code);

                HttpStatusCode statusCode = MapToHttpStatusCode(ex.ErrorType);

                context.Response.StatusCode = (int)statusCode;
                context.Response.ContentType = "application/json";

                var response = new ErrorResponse(
                    Code: ex.Code,
                    Message: ex.Message,
                    Errors: ex.Errors,
                    TraceId: context.TraceIdentifier);

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Invalid argument");

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/json";

                var response = new ErrorResponse(
                    Code: "Common.InvalidArgument",
                    Message: ex.Message,
                    Errors: null,
                    TraceId: context.TraceIdentifier);

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Invalid operation");

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/json";

                var response = new ErrorResponse(
                    Code: "Common.InvalidOperation",
                    Message: ex.Message,
                    Errors: null,
                    TraceId: context.TraceIdentifier);

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (TaskCanceledException ex) when (!context.RequestAborted.IsCancellationRequested)
            {
                // Это именно таймаут HttpClient (а не отмена клиентом)
                logger.LogWarning(ex, "Gateway timeout while calling downstream service");

                context.Response.StatusCode = (int)HttpStatusCode.GatewayTimeout;
                context.Response.ContentType = "application/json";

                var response = new ErrorResponse(
                    Code: "Common.GatewayTimeout",
                    Message: "Downstream service did not respond in time.",
                    Errors: null,
                    TraceId: context.TraceIdentifier);

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested)
            {
                // Клиент сам оборвал запрос (закрыл вкладку/навигация/abort)
                logger.LogInformation(ex, "Request aborted by client");
                // Можно просто ничего не писать в response
            }
            catch (HttpRequestException ex)
            {
                // Сюда часто попадает EnsureSuccessStatusCode() или проблемы сети/SSL/DNS
                logger.LogWarning(ex, "Bad gateway while calling downstream service");

                context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
                context.Response.ContentType = "application/json";

                var response = new ErrorResponse(
                    Code: "Common.BadGateway",
                    Message: "Downstream service error.",
                    Errors: null,
                    TraceId: context.TraceIdentifier);

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    exception: ex,
                    message: "Unhandled exception");

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var response = new ErrorResponse(
                    Code: "Common.UnexpectedError",
                    Message: "Internal server error",
                    Errors: null,
                    TraceId: context.TraceIdentifier);

                await context.Response.WriteAsJsonAsync(response);
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
                _ => HttpStatusCode.BadRequest
            };
        }
    }
}
