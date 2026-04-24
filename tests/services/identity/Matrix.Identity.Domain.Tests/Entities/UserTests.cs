using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities;

public sealed class UserTests
{
    [Fact]
    public void CreateNew_WithValidValues_SetsDefaultState()
    {
        var user = User.CreateNew(
            email: UserTestData.Email,
            username: UserTestData.Username,
            passwordHash: "hashed-password",
            createdAtUtc: UserTestData.CreatedAtUtc);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(UserTestData.Email.Value, user.Email.Value);
        Assert.Equal(UserTestData.Username.Value, user.Username.Value);
        Assert.Equal("hashed-password", user.PasswordHash);
        Assert.Equal(UserTestData.CreatedAtUtc, user.CreatedAtUtc);
        Assert.False(user.IsEmailConfirmed);
        Assert.Null(user.EmailConfirmedAtUtc);
        Assert.False(user.IsLocked);
        Assert.False(user.IsDeleted);
        Assert.Null(user.DeletedAtUtc);
        Assert.Null(user.PendingEmail);
        Assert.Equal(1, user.PermissionsVersion);
        Assert.Empty(user.RefreshTokens);
    }

    [Fact]
    public void CreateNew_WithEmptyPasswordHash_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => User.CreateNew(
            email: UserTestData.Email,
            username: UserTestData.Username,
            passwordHash: "  ",
            createdAtUtc: UserTestData.CreatedAtUtc));

        Assert.Equal("Identity.User.Password.EmptyHash", exception.Code);
        Assert.Equal("passwordHash", exception.PropertyName);
    }

    [Fact]
    public void ConfirmEmail_FirstCallSetsConfirmation_AndSecondCallIsNoOp()
    {
        var user = UserTestData.CreateUser();
        var firstConfirmedAtUtc = UserTestData.CreatedAtUtc.AddMinutes(5);
        var secondConfirmedAtUtc = UserTestData.CreatedAtUtc.AddMinutes(10);

        user.ConfirmEmail(firstConfirmedAtUtc);
        user.ConfirmEmail(secondConfirmedAtUtc);

        Assert.True(user.IsEmailConfirmed);
        Assert.Equal(firstConfirmedAtUtc, user.EmailConfirmedAtUtc);
    }

    [Fact]
    public void EmailChangeFlow_UpdatesPendingAndConfirmedEmail()
    {
        var user = UserTestData.CreateUser();
        var newEmail = Email.Create("trinity@matrix.local");
        var confirmedAtUtc = UserTestData.CreatedAtUtc.AddHours(1);

        user.RequestEmailChange(
            newEmail: newEmail,
            requestedAtUtc: UserTestData.CreatedAtUtc.AddMinutes(30));

        Assert.Equal("trinity@matrix.local", user.PendingEmail);

        user.ConfirmPendingEmailChange(confirmedAtUtc);

        Assert.Equal("trinity@matrix.local", user.Email.Value);
        Assert.Null(user.PendingEmail);
        Assert.True(user.IsEmailConfirmed);
        Assert.Equal(confirmedAtUtc, user.EmailConfirmedAtUtc);
    }

    [Fact]
    public void ConfirmPendingEmailChange_WithoutPendingEmail_ThrowsInvalidOperationException()
    {
        var user = UserTestData.CreateUser();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            user.ConfirmPendingEmailChange(UserTestData.CreatedAtUtc.AddMinutes(1)));

        Assert.Equal("Pending email is not set.", exception.Message);
    }

    [Fact]
    public void CancelPendingEmailChange_ClearsPendingEmail()
    {
        var user = UserTestData.CreateUser();

        user.RequestEmailChange(
            newEmail: Email.Create("switch@matrix.local"),
            requestedAtUtc: UserTestData.CreatedAtUtc.AddMinutes(10));
        user.CancelPendingEmailChange();

        Assert.Null(user.PendingEmail);
    }

    [Fact]
    public void ChangeDisplayName_TrimsValue_AndClearsWhitespace()
    {
        var user = UserTestData.CreateUser();

        user.ChangeDisplayName("  Thomas Anderson  ");
        Assert.Equal("Thomas Anderson", user.DisplayName);

        user.ChangeDisplayName("   ");
        Assert.Null(user.DisplayName);
    }

    [Fact]
    public void ChangeDisplayName_WithTooLongValue_ThrowsDomainException()
    {
        var user = UserTestData.CreateUser();

        var exception = Assert.Throws<DomainException>(() =>
            user.ChangeDisplayName(new string('D', User.DisplayNameMaxLength + 1)));

        Assert.Equal("Identity.User.DisplayName.InvalidLength", exception.Code);
        Assert.Equal("DisplayName", exception.PropertyName);
    }

    [Fact]
    public void ChangeUsername_UpdatesUsernameAndTimestamp()
    {
        var user = UserTestData.CreateUser();
        var changedAtUtc = UserTestData.CreatedAtUtc.AddDays(1);
        var newUsername = Username.Create("the.one");

        user.ChangeUsername(
            username: newUsername,
            changedAtUtc: changedAtUtc);

        Assert.Equal("the.one", user.Username.Value);
        Assert.Equal(changedAtUtc, user.LastUsernameChangedAtUtc);
    }

    [Fact]
    public void ChangePasswordHash_WithEmptyValue_ThrowsDomainException()
    {
        var user = UserTestData.CreateUser();

        var exception = Assert.Throws<DomainException>(() => user.ChangePasswordHash(" "));

        Assert.Equal("Identity.User.Password.EmptyHash", exception.Code);
        Assert.Equal("newPasswordHash", exception.PropertyName);
    }

    [Fact]
    public void LockDeleteRestoreAndUnlock_UpdateLoginState()
    {
        var user = UserTestData.CreateUser();
        var deletedAtUtc = UserTestData.CreatedAtUtc.AddDays(2);

        Assert.True(user.CanLogin());

        user.Lock();
        Assert.True(user.IsLocked);
        Assert.False(user.CanLogin());

        user.Unlock();
        Assert.False(user.IsLocked);
        Assert.True(user.CanLogin());

        user.RequestEmailChange(
            newEmail: Email.Create("pending@matrix.local"),
            requestedAtUtc: UserTestData.CreatedAtUtc.AddHours(2));
        user.SoftDelete(deletedAtUtc);

        Assert.True(user.IsDeleted);
        Assert.Equal(deletedAtUtc, user.DeletedAtUtc);
        Assert.Null(user.PendingEmail);
        Assert.False(user.CanLogin());

        user.Restore();

        Assert.False(user.IsDeleted);
        Assert.Null(user.DeletedAtUtc);
        Assert.True(user.CanLogin());
    }

    [Fact]
    public void BumpPermissionsVersion_IncrementsCounter()
    {
        var user = UserTestData.CreateUser();

        user.BumpPermissionsVersion();
        user.BumpPermissionsVersion();

        Assert.Equal(3, user.PermissionsVersion);
    }
}
