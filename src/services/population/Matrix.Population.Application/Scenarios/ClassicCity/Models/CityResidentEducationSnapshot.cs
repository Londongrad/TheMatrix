using Matrix.Population.Application.Integration.Education;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Models
{
    public sealed record CityResidentEducationSnapshot(
        string AttainedStage,
        string? ActiveStage,
        Guid? InstitutionId,
        Guid? InstitutionAnchorId)
    {
        public static CityResidentEducationSnapshot FromProjection(
            EducationParticipationProjection? projection)
        {
            if (projection is null)
                return new CityResidentEducationSnapshot(
                    AttainedStage: "none",
                    ActiveStage: null,
                    InstitutionId: null,
                    InstitutionAnchorId: null);

            return new CityResidentEducationSnapshot(
                AttainedStage: projection.CompletedStage ?? "none",
                ActiveStage: projection.IsEnrolled ? projection.ActiveStage : null,
                InstitutionId: projection.IsEnrolled ? projection.InstitutionId : null,
                InstitutionAnchorId: projection.IsEnrolled ? projection.InstitutionAnchorId : null);
        }
    }
}
