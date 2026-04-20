namespace Matrix.Economy.Application.UseCases.Ledger.Common
{
    public static class LedgerPageSizePolicy
    {
        public const int DefaultPageSize = 50;
        public const int MaxPageSize = 100;

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
