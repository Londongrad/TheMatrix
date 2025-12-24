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
        ICurrentUserContext currentUser)
        : IRequestHandler<GetMySessionsQuery, IReadOnlyCollection<MySessionResult>>
    {
        public async Task<IReadOnlyCollection<MySessionResult>> Handle(
            GetMySessionsQuery request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            User user = await userRepository.GetByIdAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            MySessionResult[] sessions = user.RefreshTokens
               .Where(s => s.IsActive())
               .OrderByDescending(t => t.CreatedAtUtc)
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
                    IsActive = t.IsActive()
                })
               .ToArray();

            return sessions;
        }
    }
}
