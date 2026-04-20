using System.Net;
using System.Runtime.ExceptionServices;
using System.Text.Json;
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

                EnsureResponseCanBeWritten(
                    context: context,
                    exception: ex,
                    logger: logger);

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

                EnsureResponseCanBeWritten(
                    context: context,
                    exception: ex,
                    logger: logger);

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

                EnsureResponseCanBeWritten(
                    context: context,
                    exception: ex,
                    logger: logger);

                HttpStatusCode statusCode = MapToHttpStatusCode(ex.ErrorType);

                await ApiProblemDetailsFactory.WriteAsync(
                    context: context,
                    statusCode: (int)statusCode,
                    code: ex.Code,
                    message: BuildPublicInfrastructureMessage(ex.ErrorType),
                    cancellationToken: context.RequestAborted);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Invalid argument");

                EnsureResponseCanBeWritten(
                    context: context,
                    exception: ex,
                    logger: logger);

                await ApiProblemDetailsFactory.WriteAsync(
                    context: context,
                    statusCode: (int)HttpStatusCode.BadRequest,
                    code: "Common.InvalidArgument",
                    message: ex.Message,
                    cancellationToken: context.RequestAborted);
            }
            catch (TaskCanceledException ex) when (!context.RequestAborted.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Gateway timeout while calling downstream service");

                EnsureResponseCanBeWritten(
                    context: context,
                    exception: ex,
                    logger: logger);

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

                EnsureResponseCanBeWritten(
                    context: context,
                    exception: ex,
                    logger: logger);

                if (TrySanitizeDownstreamError(
                        httpEx: httpEx,
                        code: out string code,
                        message: out string message,
                        errors: out IReadOnlyDictionary<string, string[]>? errors))
                {
                    await ApiProblemDetailsFactory.WriteAsync(
                        context: context,
                        statusCode: (int)httpEx.StatusCode,
                        code: code,
                        message: message,
                        errors: errors,
                        cancellationToken: context.RequestAborted);
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

                EnsureResponseCanBeWritten(
                    context: context,
                    exception: ex,
                    logger: logger);

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

                EnsureResponseCanBeWritten(
                    context: context,
                    exception: ex,
                    logger: logger);

                await ApiProblemDetailsFactory.WriteAsync(
                    context: context,
                    statusCode: (int)HttpStatusCode.InternalServerError,
                    code: "Common.UnexpectedError",
                    message: "Internal server error",
                    cancellationToken: context.RequestAborted);
            }
        }

        private static void EnsureResponseCanBeWritten(
            HttpContext context,
            Exception exception,
            ILogger logger)
        {
            if (!context.Response.HasStarted)
                return;

            logger.LogWarning(
                exception: exception,
                message: "Cannot write error response because the HTTP response has already started. TraceId={TraceId} Path={Path}",
                context.TraceIdentifier,
                context.Request.Path.Value);

            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        private static bool TrySanitizeDownstreamError(
            IHttpResponseException httpEx,
            out string code,
            out string message,
            out IReadOnlyDictionary<string, string[]>? errors)
        {
            code = "Common.DownstreamError";
            message = "Downstream service error.";
            errors = null;

            if (string.IsNullOrWhiteSpace(httpEx.Body))
                return false;

            if (!LooksLikeJson(httpEx.ContentType, httpEx.Body))
                return false;

            try
            {
                using JsonDocument document = JsonDocument.Parse(httpEx.Body);
                JsonElement root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return false;

                code = GetString(root, "code") ?? code;
                message = GetString(root, "detail") ??
                          GetString(root, "message") ??
                          GetString(root, "title") ??
                          message;

                errors = TryReadErrors(root);

                if ((string.IsNullOrWhiteSpace(message) || message == "Downstream service error.") &&
                    errors is not null)
                {
                    string? firstError = errors
                       .SelectMany(static kvp => kvp.Value)
                       .FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(firstError))
                        message = firstError;
                }

                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool LooksLikeJson(string? contentType, string body)
        {
            if (!string.IsNullOrWhiteSpace(contentType) &&
                (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
                 contentType.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase)))
                return true;

            body = body.TrimStart();
            return body.StartsWith("{", StringComparison.Ordinal) ||
                   body.StartsWith("[", StringComparison.Ordinal);
        }

        private static string? GetString(JsonElement root, string propertyName)
        {
            return root.TryGetProperty(propertyName, out JsonElement property) &&
                   property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
        }

        private static IReadOnlyDictionary<string, string[]>? TryReadErrors(JsonElement root)
        {
            if (!root.TryGetProperty("errors", out JsonElement errorsElement) ||
                errorsElement.ValueKind != JsonValueKind.Object)
                return null;

            var errors = new Dictionary<string, string[]>();

            foreach (JsonProperty property in errorsElement.EnumerateObject())
            {
                string[] values = property.Value.ValueKind switch
                {
                    JsonValueKind.Array => property.Value
                       .EnumerateArray()
                       .Where(static item => item.ValueKind == JsonValueKind.String)
                       .Select(static item => item.GetString())
                       .OfType<string>()
                       .ToArray(),
                    JsonValueKind.String => [property.Value.GetString() ?? string.Empty],
                    _ => []
                };

                if (values.Length > 0)
                    errors[property.Name] = values;
            }

            return errors.Count > 0 ? errors : null;
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

        private static string BuildPublicInfrastructureMessage(ApplicationErrorType errorType)
        {
            return errorType switch
            {
                ApplicationErrorType.NotFound => "Requested resource was not found.",
                ApplicationErrorType.Unauthorized => "Authentication is required.",
                ApplicationErrorType.Forbidden => "Access denied.",
                ApplicationErrorType.Conflict => "Operation could not be completed due to a server state conflict.",
                ApplicationErrorType.TooManyRequests => "Service is temporarily unavailable. Please retry later.",
                _ => "Internal server error"
            };
        }
    }
}
