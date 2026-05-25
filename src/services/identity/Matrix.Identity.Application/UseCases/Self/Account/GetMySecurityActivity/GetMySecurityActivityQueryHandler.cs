using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Abstractions.Persistence;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity
{
    public sealed class GetMySecurityActivityQueryHandler(
        ISecurityAuditReadRepository securityAuditReadRepository,
        ICurrentUserContext currentUser)
        : IRequestHandler<GetMySecurityActivityQuery, CursorPagedResult<SecurityActivityItemResult>>
    {
        public async Task<CursorPagedResult<SecurityActivityItemResult>> Handle(
            GetMySecurityActivityQuery request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();
            SecurityActivityCursor? cursor = ParseCursor(request.Cursor);

            return await securityAuditReadRepository.GetSliceByUserIdAsync(
                userId: userId,
                cursor: cursor,
                pageSize: request.PageSize,
                cancellationToken: cancellationToken);
        }

        private static SecurityActivityCursor? ParseCursor(string? rawCursor)
        {
            if (string.IsNullOrWhiteSpace(rawCursor))
                return null;

            if (SecurityActivityCursorCodec.TryDecode(
                    rawCursor: rawCursor,
                    cursor: out SecurityActivityCursor cursor))
                return cursor;

            throw new MatrixApplicationException(
                code: "Identity.SecurityActivity.InvalidCursor",
                message: "The supplied security activity cursor is invalid.",
                errorType: ApplicationErrorType.Validation);
        }
    }
}
