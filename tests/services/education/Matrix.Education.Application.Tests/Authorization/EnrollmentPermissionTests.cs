using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Education.Application.Enrollments.CompleteStudentStage;
using Matrix.Education.Application.Enrollments.EnrollStudent;
using Matrix.Education.Application.Enrollments.WithdrawStudent;
using Xunit;
using PermissionKeys = Matrix.Education.Application.Authorization.Permissions.PermissionKeys;

namespace Matrix.Education.Application.Tests.Authorization
{
    public sealed class EnrollmentPermissionTests
    {
        public static TheoryData<IRequirePermission> Commands => new()
        {
            new EnrollStudentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "School", new DateOnly(2026, 1, 1)),
            new CompleteStudentStageCommand(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1)),
            new WithdrawStudentCommand(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1))
        };

        [Theory]
        [MemberData(nameof(Commands))]
        public void EnrollmentCommand_RequiresEducationOwnedPermission(IRequirePermission command)
        {
            Assert.Equal(
                expected: PermissionKeys.EducationEnrollmentsManage,
                actual: command.PermissionKey);
        }
    }
}
