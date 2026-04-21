using System.Text;

namespace Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity
{
    public static class SecurityActivityCursorCodec
    {
        public static string Encode(SecurityActivityCursor cursor)
        {
            return Convert.ToBase64String(
                inArray: Encoding.UTF8.GetBytes(
                    s: $"{cursor.UtcTicks}:{cursor.EventId:N}"));
        }

        public static bool TryDecode(
            string rawCursor,
            out SecurityActivityCursor cursor)
        {
            cursor = default;

            if (string.IsNullOrWhiteSpace(rawCursor))
                return false;

            try
            {
                string payload = Encoding.UTF8.GetString(
                    bytes: Convert.FromBase64String(rawCursor));
                string[] parts = payload.Split(':', count: 2);

                if (parts.Length != 2 ||
                    !long.TryParse(parts[0], out long utcTicks) ||
                    !Guid.TryParseExact(parts[1], "N", out Guid eventId))
                    return false;

                cursor = new SecurityActivityCursor(
                    UtcTicks: utcTicks,
                    EventId: eventId);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
