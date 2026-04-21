namespace Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity
{
    public static class SecurityActivityPageSizePolicy
    {
        public const int DefaultPageSize = 12;
        public const int MaxPageSize = 50;

        public static int Normalize(int requestedPageSize)
        {
            if (requestedPageSize <= 0)
                return DefaultPageSize;

            return Math.Min(
                val1: requestedPageSize,
                val2: MaxPageSize);
        }
    }
}
