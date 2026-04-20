using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Matrix.BuildingBlocks.Api.Errors
{
    public static class ApiProblemDetailsFactory
    {
        public const string ProblemContentType = "application/problem+json";

        public static ProblemDetails Create(
            HttpContext context,
            int statusCode,
            string code,
            string message,
            IReadOnlyDictionary<string, string[]>? errors = null)
        {
            return Create(
                statusCode: statusCode,
                code: code,
                message: message,
                errors: errors,
                traceId: context.TraceIdentifier,
                instance: context.Request.Path.Value);
        }

        public static ProblemDetails Create(
            int statusCode,
            string code,
            string message,
            IReadOnlyDictionary<string, string[]>? errors = null,
            string? traceId = null,
            string? instance = null)
        {
            ProblemDetails problem = errors is not null
                ? new ValidationProblemDetails(errors.ToDictionary(
                    keySelector: kvp => kvp.Key,
                    elementSelector: kvp => kvp.Value.ToArray()))
                : new ProblemDetails();

            problem.Status = statusCode;
            problem.Type = $"https://httpstatuses.com/{statusCode}";
            problem.Title = ReasonPhrases.GetReasonPhrase(statusCode);
            problem.Detail = message;
            problem.Instance = instance;

            problem.Extensions["code"] = code;
            problem.Extensions["message"] = message;

            if (!string.IsNullOrWhiteSpace(traceId))
                problem.Extensions["traceId"] = traceId;

            return problem;
        }

        public static ObjectResult CreateObjectResult(
            HttpContext context,
            int statusCode,
            string code,
            string message,
            IReadOnlyDictionary<string, string[]>? errors = null)
        {
            var result = new ObjectResult(Create(
                context: context,
                statusCode: statusCode,
                code: code,
                message: message,
                errors: errors))
            {
                StatusCode = statusCode
            };

            result.ContentTypes.Add(ProblemContentType);
            return result;
        }

        public static Task WriteAsync(
            HttpContext context,
            int statusCode,
            string code,
            string message,
            IReadOnlyDictionary<string, string[]>? errors = null,
            CancellationToken cancellationToken = default)
        {
            context.Response.StatusCode = statusCode;

            return context.Response.WriteAsJsonAsync(
                value: Create(
                    context: context,
                    statusCode: statusCode,
                    code: code,
                    message: message,
                    errors: errors),
                options: (JsonSerializerOptions?)null,
                contentType: ProblemContentType,
                cancellationToken: cancellationToken);
        }
    }
}
