using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Education.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Education.Application.Enrollments.CompleteStudentStage
{
    public sealed record CompleteStudentStageCommand(
        Guid SimulationHostId,
        Guid ResidentId,
        DateOnly CompletedOn)
        : IRequest<CompleteStudentStageResult>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EducationEnrollmentsManage;
    }
}
