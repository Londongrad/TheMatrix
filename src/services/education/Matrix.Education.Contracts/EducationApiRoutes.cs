namespace Matrix.Education.Contracts
{
    public static class EducationApiRoutes
    {
        public const string Base = "api/simulation-hosts/{simulationHostId:guid}/education";
        public const string Institutions = Base + "/institutions";
        public const string Enrollments = Base + "/enrollments";
        public const string Students = Base + "/students";
    }
}
