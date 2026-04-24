using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities;

public sealed class DefaultUserAccessPolicyTests
{
    [Fact]
    public void CreateDefault_SetsSingletonIdentityAndInitialVersion()
    {
        var nowUtc = new DateTime(2047, 5, 7, 8, 9, 10, DateTimeKind.Utc);

        var policy = DefaultUserAccessPolicy.CreateDefault(nowUtc);

        Assert.Equal(DefaultUserAccessPolicy.SingletonId, policy.Id);
        Assert.Equal(1, policy.Version);
        Assert.Equal(nowUtc, policy.CreatedAtUtc);
        Assert.Equal(nowUtc, policy.UpdatedAtUtc);
    }

    [Fact]
    public void Touch_IncrementsVersionAndUpdatesTimestamp()
    {
        var createdAtUtc = new DateTime(2047, 5, 7, 8, 9, 10, DateTimeKind.Utc);
        var updatedAtUtc = createdAtUtc.AddHours(1);
        var policy = DefaultUserAccessPolicy.CreateDefault(createdAtUtc);

        policy.Touch(updatedAtUtc);

        Assert.Equal(2, policy.Version);
        Assert.Equal(createdAtUtc, policy.CreatedAtUtc);
        Assert.Equal(updatedAtUtc, policy.UpdatedAtUtc);
    }
}
