using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities
{
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

            Assert.NotEqual(
                expected: Guid.Empty,
                actual: user.Id);
            Assert.Equal(
                expected: UserTestData.Email.Value,
                actual: user.Email.Value);
            Assert.Equal(
                expected: UserTestData.Username.Value,
                actual: user.Username.Value);
            Assert.Equal(
                expected: "hashed-password",
                actual: user.PasswordHash);
            Assert.Equal(
                expected: UserTestData.CreatedAtUtc,
                actual: user.CreatedAtUtc);
            Assert.False(user.IsEmailConfirmed);
            Assert.Null(user.EmailConfirmedAtUtc);
            Assert.False(user.IsLocked);
            Assert.False(user.IsDeleted);
            Assert.Null(user.DeletedAtUtc);
            Assert.Null(user.PendingEmail);
            Assert.Equal(
                expected: 1,
                actual: user.PermissionsVersion);
            Assert.Empty(user.RefreshTokens);
        }

        [Fact]
        public void CreateNew_WithEmptyPasswordHash_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => User.CreateNew(
                email: UserTestData.Email,
                username: UserTestData.Username,
                passwordHash: "  ",
                createdAtUtc: UserTestData.CreatedAtUtc));

            Assert.Equal(
                expected: "Identity.User.Password.EmptyHash",
                actual: exception.Code);
            Assert.Equal(
                expected: "passwordHash",
                actual: exception.PropertyName);
        }

        [Fact]
        public void ConfirmEmail_FirstCallSetsConfirmation_AndSecondCallIsNoOp()
        {
            User user = UserTestData.CreateUser();
            DateTime firstConfirmedAtUtc = UserTestData.CreatedAtUtc.AddMinutes(5);
            DateTime secondConfirmedAtUtc = UserTestData.CreatedAtUtc.AddMinutes(10);

            user.ConfirmEmail(firstConfirmedAtUtc);
            user.ConfirmEmail(secondConfirmedAtUtc);

            Assert.True(user.IsEmailConfirmed);
            Assert.Equal(
                expected: firstConfirmedAtUtc,
                actual: user.EmailConfirmedAtUtc);
        }

        [Fact]
        public void EmailChangeFlow_UpdatesPendingAndConfirmedEmail()
        {
            User user = UserTestData.CreateUser();
            var newEmail = Email.Create("trinity@matrix.local");
            DateTime confirmedAtUtc = UserTestData.CreatedAtUtc.AddHours(1);

            user.RequestEmailChange(
                newEmail: newEmail,
                requestedAtUtc: UserTestData.CreatedAtUtc.AddMinutes(30));

            Assert.Equal(
                expected: "trinity@matrix.local",
                actual: user.PendingEmail);

            user.ConfirmPendingEmailChange(confirmedAtUtc);

            Assert.Equal(
                expected: "trinity@matrix.local",
                actual: user.Email.Value);
            Assert.Null(user.PendingEmail);
            Assert.True(user.IsEmailConfirmed);
            Assert.Equal(
                expected: confirmedAtUtc,
                actual: user.EmailConfirmedAtUtc);
        }

        [Fact]
        public void ConfirmPendingEmailChange_WithoutPendingEmail_ThrowsInvalidOperationException()
        {
            User user = UserTestData.CreateUser();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                user.ConfirmPendingEmailChange(UserTestData.CreatedAtUtc.AddMinutes(1)));

            Assert.Equal(
                expected: "Pending email is not set.",
                actual: exception.Message);
        }

        [Fact]
        public void CancelPendingEmailChange_ClearsPendingEmail()
        {
            User user = UserTestData.CreateUser();

            user.RequestEmailChange(
                newEmail: Email.Create("switch@matrix.local"),
                requestedAtUtc: UserTestData.CreatedAtUtc.AddMinutes(10));
            user.CancelPendingEmailChange();

            Assert.Null(user.PendingEmail);
        }

        [Fact]
        public void ChangeDisplayName_TrimsValue_AndClearsWhitespace()
        {
            User user = UserTestData.CreateUser();

            user.ChangeDisplayName("  Thomas Anderson  ");
            Assert.Equal(
                expected: "Thomas Anderson",
                actual: user.DisplayName);

            user.ChangeDisplayName("   ");
            Assert.Null(user.DisplayName);
        }

        [Fact]
        public void ChangeDisplayName_WithTooLongValue_ThrowsDomainException()
        {
            User user = UserTestData.CreateUser();

            DomainException exception = Assert.Throws<DomainException>(() =>
                user.ChangeDisplayName(
                    new string(
                        c: 'D',
                        count: User.DisplayNameMaxLength + 1)));

            Assert.Equal(
                expected: "Identity.User.DisplayName.InvalidLength",
                actual: exception.Code);
            Assert.Equal(
                expected: "DisplayName",
                actual: exception.PropertyName);
        }

        [Fact]
        public void ChangeUsername_UpdatesUsernameAndTimestamp()
        {
            User user = UserTestData.CreateUser();
            DateTime changedAtUtc = UserTestData.CreatedAtUtc.AddDays(1);
            var newUsername = Username.Create("the.one");

            user.ChangeUsername(
                username: newUsername,
                changedAtUtc: changedAtUtc);

            Assert.Equal(
                expected: "the.one",
                actual: user.Username.Value);
            Assert.Equal(
                expected: changedAtUtc,
                actual: user.LastUsernameChangedAtUtc);
        }

        [Fact]
        public void ChangePasswordHash_WithEmptyValue_ThrowsDomainException()
        {
            User user = UserTestData.CreateUser();

            DomainException exception = Assert.Throws<DomainException>(() => user.ChangePasswordHash(" "));

            Assert.Equal(
                expected: "Identity.User.Password.EmptyHash",
                actual: exception.Code);
            Assert.Equal(
                expected: "newPasswordHash",
                actual: exception.PropertyName);
        }

        [Fact]
        public void LockDeleteRestoreAndUnlock_UpdateLoginState()
        {
            User user = UserTestData.CreateUser();
            DateTime deletedAtUtc = UserTestData.CreatedAtUtc.AddDays(2);

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
            Assert.Equal(
                expected: deletedAtUtc,
                actual: user.DeletedAtUtc);
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
            User user = UserTestData.CreateUser();

            user.BumpPermissionsVersion();
            user.BumpPermissionsVersion();

            Assert.Equal(
                expected: 3,
                actual: user.PermissionsVersion);
        }
    }
}
