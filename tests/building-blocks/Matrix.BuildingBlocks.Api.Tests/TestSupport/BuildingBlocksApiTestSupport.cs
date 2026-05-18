using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Matrix.BuildingBlocks.Api.Tests.TestSupport;

public static class BuildingBlocksApiTestSupport
{
    public static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    public static DefaultHttpContext CreateHttpContext(
        string path = "/api/test",
        string traceIdentifier = "trace-123")
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Path = path;
        httpContext.TraceIdentifier = traceIdentifier;
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    public static async Task<JsonDocument> ReadJsonAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(httpContext.Response.Body);
    }

    public static async Task<string> ReadBodyAsStringAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        using StreamReader reader = new(httpContext.Response.Body, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        httpContext.Response.Body.Position = 0;
        return body;
    }

    public static string CreateUnsignedJwt(params Claim[] claims)
    {
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims: claims));
    }
}
