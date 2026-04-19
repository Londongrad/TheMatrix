namespace Matrix.Identity.Api.Authorization.Internal
{
    public static class TrustedGatewayRequestContext
    {
        public const string IsTrustedGatewayRequestItemKey = "__identity_trusted_gateway_request";

        public static void Mark(HttpContext context)
        {
            context.Items[IsTrustedGatewayRequestItemKey] = true;
        }

        public static bool IsTrusted(HttpContext context)
        {
            return context.Items.TryGetValue(
                       key: IsTrustedGatewayRequestItemKey,
                       value: out object? value) &&
                   value is true;
        }
    }
}
