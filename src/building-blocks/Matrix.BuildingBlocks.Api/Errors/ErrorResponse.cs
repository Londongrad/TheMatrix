namespace Matrix.BuildingBlocks.Api.Errors
{
    public sealed record ErrorResponse(
        string Code,
        string Message,
        IDictionary<string, string[]>? Errors = null,
        string? TraceId = null);
}
