namespace Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices
{
    public static class InternalServicePrincipals
    {
        public static readonly InternalServiceIdentity Resources = new(
            SubjectId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ServiceName: "resources");

        public static readonly InternalServiceIdentity SimulationSystems = new(
            SubjectId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ServiceName: "simulationsystems");

        public static readonly InternalServiceIdentity Population = new(
            SubjectId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ServiceName: "population");

        public static readonly InternalServiceIdentity SimulationCore = new(
            SubjectId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            ServiceName: "simulationcore");
    }
}
