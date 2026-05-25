using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities
{
    public sealed class DefaultUserAccessPolicyTests
    {
        [Fact]
        public void CreateDefault_SetsSingletonIdentityAndInitialVersion()
        {
            var nowUtc = new DateTime(
                year: 2047,
                month: 5,
                day: 7,
                hour: 8,
                minute: 9,
                second: 10,
                kind: DateTimeKind.Utc);

            var policy = DefaultUserAccessPolicy.CreateDefault(nowUtc);

            Assert.Equal(
                expected: DefaultUserAccessPolicy.SingletonId,
                actual: policy.Id);
            Assert.Equal(
                expected: 1,
                actual: policy.Version);
            Assert.Equal(
                expected: nowUtc,
                actual: policy.CreatedAtUtc);
            Assert.Equal(
                expected: nowUtc,
                actual: policy.UpdatedAtUtc);
        }

        [Fact]
        public void Touch_IncrementsVersionAndUpdatesTimestamp()
        {
            var createdAtUtc = new DateTime(
                year: 2047,
                month: 5,
                day: 7,
                hour: 8,
                minute: 9,
                second: 10,
                kind: DateTimeKind.Utc);
            DateTime updatedAtUtc = createdAtUtc.AddHours(1);
            var policy = DefaultUserAccessPolicy.CreateDefault(createdAtUtc);

            policy.Touch(updatedAtUtc);

            Assert.Equal(
                expected: 2,
                actual: policy.Version);
            Assert.Equal(
                expected: createdAtUtc,
                actual: policy.CreatedAtUtc);
            Assert.Equal(
                expected: updatedAtUtc,
                actual: policy.UpdatedAtUtc);
        }
    }
}
