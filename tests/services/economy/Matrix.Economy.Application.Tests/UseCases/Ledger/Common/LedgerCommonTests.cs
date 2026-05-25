using Matrix.Economy.Application.UseCases.Ledger.Common;
using Xunit;

namespace Matrix.Economy.Application.Tests.UseCases.Ledger.Common
{
    public sealed class LedgerCommonTests
    {
        [Fact]
        public void LedgerCursorCodec_RoundTripsValidCursor()
        {
            var cursor = new LedgerCursor(
                UtcTicks: 638505180000000000,
                EntryId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

            string encoded = LedgerCursorCodec.Encode(cursor);
            bool decoded = LedgerCursorCodec.TryDecode(
                rawCursor: encoded,
                cursor: out LedgerCursor roundTripped);

            Assert.True(decoded);
            Assert.Equal(
                expected: cursor,
                actual: roundTripped);
        }

        [Fact]
        public void LedgerCursorCodec_RejectsMalformedCursor()
        {
            bool decoded = LedgerCursorCodec.TryDecode(
                rawCursor: "bad-cursor",
                cursor: out LedgerCursor cursor);

            Assert.False(decoded);
            Assert.Equal(
                expected: default(LedgerCursor),
                actual: cursor);
        }

        [Theory]
        [InlineData(
            0,
            50)]
        [InlineData(
            -5,
            50)]
        [InlineData(
            25,
            25)]
        [InlineData(
            250,
            100)]
        public void LedgerPageSizePolicy_NormalizesRequestedSize(
            int requested,
            int expected)
        {
            int normalized = LedgerPageSizePolicy.Normalize(requested);

            Assert.Equal(
                expected: expected,
                actual: normalized);
        }
    }
}
