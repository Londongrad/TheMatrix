using System.Linq.Expressions;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Matrix.Identity.Domain.Entities;

namespace Matrix.Identity.Infrastructure.Persistence.Projections
{
    internal static class UserProjections
    {
        public static readonly Expression<Func<User, UserListItemResult>> ToListItem =
            u => new UserListItemResult
            {
                Id = u.Id,
                AvatarUrl = u.AvatarUrl,
                Email = u.Email.Value,
                Username = u.Username.Value,
                IsEmailConfirmed = u.IsEmailConfirmed,
                IsLocked = u.IsLocked,
                CreatedAtUtc = u.CreatedAtUtc
            };
    }
}
