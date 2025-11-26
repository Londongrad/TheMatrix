using Matrix.BuildingBlocks.Api.Errors;
using Matrix.BuildingBlocks.Application.Exceptions;
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
            catch (MatrixApplicationException ex)
            {
                _logger.LogWarning(ex, "Handled domain exception with code {Code}", ex.Code);

                context.Response.StatusCode = (int)ex.StatusCode;
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
    }
}
