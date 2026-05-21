using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessions;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessionHistoryPage
{
    public sealed class GetMySessionHistoryPageQueryHandler(
        IUserRepository userRepository,
        IUserSessionRepository userSessionRepository,
        TimeProvider timeProvider,
        ICurrentUserContext currentUser)
        : IRequestHandler<GetMySessionHistoryPageQuery, PagedResult<MySessionResult>>
    {
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task<PagedResult<MySessionResult>> Handle(
            GetMySessionHistoryPageQuery request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            bool userExists = await userRepository.ExistsAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            if (!userExists)
                throw ApplicationErrorsFactory.UserNotFound(userId);

            DateTime utcNow = _timeProvider.GetUtcNow()
               .UtcDateTime;

            (IReadOnlyCollection<UserSession> sessions, int totalCount) =
                await userSessionRepository.GetEndedPageByUserIdAsync(
                    userId: userId,
                    utcNow: utcNow,
                    pagination: request.Pagination,
                    cancellationToken: cancellationToken);

            IReadOnlyCollection<MySessionResult> items = sessions
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
                    IsActive = false,
                    IsPersistent = t.IsPersistent
                })
               .ToArray();

            return new PagedResult<MySessionResult>(
                items: items,
                totalCount: totalCount,
                pageNumber: request.Pagination.PageNumber,
                pageSize: request.Pagination.PageSize);
        }
    }
}
