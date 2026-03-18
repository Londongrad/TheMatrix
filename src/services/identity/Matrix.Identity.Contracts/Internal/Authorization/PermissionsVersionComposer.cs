namespace Matrix.Identity.Contracts.Internal.Authorization
{
    public static class PermissionsVersionComposer
    {
        public static int Compose(
            int userPermissionsVersion,
            int defaultUserAccessVersion)
        {
            unchecked
            {
                uint hash = 2166136261;
                hash = (hash ^ (uint)userPermissionsVersion) * 16777619;
                hash = (hash ^ (uint)defaultUserAccessVersion) * 16777619;
                return (int)(hash & int.MaxValue);
            }
        }
    }
}
