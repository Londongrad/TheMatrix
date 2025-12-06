namespace Matrix.BuildingBlocks.Api.Errors
{
    public sealed record ErrorResponse(
        string Code,
        string Message,
        IReadOnlyDictionary<string, string[]>? Errors = null,
        string? TraceId = null);
}
