using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Errors;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Sessions.GetUserSessions
{
    public sealed class GetUserSessionsQueryHandler(
    IUserRepository userRepository)
    : IRequestHandler<GetUserSessionsQuery, IReadOnlyCollection<UserSessionResult>>
    {
        private readonly IUserRepository _userRepository = userRepository;

        public async Task<IReadOnlyCollection<UserSessionResult>> Handle(
            GetUserSessionsQuery request,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            var sessions = user.RefreshTokens
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
