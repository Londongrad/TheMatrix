using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities
{
    public sealed class UserRefreshTokenTests
    {
        [Fact]
        public void IssueRefreshToken_WithValidValues_AddsTokenToUser()
        {
            User user = UserTestData.CreateUser();
            var sessionId = Guid.Parse("70000000-0000-0000-0000-000000000001");
            DateTime createdAtUtc = UserTestData.CreatedAtUtc.AddMinutes(1);

            RefreshToken refreshToken = user.IssueRefreshToken(
                sessionId: sessionId,
                tokenHash: "issued-hash",
                expiresAtUtc: createdAtUtc.AddMinutes(30),
                deviceInfo: TokenTestData.CreateDeviceInfo(),
                geoLocation: TokenTestData.CreateGeoLocation(),
                isPersistent: true,
                createdAtUtc: createdAtUtc);

            Assert.Single(user.RefreshTokens);
            Assert.Same(
                expected: refreshToken,
                actual: user.RefreshTokens.Single());
            Assert.Equal(
                expected: user.Id,
                actual: refreshToken.UserId);
            Assert.Equal(
                expected: sessionId,
                actual: refreshToken.SessionId);
            Assert.Equal(
                expected: "issued-hash",
                actual: refreshToken.TokenHash);
        }

        [Fact]
        public void IssueRefreshToken_WithInvalidInputs_ThrowsDomainException()
        {
            User user = UserTestData.CreateUser();

            DomainException emptySessionException = Assert.Throws<DomainException>(() => user.IssueRefreshToken(
                sessionId: Guid.Empty,
                tokenHash: "issued-hash",
                expiresAtUtc: UserTestData.CreatedAtUtc.AddMinutes(30),
                deviceInfo: TokenTestData.CreateDeviceInfo(),
                geoLocation: TokenTestData.CreateGeoLocation(),
                isPersistent: true,
                createdAtUtc: UserTestData.CreatedAtUtc));

            Assert.Equal(
                expected: "Identity.Common.EmptyId",
                actual: emptySessionException.Code);
            Assert.Equal(
                expected: "sessionId",
                actual: emptySessionException.PropertyName);

            DomainException emptyTokenException = Assert.Throws<DomainException>(() => user.IssueRefreshToken(
                sessionId: Guid.Parse("70000000-0000-0000-0000-000000000001"),
                tokenHash: "   ",
                expiresAtUtc: UserTestData.CreatedAtUtc.AddMinutes(30),
                deviceInfo: TokenTestData.CreateDeviceInfo(),
                geoLocation: TokenTestData.CreateGeoLocation(),
                isPersistent: true,
                createdAtUtc: UserTestData.CreatedAtUtc));

            Assert.Equal(
                expected: "Identity.User.RefreshToken.NotFound",
                actual: emptyTokenException.Code);
            Assert.Equal(
                expected: "tokenHash",
                actual: emptyTokenException.PropertyName);
        }

        [Fact]
        public void RevokeRefreshToken_RevokesMatchingToken_AndIgnoresMissingToken()
        {
            User user = UserTestData.CreateUser();
            RefreshToken token = user.IssueRefreshToken(
                sessionId: Guid.Parse("70000000-0000-0000-0000-000000000001"),
                tokenHash: "issued-hash",
                expiresAtUtc: UserTestData.CreatedAtUtc.AddMinutes(30),
                deviceInfo: TokenTestData.CreateDeviceInfo(),
                geoLocation: TokenTestData.CreateGeoLocation(),
                isPersistent: true,
                createdAtUtc: UserTestData.CreatedAtUtc);
            DateTime revokedAtUtc = UserTestData.CreatedAtUtc.AddMinutes(5);

            user.RevokeRefreshToken(
                refreshTokenId: token.Id,
                reason: RefreshTokenRevocationReason.UserRevoked,
                revokedAtUtc: revokedAtUtc);
            user.RevokeRefreshToken(
                refreshTokenId: Guid.NewGuid(),
                reason: RefreshTokenRevocationReason.AdminRevoked,
                revokedAtUtc: revokedAtUtc.AddMinutes(1));

            Assert.True(token.IsRevoked);
            Assert.Equal(
                expected: revokedAtUtc,
                actual: token.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.UserRevoked,
                actual: token.RevokedReason);
        }

        [Fact]
        public void RevokeAllRefreshTokens_OnlyRevokesActiveTokens()
        {
            User user = UserTestData.CreateUser();
            RefreshToken activeToken = user.IssueRefreshToken(
                sessionId: Guid.Parse("70000000-0000-0000-0000-000000000001"),
                tokenHash: "active-token",
                expiresAtUtc: UserTestData.CreatedAtUtc.AddMinutes(40),
                deviceInfo: TokenTestData.CreateDeviceInfo(),
                geoLocation: TokenTestData.CreateGeoLocation(),
                isPersistent: true,
                createdAtUtc: UserTestData.CreatedAtUtc);
            RefreshToken expiredToken = user.IssueRefreshToken(
                sessionId: Guid.Parse("70000000-0000-0000-0000-000000000002"),
                tokenHash: "expired-token",
                expiresAtUtc: UserTestData.CreatedAtUtc.AddMinutes(2),
                deviceInfo: TokenTestData.CreateDeviceInfo(),
                geoLocation: null,
                isPersistent: false,
                createdAtUtc: UserTestData.CreatedAtUtc);
            DateTime revokedAtUtc = UserTestData.CreatedAtUtc.AddMinutes(10);

            user.RevokeAllRefreshTokens(
                reason: RefreshTokenRevocationReason.SecurityEvent,
                revokedAtUtc: revokedAtUtc);

            Assert.True(activeToken.IsRevoked);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.SecurityEvent,
                actual: activeToken.RevokedReason);
            Assert.False(expiredToken.IsRevoked);
        }

        [Fact]
        public void RevokeActiveRefreshTokensByDevice_RevokesOnlyMatchingActiveTokens()
        {
            User user = UserTestData.CreateUser();
            var primaryDevice = DeviceInfo.Create(
                deviceId: "device-a",
                deviceName: "Phone",
                userAgent: "UA",
                ipAddress: "127.0.0.1");
            var secondaryDevice = DeviceInfo.Create(
                deviceId: "device-b",
                deviceName: "Tablet",
                userAgent: "UA",
                ipAddress: "127.0.0.2");
            RefreshToken tokenToKeep = user.IssueRefreshToken(
                sessionId: Guid.Parse("70000000-0000-0000-0000-000000000001"),
                tokenHash: "keep-token",
                expiresAtUtc: UserTestData.CreatedAtUtc.AddHours(1),
                deviceInfo: primaryDevice,
                geoLocation: null,
                isPersistent: true,
                createdAtUtc: UserTestData.CreatedAtUtc);
            RefreshToken tokenToRevoke = user.IssueRefreshToken(
                sessionId: Guid.Parse("70000000-0000-0000-0000-000000000002"),
                tokenHash: "revoke-token",
                expiresAtUtc: UserTestData.CreatedAtUtc.AddHours(1),
                deviceInfo: primaryDevice,
                geoLocation: null,
                isPersistent: true,
                createdAtUtc: UserTestData.CreatedAtUtc);
            RefreshToken differentDeviceToken = user.IssueRefreshToken(
                sessionId: Guid.Parse("70000000-0000-0000-0000-000000000003"),
                tokenHash: "other-device-token",
                expiresAtUtc: UserTestData.CreatedAtUtc.AddHours(1),
                deviceInfo: secondaryDevice,
                geoLocation: null,
                isPersistent: true,
                createdAtUtc: UserTestData.CreatedAtUtc);
            DateTime nowUtc = UserTestData.CreatedAtUtc.AddMinutes(20);

            int revokedCount = user.RevokeActiveRefreshTokensByDevice(
                deviceId: "device-a",
                reason: RefreshTokenRevocationReason.UserRevoked,
                utcNow: nowUtc,
                excludedRefreshTokenId: tokenToKeep.Id);

            Assert.Equal(
                expected: 1,
                actual: revokedCount);
            Assert.False(tokenToKeep.IsRevoked);
            Assert.True(tokenToRevoke.IsRevoked);
            Assert.False(differentDeviceToken.IsRevoked);
        }

        [Fact]
        public void RevokeActiveRefreshTokensByDevice_WithWhitespaceDeviceId_ThrowsDomainException()
        {
            User user = UserTestData.CreateUser();

            DomainException exception = Assert.Throws<DomainException>(() => user.RevokeActiveRefreshTokensByDevice(
                deviceId: " ",
                reason: RefreshTokenRevocationReason.UserRevoked,
                utcNow: UserTestData.CreatedAtUtc.AddMinutes(1)));

            Assert.Equal(
                expected: "Identity.DeviceInfo.InvalidDeviceId",
                actual: exception.Code);
            Assert.Equal(
                expected: "deviceId",
                actual: exception.PropertyName);
        }

        [Fact]
        public void RevokeActiveRefreshTokensBySession_RevokesOnlyMatchingActiveTokens()
        {
            User user = UserTestData.CreateUser();
            var targetSessionId = Guid.Parse("70000000-0000-0000-0000-000000000010");
            RefreshToken tokenToKeep = user.IssueRefreshToken(
                sessionId: targetSessionId,
                tokenHash: "keep-token",
                expiresAtUtc: UserTestData.CreatedAtUtc.AddHours(1),
                deviceInfo: TokenTestData.CreateDeviceInfo(),
                geoLocation: null,
                isPersistent: true,
                createdAtUtc: UserTestData.CreatedAtUtc);
            RefreshToken tokenToRevoke = user.IssueRefreshToken(
                sessionId: targetSessionId,
                tokenHash: "revoke-token",
                expiresAtUtc: UserTestData.CreatedAtUtc.AddHours(1),
                deviceInfo: TokenTestData.CreateDeviceInfo(),
                geoLocation: null,
                isPersistent: true,
                createdAtUtc: UserTestData.CreatedAtUtc);
            RefreshToken otherSessionToken = user.IssueRefreshToken(
                sessionId: Guid.Parse("70000000-0000-0000-0000-000000000011"),
                tokenHash: "other-token",
                expiresAtUtc: UserTestData.CreatedAtUtc.AddHours(1),
                deviceInfo: TokenTestData.CreateDeviceInfo(),
                geoLocation: null,
                isPersistent: true,
                createdAtUtc: UserTestData.CreatedAtUtc);
            DateTime nowUtc = UserTestData.CreatedAtUtc.AddMinutes(20);

            int revokedCount = user.RevokeActiveRefreshTokensBySession(
                sessionId: targetSessionId,
                reason: RefreshTokenRevocationReason.SessionReplaced,
                utcNow: nowUtc,
                excludedRefreshTokenId: tokenToKeep.Id);

            Assert.Equal(
                expected: 1,
                actual: revokedCount);
            Assert.False(tokenToKeep.IsRevoked);
            Assert.True(tokenToRevoke.IsRevoked);
            Assert.False(otherSessionToken.IsRevoked);
        }

        [Fact]
        public void RevokeActiveRefreshTokensBySession_WithEmptySessionId_ThrowsDomainException()
        {
            User user = UserTestData.CreateUser();

            DomainException exception = Assert.Throws<DomainException>(() => user.RevokeActiveRefreshTokensBySession(
                sessionId: Guid.Empty,
                reason: RefreshTokenRevocationReason.SessionReplaced,
                utcNow: UserTestData.CreatedAtUtc.AddMinutes(1)));

            Assert.Equal(
                expected: "Identity.Common.EmptyId",
                actual: exception.Code);
            Assert.Equal(
                expected: "sessionId",
                actual: exception.PropertyName);
        }
    }
}
