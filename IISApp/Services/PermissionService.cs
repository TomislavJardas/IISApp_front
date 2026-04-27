namespace IISApp.Services
{
    public enum FrontendAccessMode
    {
        ReadOnly,
        FullAccess
    }

    public class PermissionService
    {
        public PermissionService(FrontendAccessMode mode)
        {
            Mode = mode;
        }

        public FrontendAccessMode Mode { get; }

        public bool CanReadPlayers => true;

        public bool CanMutatePlayers => Mode == FrontendAccessMode.FullAccess;

        public static PermissionService FromConfiguration() => new(AppConfig.AccessMode);
    }
}
