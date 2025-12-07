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
        private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;
        private readonly RequestDelegate _next = next;

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(exception: ex, message: "Handled domain exception with code {Code}", ex.Code);

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
                _logger.LogWarning(exception: ex, message: "Handled application exception with code {Code}", ex.Code);

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
                _logger.LogWarning(exception: ex, message: "Invalid argument");

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
                _logger.LogWarning(exception: ex, message: "Invalid operation");

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
                _logger.LogError(exception: ex, message: "Unhandled exception");

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
