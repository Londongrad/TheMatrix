namespace Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity
{
    public readonly record struct SecurityActivityCursor(
        long UtcTicks,
        Guid EventId);
}
