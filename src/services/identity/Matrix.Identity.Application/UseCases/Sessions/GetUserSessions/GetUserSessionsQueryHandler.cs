using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Sessions.GetUserSessions
{
    public sealed class GetUserSessionsQueryHandler(IUserRepository userRepository)
        : IRequestHandler<GetUserSessionsQuery, IReadOnlyCollection<UserSessionResult>>
    {
        public async Task<IReadOnlyCollection<UserSessionResult>> Handle(
            GetUserSessionsQuery request,
            CancellationToken cancellationToken)
        {
            User user = await userRepository.GetByIdAsync(
                            userId: request.UserId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            UserSessionResult[] sessions = user.RefreshTokens
               .Where(s => s.IsActive())
               .OrderByDescending(t => t.CreatedAtUtc)
               .Select(t => new UserSessionResult
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
