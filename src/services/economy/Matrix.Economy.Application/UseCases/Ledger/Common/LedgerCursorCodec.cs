using System.Globalization;

namespace Matrix.Economy.Application.UseCases.Ledger.Common
{
    public static class LedgerCursorCodec
    {
        public static string Encode(LedgerCursor cursor)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{cursor.UtcTicks}:{cursor.EntryId:N}");
        }

        public static bool TryDecode(
            string? rawCursor,
            out LedgerCursor cursor)
        {
            cursor = default;

            if (string.IsNullOrWhiteSpace(rawCursor))
                return false;

            string[] parts = rawCursor.Split(
                separator: ':',
                count: 2,
                options: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2 ||
                !long.TryParse(
                    s: parts[0],
                    style: NumberStyles.Integer,
                    provider: CultureInfo.InvariantCulture,
                    result: out long utcTicks) ||
                !Guid.TryParseExact(
                    input: parts[1],
                    format: "N",
                    result: out Guid entryId))
                return false;

            cursor = new LedgerCursor(
                UtcTicks: utcTicks,
                EntryId: entryId);
            return true;
        }
    }
}
