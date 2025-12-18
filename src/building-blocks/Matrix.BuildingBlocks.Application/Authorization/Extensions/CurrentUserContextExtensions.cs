using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;

namespace Matrix.BuildingBlocks.Application.Authorization.Extensions
{
    public static class CurrentUserContextExtensions
    {
        public static Guid GetUserIdOrThrow(this ICurrentUserContext ctx)
        {
            if (!ctx.IsAuthenticated || ctx.UserId is null)
                throw new MatrixApplicationException(
                    code: "Common.Unauthorized",
                    message: "Authentication is required.",
                    errorType: ApplicationErrorType.Unauthorized,
                    errors: null);

            return ctx.UserId.Value;
        }
    }
}
