using Matrix.BuildingBlocks.Api.Errors;
using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Matrix.BuildingBlocks.Api.Middleware
{
    public sealed class ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Handled domain exception with code {Code}", ex.Code);

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/json";

                IDictionary<string, string[]>? errors = null;

                if (ex.PropertyName is not null)
                {
                    errors = new Dictionary<string, string[]>
                    {
                        [ex.PropertyName] = new[] { ex.Message }
                    };
                }

                var response = new ErrorResponse(
                    Code: ex.Code,
                    Message: ex.Message,
                    Errors: errors,
                    TraceId: context.TraceIdentifier);

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (MatrixApplicationException ex)
            {
                _logger.LogWarning(ex, "Handled application exception with code {Code}", ex.Code);

                var statusCode = MapToHttpStatusCode(ex.ErrorType);

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
                _logger.LogWarning(ex, "Invalid argument");

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
                _logger.LogWarning(ex, "Invalid operation");

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/json";

                var response = new ErrorResponse(
                    Code: "Common.InvalidOperation",
                    Message: ex.Message,
                    Errors: null,
                    TraceId: context.TraceIdentifier);

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

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
