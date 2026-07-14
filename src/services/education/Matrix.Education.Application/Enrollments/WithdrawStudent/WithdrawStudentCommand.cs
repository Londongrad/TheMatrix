using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Education.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Education.Application.Enrollments.WithdrawStudent
{
    public sealed record WithdrawStudentCommand(
        Guid SimulationHostId,
        Guid ResidentId,
        DateOnly WithdrawnOn)
        : IRequest<WithdrawStudentResult>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EducationEnrollmentsManage;
    }
}
