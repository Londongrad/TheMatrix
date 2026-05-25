using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessions
{
    public sealed class GetUserSessionsQueryHandler(
        IUserRepository userRepository,
        IUserSessionRepository userSessionRepository,
        TimeProvider timeProvider,
        ICurrentUserContext currentUser)
        : IRequestHandler<GetMySessionsQuery, IReadOnlyCollection<MySessionResult>>
    {
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task<IReadOnlyCollection<MySessionResult>> Handle(
            GetMySessionsQuery request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();
            DateTime utcNow = _timeProvider.GetUtcNow()
               .UtcDateTime;

            bool userExists = await userRepository.ExistsAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            if (!userExists)
                throw ApplicationErrorsFactory.UserNotFound(userId);

            IReadOnlyCollection<UserSession> sessions = await userSessionRepository.ListActiveByUserIdAsync(
                userId: userId,
                utcNow: utcNow,
                cancellationToken: cancellationToken);

            return sessions
               .OrderByDescending(t => t.LastUsedAtUtc ?? t.CreatedAtUtc)
               .Select(t => new MySessionResult
               {
                   Id = t.Id,
                   DeviceId = t.DeviceInfo.DeviceId,
                   DeviceName = t.DeviceInfo.DeviceName,
                   UserAgent = t.DeviceInfo.UserAgent,
                   IpAddress = t.DeviceInfo.IpAddress,
                   Country = t.GeoLocation?.Country,
                   Region = t.GeoLocation?.Region,
                   City = t.GeoLocation?.City,
                   CreatedAtUtc = t.CreatedAtUtc,
                   LastUsedAtUtc = t.LastUsedAtUtc,
                   RefreshTokenExpiresAtUtc = t.RefreshTokenExpiresAtUtc,
                   IsActive = t.IsActive(utcNow),
                   IsPersistent = t.IsPersistent
               })
               .ToArray();
        }
    }
}
