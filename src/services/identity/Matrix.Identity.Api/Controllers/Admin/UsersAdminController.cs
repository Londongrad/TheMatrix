using Matrix.BuildingBlocks.Application.Authorization;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Matrix.Identity.Contracts.Admin.Users.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Identity.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Policy = PermissionKeys.IdentityUsersRead)]
    public sealed class UsersAdminController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpGet]
        public async Task<ActionResult<PagedResult<UserListItemResponse>>> GetUsersPage(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            // Минимальная защита от "дай мне 1_000_000"
            pageSize = Math.Clamp(
                value: pageSize,
                min: 1,
                max: 200);

            var query = new GetUsersPageQuery(
                new Pagination(
                    pageNumber: pageNumber,
                    pageSize: pageSize));

            PagedResult<UserListItemResult> result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            var mapped = new PagedResult<UserListItemResponse>(
                items: result.Items.Select(u => new UserListItemResponse
                    {
                        Id = u.Id,
                        AvatarUrl = u.AvatarUrl,
                        Email = u.Email,
                        Username = u.Username,
                        IsEmailConfirmed = u.IsEmailConfirmed,
                        IsLocked = u.IsLocked,
                        CreatedAtUtc = u.CreatedAtUtc
                    })
                   .ToList(),
                totalCount: result.TotalCount,
                pageNumber: result.PageNumber,
                pageSize: result.PageSize);

            return Ok(mapped);
        }
    }
}
